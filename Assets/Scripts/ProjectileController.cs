using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ProjectileController : Singleton<ProjectileController> {
    public enum Owner {
        Player,
        Enemy,
        World
    }

    [NonNullField] public GameObject ProjectilePrefab;
    [NonNullField] public GameObject MissilePrefab;
    [NonNullField] public GameObject PanelPrefab;
    public float WorldMaxBoundary = 1000.0f;

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

    public Projectile SpawnPanel(Vector3 origin, float speed, Vector3 angularVelocity, float angularVelocityDecay,
        Vector3 velocityOffset) {
        GameObject newProjectile = Instantiate(PanelPrefab, origin, Quaternion.LookRotation(Vector3.up, Vector3.back),
            this.transform);
        Projectile projectileComponent = newProjectile.GetComponent<Projectile>();
        projectileComponent.ProjectileOwner = Owner.World;
        projectileComponent.velocity = Vector3.up * speed;
        projectileComponent.AdditionalVelocityOffset = velocityOffset;
        projectileComponent.AngularVelocity = angularVelocity;
        projectileComponent.FaceForward = false;
        projectileComponent.AngularVelocityDecay = angularVelocityDecay;
        _projectiles.Add(newProjectile.GetInstanceID(), projectileComponent);
        return projectileComponent;
    }

    private void FixedUpdate() {
        _projectilesToDestroy.Clear();

        // Update all positions of projectiles
        foreach (var kv in _projectiles) {
            Projectile projectile = kv.Value;

            // Update 
            projectile.transform.position +=
                (projectile.velocity + projectile.AdditionalVelocityOffset) * Time.fixedDeltaTime;
            // projectile.transform.position +=
            //     projectile.velocity * Time.fixedDeltaTime;

            // If there's an angular velocity, rotate the projectile
            if (projectile.AngularVelocity != Vector3.zero) {
                Quaternion rotation = Quaternion.Euler(projectile.AngularVelocity);
                projectile.transform.rotation *= rotation;
                if (projectile.AngularVelocityDecay > 0) {
                    float angularSpeed = projectile.AngularVelocity.magnitude -
                                         projectile.AngularVelocityDecay * Time.fixedDeltaTime;
                    projectile.AngularVelocity = projectile.AngularVelocity.normalized * angularSpeed;
                }
            }

            // If there's a target that's being tracked, turn the projectile towards it.
            if (projectile.TrackedTarget != null) {
                Vector3 missileToTargetVector =
                    (projectile.TrackedTarget.position - projectile.transform.position).normalized;
                float angleDeltaDegrees = projectile.TurningSpeed * Time.fixedDeltaTime;

                // Rotate the forward vector towards the target direction.
                projectile.velocity =
                    Vector3.RotateTowards(projectile.velocity, missileToTargetVector, angleDeltaDegrees, 0.0f);
                Quaternion forwardRotation = Quaternion.LookRotation(projectile.velocity, projectile.transform.up);
                if (projectile.FaceForward) {
                    projectile.transform.rotation = forwardRotation;
                } else if (projectile.TurnTowardsTarget) {
                    projectile.transform.rotation =
                        Quaternion.RotateTowards(projectile.transform.rotation, forwardRotation,
                            projectile.TurnTowardsTargetSpeed * Time.fixedDeltaTime);
                }
            }

            if (projectile.Acceleration > 0) {
                projectile.velocity += projectile.velocity.normalized * (projectile.Acceleration * Time.fixedDeltaTime);
            }

            // Cap the max speed and stop accelerating
            // if (projectile.MaxSpeed >= 0.0f && projectile.velocity.magnitude > projectile.MaxSpeed) {
            //     projectile.velocity = projectile.velocity.normalized * projectile.MaxSpeed;
            //     projectile.Acceleration = 0;
            // }

            Vector3 position = projectile.transform.position;
            float projectileDistanceToPlayer =
                (PlayerManager.Instance.PlayerController.transform.position - position).magnitude;
            if (projectileDistanceToPlayer > 100) {
                projectile.gameObject.SetLayerAllChildren(LayerMask.NameToLayer("Background"));
            }

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
