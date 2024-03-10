using System;
using System.Collections;
using UnityEngine;

public class BossController : Singleton<BossController> {
    [NonSerialized] public EntityStats Stats;

    public bool IsLocomotionEnabled = true;
    public float TurningSpeed = 1.0f;
    public float MovementSpeed = 5.0f;
    public float OptimalDistanceToPlayer = 30.0f;

    public float OptimalDistanceTolerance = 1.0f;
    public float CriticalThreshold = 10.0f; // If the distance to player is less than this, rapidly exit

    public int MaxBossLives = 3;
    public int CurrentBossLives = 3;
    public event EventHandler<int> OnBossLivesChanged;

    // public List<BulletSpawner> BulletSpawners;
    public BulletSpawner spawner;
    private Transform _playerTarget = null;
    private Quaternion _targetRotation = Quaternion.identity;
    private Vector3 _targetPosition = Vector3.zero;
    private Vector3 ArenaCenter = Vector3.zero;

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
    }

    private void Start() {
        Stats.NotifyHealthChanged();
        Stats.OnHealthChanged += OnHealthChanged;
        _playerTarget = PlayerManager.Instance.PlayerController.EnemyAimTargetLocation;
        Reset();
    }

    public void Reset() {
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
        OnBossLivesChanged?.Invoke(this, CurrentBossLives);
    }

    private void ConsumeBossLife() {
        SetBossLives(CurrentBossLives - 1);
        if (CurrentBossLives == 0) {
            GameLifecycleManager.Instance.EndGame();
        } else {
            // Heal the boss back up to 100%
            Stats.SetHealthToPercentage(1.0f);
        }
    }

    private void HandleLocomotion() {
        Vector3 bossToTargetVector = _playerTarget.position - transform.position;
        bossToTargetVector.y = 0;
        Vector3 bossToCenterVector = ArenaCenter - transform.position;
        Vector2 playerTargetPosXZ = new Vector2(_playerTarget.position.x, _playerTarget.position.z);
        float distanceToPlayer = bossToTargetVector.magnitude;

        // Whether the player is on the closer or far side of the arena
        Vector2 bossToCenterOrthogonalVector = new Vector2(bossToCenterVector.z, -bossToCenterVector.x);
        int closeFarSideOfCenter = PointSideOfLine(Vector2.zero, bossToCenterOrthogonalVector,
            new Vector2(_playerTarget.position.x, _playerTarget.position.z));
        ReactUnityBridge.Instance.UpdateDebugString("closeFarSideOfCenter",
            closeFarSideOfCenter.ToString());
        VectorDebug.Instance.DrawDebugVector("BossToCenterOrthogonalVector",
            new Vector3(bossToCenterOrthogonalVector.x, 0, bossToCenterOrthogonalVector.y), ArenaCenter,
            Color.magenta);

        // Aim towards player
        Vector3 lookVector = bossToTargetVector;
        lookVector = lookVector.normalized;
        _targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);

        // ===== Compute Target Position =====

        ReactUnityBridge.Instance.UpdateDebugString("IsGreaterThanOptimalDistance",
            (distanceToPlayer > OptimalDistanceToPlayer).ToString());
        VectorDebug.Instance.DrawDebugVector("bossToCenterVector", bossToCenterVector, transform.position,
            Color.magenta);
        int behaviorTreeCase = -1;
        // if (distanceToPlayer < CriticalThreshold) {
        // Case 0: Boss is critically close to player. Get out
        // }
        if (distanceToPlayer > OptimalDistanceToPlayer) {
            // Case 1: Boss is far from the player
            // Two more cases depending on if the player is on the boss' side of the center or not.
            if (closeFarSideOfCenter > 0) {
                // Case 1.A: Player is on the other side of the arena.
                // We'll just move towards the center.
                _targetPosition = ArenaCenter;
                behaviorTreeCase = 0;
            } else {
                // Compute the shortest vector from the Player to bossToCenterVector.
                Vector2 playerToBossToCenterLineVector = PointToLineVector(Vector2.zero,
                    new Vector2(transform.position.x, transform.position.z),
                    playerTargetPosXZ);
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
        } else if (distanceToPlayer > OptimalDistanceTolerance) {
            // Case 2: Two more cases depending on if the player is on the boss' side of the center or not.
            // We will need to back up to get away from the player, but also be mindful of trying to go to the center.
            // The theory here is that there are two possible vectors we can take.
            // If we want to go back to the optimum distance ASAP, then we'd want to take the retreat vector directly away from player.
            // If we were farther away (almost at the optimal distance), then we would want to go left or right to possibly open up a path to the center.

            Vector3 retreatVector = -bossToTargetVector.normalized;
            VectorDebug.Instance.DrawDebugVector("retreatVector",
                retreatVector, transform.position,
                Color.green);

            if (closeFarSideOfCenter > 0) {
                // Case 2.A: Player is on the other side of the arena.
                // We'll just move backwards away from the player. The center is between us and the player.
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
                float t = Mathf.Clamp(distanceToPlayer / OptimalDistanceToPlayer, 0, 1);
                ReactUnityBridge.Instance.UpdateDebugString("Distance Ratio",
                    t.ToString());
                Vector3 targetVector = Vector3.Slerp(retreatVector, tangentialVector, t);
                behaviorTreeCase = 4;

                _targetPosition = transform.position + targetVector;
                VectorDebug.Instance.DrawDebugVector("TargetVector",
                    _targetPosition, transform.position,
                    Color.red);
            }
        }

        // ===== Apply Movement =====
        //  TODO: If distance is too far, then dash?
        // Move towards target position
        Vector3 movementVector = _targetPosition - transform.position;
        Vector3 absoluteMovementVector = movementVector.normalized * (MovementSpeed * Time.deltaTime);
        transform.position += absoluteMovementVector;
        if ((_targetPosition - transform.position).magnitude < 0.1f) {
            _targetPosition = absoluteMovementVector;
        }

        VectorDebug.Instance.DrawDebugVector("absoluteMovementVector",
            absoluteMovementVector, transform.position,
            Color.red);

        ReactUnityBridge.Instance.UpdateDebugString("Behavior Tree Case",
            behaviorTreeCase.ToString());

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
