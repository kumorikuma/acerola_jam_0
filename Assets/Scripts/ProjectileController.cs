﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileController : Singleton<ProjectileController> {
    public enum Owner {
        Player,
        Enemy
    }

    [NonNullField] public GameObject ProjectilePrefab;
    [NonNullField] public GameObject MissilePrefab;
    public float WorldMaxBoundary = 100.0f;

    private Dictionary<int, Projectile> _projectiles = new();
    private List<Projectile> _projectilesToDestroy = new();

    public Projectile SpawnProjectile(Owner owner, Vector3 origin, Quaternion rotation, Vector3 velocity) {
        GameObject newProjectile = Instantiate(ProjectilePrefab, origin, rotation, this.transform);
        Projectile projectileComponent = newProjectile.GetComponent<Projectile>();
        projectileComponent.ProjectileOwner = owner;
        projectileComponent.velocity = velocity;
        _projectiles.Add(newProjectile.GetInstanceID(), projectileComponent);
        return projectileComponent;
    }

    public Projectile SpawnMissile(Owner owner, Vector3 origin, Quaternion rotation, float speed) {
        GameObject newProjectile = Instantiate(MissilePrefab, origin, rotation, this.transform);
        Projectile projectileComponent = newProjectile.GetComponent<Projectile>();
        projectileComponent.ProjectileOwner = owner;
        projectileComponent.velocity = rotation * Vector3.forward * speed;
        _projectiles.Add(newProjectile.GetInstanceID(), projectileComponent);
        return projectileComponent;
    }

    private void FixedUpdate() {
        _projectilesToDestroy.Clear();

        // Update all positions of projectiles
        foreach (var kv in _projectiles) {
            Projectile projectile = kv.Value;

            // Update 
            projectile.transform.position += projectile.velocity * Time.fixedDeltaTime;

            // If there's a target that's being tracked, turn the projectile towards it.
            if (projectile.TrackedTarget != null) {
                Vector3 missileToTargetVector =
                    (projectile.TrackedTarget.position - projectile.transform.position).normalized;
                float angleDeltaDegrees = projectile.TurningSpeed * Time.fixedDeltaTime;

                // Rotate the forward vector towards the target direction.
                projectile.velocity =
                    Vector3.RotateTowards(projectile.velocity, missileToTargetVector, angleDeltaDegrees, 0.0f);
                projectile.transform.rotation = Quaternion.LookRotation(projectile.velocity, projectile.transform.up);
            }

            if (projectile.Acceleration > 0) {
                projectile.velocity += projectile.velocity * (projectile.Acceleration * Time.fixedDeltaTime);
            }

            Vector3 position = projectile.transform.position;
            // Destroy the projectile if it's out of bounds.
            if (position.x < -WorldMaxBoundary || position.x > WorldMaxBoundary || position.z < -WorldMaxBoundary ||
                position.z > WorldMaxBoundary) {
                _projectilesToDestroy.Add(projectile);
            }
        }

        foreach (Projectile projectile in _projectilesToDestroy) {
            DestroyProjectile(projectile);
        }
    }

    public void DestroyProjectile(Projectile projectile) {
        _projectiles.Remove(projectile.gameObject.GetInstanceID());
        UnityEngine.Object.Destroy(projectile.gameObject);
    }
}
