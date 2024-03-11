﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PhaseData {
    public int BossHealth;
    public List<BulletSpawner> BulletSpawners;

    public PhaseData() {
        BulletSpawners = new();
        BossHealth = 100;
    }
}

public class BossController : Singleton<BossController> {
    [NonSerialized] public EntityStats Stats;

    public bool IsLocomotionEnabled = true;
    public bool IsAttackingEnabled = true;
    public float SlowMovementSpeed = 5.0f;
    public float FastMovementSpeed = 5.0f;
    public float OptimalDistanceToPlayer = 30.0f;
    public float ReturnToCenterThreshold = 150.0f;
    public float RetreatVectorBias = 1.0f;

    private int MaxBossLives = 3;
    private int CurrentBossLives = 3;
    public event EventHandler<int> OnBossLivesChanged;

    // public List<BulletSpawner> BulletSpawners;
    public BulletSpawner spawner;
    private Transform _playerTarget = null;
    private Quaternion _targetRotation = Quaternion.identity;
    private Vector3 _targetPosition = Vector3.zero;
    private Vector3 ArenaCenter = Vector3.zero;

    public AnimationCurve MovementSpeedScaleCurve;

    // Attacks
    public float InitialAttackCooldown = 10.0f;
    public float AttackCooldown = 5.0f;
    private float _attackCooldownCountdown = 0.0f;
    private bool _isAttacking = false;

    public List<PhaseData> BossPhaseData = new();
    private PhaseData _currentBossPhaseData;
    private List<BulletSpawner> _bulletSpawnersBag = new();
    private int _currentPhase = 0;

    public void FireMissiles() {
        spawner.Play();

        // Quaternion verticalRotation = Quaternion.LookRotation(this.transform.up, -this.transform.forward);
        // Projectile missile = ProjectileController.Instance.SpawnMissile(ProjectileController.Owner.Enemy,
        //     this.transform.position,
        //     verticalRotation, 5.0f);
        //
        // // TODO: Do some smarter logic with the targeting.
        // // The targeting system should aim towards the player's movement based on how fast the character is moving.
        // StartCoroutine(MissileSecondStage(missile, PlayerManager.Instance.PlayerController.PlayerModel.transform));
    }

    IEnumerator MissileSecondStage(Projectile missile, Transform trackedTarget) {
        yield return new WaitForSeconds(1.0f);

        missile.TrackedTarget = trackedTarget;
        // missile.velocity = missile.velocity * 5;
        missile.Acceleration = 2.0f;
    }

    protected override void Awake() {
        base.Awake();
        Stats = GetComponent<EntityStats>();
    }

    private void Update() {
        // If the game is over, don't do anything
        if (!GameLifecycleManager.Instance.IsGamePlaying()) {
            return;
        }

        if (IsLocomotionEnabled) {
            HandleLocomotion();
        }

        if (IsAttackingEnabled && !_isAttacking) {
            HandleAttack();
        }

        ReactUnityBridge.Instance.UpdateDebugString("Boss IsAttacking", _isAttacking.ToString());
    }

    private void Start() {
        Stats.NotifyHealthChanged();
        Stats.OnHealthChanged += OnHealthChanged;
        _playerTarget = PlayerManager.Instance.PlayerController.EnemyAimTargetLocation;

        if (BossPhaseData.Count == 0) {
            Debug.LogError("[BossController] Boss Phase data is not set!");
        }

        MaxBossLives = BossPhaseData.Count;
        Reset();
    }

    private void OnSpawningStopped() {
        _isAttacking = false;
        Debug.Log("ON SPAWNING STOPPED");
    }

    public void Reset() {
        _attackCooldownCountdown = InitialAttackCooldown;
        _isAttacking = false;
        SetBossLives(MaxBossLives);
        Stats.Reset();
    }

    private void OnHealthChanged(object sender, float health) {
        if (health <= 0.0f) {
            ConsumeBossLife();
        }
    }

