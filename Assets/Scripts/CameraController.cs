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

    public void OnLook(Vector2 lookVector) {
        inputLookDirection = lookVector * LookSpeed;
    }

    private void Update() {
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
        Pivot.transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, localEulerRotation.z);

        // If the Pivot is under the map, then move it closer to the camera.
        // If the vertical rotation is negative over a threshold, start moving it closer to the 
    }
}
