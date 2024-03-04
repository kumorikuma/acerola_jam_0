using System;
using UnityEngine;

public class BossController : Singleton<BossController> {
    [NonSerialized] public EntityStats Stats;
    public int MaxBossLives = 3;
    public int CurrentBossLives = 3;
    public event EventHandler<int> OnBossLivesChanged;

    protected override void Awake() {
        base.Awake();
        Stats = GetComponent<EntityStats>();
    }

    private void Start() {
        Stats.NotifyHealthChanged();
        Stats.OnHealthChanged += OnHealthChanged;
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
}