    private void SetBossLives(int bossLives) {
        CurrentBossLives = Mathf.Clamp(bossLives, 0, MaxBossLives);
        _currentPhase = MaxBossLives - CurrentBossLives;
        _currentBossPhaseData = BossPhaseData[_currentPhase];
        _bulletSpawnersBag.Clear();

        if (CurrentBossLives == 0) {
            // Player wins!
            GameLifecycleManager.Instance.EndGame();
        } else {
            // Heal the boss back up to 100%
            Stats.MaxHealth = _currentBossPhaseData.BossHealth;
            Stats.SetHealthToPercentage(1.0f);
        }

        // TODO: Have some kind of animation for this
        // TODO: Boss moveset should change.
        // TODO: There should be some visual indication of things changing?
        // TODO: The rate of planetary decay should increase.
        // Phase 1: 0
        // Phase 2: Half Rate
        // Phase 3: Increased Rate?
        // Phase 4?

        OnBossLivesChanged?.Invoke(this, CurrentBossLives);
    }

    private void ConsumeBossLife() {
        SetBossLives(CurrentBossLives - 1);
    }

    private void HandleAttack() {
        // Basically every once in a while (lets say every COOLDOWN seconds), we will pick a random attack.
        // An attack can be by itself, or it can be changed together with a series of attacks.
        // While the boss is attacking, it cannot move.
        // You could say this is Choreographed.
        if (_attackCooldownCountdown <= 0.0f) {
            // Execute an attack.
            _attackCooldownCountdown = AttackCooldown;
            _isAttacking = true;
            PerformRandomAttack();
        } else {
            _attackCooldownCountdown -= Time.deltaTime;
        }
    }

    private void PerformRandomAttack() {
        // What is an attack? Perhaps we should be using a timeline for this?
        // Maybe not for now. Right now they are just a bunch of BulletSpawners. And each attack is we do 
        // BulletSpawner.Play(). 
        // When BulletSpawner stops, then the attack has ended.
        // TODO: Sometimes we will need to forcibly stop the bullet spawners. AKA cancel and stop the attack.
        // For each phase, we will have a list of bullet spawners. We will pick from them randomly "bag" style.
        // As in, add them to a list and randomly pick from the list. If we run out, we restock the bag.
        if (_bulletSpawnersBag.Count == 0) {
            // Reload the bag
            foreach (BulletSpawner spawner in _currentBossPhaseData.BulletSpawners) {
                _bulletSpawnersBag.Add(spawner);
            }
        }

        // Pick a random spawner
        int spawnerIdx = UnityEngine.Random.Range(0, _bulletSpawnersBag.Count);
        BulletSpawner selectedSpawner = _bulletSpawnersBag[spawnerIdx];

        // Unsubcribe first just in case we've already subscribed (duplicate reference).
        selectedSpawner.OnSpawningStopped -= OnSpawningStopped;
        selectedSpawner.OnSpawningStopped += OnSpawningStopped;
        Debug.Log("Subscribing to spawner: " + spawner.name);

        // Remove it so it's not picked again
        _bulletSpawnersBag.RemoveAt(spawnerIdx);
        selectedSpawner.Play();
    }


    // TODO: Could try improving this by making the behavior always where the boss tries to the center of the arena
    // between it and you. But alas no time!.
    private void HandleLocomotion() {
        Vector3 bossToTargetVector = _playerTarget.position - transform.position;
        bossToTargetVector.y = 0;
        Vector3 bossToCenterVector = ArenaCenter - transform.position;
        Vector2 playerTargetPosXZ = new Vector2(_playerTarget.position.x, _playerTarget.position.z);
        float distanceToPlayer = bossToTargetVector.magnitude;
        float distanceToCenter = bossToCenterVector.magnitude;

        float movementSpeed = 0;

        // Whether the player is on the closer or far side of the arena
        Vector2 bossToCenterOrthogonalVector = new Vector2(bossToCenterVector.z, -bossToCenterVector.x);
        int closeFarSideOfCenter = PointSideOfLine(Vector2.zero, bossToCenterOrthogonalVector,
            new Vector2(_playerTarget.position.x, _playerTarget.position.z));
        ReactUnityBridge.Instance.UpdateDebugString("closeFarSideOfCenter",
            closeFarSideOfCenter.ToString());
        VectorDebug.Instance.DrawDebugVector("BossToCenterOrthogonalVector",
            new Vector3(bossToCenterOrthogonalVector.x, 0, bossToCenterOrthogonalVector.y), ArenaCenter,
            Color.magenta);

        // Compute the shortest vector from the Player to bossToCenterVector.
        Vector2 playerToBossToCenterLineVector = PointToLineVector(Vector2.zero,
            new Vector2(transform.position.x, transform.position.z),
            playerTargetPosXZ);

        // Aim towards player
        Vector3 lookVector = bossToTargetVector;
        lookVector = lookVector.normalized;
        _targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);

