using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
    // Can either be an enemy projectile, or a player projectile
    public ProjectileController.Owner ProjectileOwner = ProjectileController.Owner.Player;
    public Vector3 velocity = Vector3.zero;

    private void OnTriggerEnter(Collider other) {
        if (ProjectileOwner == ProjectileController.Owner.Player && other.CompareTag("Targetable")) {
            ProjectileController.Instance.DestroyProjectile(this);
        }
    }
}
