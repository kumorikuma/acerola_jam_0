using ReactUnity;
using ReactUnity.Reactive;
using UnityEngine;

public class ReactUnityBridge : Singleton<ReactUnityBridge> {
    public ReactiveValue<string> route = new();
    public ReactiveValue<bool> debugModeEnabled = new();
    public ReactiveValue<string> debugGameState = new();
    public ReactiveValue<Leaderboards.LeaderboardScores> leaderboardScores = new();
    public ReactiveValue<Vector2Int> screenSpaceAimPosition = new();

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

    public void UpdateScreenSpaceAimPosition(Vector2Int aimPosition) {
        if (screenSpaceAimPosition.Value != aimPosition) {
            screenSpaceAimPosition.Value = aimPosition;
        }
    }
}
