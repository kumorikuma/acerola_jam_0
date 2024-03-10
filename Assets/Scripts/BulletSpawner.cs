using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BulletWave {
    public List<int> ProjectileIndices;

    public BulletWave() {
        ProjectileIndices = new();
    }
}

// Meant to be composed together with other prefabs.
public class BulletSpawner : MonoBehaviour {
    public enum AnimationTypes {
        None,
        RotateCCW,
        RotateCW,
        RotateSpiral
    }

    // Which animation to use
    public AnimationTypes AnimationType = AnimationTypes.None;

    // Having this on will let us have more control over the pattern.
    public bool OnlyUseLocalRotation = false;

    public List<GameObject> ProjectileTypes = new();
    public float StartAngleDegrees = 0;
    public float EndAngleDegrees = 0;

    public float TimeBetweenWavesMs = 200;
    public float InitialPositionOffset = 0f;
    public float ProjectileSpeed = 10.0f;
    private bool _isEnabled = false;
    private Animator _animator = null;

    // Wave defnitions
    public int WavesToSpawn = 10;

    // Defines the wave pattern. If WavesToSpawn is greater than this, it will just wrap around.
    public List<BulletWave> BulletWaves = new();

    // When this bullet spawners is played or stopped, these are as well.
    public List<BulletSpawner> ChildBulletSpawners = new();

    private int _wavesSpawned = 0;
    private float _timeBetweenWavesSeconds;
    private float _timeUntilNextWave = 0;
    private Transform _playerTarget = null;
    private Quaternion _rotationAtSpawn = Quaternion.identity;

    // If this is set, then we will point our bullet spawner towards this target.
    // Note: This means the children will be rotated to.
    // We can compose targeted and non targeted bullet spawners, the targeted ones just need to be children of a 
    // non-targeteed one.
    public bool ShouldAimAtPlayer = false;

    public event Action OnSpawningStopped;

    private void Start() {
        _animator = GetComponent<Animator>();
        if (ShouldAimAtPlayer) {
            _playerTarget = PlayerManager.Instance.PlayerController.EnemyAimTargetLocation;
        }
    }

    private void FixedUpdate() {
        if (!_isEnabled) {
            return;
        }

        // TODO: Implement should aim at player??
        // Actually we might not want this because the boss itself will face the player.

        if (_timeUntilNextWave <= 0.0f) {
            SpawnWave();
            _wavesSpawned += 1;
            _timeUntilNextWave = _timeBetweenWavesSeconds;
            if (_wavesSpawned >= WavesToSpawn) {
                Stop();
            }
        } else {
            _timeUntilNextWave -= Time.fixedDeltaTime;
        }
    }

    public void Play() {
        if (BulletWaves.Count == 0) {
            Debug.LogError("[BulletSpawner] No bullet waves defined!");
            return;
        }

        Reset();
        _isEnabled = true;
        _rotationAtSpawn = transform.rotation;
        SetAnimation(AnimationType);

        foreach (BulletSpawner spawner in ChildBulletSpawners) {
            spawner.Play();
        }
    }

    // Stops the spawner before its finished.
    public void Stop() {
        Reset();

        foreach (BulletSpawner spawner in ChildBulletSpawners) {
            spawner.Stop();
        }

        OnSpawningStopped?.Invoke();
    }

    private void Reset() {
        SetAnimation(AnimationTypes.None);

        _timeUntilNextWave = 0;
        _isEnabled = false;
        _wavesSpawned = 0;
    }

    private void SetAnimation(AnimationTypes animationType) {
        if (_animator == null) {
            return;
        }

        _animator.SetInteger("AnimationType", (int)animationType);
    }

    private void SpawnWave() {
        int waveIndex = _wavesSpawned % BulletWaves.Count;
        List<int> bulletWave = BulletWaves[waveIndex].ProjectileIndices;
        int numBullets = bulletWave.Count;
        // Put this here so it can be changed in inspector
        _timeBetweenWavesSeconds = TimeBetweenWavesMs / 1000.0f;

        for (int i = 0; i < numBullets; i++) {
            // Calculate the angle for this object
            float angleT = 0;
            if (numBullets > 1) {
                angleT = i / (numBullets - 1.0f);
            }

            float angle = Mathf.Lerp(StartAngleDegrees, EndAngleDegrees, angleT);

            // There can be two sources of rotation:
            // - Local rotation of the spawner itself, that can be driven by animation.
            // - The parent transform of the spawner changing (for instance due to be being attached to the boss and moving)
            // For the second source: we sometimes don't want to keep using that when spawning multiple waves.
            Quaternion localRotation = transform.localRotation;
            Quaternion baseRotation = transform.rotation;
            if (OnlyUseLocalRotation) {
                baseRotation = _rotationAtSpawn;
            }

            Quaternion rotation = Quaternion.Euler(0, angle, 0) * baseRotation * transform.localRotation;

            // Calculate the position for this object
            Vector3 direction = rotation * Vector3.forward;
            Vector3 position = transform.position + direction * InitialPositionOffset;

            // Instantiate the object
            int projectileIndex = bulletWave[i];
            if (projectileIndex >= ProjectileTypes.Count) {
                Debug.LogError("[BulletSpawner] Out of bounds index access! Check projectile types");
                continue;
            }

            GameObject projectilePrefab = ProjectileTypes[projectileIndex];
            Projectile projectile = ProjectileController.Instance.SpawnProjectile(ProjectileController.Owner.Enemy,
                projectilePrefab, position,
                rotation, direction * ProjectileSpeed);
            // TODO: We probably should remove the collider but the logic right now where if it's too far from the player doesn't work if the projectile wants to move towards the player.
            projectile.ShouldBackgroundWhenHigh = false;
            projectile.ShouldRemoveCollider = false;

            // Optionally, make the spawned object face outward from the arc's center
            // This assumes that the object's "forward" should point directly away from the arc's center
            // spawnedObject.transform.LookAt(2 * spawnedObject.transform.position - transform.position);
        }
    }
}
