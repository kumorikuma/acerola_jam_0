using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameLifecycleManager : Singleton<GameLifecycleManager> {
    public enum GameState {
        MainMenu,
        GameIntroSequence,
        GameStarted,
        GamePaused,
        GameOver,
        GameOverLose,
    }

    [NonNullField] public GameObject GameplayContainer;
    [NonNullField] public GameObject MainMenuContainer;
    [NonNullField] public Animator SequenceAnimator;

    public bool Debug_IsDebugModeEnabled = false;
    public GameState Debug_StartingGameState = GameState.GameStarted;
    public float HealingPhaseLength = 3.0f;

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

    private void Update() {
        if (_currentGameState != GameState.MainMenu) {
            return;
        }

        // The "Press Any Key to Start" in the main menu.
        if (Keyboard.current.anyKey.wasReleasedThisFrame) {
            StartGame();
        }
    }

    public void OnIntroSequenceEnd() {
        // Show the boss.
        BossController.Instance.gameObject.SetActive(true);
        SwitchGameState(GameState.GameStarted);
    }

    private void SwitchGameState(GameState gameState) {
        switch (gameState) {
            case GameState.MainMenu:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.MainMenu);
                PlayerManager.Instance.SwitchActionMaps("menu");
                GameplayContainer.SetActive(false);
                MainMenuContainer.SetActive(true);
                Time.timeScale = 1;
                // Despawn any projectiles and reset the terrain.
                if (PanelsController.Instance) {
                    PanelsController.Instance.Reset();
                }

                if (ProjectileController.Instance) {
                    ProjectileController.Instance.Reset();
                }

                PlayerManager.Instance.PlayerController.SetProcessedEnabled(false);

                break;
            case GameState.GameIntroSequence:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.None);
                ToggleCursor(false);
                // Hide the main menu and show the rest of the game
                GameplayContainer.SetActive(true);
                MainMenuContainer.SetActive(false);
                // Reset the game
                ReactUnityBridge.Instance.InitializeGameStuff(); // UI Comes first
                PlayerManager.Instance.PlayerController.Reset();
                PlayerManager.Instance.CameraController.Reset();
                PlayerManager.Instance.CameraController.SetEnabled(false);
                BossController.Instance.Reset();
                PanelsController.Instance.Reset();
                ProjectileController.Instance.Reset();
                // Hide the boss
                BossController.Instance.gameObject.SetActive(false);
                // Play the animation.
                SequenceAnimator.SetTrigger("PlayIntro");
                break;
            case GameState.GameStarted:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.Hud);
                // GameplayContainer.SetActive(true);
                // MainMenuContainer.SetActive(false);
                // Unpause the game
                Time.timeScale = 1;
                PlayerManager.Instance.SwitchActionMaps("gameplay");
                PlayerManager.Instance.CameraController.SetEnabled(true);
                ToggleCursor(false);
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
            case GameState.GameOverLose:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.GameOverLose);
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
        SwitchGameState(GameState.GameIntroSequence);
    }

    public void WinGame() {
        BossController.Instance.StopDoingStuff();
        SequenceAnimator.SetTrigger("PlayWinSequence");
    }

    public void LoseGame() {
        BossController.Instance.StopDoingStuff();
        SwitchGameState(GameState.GameOverLose);
    }

    public void GameOver() {
        SwitchGameState(GameState.GameOver);
    }

    public void PauseGame() {
        if (_currentGameState == GameState.GameStarted) {
            SwitchGameState(GameState.GamePaused);
        }
    }

    public void UnpauseGame() {
        SwitchGameState(GameState.GameStarted);
    }

    public void ReturnToMainMenu() {
        SwitchGameState(GameState.MainMenu);
    }
}
