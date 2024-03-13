using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
    // Can either be an enemy projectile, or a player projectile
    public ProjectileController.Owner ProjectileOwner = ProjectileController.Owner.Player;
    public Vector3 velocity = Vector3.zero;
    public Collider Collider;

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

    // This projectile should be moved to the background layer after it reaches a certain height
    public bool ShouldBackgroundWhenHigh = false;

    // Has a collider that needs to be removed when it's too far from the player
    public bool ShouldRemoveCollider = false;

    // Maybe there's a delay before the "boosters" turn on and it accelerates towards the target.


    private void OnTriggerEnter(Collider other) {
        if (ProjectileOwner == ProjectileController.Owner.Player) {
            if (other.CompareTag("Targetable")) {
                // We've hit the Boss    
                BossController bossController = other.GetComponent<BossController>();
                bossController.ApplyShieldDamage();

                ProjectileController.Instance.DestroyProjectile(this);
            } else if (other.CompareTag("Destructible")) {
                // We've hit another destructible projectile
                Projectile otherProjectile = other.GetComponent<Projectile>();
                // Destroy both.
                // TODO: Play VFX Here.
                ProjectileController.Instance.DestroyProjectile(otherProjectile);
                ProjectileController.Instance.DestroyProjectile(this);
            } else if (other.CompareTag("InDestructible")) {
                ProjectileController.Instance.DestroyProjectile(this);
            }
        } else if (ProjectileOwner == ProjectileController.Owner.Enemy && other.CompareTag("Player")) {
            PlayerManager.Instance.PlayerController.ApplyDamage();
            ProjectileController.Instance.DestroyProjectile(this);
        }
    }
}