        // ===== Compute Target Position =====

        float playerDistanceToCenter = (ArenaCenter - _playerTarget.position).magnitude;
        bool bossIsCloserToCenterThanPlayer = distanceToCenter < playerDistanceToCenter;

        // ReactUnityBridge.Instance.UpdateDebugString("IsGreaterThanOptimalDistance",
        //     (distanceToPlayer > OptimalDistanceToPlayer).ToString());
        // VectorDebug.Instance.DrawDebugVector("bossToCenterVector", bossToCenterVector, transform.position,
        //     Color.magenta);
        int behaviorTreeCase = -1;
        // if (distanceToPlayer < CriticalThreshold) {
        // Case 0: Boss is critically close to player. Get out
        // }
        if (distanceToPlayer > OptimalDistanceToPlayer) {
            if (distanceToCenter < ReturnToCenterThreshold) {
                // Case 0: Boss is far from the player but doesn't feel like it needs to return to the center.

                // Idle
                _targetPosition = transform.position;
                behaviorTreeCase = 5;
            } else {
                // Case 1: Boss is far from the player
                // Two more cases depending on if the player is on the boss' side of the center or not.
                if (closeFarSideOfCenter > 0) {
                    // Case 1.A: Player is on the other side of the arena.
                    // We'll just move towards the center.
                    _targetPosition = ArenaCenter;
                    behaviorTreeCase = 0;
                } else {
                    // Two more cases depending on the length
                    if (playerToBossToCenterLineVector.magnitude > OptimalDistanceToPlayer) {
                        // Case 1.B: which is same as case A. Player is too far for this to matter.
                        _targetPosition = ArenaCenter;
                        behaviorTreeCase = 1;
                    } else {
                        // Case 1.C: We need to navigate away from the player as well as make progress towards the center.
                        playerToBossToCenterLineVector = playerToBossToCenterLineVector.normalized;
                        Vector3 playerToBossToCenterLineVec3 = new Vector3(playerToBossToCenterLineVector.x, 0,
                            playerToBossToCenterLineVector.y);
                        _targetPosition = playerToBossToCenterLineVec3 * OptimalDistanceToPlayer;
                        behaviorTreeCase = 2;
                    }
                }

                // Adjust the speed based on the distance to the center.
                // If distance is larger, we want to be faster.
                // Curve is defined as 0 = largest, 1 = smallest.
                float t = 1 - distanceToCenter / 300;
                float adjustedT = MovementSpeedScaleCurve.Evaluate(t);
                movementSpeed = Mathf.Lerp(SlowMovementSpeed, FastMovementSpeed, adjustedT);
            }
        } else {
            // Case 2: Two more cases depending on if the player is on the boss' side of the center or not.
            // We will need to back up to get away from the player, but also be mindful of trying to go to the center.
            // The theory here is that there are two possible vectors we can take.
            // If we want to go back to the optimum distance ASAP, then we'd want to take the retreat vector directly away from player.
            // If we were farther away (almost at the optimal distance), then we would want to go left or right to possibly open up a path to the center.

            Vector3 retreatVector = -bossToTargetVector.normalized;
            VectorDebug.Instance.DrawDebugVector("retreatVector",
                retreatVector, transform.position,
                Color.green);

            float distanceT = Mathf.Clamp(distanceToPlayer / OptimalDistanceToPlayer, 0, 1);

            if (closeFarSideOfCenter > 0 || bossIsCloserToCenterThanPlayer) {
                // Case 2.A: Player is on the other side of the arena.
                // We'll just move backwards away from the player. The center is between us and the player.
                // HACK: bossIsCloserToCenterThanPlayer check is a hack to get around a bug.
                // There is some case where this is true when it shouldn't be and the boss is closer to the center
                // than the player is. If that's the case, also do this.
                _targetPosition = transform.position + retreatVector;
                behaviorTreeCase = 3;
            } else {
                // Case 2.B: Player is on the close side of the arena.

                // Figure out which side of the center the player is on (left or right)
                Vector2 bossToCenterVector2 = new Vector2(bossToCenterVector.x, bossToCenterVector.z);
                int sideOfCenter = PointSideOfLine(Vector2.zero, bossToCenterVector2,
                    playerTargetPosXZ);
                ReactUnityBridge.Instance.UpdateDebugString("sideOfCenter",
                    sideOfCenter.ToString());


                // This vector would take us on a better path towards reaching the center.
                Vector3 tangentialVector = Vector3.zero;
                if (sideOfCenter < 0) {
                    // Left of center
                    tangentialVector = transform.rotation * Vector3.left;
                } else {
                    // On or right of center
                    tangentialVector = transform.rotation * Vector3.right;
                }

                VectorDebug.Instance.DrawDebugVector("TangentialVector",
                    tangentialVector, transform.position,
                    Color.magenta);

                // Based on the idea that if the boss is right next to the player, it would want to take the retreatVector.
                // And if it was far away, it'd take the sideways tangential vector, we will LERP between them.
                // ReactUnityBridge.Instance.UpdateDebugString("Distance Ratio",
                //     distanceT.ToString());
                // We can bias towards the retreat vector by making it larger.
                Vector3 targetVector = Vector3.Slerp(retreatVector * RetreatVectorBias, tangentialVector, distanceT);
                behaviorTreeCase = 4;

                _targetPosition = transform.position + targetVector;
                VectorDebug.Instance.DrawDebugVector("TargetVector",
                    _targetPosition, transform.position,
                    Color.red);
            }

            // Adjust the speed based on the distance to the player.
            // If distance is smaller, we want to be faster.
            // Curve is defined as 0 = largest, 1 = smallest.
            float adjustedT = MovementSpeedScaleCurve.Evaluate(distanceT);
            movementSpeed = Mathf.Lerp(SlowMovementSpeed, FastMovementSpeed, adjustedT);
        }

