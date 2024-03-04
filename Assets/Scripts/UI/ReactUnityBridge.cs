using ReactUnity;
using ReactUnity.Reactive;
using UnityEngine;

public class ReactUnityBridge : Singleton<ReactUnityBridge> {
    private ReactiveValue<string> route = new();
    private ReactiveValue<bool> debugModeEnabled = new();
    private ReactiveValue<string> debugGameState = new();
    private ReactiveValue<Leaderboards.LeaderboardScores> leaderboardScores = new();
    private ReactiveValue<Vector2Int> screenSpaceAimPosition = new();

    // Stats. All values [0, 1].
    private ReactiveValue<float> targetHealth = new();
    private ReactiveValue<float> playerHealth = new();
    private ReactiveValue<float> playerPrimaryFireCooldown = new();
    private ReactiveValue<float> playerSecondaryFireCooldown = new();

    private ReactRendererBase reactRenderer;

    protected override void Awake() {
        base.Awake();

        reactRenderer = GetComponentInChildren<ReactUnity.UGUI.ReactRendererUGUI>();

        // Routing
        reactRenderer.Globals["route"] = route;

        reactRenderer.Globals["leaderboardScores"] = leaderboardScores;

        // Debug values
        reactRenderer.Globals["debugGameState"] = debugGameState;
        reactRenderer.Globals["debugModeEnabled"] = debugModeEnabled;

        // Hud
        reactRenderer.Globals["screenSpaceAimPosition"] = screenSpaceAimPosition;
        reactRenderer.Globals["targetHealth"] = targetHealth;
        reactRenderer.Globals["playerHealth"] = playerHealth;
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

    private void OnTargetHealthUpdated(float value) {
    }

    private void OnPlayerHealthUpdated(float value) {
    }

    public void UpdateScreenSpaceAimPosition(Vector2Int aimPosition) {
        if (screenSpaceAimPosition.Value != aimPosition) {
            screenSpaceAimPosition.Value = aimPosition;
        }
    }

    public void UpdatePrimaryFireCooldown(float value) {
        if (playerPrimaryFireCooldown.Value != value) {
            playerPrimaryFireCooldown.Value = value;
        }
    }

    public void UpdateSecondaryFireCooldown(float value) {
        if (playerSecondaryFireCooldown.Value != value) {
            playerSecondaryFireCooldown.Value = value;
        }
    }
}
