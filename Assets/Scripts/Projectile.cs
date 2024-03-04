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

    private void OnTriggerEnter(Collider other) {
        if (ProjectileOwner == ProjectileController.Owner.Player && other.CompareTag("Targetable")) {
            EntityStats stats = other.GetComponent<EntityStats>();
            if (stats == null) {
                Debug.LogError("[Projectile] Collided object does not have stats component!");
            } else {
                stats.ApplyDamage(5);
            }

            ProjectileController.Instance.DestroyProjectile(this);
        }
    }
}
