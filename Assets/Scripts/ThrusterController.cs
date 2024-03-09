using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrusterController : Singleton<ThrusterController> {
    public float ScaleFactor = 1.0f;

    [NonNullField] public GameObject RightUpperBackThruster;
    [NonNullField] public GameObject RightLowerBackThruster;
    [NonNullField] public GameObject RightSideThruster;
    [NonNullField] public GameObject RightJumpThruster;

    [NonNullField] public GameObject LeftUpperBackThruster;
    [NonNullField] public GameObject LeftLowerBackThruster;
    [NonNullField] public GameObject LeftSideThruster;
    [NonNullField] public GameObject LeftJumpThruster;

    public float RightUpperBackThrusterScaleFactor = 1.5f;
    public float RightLowerBackThrusterScaleFactor = 1.5f;
    public float RightSideThrusterScaleFactor = 1.5f;
    public float RightJumpThrusterScaleFactor = 1.5f;

    public float LeftUpperBackThrusterScaleFactor = 1.5f;
    public float LeftLowerBackThrusterScaleFactor = 1.5f;
    public float LeftSideThrusterScaleFactor = 1.5f;
    public float LeftJumpThrusterScaleFactor = 1.5f;

    private Vector3 _rightUpperBackThrusterOriginalScale;
    private Vector3 _rightLowerBackThrusterOriginalScale;
    private Vector3 _rightSideThrusterOriginalScale;
    private Vector3 _rightJumpThrusterOriginalScale;

    private Vector3 _leftUpperBackThrusterOriginalScale;
    private Vector3 _leftLowerBackThrusterOriginalScale;
    private Vector3 _leftSideThrusterOriginalScale;
    private Vector3 _leftJumpThrusterOriginalScale;

    // Each thruster will have an original scale.
    // Depending on the speed, we will lerp between that original scale a new one.
    // We need to define what the max scale is.
    // We also need to define the min and max domain for the speed.
    // Minimum here would be the regular movement speed (assuming we're dashing always). And max would be the 
    // maximum speed possible.
    // Or we could just take speed / regular speed as the "scale factor". That would also take care of "stopping"
    // if we drop below regular speed.


    private float _playerMovementSpeed;

    // TODO: Change material color of the inner thruster if we ever implement walking.

    void Start() {
        _playerMovementSpeed = PlayerManager.Instance.PlayerController.RunSpeed;

        _rightUpperBackThrusterOriginalScale = RightUpperBackThruster.transform.localScale;
        _rightUpperBackThrusterOriginalScale = RightLowerBackThruster.transform.localScale;
        _rightSideThrusterOriginalScale = RightSideThruster.transform.localScale;
        _rightJumpThrusterOriginalScale = RightJumpThruster.transform.localScale;

        _leftUpperBackThrusterOriginalScale = LeftUpperBackThruster.transform.localScale;
        _leftUpperBackThrusterOriginalScale = LeftLowerBackThruster.transform.localScale;
        _leftSideThrusterOriginalScale = LeftSideThruster.transform.localScale;
        _leftJumpThrusterOriginalScale = LeftJumpThruster.transform.localScale;
    }

    public void HandleThrusterUpdates(float moveSpeed) {
        // TODO: We need to handle separate axes.
        // Need the velocity in each separate axis.
        // This should be relative to the player's facing direction.

        // Rethink this: Let's use the movement speed instead of the noisy value.
        // Then we render the thrusters based on direction.
        // Basically choosing which thrusters to use based on direction.
        float scaleRatio = moveSpeed / _playerMovementSpeed;

        // TODO: Sudden changes will need to be attenuated.

        SetThrusterScale(RightUpperBackThruster, _rightUpperBackThrusterOriginalScale,
            RightUpperBackThrusterScaleFactor, scaleRatio);
        SetThrusterScale(RightLowerBackThruster, _rightLowerBackThrusterOriginalScale,
            RightLowerBackThrusterScaleFactor, scaleRatio);
        SetThrusterScale(RightSideThruster, _rightSideThrusterOriginalScale, RightSideThrusterScaleFactor, scaleRatio);
        SetThrusterScale(RightJumpThruster, _rightJumpThrusterOriginalScale, RightJumpThrusterScaleFactor, scaleRatio);

        SetThrusterScale(LeftUpperBackThruster, _leftUpperBackThrusterOriginalScale, LeftUpperBackThrusterScaleFactor,
            scaleRatio);
        SetThrusterScale(LeftLowerBackThruster, _leftLowerBackThrusterOriginalScale, LeftLowerBackThrusterScaleFactor,
            scaleRatio);
        SetThrusterScale(LeftSideThruster, _leftSideThrusterOriginalScale, LeftSideThrusterScaleFactor, scaleRatio);
        SetThrusterScale(LeftJumpThruster, _leftJumpThrusterOriginalScale, LeftJumpThrusterScaleFactor, scaleRatio);
    }

    public void SetThrusterScale(GameObject thrusterObject, Vector3 originalScale, float scaleFactor,
        float scaleRatio) {
        // Scales up/down the ratio but keeps it centered around 1.
        scaleRatio = 1 + (scaleRatio - 1) * scaleFactor;

        float scaleX = originalScale.x * scaleRatio;
        thrusterObject.transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
    }
}
