using System;
using System.Collections;
using UnityEngine;

public class BossController : Singleton<BossController> {
    [NonSerialized] public EntityStats Stats;
    public int MaxBossLives = 3;
    public int CurrentBossLives = 3;
    public event EventHandler<int> OnBossLivesChanged;

    // public List<BulletSpawner> BulletSpawners;
    public BulletSpawner spawner;
    private Transform _playerTarget = null;

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

        HandleLocomotion();
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
        // Aim towards player
        Vector3 bossToTargetVector = _playerTarget.position - transform.position;
        bossToTargetVector.y = 0;
        bossToTargetVector = bossToTargetVector.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(bossToTargetVector, Vector3.up);
        transform.rotation = targetRotation;
    }
}
