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


    public float BlockDuration = 1.0f;

    // Should there be a dash cooldown and also a dash duration?
    // How would we handle windup and winddown?

    [Header("Movement")] [Tooltip("Walking controller speed")] [SerializeField]
    private float WalkSpeed = 1.0f;

    [Tooltip("Normal controller speed")] [SerializeField]
    private float RunSpeed = 3.0f;

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

        // Find closest target to lock on to
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Targetable");
        foreach (GameObject enemy in enemies) {
            SetLockOnTarget(enemy.transform);
            break;
        }
    }

    public void OnDash() {
        inputDashOnNextFrame = true;
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
    }

    private void LateUpdate() {
        UpdateUI();
    }

    //Character controller movement
    private void HandleMovement() {
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

        // Using KBM controls, there's a specific button to walk.
        // Otherwise, use the amount that user is pushing stick.
        bool isWalking = isWalkKeyHeld || inputMoveVector.magnitude < 0.5f;
        float moveSpeed = isWalking ? WalkSpeed : RunSpeed;

        // Boost the move speed if we're dashing.
        if (_isExecutingDash) {
            float dashT = Mathf.Clamp((DashDuration - _dashDurationCountDown) / DashDuration, 0, 1);
            float dashSpeed = DashVelocityCurve.Evaluate(dashT);
            moveSpeed = dashSpeed * RunSpeed;
        }

        // Character should move in the direction of the camera.
        // If dashing, we just use the same one from last time.
        Vector3 desiredMoveDirection = _previousDesiredMoveDirection;
        if (!_isExecutingDash) {
            desiredMoveDirection = GetDesiredMoveDirection();
        }

        // If we're not doing a fast turn, then calculate the direction that the target should be facing.
        float finalTurnSpeed = TurnSpeed;
        bool isMoving = desiredMoveDirection.magnitude > 0;
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

        // TODO: When dashing while locked on, we strafe instead of turn?

        Animator.SetBool("IsRunning", isMoving);
        Animator.SetBool("IsDashing", _isExecutingDash);

        // Turn the player incrementally towards the direction of movement
        if (!_isExecutingDash) {
            PlayerModel.transform.rotation = Quaternion.RotateTowards(PlayerModel.transform.rotation,
                targetRotation,
                finalTurnSpeed * Time.deltaTime);
        } else {
            // Complete the turn immediately
            PlayerModel.transform.rotation = targetRotation;
        }

        // The player should always move in the direction the player model is facing.
        Vector3 absoluteMoveVector = PlayerModel.transform.forward *
                                     (desiredMoveDirection.magnitude * (moveSpeed * Time.deltaTime));

        // CharacterController.Move should only be called once, see:
        // https://forum.unity.com/threads/charactercontroller-isgrounded-unreliable-or-bad-code.373492/
        characterController.Move(_velocity * Time.deltaTime + absoluteMoveVector);

        // Update velocity from gravity
        _velocity.y += gravity * Time.deltaTime;

        _previousInputMoveVector = inputMoveVector;
        _previousDesiredMoveDirection = desiredMoveDirection;
    }

    // Based on the players input and the camera direction.
    // Such that:
    // - "Down" moves towards the camera
    // - "Right" moves to the right of the camera
    private Vector3 GetDesiredMoveDirection() {
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
        Vector3 desiredMoveDirection =
            activeCameraRotation * inputMoveVector;
        // Y component should be 0
        desiredMoveDirection.y = 0;
        desiredMoveDirection = desiredMoveDirection.normalized;
        return desiredMoveDirection;
    }

    private void HandleAttack() {
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

    private void FirePrimaryWeapon() {
        _primaryFireCooldownCountdown = PrimaryFireCooldown;

        Vector3 initialVelocity = PlayerModel.transform.forward * ProjectileVelocity;
        if (_lockedOnTarget != null) {
            initialVelocity = (_lockedOnTarget.position - PrimaryWeaponMountPoint.position).normalized *
                              ProjectileVelocity;

            // Rotate the player to turn towards the target
            targetRotation = Quaternion.Euler(0,
                Mathf.Atan2(initialVelocity.x, initialVelocity.z) * Mathf.Rad2Deg, 0);
            PlayerModel.transform.rotation = targetRotation;
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
            targetRotation = Quaternion.Euler(0,
                Mathf.Atan2(initialVelocity.x, initialVelocity.z) * Mathf.Rad2Deg, 0);
            PlayerModel.transform.rotation = targetRotation;
        }

        ProjectileController.Instance.SpawnProjectile(ProjectileController.Owner.Player,
            SecondaryWeaponMountPoint.position, Quaternion.identity,
            initialVelocity);
    }

    private void UpdateUI() {
        Vector2Int screenSpaceAimPosition = new(Screen.width / 2, Screen.height / 2);
        if (_lockedOnTarget != null) {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(_lockedOnTarget.transform.position);
            if (screenPosition.x < 0 || screenPosition.x > Screen.width || screenPosition.y < 0 ||
                screenPosition.y > Screen.height || screenPosition.z < 0) {
                // The target is outside of the screen
                SetLockOnTarget(null);
            } else {
                screenSpaceAimPosition = new((int)screenPosition.x, (int)screenPosition.y);
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

    private void SetLockOnTarget(Transform lockedOnTarget) {
        _lockedOnTarget = lockedOnTarget;
        OnLockedOnTargetChanged?.Invoke(this, _lockedOnTarget);
    }
}
