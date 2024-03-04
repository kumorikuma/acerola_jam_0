using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
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

    // Lockon
    private Transform _lockedOnTarget;
    public event EventHandler<Transform> OnLockedOnTargetChanged;

    private CinemachineBrain _cinemachineBrain;
    private Transform _previousActiveVirtualCamera;
    private Vector3 _previousInputMoveVector = Vector3.zero;

    private EntityStats _entityStats;

    private void Awake() {
        _cinemachineBrain = GetComponentInChildren<CinemachineBrain>();
        characterController = GetComponent<CharacterController>();
        _entityStats = GetComponent<EntityStats>();
        _velocity.y = -2f;
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

        // Using KBM controls, there's a specific button to walk.
        // Otherwise, use the amount that user is pushing stick.
        bool isWalking = isWalkKeyHeld || inputMoveVector.magnitude < 0.5f;
        float moveSpeed = isWalking ? WalkSpeed : RunSpeed;

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

        Animator.SetBool("IsRunning", isMoving);

        // Turn the player incrementally towards the direction of movement
        PlayerModel.transform.rotation = Quaternion.RotateTowards(PlayerModel.transform.rotation,
            targetRotation,
            finalTurnSpeed * Time.deltaTime);

        // The player should always move in the direction the player model is facing.
        Vector3 absoluteMoveVector = PlayerModel.transform.forward *
                                     (desiredMoveDirection.magnitude * (moveSpeed * Time.deltaTime));

        // CharacterController.Move should only be called once, see:
        // https://forum.unity.com/threads/charactercontroller-isgrounded-unreliable-or-bad-code.373492/
        characterController.Move(_velocity * Time.deltaTime + absoluteMoveVector);

        // Update velocity from gravity
        _velocity.y += gravity * Time.deltaTime;
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
    }

    private void SetLockOnTarget(Transform lockedOnTarget) {
        _lockedOnTarget = lockedOnTarget;
        OnLockedOnTargetChanged?.Invoke(this, _lockedOnTarget);
    }
}
