using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameLifecycleManager : Singleton<GameLifecycleManager> {
    public enum GameState {
        MainMenu,
        Controls,
        GameIntroSequence,
        GameStarted,
        GamePaused,
        GameOver,
        GameOverLose,
    }

    [NonNullField] public GameObject GameplayContainer;
    [NonNullField] public GameObject MainMenuContainer;
    [NonNullField] public Animator SequenceAnimator;
    [NonNullField] public MeshRenderer MenuFaderRenderer;
    [NonNullField] public MeshRenderer GameFaderRenderer;
    [NonNullField] public GameObject BlackHoleObject;
    private Material _menuFaderMaterialInstance;
    private Material _gameFaderMaterialInstance;

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
            if (_currentGameState == GameState.GameStarted) {
                SoundController.Instance.PlayMenuMusic(false);
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
                BlackHoleObject.SetActive(false);
                // Hide the boss
                BossController.Instance.gameObject.SetActive(false);
            }
        }

        _menuFaderMaterialInstance = MenuFaderRenderer.material;
        _gameFaderMaterialInstance = GameFaderRenderer.material;

        SwitchGameState(_currentGameState);
    }

    private void Update() {
        if (_currentGameState != GameState.MainMenu) {
            return;
        }

        // The "Press Any Key to Start" in the main menu.
        if (Keyboard.current.anyKey.wasReleasedThisFrame) {
            ViewControls();
        }
    }

    public void OnIntroSequenceEnd() {
        // Show the boss.
        BossController.Instance.gameObject.SetActive(true);
        SwitchGameState(GameState.GameStarted);
        // Start playing music
        SoundController.Instance.PlayGameMusic1(true);
    }

    private void SwitchGameState(GameState gameState) {
        switch (gameState) {
            case GameState.MainMenu:
                _menuFaderMaterialInstance.SetFloat("_AnimationTime", 0);
                _gameFaderMaterialInstance.SetFloat("_AnimationTime", 1);

                UIRouter.Instance.SwitchRoutes(UIRouter.Route.MainMenu);
                PlayerManager.Instance.SwitchActionMaps("none");
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
                SoundController.Instance.PlayMenuMusic(true);
                break;
            case GameState.Controls:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.Controls);
                SoundController.Instance.PlayMenuMusic(false);
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
                BlackHoleObject.SetActive(false);
                // Hide the boss
                BossController.Instance.gameObject.SetActive(false);
                break;
            case GameState.GameIntroSequence:
                SoundController.Instance.PlayWarningSound(true);
                StartCoroutine(StopWarningSoundAfter(2.0f));
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.None);
                ToggleCursor(false);
                // Play the animation.
                SequenceAnimator.SetTrigger("PlayIntro");
                break;
            case GameState.GameStarted:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.Hud);
                // The actual music playing will not be controlled here because we don't want to unpause into a different track.
                // Unpause the game
                Time.timeScale = 1;
                PlayerManager.Instance.SwitchActionMaps("gameplay");
                PlayerManager.Instance.CameraController.SetEnabled(true);
                SoundController.Instance.PauseAllTracks(false);
                ToggleCursor(false);
                break;
            case GameState.GamePaused:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.PauseMenu);
                SoundController.Instance.PauseAllTracks(true);
                // Pause the Game
                Time.timeScale = 0;
                PlayerManager.Instance.SwitchActionMaps("menu");
                ToggleCursor(true);
                break;
            case GameState.GameOver:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.GameOver);
                SoundController.Instance.StopAllTracks();
                SoundController.Instance.PlayMenuMusic(true);
                PlayerManager.Instance.SwitchActionMaps("none");
                ToggleCursor(true);
                break;
            case GameState.GameOverLose:
                UIRouter.Instance.SwitchRoutes(UIRouter.Route.GameOverLose);
                SoundController.Instance.StopAllTracks();
                PlayerManager.Instance.SwitchActionMaps("none");
                ToggleCursor(true);
                break;
        }

        _currentGameState = gameState;
        OnGameStateUpdated?.Invoke(this, _currentGameState);
    }

    private IEnumerator StopWarningSoundAfter(float delaySec) {
        yield return new WaitForSeconds(delaySec);
        SoundController.Instance.PlayWarningSound(false);
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

    public void ViewControls() {
        _menuFaderMaterialInstance.DOFloat(1, "_AnimationTime", 1.0f).OnComplete(() => {
            SwitchGameState(GameState.Controls);
        });
    }

    public void StartGame() {
        UIRouter.Instance.SwitchRoutes(UIRouter.Route.None);
        _gameFaderMaterialInstance.DOFloat(0, "_AnimationTime", 1.0f).OnComplete(() => {
            SwitchGameState(GameState.GameIntroSequence);
            BlackHoleObject.SetActive(true);
        });
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
        Debug.Log("Pause Game?!");
        if (_currentGameState == GameState.GameStarted) {
            Debug.Log("Pause Game?!???");
            SwitchGameState(GameState.GamePaused);
        }
    }

    public void UnpauseGame() {
        SwitchGameState(GameState.GameStarted);
    }

    public void ReturnToMainMenu() {
        SwitchGameState(GameState.MainMenu);
    }

    public void QuitGame() {
        Application.Quit();
    }
}
