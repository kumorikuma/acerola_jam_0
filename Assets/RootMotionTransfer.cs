using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionTransfer : MonoBehaviour {
    [NonNullField] public Transform TransferTarget;
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

        ReactUnityBridge.Instance.UpdateDebugString("Animator Root Pos", _animator.rootPosition.ToString());
        ReactUnityBridge.Instance.UpdateDebugString("Transform Pos", TransferTarget.position.ToString());
        TransferTarget.position = _animator.rootPosition;
        // TransferTarget.rotation = _animator.rootRotation;
    }

    public void SetApplyRootMotion(bool applyRootMotion) {
        _shouldApplyRootMotion = applyRootMotion;
    }
}
