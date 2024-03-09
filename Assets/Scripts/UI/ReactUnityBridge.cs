using System;
using System.Collections.Generic;
using ReactUnity;
using ReactUnity.Reactive;
using UnityEngine;

public class ReactUnityBridge : Singleton<ReactUnityBridge> {
    private const float FP_EPSILON = 0.01f;

    private ReactiveValue<string> route = new();
    private ReactiveValue<bool> debugModeEnabled = new();
    private ReactiveValue<string> debugGameState = new();
    private ReactiveValue<Leaderboards.LeaderboardScores> leaderboardScores = new();
    private ReactiveValue<Vector2Int> screenSpaceAimPosition = new();

    // Stats. All values [0, 1].
    private ReactiveValue<float> targetHealth = new();
    private ReactiveValue<float> playerHealth = new();
    private ReactiveValue<int> playerLives = new();
    private ReactiveValue<float> bossHealth = new();
    private ReactiveValue<int> bossLives = new();
    private ReactiveValue<float> playerPrimaryFireCooldown = new();
    private ReactiveValue<float> playerSecondaryFireCooldown = new();

    private ReactRendererBase reactRenderer;
    private ReactiveValue<List<string>> debugStrings = new();
    private Dictionary<string, string> _debugStrings = new();

    protected override void Awake() {
        base.Awake();

        reactRenderer = GetComponentInChildren<ReactUnity.UGUI.ReactRendererUGUI>();

        // Routing
        reactRenderer.Globals["route"] = route;

        reactRenderer.Globals["leaderboardScores"] = leaderboardScores;

        // Debug values
        reactRenderer.Globals["debugGameState"] = debugGameState;
        reactRenderer.Globals["debugModeEnabled"] = debugModeEnabled;
        reactRenderer.Globals["debugStrings"] = debugStrings;
        debugStrings.Value = new List<string>();

        // Hud
        reactRenderer.Globals["screenSpaceAimPosition"] = screenSpaceAimPosition;
        reactRenderer.Globals["targetHealth"] = targetHealth;
        reactRenderer.Globals["playerHealth"] = playerHealth;
        reactRenderer.Globals["playerLives"] = playerLives;
        reactRenderer.Globals["maxPlayerLives"] = PlayerManager.Instance.PlayerController.MaxPlayerLives;
        reactRenderer.Globals["bossHealth"] = bossHealth;
        reactRenderer.Globals["bossLives"] = bossLives;
        reactRenderer.Globals["maxBossLives"] = BossController.Instance.MaxBossLives;
        reactRenderer.Globals["playerPrimaryFireCooldown"] = playerPrimaryFireCooldown;
        reactRenderer.Globals["playerSecondaryFireCooldown"] = playerSecondaryFireCooldown;

        // Enable Debug Mode when in Unity Editor
        debugModeEnabled.Value = false;
#if UNITY_EDITOR
        debugModeEnabled.Value = true;
#endif

        // Singletons become available after Awake. ScriptExecutionOrder should make sure this is executed last.
        UIRouter.Instance.OnRouteUpdate += OnRouteUpdate;
        GameLifecycleManager.Instance.OnGameStateUpdated += OnGameStateUpdated;
        if (Leaderboards.Instance != null) {
            Leaderboards.Instance.OnLeaderboardScoresUpdated += LeaderboardsOnOnLeaderboardScoresUpdated;
            // To enable leaderboards, need to connect to a Unity Project and add the Leaderboards singleton to the game.
        }

        PlayerManager.Instance.PlayerController.Stats.OnHealthChanged += OnPlayerHealthUpdated;
        PlayerManager.Instance.PlayerController.OnPlayerLivesChanged += OnPlayerLivesUpdated;
        BossController.Instance.Stats.OnHealthChanged += OnBossHealthUpdated;
        BossController.Instance.OnBossLivesChanged += OnBossLivesUpdated;

        // Game System References   
        reactRenderer.Globals["gameLifecycleManager"] = GameLifecycleManager.Instance;
    }

    private void LeaderboardsOnOnLeaderboardScoresUpdated(object sender, Leaderboards.LeaderboardScores data) {
        leaderboardScores.Value = data;
    }

    private void OnRouteUpdate(object sender, string data) {
        route.Value = data;
    }

    private void OnGameStateUpdated(object sender, GameLifecycleManager.GameState data) {
        debugGameState.Value = data.ToString();
    }

    private void OnPlayerHealthUpdated(object sender, float value) {
        if (Math.Abs(playerHealth.Value - value) > FP_EPSILON) {
            playerHealth.Value = value;
        }
    }

    private void OnPlayerLivesUpdated(object sender, int value) {
        if (playerLives.Value != value) {
            playerLives.Value = value;
        }
    }

    private void OnBossHealthUpdated(object sender, float value) {
        if (Math.Abs(bossHealth.Value - value) > FP_EPSILON) {
            bossHealth.Value = value;
        }
    }

    private void OnBossLivesUpdated(object sender, int value) {
        if (bossLives.Value != value) {
            bossLives.Value = value;
        }
    }

    public void UpdateTargetHealth(float value) {
        if (Math.Abs(targetHealth.Value - value) > FP_EPSILON) {
            targetHealth.Value = value;
        }
    }

    public void UpdateScreenSpaceAimPosition(Vector2Int aimPosition) {
        if (screenSpaceAimPosition.Value != aimPosition) {
            screenSpaceAimPosition.Value = aimPosition;
        }
    }

    public void UpdatePrimaryFireCooldown(float value) {
        if (Math.Abs(playerPrimaryFireCooldown.Value - value) > FP_EPSILON) {
            playerPrimaryFireCooldown.Value = value;
        }
    }

    public void UpdateSecondaryFireCooldown(float value) {
        if (Math.Abs(playerSecondaryFireCooldown.Value - value) > FP_EPSILON) {
            playerSecondaryFireCooldown.Value = value;
        }
    }

    public void UpdateDebugString(string name, string value) {
        _debugStrings[name] = value;
        OnDebugStringsUpdated();
    }

    private void OnDebugStringsUpdated() {
        List<string> debugStringsList = new();
        foreach (KeyValuePair<string, string> kv in _debugStrings) {
            debugStringsList.Add($"{kv.Key}: {kv.Value}");
        }

        debugStrings.Value = debugStringsList;
    }
}
