using System;
using System.Collections;
using UnityEngine;

public class BossController : Singleton<BossController> {
    [NonSerialized] public EntityStats Stats;

    public bool IsLocomotionEnabled = true;
    public float TurningSpeed = 1.0f;
    public float MovementSpeed = 5.0f;
    public float OptimalDistanceToPlayer = 30.0f;

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

    private void FixedUpdate() {
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
        Vector3 bossToCenterVector = ArenaCenter - transform.position;
        float distanceToPlayer = bossToTargetVector.magnitude;

        // Aim towards player
        Vector3 lookVector = bossToTargetVector;
        lookVector.y = 0;
        lookVector = lookVector.normalized;
        _targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);

        // ===== Compute Target Position =====
        // Case 1: Boss is far from the player
        ReactUnityBridge.Instance.UpdateDebugString("IsGreaterThanOptimalDistance",
            (distanceToPlayer > OptimalDistanceToPlayer).ToString());
        VectorDebug.Instance.DrawDebugVector("bossToCenterVector", bossToCenterVector, transform.position,
            Color.magenta);
        if (distanceToPlayer > OptimalDistanceToPlayer) {
            // Two more cases depending on if the player is on the boss' side of the center or not.
            Vector2 bossToCenterOrthogonalVector = new Vector2(bossToCenterVector.z, -bossToCenterVector.x);
            int sideOfCenter = PointSideOfLine(Vector2.zero, bossToCenterOrthogonalVector,
                new Vector2(_playerTarget.position.x, _playerTarget.position.z));
            ReactUnityBridge.Instance.UpdateDebugString("sideOfCenter",
                sideOfCenter.ToString());
            VectorDebug.Instance.DrawDebugVector("BossToCenterOrthogonalVector",
                new Vector3(bossToCenterOrthogonalVector.x, 0, bossToCenterOrthogonalVector.y), ArenaCenter,
                Color.magenta);
            if (sideOfCenter > 0) {
                // Case A: Player is on the other side of the arena.
                // We'll just move towards the center.
                _targetPosition = ArenaCenter;
            } else {
            }
        }

        // ===== Apply Movement =====
        //  TODO: If distance is too far, then dash?
        // Move towards target position
        Vector3 movementVector = _targetPosition - transform.position;
        Vector3 absoluteMovementVector = movementVector.normalized * MovementSpeed;
        transform.position += absoluteMovementVector;
        VectorDebug.Instance.DrawDebugVector("absoluteMovementVector",
            absoluteMovementVector, transform.position,
            Color.red);

        // ===== Apply Rotation =====
        // Rotate towards player gradually
        transform.rotation =
            Quaternion.RotateTowards(transform.rotation, _targetRotation, TurningSpeed * Time.fixedDeltaTime);
    }

    private int PointSideOfLine(Vector2 A, Vector2 B, Vector2 P) {
        float determinant = (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);

        if (determinant > 0) return 1; // P is on the left side of the line
        if (determinant < 0) return -1; // P is on the right side of the line

        return 0; // P is on the line
    }
}
