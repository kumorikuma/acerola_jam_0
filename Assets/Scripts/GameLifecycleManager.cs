using System;
using UnityEngine;

public class GameLifecycleManager : Singleton<GameLifecycleManager> {
    public enum GameState {
        MainMenu,
        GameStarted,
        GamePaused,
        GameOver,
    }

    public bool Debug_IsDebugModeEnabled = false;
    public GameState Debug_StartingGameState = GameState.GameStarted;

    public event EventHandler<GameState> OnGameStateUpdated;
    private GameState _currentGameState = GameState.MainMenu;

    public GameState CurrentGameState {
        get { return _currentGameState; }
    }

    public bool IsGamePlaying() {
        return _currentGameState == GameState.GameStarted;
    }

    void Start() {
        // Disable Debug Mode in production
#if !UNITY_EDITOR
        Debug_IsDebugModeEnabled = false;
#endif

        if (Debug_IsDebugModeEnabled) {
            _currentGameState = Debug_StartingGameState;
        }

        SwitchGameState(_currentGameState);
    }

    private void SwitchGameState(GameState gameState) {
        switch (gameState) {
            case GameState.MainMenu:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.MainMenu);
                break;
            case GameState.GameStarted:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.Hud);
                // Unpause the game
                Time.timeScale = 1;
                PlayerManager.Instance.SwitchActionMaps("gameplay");
                ToggleCursor(false);
                // Reset game
                PlayerManager.Instance.PlayerController.Reset();
                BossController.Instance.Reset();
                break;
            case GameState.GamePaused:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.PauseMenu);
                // Pause the Game
                Time.timeScale = 0;
                PlayerManager.Instance.SwitchActionMaps("menu");
                ToggleCursor(true);
                break;
            case GameState.GameOver:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.GameOver);
                PlayerManager.Instance.SwitchActionMaps("menu");
                ToggleCursor(true);
                break;
        }

        _currentGameState = gameState;
        OnGameStateUpdated?.Invoke(this, _currentGameState);
    }

    private void ToggleCursor(bool enableCursor) {
        if (enableCursor) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void StartGame() {
        SwitchGameState(GameState.GameStarted);
    }

    public void EndGame() {
        SwitchGameState(GameState.GameOver);
    }

    public void PauseGame() {
        SwitchGameState(GameState.GamePaused);
    }

    public void UnpauseGame() {
        SwitchGameState(GameState.GameStarted);
    }

    public void ReturnToMainMenu() {
        SwitchGameState(GameState.MainMenu);
    }
}
