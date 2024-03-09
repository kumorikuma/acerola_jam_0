using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

/*Simple player movement controller, based on character controller component,
with footstep system based on check the current texture of the component*/
[RequireComponent(typeof(EntityStats))]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
    [NonNullField] public Animator Animator;

    [NonNullField] public Transform PrimaryWeaponMountPoint;
    [NonNullField] public Transform SecondaryWeaponMountPoint;
    [NonNullField] public Transform EnemyAimTargetLocation;

    public enum AimMode {
        NoTargetLock,
        TargetLock,
    }

    public float ProjectileVelocity = 1.0f;
    public float PrimaryFireCooldown = 1.0f;
    private float _primaryFireCooldownCountdown = 0.0f;

    public float SecondaryFireCooldown = 0.3f;
    private float _secondaryFireCooldownCountdown = 0.0f;

    public AnimationCurve DashVelocityCurve;
    public float DashDuration = 1.0f;
    public float DashCooldown = 0.5f;
    private float _dashDurationCountDown = 0.0f;
    private float _dashCooldownCountDown = 0.0f;

    // Should there be a dash cooldown and also a dash duration?
    // How would we handle windup and winddown?

    [Header("Movement")] [Tooltip("Walking controller speed")] [SerializeField]
    private float WalkSpeed = 1.0f;

    [Tooltip("Normal controller speed")] public float RunSpeed = 3.0f;

    [Tooltip("Turning controller speed")] [SerializeField]
    private float TurnSpeed = 360.0f;

    [Tooltip("Force of the jump with which the controller rushes upwards")] [SerializeField]
    private float JumpForce = 1.0f;

    [Tooltip("Gravity, pushing down controller when it jumping")] [SerializeField]
    private float gravity = -9.81f;

    [NonNullField] public GameObject PlayerModel;

    //Private movement variables
    private Vector3 inputMoveVector;
    private bool inputJumpOnNextFrame = false;
    private bool inputDashOnNextFrame = false;
    private bool inputPrimaryFireOnNextFrame = false;
    private bool inputSecondaryFireHeld = false;
    private Vector3 _velocity; // Used for handling jumping
    private CharacterController characterController;
    private bool isWalkKeyHeld = false;
    Quaternion targetRotation;
    private bool _isExecutingFastTurn = false;
    private bool _isExecutingDash = false;

    // Lockon
    private Transform _lockedOnTarget;
    private AimMode _currentAimMode;
    public event EventHandler<Transform> OnLockedOnTargetChanged;

    private CinemachineBrain _cinemachineBrain;
    private Transform _previousActiveVirtualCamera;
    private Vector3 _previousInputMoveVector = Vector3.zero;
    private Vector3 _previousDesiredMoveDirection = Vector3.zero;

    // Stats
    [NonSerialized] public EntityStats Stats;
    public int MaxPlayerLives = 3;
    public int CurrentPlayerLives = 3;
    public event EventHandler<int> OnPlayerLivesChanged;

    // Shield
    private ShieldController _shieldController;
    public float BlockDuration = 1.0f;
    public float BlockCooldown = 0.5f;
    private float _blockDurationCountDown = 0.0f;
    private float _blockCooldownCountDown = 0.0f;
    private bool _inputBlockHeld = false;
    private bool _isBlocking = false;

    // Status Effects
    public float PlanetDamagePositionThreshold = -1.5f;
    public float PlanetDamageCooldown = 1.0f;
    public int PlanetTickDamage = 20;
    private float _planetDamageCooldownCountdown = 0.0f;

    private void SetPlayerLives(int playerLives) {
        CurrentPlayerLives = Mathf.Clamp(playerLives, 0, MaxPlayerLives);
        OnPlayerLivesChanged?.Invoke(this, CurrentPlayerLives);
    }

    private void ConsumeLife() {
        SetPlayerLives(CurrentPlayerLives - 1);
        if (CurrentPlayerLives == 0) {
            GameLifecycleManager.Instance.EndGame();
        } else {
            // Heal the player back up to 50%
            Stats.SetHealthToPercentage(0.5f);
        }
    }

    private void Awake() {
        _cinemachineBrain = GetComponentInChildren<CinemachineBrain>();
        characterController = GetComponent<CharacterController>();
        _shieldController = GetComponentInChildren<ShieldController>();
        Stats = GetComponent<EntityStats>();
        _velocity.y = -2f;
    }

    private void Start() {
        Stats.NotifyHealthChanged();
        Stats.OnHealthChanged += OnHealthChanged;
        Reset();
    }

    public void Reset() {
        SetPlayerLives(MaxPlayerLives);
        Stats.Reset();
        _shieldController.DeactivateShield();
    }

    public float GetRegularMovementSpeed() {
        return RunSpeed * Time.deltaTime;
    }

    private void OnHealthChanged(object sender, float health) {
        // If the health reaches 0, we've died.
        // If we have more lives, we can continue. (restore health to 50%)
        // Otherwise, we end the game.
        if (health <= 0.0f) {
            ConsumeLife();
        }
    }

    public void OnMove(Vector2 moveVector) {
        inputMoveVector = new Vector3(moveVector.x, 0, moveVector.y);
    }

    public void OnJump() {
        inputJumpOnNextFrame = true;
        Debug.Log("OnJump");
    }

    public void OnLockOn() {
        if (_lockedOnTarget != null) {
            SetLockOnTarget(null);
            return;
        }

        // Find closest target to the center of the screen to lock on to.
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Targetable");
        foreach (GameObject enemy in enemies) {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(enemy.transform.position);
            if (IsScreenPositionOutOfBounds(screenPosition)) {
                continue;
            }

            SetLockOnTarget(enemy.transform);
            PlayerManager.Instance.CameraController.UpdateCamera();
            break;
        }
    }

    public void OnDash() {
        inputDashOnNextFrame = true;
    }

    public void OnBlock(bool value) {
        _inputBlockHeld = value;
    }

    public void OnPrimaryFire() {
        inputPrimaryFireOnNextFrame = true;
    }

    public void OnSecondaryFire(bool value) {
        inputSecondaryFireHeld = value;
    }

    private void Update() {
        HandleMovement();
        HandleAttack();
        HandleStatusEffects();
        PlayerManager.Instance.CameraController.UpdateCamera();
    }

    private void FixedUpdate() {
        if (_primaryFireCooldownCountdown > 0.0f) {
            _primaryFireCooldownCountdown -= Time.fixedDeltaTime;
        }

        if (_secondaryFireCooldownCountdown > 0.0f) {
            _secondaryFireCooldownCountdown -= Time.fixedDeltaTime;
        }

        if (_dashDurationCountDown > 0.0f) {
            _dashDurationCountDown -= Time.fixedDeltaTime;
        }

        if (_dashCooldownCountDown > 0.0f) {
            _dashCooldownCountDown -= Time.fixedDeltaTime;
        }

        if (_blockCooldownCountDown > 0.0f) {
            _blockCooldownCountDown -= Time.fixedDeltaTime;
        }

        if (_blockDurationCountDown > 0.0f) {
            _blockDurationCountDown -= Time.fixedDeltaTime;
        }

        if (_planetDamageCooldownCountdown > 0.0f) {
            _planetDamageCooldownCountdown -= Time.fixedDeltaTime;
        }
    }

    private void LateUpdate() {
        UpdateUI();
    }

    //Character controller movement
    private void HandleMovement() {
        if (characterController.isGrounded && _velocity.y < 0) {
            // IsGrounded check becomes false if velocity is set to 0.
            // See: https://forum.unity.com/threads/charactercontroller-isgrounded-unreliable-or-bad-code.373492/
            _velocity.y = -.1f;
        }

        if (inputJumpOnNextFrame && characterController.isGrounded) {
            _velocity.y = Mathf.Sqrt(JumpForce * -2f * gravity);
        }

        inputJumpOnNextFrame = false;

        // Start a dash (cannot dash while dashing)
        if (!_isExecutingDash && inputDashOnNextFrame && _dashCooldownCountDown <= 0.0f) {
            inputDashOnNextFrame = false;
            _isExecutingDash = true;
            _dashDurationCountDown = DashDuration;
        }

        // Check to see if the dash is over
        if (_isExecutingDash && _dashDurationCountDown <= 0.0f) {
            _isExecutingDash = false;
            // Start the cooldown after the dash is over.
            _dashCooldownCountDown = DashCooldown;
        }

        // ==== COMPUTE MOVEMENT SPEED MODIFIER =====
        // Using KBM controls, there's a specific button to walk.
        // Otherwise, use the amount that user is pushing stick.
        bool isWalking = isWalkKeyHeld || inputMoveVector.magnitude < 0.5f;
        float moveSpeed = isWalking ? WalkSpeed : RunSpeed;
        bool isMoving = inputMoveVector.magnitude > 0;
        if (!isMoving) {
            moveSpeed = 0;
        }

        // Boost the move speed if we're dashing.
        if (_isExecutingDash) {
            float dashT = Mathf.Clamp((DashDuration - _dashDurationCountDown) / DashDuration, 0, 1);
            float dashSpeed = DashVelocityCurve.Evaluate(dashT);
            moveSpeed = dashSpeed * RunSpeed;
        }

        // ==== DETERMINE MOVEMENT DIRECTION =====
        bool isStrafing = _lockedOnTarget != null;
        Vector3 desiredMoveDirection = GetDesiredMoveDirection(_isExecutingDash, isStrafing);

        // ==== DETERMINE TARGET ROTATION =====
        float finalTurnSpeed = TurnSpeed;
        // If we're in target lock mode, keep the player facing the enemy. We will always move to the left/right.
        if (_lockedOnTarget != null) {
            Vector3 playerToTargetVector = _lockedOnTarget.position - PlayerModel.transform.position;
            playerToTargetVector.y = 0;
            playerToTargetVector = playerToTargetVector.normalized;
            // TODO: Instead of making the facing instantaneous, we could try doing a fast turn.
            targetRotation = Quaternion.LookRotation(playerToTargetVector, Vector3.up);
        } else {
            // If we're not doing a fast turn, then calculate the direction that the target should be facing.
            if (!_isExecutingFastTurn) {
                if (isMoving) {
                    // Face the character in the direction of movement
                    targetRotation = Quaternion.Euler(0,
                        Mathf.Atan2(desiredMoveDirection.x, desiredMoveDirection.z) * Mathf.Rad2Deg, 0);

                    // If the target rotation is too far away (180 degrees), we do a fast turn
                    float angleDifferenceDegrees = Quaternion.Angle(PlayerModel.transform.rotation, targetRotation);
                    if (angleDifferenceDegrees > 175) {
                        _isExecutingFastTurn = true;
                    }
                }
            } else {
                // Otherwise we keep the target rotation the same but accelerate the turn speed.
                finalTurnSpeed = 3 * TurnSpeed;

                // If the target rotation is reached, the fast turn is done.
                float angleDifferenceDegrees = Quaternion.Angle(PlayerModel.transform.rotation, targetRotation);
                if (angleDifferenceDegrees < 0.1f) {
                    _isExecutingFastTurn = false;
                }
            }
        }

        // TODO: When dashing while locked on, we strafe instead of turn?

        Animator.SetBool("IsRunning", isMoving);
        // Animator.SetBool("IsDashing", _isExecutingDash);

        // ==== PERFORM ROTATION =====
        if (_isExecutingDash || _lockedOnTarget != null) {
            // Complete the turn immediately when:
            // - Dashing
            // - Target locked.
            PlayerModel.transform.rotation = targetRotation;
        } else {
            // Turn the player incrementally towards the direction of movement otherwise.
            PlayerModel.transform.rotation = Quaternion.RotateTowards(PlayerModel.transform.rotation,
                targetRotation,
                finalTurnSpeed * Time.deltaTime);
        }

        // ==== PERFORM MOVEMENT =====
        Vector3 absoluteMoveVector = Vector3.zero;
        if (isStrafing) {
            // If we're strafing, then just move the player model in the desired direction
            absoluteMoveVector = desiredMoveDirection * (moveSpeed * Time.deltaTime);
        } else {
            // Otherwise, the player should always move in the direction the player model is facing.
            absoluteMoveVector = PlayerModel.transform.forward *
                                 (desiredMoveDirection.magnitude * (moveSpeed * Time.deltaTime));
        }

        // Gravity doesn't affect us while dashing
        if (_isExecutingDash) {
            _velocity.y = 0;
        }

        // CharacterController.Move should only be called once, see:
        // https://forum.unity.com/threads/charactercontroller-isgrounded-unreliable-or-bad-code.373492/
        Vector3 combinedMoveVector = _velocity * Time.deltaTime + absoluteMoveVector;
        characterController.Move(combinedMoveVector);
        // Update velocity from gravity
        _velocity.y += gravity * Time.deltaTime;

        // ==== UPDATE STATE =====
        ThrusterController.Instance.HandleThrusterUpdates(moveSpeed);
        _previousInputMoveVector = inputMoveVector;
        _previousDesiredMoveDirection = desiredMoveDirection;
    }

    // Transforms the player's InputMoveVector to a desired move direction.
    // Depends on Camera's position, target's position. Whether or not we're target locked.
    private Vector3 GetDesiredMoveDirection(bool isDashing, bool isStrafing) {
        Vector3 desiredMoveDirection = Vector3.zero;
        Vector3 inputMoveVectorModified = inputMoveVector;
        // If we're dashing with a locked camera, then use the previous input vector.
        if (isDashing && isStrafing) {
            inputMoveVectorModified = _previousInputMoveVector;
        } else if (isDashing) {
            // If we're dashing without locked camera, then just use the previous direction.
            return _previousDesiredMoveDirection;
        }

        // Stop here if we don't need to any more calculations (user is not moving).
        if (inputMoveVectorModified.magnitude <= 0.0f) {
            return desiredMoveDirection;
        }

        if (_lockedOnTarget != null) {
            // "Down" moves away from the target. "Up" moves towards the target.
            // "Right" moves to the right of the target. "Left" moves to the left of the target.
            // To achieve this, we need to convert the InputMoveVector from WorldSpace to a space that is oriented
            // towards the target. The transformation is just whatever the rotation is that would cause the player
            // to be pointed towards the target.
            Vector3 playerToTargetVector = _lockedOnTarget.position - PlayerModel.transform.position;
            playerToTargetVector.y = 0;
            playerToTargetVector = playerToTargetVector.normalized;

            // Use Atan2 instead of Acos because Acos only outputs to range [0, PI] instead which leads to ambiguities.
            float forwardAngleRadians = Mathf.Atan2(Vector3.forward.z, Vector3.forward.x);
            float playerToTargetAngleRadians = Mathf.Atan2(playerToTargetVector.z, playerToTargetVector.x);
            // Subtract playerToTargetAngleRadians from forwardAngleRadians to get angle from forwardVector to playerToTargetVector.
            float angleDeltaDegrees = (forwardAngleRadians - playerToTargetAngleRadians) * Mathf.Rad2Deg;
            // Apply the rotation to the input
            Quaternion rotation = Quaternion.Euler(0, angleDeltaDegrees, 0);
            desiredMoveDirection = rotation * inputMoveVectorModified;

            // Vector3 playerPos = PlayerModel.transform.position;
            // VectorDebug.Instance.DrawDebugVector("Forward", Vector3.forward, playerPos, Color.magenta);
            // VectorDebug.Instance.DrawDebugVector("PlayerToTarget", playerToTargetVector, playerPos, Color.magenta);
            // VectorDebug.Instance.DrawDebugVector("InputMoveVector", inputMoveVector, playerPos, Color.green);
            // VectorDebug.Instance.DrawDebugVector("DesiredMoveDirection", desiredMoveDirection, playerPos, Color.green);
        } else {
            // "Down" moves towards the camera. "Up" moves away from the camera.
            // "Right" moves to the right of the camera. "Left" moves to the left of the camera.

            // Don't check for the active virtual camera every frame, but only do this when the player lets go of the input.
            // So we'll cache this, and then only check it if the cache wasn't set or if the previous inputMoveVector was 0.
            Transform activeVirtualCameraTransform =
                _cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject.transform;
            CinemachineClearShot cmClearShot = _cinemachineBrain.ActiveVirtualCamera as CinemachineClearShot;
            if (cmClearShot != null) {
                // If the active camera is a clearshot camera, then we should use the live child instead of the actual camera.
                activeVirtualCameraTransform = cmClearShot.LiveChild.VirtualCameraGameObject.transform;
            }

            // Apply the camera's rotation to the input move vector
            Quaternion activeCameraRotation = activeVirtualCameraTransform.transform.rotation;

            // Character should move in the direction of the camera
            desiredMoveDirection =
                activeCameraRotation * inputMoveVectorModified;
            // Y component should be 0
            desiredMoveDirection.y = 0;
            desiredMoveDirection = desiredMoveDirection.normalized;
        }

        return desiredMoveDirection;
    }

    private void HandleAttack() {
        if (_inputBlockHeld) {
            if (!_isBlocking && _blockCooldownCountDown <= 0.0f) {
                _isBlocking = true;
                _blockDurationCountDown = BlockDuration;
                _shieldController.ActivateShield();
            }
        }

        // Shield is brought down if duration ran out or let go of input
        if (!_inputBlockHeld || _blockDurationCountDown <= 0.0f) {
            if (_isBlocking) {
                _isBlocking = false;
                _blockCooldownCountDown = BlockCooldown;
                _shieldController.DeactivateShield();
            }
        }

        // Can't attack while blocking
        if (_inputBlockHeld) {
            return;
        }

        if (inputPrimaryFireOnNextFrame) {
            inputPrimaryFireOnNextFrame = false;

            if (_primaryFireCooldownCountdown <= 0.0f) {
                FirePrimaryWeapon();
            }
        }

        if (inputSecondaryFireHeld && _secondaryFireCooldownCountdown <= 0.0f) {
            FireSecondaryWeapon();
        }
    }

    private void HandleStatusEffects() {
        // If the player's feet are touching the inner planet, the player will periodically take damage (every second?).
        // To cheaply check this, we'll take advantage of the fact that ground level == 0, and that if the player's feet 
        // are below -1.5, then they're considered to be in the "damage" zone.
        // TODO: We will need to not do this when in transitioning to the second stage.
        if (transform.position.y <= PlanetDamagePositionThreshold && _planetDamageCooldownCountdown <= 0.0f) {
            _planetDamageCooldownCountdown = PlanetDamageCooldown;
            Stats.ApplyDamage(PlanetTickDamage);
        }
    }

    private void FirePrimaryWeapon() {
        _primaryFireCooldownCountdown = PrimaryFireCooldown;

        Vector3 initialVelocity = PlayerModel.transform.forward * ProjectileVelocity;
        if (_lockedOnTarget != null) {
            initialVelocity = (_lockedOnTarget.position - PrimaryWeaponMountPoint.position).normalized *
                              ProjectileVelocity;

            // Rotate the player to turn towards the target
            // targetRotation = Quaternion.Euler(0,
            //     Mathf.Atan2(initialVelocity.x, initialVelocity.z) * Mathf.Rad2Deg, 0);
            // PlayerModel.transform.rotation = targetRotation;
        }

        ProjectileController.Instance.SpawnProjectile(ProjectileController.Owner.Player,
            PrimaryWeaponMountPoint.position, Quaternion.identity,
            initialVelocity);
    }

    private void FireSecondaryWeapon() {
        _secondaryFireCooldownCountdown = SecondaryFireCooldown;

        Vector3 initialVelocity = PlayerModel.transform.forward * ProjectileVelocity;
        if (_lockedOnTarget != null) {
            initialVelocity = (_lockedOnTarget.position - SecondaryWeaponMountPoint.position).normalized *
                              ProjectileVelocity;

            // Rotate the player to turn towards the target
            // targetRotation = Quaternion.Euler(0,
            //     Mathf.Atan2(initialVelocity.x, initialVelocity.z) * Mathf.Rad2Deg, 0);
            // PlayerModel.transform.rotation = targetRotation;
        }

        ProjectileController.Instance.SpawnProjectile(ProjectileController.Owner.Player,
            SecondaryWeaponMountPoint.position, Quaternion.identity,
            initialVelocity);
    }

    private void UpdateUI() {
        Vector2Int screenSpaceAimPosition = new(Screen.width / 2, Screen.height / 2);
        if (_lockedOnTarget != null) {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(_lockedOnTarget.transform.position);
            if (IsScreenPositionOutOfBounds(screenPosition)) {
                SetLockOnTarget(null);
            } else {
                screenSpaceAimPosition = new(Mathf.RoundToInt(screenPosition.x), Mathf.RoundToInt(screenPosition.y));
            }
        }

        ReactUnityBridge.Instance.UpdateScreenSpaceAimPosition(screenSpaceAimPosition);

        ReactUnityBridge.Instance.UpdatePrimaryFireCooldown(1 - (_primaryFireCooldownCountdown / PrimaryFireCooldown));
        ReactUnityBridge.Instance.UpdateSecondaryFireCooldown(1 - (_secondaryFireCooldownCountdown /
                                                                   SecondaryFireCooldown));

        if (_lockedOnTarget != null) {
            EntityStats stats = _lockedOnTarget.GetComponent<EntityStats>();
            if (stats == null) {
                Debug.LogError("[PlayerController] Locked on target does not have EntityStats component!");
            } else {
                ReactUnityBridge.Instance.UpdateTargetHealth(stats.GetHealthPercentage());
            }
        } else {
            ReactUnityBridge.Instance.UpdateTargetHealth(0);
        }
    }

    private bool IsScreenPositionOutOfBounds(Vector3 screenPosition) {
        return screenPosition.x < 0 || screenPosition.x > Screen.width || screenPosition.y < 0 ||
               screenPosition.y > Screen.height || screenPosition.z < 0;
    }

    private void SetLockOnTarget(Transform lockedOnTarget) {
        _lockedOnTarget = lockedOnTarget;
        OnLockedOnTargetChanged?.Invoke(this, _lockedOnTarget);
    }
}
