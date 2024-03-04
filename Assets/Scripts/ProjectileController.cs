using System;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileController : Singleton<ProjectileController> {
    public enum Owner {
        Player,
        Enemy
    }

    [NonNullField] public GameObject ProjectilePrefab;
    public float WorldMaxBoundary = 100.0f;

    private Dictionary<int, Projectile> _projectiles = new();
    private List<Projectile> _projectilesToDestroy = new();

    public void SpawnProjectile(Owner owner, Vector3 origin, Quaternion rotation, Vector3 velocity) {
        GameObject newProjectile = Instantiate(ProjectilePrefab, origin, rotation, this.transform);
        Projectile projectileComponent = newProjectile.GetComponent<Projectile>();
        projectileComponent.velocity = velocity;
        _projectiles.Add(newProjectile.GetInstanceID(), projectileComponent);
    }

    private void FixedUpdate() {
        _projectilesToDestroy.Clear();

        // Update all positions of projectiles
        foreach (var kv in _projectiles) {
            Projectile projectile = kv.Value;
            projectile.transform.position += projectile.velocity * Time.deltaTime;
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
