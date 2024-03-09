using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using Vector3 = Tripolygon.UModelerX.Runtime.DataPackV1.Vector3;

public class RootMotionTransfer : MonoBehaviour {
    [NonNullField] public Transform TransferTarget;
    [NonNullField] public CharacterController CharacterTarget;
    private bool _shouldApplyRootMotion = false;
    private Animator _animator;

    // Start is called before the first frame update
    void Start() {
        _animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove() {
        if (!_shouldApplyRootMotion) {
            return;
        }

        // ReactUnityBridge.Instance.UpdateDebugString("Animator Root Pos", _animator.rootPosition.ToString());
        // ReactUnityBridge.Instance.UpdateDebugString("Transform Pos", TransferTarget.position.ToString());
        // TransferTarget.position = _animator.rootPosition;
        // TransferTarget.rotation = _animator.rootRotation;

        // Calculate the delta between the current position and the root position and apply a movement.
        // Doing this will affect collision.
        Vector3 deltaVector = _animator.rootPosition - TransferTarget.position;
        // Debug.Log(deltaVector);
        CharacterTarget.Move(deltaVector);
    }

    public void SetApplyRootMotion(bool applyRootMotion) {
        _shouldApplyRootMotion = applyRootMotion;
    }
}
