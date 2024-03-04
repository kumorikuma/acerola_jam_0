using System.Collections.Generic;
using UnityEngine;

/*Simple player movement controller, based on character controller component,
with footstep system based on check the current texture of the component*/
public class PlayerController : MonoBehaviour {
    [NonNullField] public Animator Animator;

    //Variables for footstep system list
    [System.Serializable]
    public class GroundLayer {
        public string layerName;
        public Texture2D[] groundTextures;
        public AudioClip[] footstepSounds;
    }

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

    public GameObject PlayerModel;

    //Private movement variables
    private Vector3 inputMoveVector;
    private bool inputJumpOnNextFrame = false;
    private Vector3 _velocity; // Used for handling jumping
    private CharacterController characterController;
    private bool isWalkKeyHeld = false;
    Quaternion targetRotation;
    private bool _isExecutingFastTurn = false;

    // Lockon
    private Transform _lockedOnTarget;

    private void Awake() {
        characterController = GetComponent<CharacterController>();
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
            _lockedOnTarget = null;
            return;
        }

        // Find closest target to lock on to
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Targetable");
        foreach (GameObject enemy in enemies) {
            _lockedOnTarget = enemy.transform;
            break;
        }
    }

    // Using KBM controls, there's a specific button to walk.
    public void OnWalk(bool isWalking) {
        isWalkKeyHeld = isWalking;
    }

    private void Update() {
        Movement();
        UpdateUI();
    }

    //Character controller movement
    private void Movement() {
        if (inputJumpOnNextFrame && characterController.isGrounded) {
            _velocity.y = Mathf.Sqrt(JumpForce * -2f * gravity);
        }

        inputJumpOnNextFrame = false;

        // Using KBM controls, there's a specific button to walk.
        // Otherwise, use the amount that user is pushing stick.
        bool isWalking = isWalkKeyHeld || inputMoveVector.magnitude < 0.5f;
        float moveSpeed = isWalking ? WalkSpeed : RunSpeed;

        // Character should move in the direction of the camera
        Vector3 desiredMoveDirection =
            PlayerManager.Instance.CameraController.Pivot.transform.rotation * inputMoveVector;
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

    private void UpdateUI() {
        Vector2Int screenSpaceAimPosition = new(Screen.width / 2, Screen.height / 2);
        if (_lockedOnTarget != null) {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(_lockedOnTarget.transform.position);
            if (screenPosition.x < 0 || screenPosition.x > Screen.width || screenPosition.y < 0 ||
                screenPosition.y > Screen.height || screenPosition.z < 0) {
                // The target is outside of the screen
                _lockedOnTarget = null;
            } else {
                screenSpaceAimPosition = new((int)screenPosition.x, (int)screenPosition.y);
            }
        }

        ReactUnityBridge.Instance.UpdateScreenSpaceAimPosition(screenSpaceAimPosition);
    }
}
