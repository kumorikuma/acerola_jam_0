using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour {
    [NonNullField] public GameObject Pivot;
    public float MinVerticalRotation = -20.0f;
    public float MaxVerticalRotation = 40.0f;

    private Vector2 inputLookDirection;

    public bool InvertLookDirection = false;
    public float LookSpeed = 0.5f;
    private int _defaultCameraPriority = 10;
    private int _boostedCameraPriority = 100;

    [NonNullField] public CinemachineVirtualCamera MainCamera;
    [NonNullField] public CinemachineVirtualCamera LeftShoulderCamera;
    [NonNullField] public CinemachineVirtualCamera RightShoulderCamera;
    // [NonNullField] public CinemachineTargetGroup TargetGroup;

    private Transform _lockedOnTarget = null;

    public void OnLook(Vector2 lookVector) {
        inputLookDirection = lookVector * LookSpeed;
    }

    private void Start() {
        PlayerManager.Instance.PlayerController.OnLockedOnTargetChanged += OnLockedOnTargetChanged;
    }

    public void Reset() {
        _lockedOnTarget = null;
        Pivot.transform.rotation = Quaternion.identity;
    }

    private void OnLockedOnTargetChanged(object sender, Transform lockedOnTarget) {
        _lockedOnTarget = lockedOnTarget;
        if (lockedOnTarget == null) {
            ResetCameraPriorities();
            // TargetGroup.RemoveMember(lockedOnTarget);
            return;
        }

        // TargetGroup.AddMember(lockedOnTarget, 1, 1);

        // Switch to the left or right camera depending on which side of the player the target is on.
        Camera mainCamera = Camera.main;
        Vector3 targetScreenPosition = mainCamera.WorldToScreenPoint(lockedOnTarget.transform.position);
        Vector3 playerScreenPosition =
            mainCamera.WorldToScreenPoint(PlayerManager.Instance.PlayerController.PlayerModel.transform.position);
        // MainCamera.LookAt = lockedOnTarget;
        if (targetScreenPosition.x < playerScreenPosition.x) {
            BoostLeftShoulderCamera();
            LeftShoulderCamera.LookAt = lockedOnTarget;
            // Pivot.transform.localPosition = new(-5, 2, 0);
        } else {
            BoostRightShoulderCamera();
            RightShoulderCamera.LookAt = lockedOnTarget;
            // Pivot.transform.localPosition = new(5, 2, 0);
        }
    }

    private void BoostLeftShoulderCamera() {
        ResetCameraPriorities();
        LeftShoulderCamera.Priority = _boostedCameraPriority;
    }

    private void BoostRightShoulderCamera() {
        ResetCameraPriorities();
        RightShoulderCamera.Priority = _boostedCameraPriority;
    }

    private void ResetCameraPriorities() {
        // Pivot.transform.localPosition = new(0, 2, 0);
        LeftShoulderCamera.Priority = _defaultCameraPriority;
        RightShoulderCamera.Priority = _defaultCameraPriority;
        // MainCamera.LookAt = null;
        LeftShoulderCamera.LookAt = null;
        RightShoulderCamera.LookAt = null;
    }

    public void UpdateCamera() {
        if (_lockedOnTarget == null) {
            // Let the user control the camera

            // Update camera transform
            Vector3 localEulerRotation = Pivot.transform.localRotation.eulerAngles;
            float horizontalRotation = localEulerRotation.y;
            horizontalRotation += inputLookDirection.x;
            float verticalRotation = localEulerRotation.x;
            // Fixes an issue where we might read values like 350deg, and then it'd get clamped down to 40, when it should've been -10.
            if (verticalRotation > 180) {
                verticalRotation -= 360;
            }

            if (InvertLookDirection) {
                inputLookDirection.y = -inputLookDirection.y;
            }

            verticalRotation += -inputLookDirection.y;
            verticalRotation = Mathf.Clamp(verticalRotation, MinVerticalRotation, MaxVerticalRotation);
            Pivot.transform.localRotation =
                Quaternion.Euler(verticalRotation, horizontalRotation, localEulerRotation.z);

            // TODO:
            // If the Pivot is under the map, then move it closer to the camera.
            // If the vertical rotation is negative over a threshold, start moving it closer to the 
        } else {
            // The camera should remain static behind the player.
            // When the player is rotated, the camera needs to be rotated too.
            Pivot.transform.rotation = PlayerManager.Instance.PlayerController.PlayerModel.transform.rotation;
        }
    }
}