        // ===== Apply Movement =====
        //  TODO: If distance is too far, then dash?
        // Move towards target position
        Vector3 movementVector = _targetPosition - transform.position;
        Vector3 absoluteMovementVector = movementVector.normalized * (movementSpeed * Time.deltaTime);
        transform.position += absoluteMovementVector;
        if ((_targetPosition - transform.position).magnitude < 0.1f) {
            _targetPosition = absoluteMovementVector;
        }

        VectorDebug.Instance.DrawDebugVector("absoluteMovementVector",
            absoluteMovementVector, transform.position,
            Color.red);

        // ReactUnityBridge.Instance.UpdateDebugString("Boss Movement Speed",
        //     movementSpeed.ToString());
        // ReactUnityBridge.Instance.UpdateDebugString("Behavior Tree Case",
        //     behaviorTreeCase.ToString());

        // ===== Apply Rotation =====
        // Rotate towards player gradually
        // transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, TurningSpeed * Time.fixedDeltaTime);
        transform.rotation = _targetRotation;
    }

    private int PointSideOfLine(Vector2 A, Vector2 B, Vector2 P) {
        float determinant = (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);

        if (determinant > 0) return 1; // P is on the left side of the line
        if (determinant < 0) return -1; // P is on the right side of the line

        return 0; // P is on the line
    }

    public Vector2 PointToLineVector(Vector2 A, Vector2 B, Vector2 P) {
        // Line's direction vector
        Vector2 lineDir = B - A;
        lineDir.Normalize();

        // Vector from A to P
        Vector2 AP = P - A;

        // Project AP onto the line's direction vector to find the magnitude of the projection
        float magnitude = Vector2.Dot(AP, lineDir);

        // Get the point Q on the line and construct the vector PQ
        Vector2 Q = A + lineDir * magnitude;
        Vector2 PQ = Q - P;

        // Return the PQ vector, which is orthogonal to AB and points from P to the line
        return PQ;
    }
}
