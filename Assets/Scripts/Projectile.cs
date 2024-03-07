using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
    // Can either be an enemy projectile, or a player projectile
    public ProjectileController.Owner ProjectileOwner = ProjectileController.Owner.Player;
    public Vector3 velocity = Vector3.zero;

    // To turn it into a missile...
    // Target Seeking
    // Acceleration
    // Some kind of timeline for the behavior...
    // Follows a certain trajectory for a certain amount of time.
    // Then trajectory changes.
    public Transform TrackedTarget;
    public float Acceleration = 0.1f;
    public float TurningSpeed = 20.0f;
    public Vector3 AngularVelocity = Vector3.zero;
    public float AngularVelocityDecay = 0f;
    public bool FaceForward = true;
    public float MaxSpeed = -1.0f;
    public Vector3 AdditionalVelocityOffset = Vector3.zero;
    public bool TurnTowardsTarget = false;
    public float TurnTowardsTargetSpeed = 5.0f;
    public bool BackGrounded = false;

    // Maybe there's a delay before the "boosters" turn on and it accelerates towards the target.

    private void OnTriggerEnter(Collider other) {
        if (ProjectileOwner == ProjectileController.Owner.Player && other.CompareTag("Targetable")) {
            EntityStats stats = other.GetComponent<EntityStats>();
            if (stats == null) {
                Debug.LogError("[Projectile] Collided object does not have stats component!");
            } else {
                stats.ApplyDamage(5);
            }

            ProjectileController.Instance.DestroyProjectile(this);
        } else if (ProjectileOwner == ProjectileController.Owner.Enemy && other.CompareTag("Player")) {
            EntityStats stats = other.GetComponent<EntityStats>();
            if (stats == null) {
                Debug.LogError("[Projectile] Collided object does not have stats component!");
            } else {
                stats.ApplyDamage(10);
            }

            ProjectileController.Instance.DestroyProjectile(this);
        }
    }
}
