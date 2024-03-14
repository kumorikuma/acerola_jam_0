using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeamAttack : MonoBehaviour {
    [NonNullField] public GameObject LaserBeamBody;
    [NonNullField] public GameObject LaserBeamEnd;
    public AnimationCurve LaserSpeedScale;
    public float LaserSpeed = 50.0f;
    public float BeamSizeScale = 0.05f;
    public float AttackDuration = 3.0f;
    public float BeamStopTrackingThreshold = 5.0f;

    private Quaternion _originalLaserBeamBodyRotation;
    private Vector3 _originalLaserBeamBodyLocalScale;
    private Vector3 _previousBeamDirection = Vector3.zero;
    private bool _isTracking = false;
    private bool _isAttacking = false;
    private float _attackTimer = 0.0f;

    void Start() {
        _originalLaserBeamBodyRotation = LaserBeamBody.transform.rotation;
        _originalLaserBeamBodyLocalScale = LaserBeamBody.transform.localScale;
        LaserBeamBody.SetActive(false);
        LaserBeamEnd.SetActive(false);
    }

    // Update is called once per frame
    void Update() {
        if (!_isAttacking) {
            return;
        }

        if (_attackTimer >= AttackDuration) {
            StopAttack();
            return;
        }

        Vector3 beamEndToPlayer = PlayerManager.Instance.PlayerController.EnemyAimTargetLocation.position -
                                  LaserBeamEnd.transform.position;

        float t = _attackTimer / AttackDuration;
        float finalLaserSpeed = (LaserSpeed * LaserSpeedScale.Evaluate(t) * Time.deltaTime);
        if (_isTracking) {
            // If the laser beam end is close enough to the player, then stop tracking.
            if (beamEndToPlayer.magnitude <= BeamStopTrackingThreshold) {
                _isTracking = false;
            }

            beamEndToPlayer.y = 0;
            beamEndToPlayer = beamEndToPlayer.normalized;
            LaserBeamEnd.transform.position +=
                beamEndToPlayer * finalLaserSpeed;
            _previousBeamDirection = beamEndToPlayer;
        } else {
            LaserBeamEnd.transform.position += _previousBeamDirection * finalLaserSpeed;
        }

        // Set the scale of the body
        Vector3 beamOriginToNewBeamEnd = LaserBeamEnd.transform.position - transform.position;
        LaserBeamBody.transform.localScale = new Vector3(LaserBeamBody.transform.localScale.x,
            LaserBeamBody.transform.localScale.y, beamOriginToNewBeamEnd.magnitude * BeamSizeScale);
        // Point the body towards the end
        LaserBeamBody.transform.rotation = Quaternion.LookRotation(beamOriginToNewBeamEnd, Vector3.up);

        _attackTimer += Time.deltaTime;
    }

    public void StartAttack() {
        _isAttacking = true;
        _isTracking = true;
        LaserBeamBody.SetActive(true);
        LaserBeamBody.transform.rotation = _originalLaserBeamBodyRotation;
        LaserBeamBody.transform.localScale = _originalLaserBeamBodyLocalScale;
        LaserBeamEnd.SetActive(true);
        // Spawn it below on the ground
        LaserBeamEnd.transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        _attackTimer = 0.0f;
    }

    public void StopAttack() {
        _isAttacking = false;
        LaserBeamBody.SetActive(false);
        LaserBeamEnd.SetActive(false);
    }
}
