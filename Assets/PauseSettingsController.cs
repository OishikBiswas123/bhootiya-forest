using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseSettingsController : MonoBehaviour
{
    public static PauseSettingsController Instance;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
    }
    
    [Header("UI References")]
    public GameObject pauseOverlay;   // Optional: dim/paused panel
    public GameObject settingsPanel;  // Settings UI panel
    public GameObject pauseMenuPanel; // Panel with Resume/Restart/Settings buttons
    public UnityEngine.UI.Image pauseButtonIcon;
    public Sprite pauseIconSprite; // double-lines
    public Sprite playIconSprite;  // triangle
    public GameObject pauseIconObject; // optional separate icon object
    public GameObject playIconObject;  // optional separate icon object
    public GameObject pauseButton;
    public GameObject settingsButton;
    public GameObject gameInfoButton; // New: Game Info button
    public GameObject runButton;
    public GameObject xButtonMobile; // X button to show during Game Info
    public GameObject dpadButton; // D-Pad button
    [Header("Mobile Controls (Optional)")]
    public GameObject mobileControls;

    [Header("Pause Behavior")]
    public bool pauseOnSettingsOpen = true;
    public bool pauseWithTimeScale = true;
    public float pausedTimeScale = 0f;
    public bool muteAudioOnPause = true;
    public bool showPausedPromptInCity = true;
    public string pausedPromptText = "Game Paused";

    private bool isPaused = false;
    private bool settingsOpenedFromPauseMenu = false;
    private bool wasPausedBeforeSettings = false;
    private bool forceBlur = false;

    void Start()
    {
        UpdatePauseIcon();
        SetMainButtonsVisible(true);
        EnsureOverlayDoesNotBlockClicks();
        
        // Hide Game Info button initially - only shows when paused
        if (gameInfoButton != null)
            gameInfoButton.SetActive(false);
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    public void SetForceBlur(bool value)
    {
        forceBlur = value;
        UpdateOverlay();
    }

    void Update()
    {
        UpdateMainButtonsVisibilityByUIState();
        // Keep blur overlay consistent even if panels are toggled elsewhere (reset/default/customize).
        UpdateOverlay();
    }

    public void TogglePause()
    {
        bool gameStarted = GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameStarted();

        if (gameStarted)
        {
            OpenPauseMenu();
            return;
        }

        SetPaused(!isPaused);
    }

public void Pause()
    {
        SetPaused(true);
    }
    
    public void Resume()
    {
        SetPaused(false);
    }

    // Opened from the main (city) settings button. Do NOT pause.
    public void OpenSettingsFromMain()
    {
        OpenSettingsInternal(pauseGame: false);
    }

    // Opened from pause menu. Keep game paused.
    public void OpenSettingsFromPauseMenu()
    {
        OpenSettingsInternal(pauseGame: true);
    }

    // Backward-compatible (treat as main).
    public void OpenSettings()
    {
        OpenSettingsFromMain();
    }

    void OpenSettingsInternal(bool pauseGame)
    {
        settingsOpenedFromPauseMenu = pauseGame;
        wasPausedBeforeSettings = isPaused;

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (pauseGame && pauseOnSettingsOpen)
            SetPaused(true);
        else
        {
            // Only freeze player movement when settings opened from main (no pause).
            if (PlayerMove.Instance != null)
                PlayerMove.Instance.SetExternalFreeze(true);
        }

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Hide main buttons while settings is open.
        SetMainButtonsVisible(false);
        
        // Hide Game Info button when settings panel is open
        if (gameInfoButton != null)
            gameInfoButton.SetActive(false);
        
        // Hide mobile buttons (D-Pad, Run, X) when settings is open
        HideMobileButtonsOnly();
        
        HidePausedPrompt();
        UpdateOverlay();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (settingsOpenedFromPauseMenu)
        {
            // Return to pause menu, keep paused.
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
            SetMainButtonsVisible(false);
            // Restore Game Info button when going back to pause menu
            if (gameInfoButton != null)
                gameInfoButton.SetActive(true);
            if (!isPaused)
                SetPaused(true);
        }
        else
        {
            // From main settings: resume normal UI (no pause).
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            SetMainButtonsVisible(true);
            // Restore pause state that existed before opening settings.
            if (pauseOnSettingsOpen)
            {
                if (wasPausedBeforeSettings && !isPaused)
                    SetPaused(true);
                else if (!wasPausedBeforeSettings && isPaused)
                    SetPaused(false);
            }

            if (PlayerMove.Instance != null)
                PlayerMove.Instance.SetExternalFreeze(false);
        }

        UpdateOverlay();
    }

public void OpenPauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        
        SetPaused(true);
        SetMainButtonsVisible(false);
        
        // Hide mobile buttons when pause menu is open
        HideMobileButtonsOnly();
    }
    
    public void ClosePauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        SetMainButtonsVisible(true);
    }

    public void ResumeFromPauseMenu()
    {
        // Close Game Info panel if open
        if (UIManager.Instance != null)
            UIManager.Instance.CloseGameInfo();
        
        ClosePauseMenu();
        SetPaused(false);
        
        // Show X button when game resumes
        if (xButtonMobile != null) xButtonMobile.SetActive(true);
    }

    public void RestartFromPauseMenu()
    {
        // Restart the forest run from landing (no takeoff).
        SetPaused(false);
        ClosePauseMenu();
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.RestartForestFromPause();
    }

    void SetPaused(bool paused)
    {
        isPaused = paused;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(paused || (settingsPanel != null && settingsPanel.activeSelf));

        if (pauseWithTimeScale)
            Time.timeScale = paused ? pausedTimeScale : 1f;

        if (muteAudioOnPause)
            AudioListener.pause = paused;
        
        // Show/hide Game Info button based on pause state
        if (gameInfoButton != null)
            gameInfoButton.SetActive(paused);

        UpdatePauseIcon();
        UpdatePausedPrompt();
        EnsureOverlayDoesNotBlockClicks();

        // When paused in city (no pause menu), hide mobile controls.
        if (mobileControls != null || dpadButton != null || runButton != null)
        {
            bool settingsOpen = settingsPanel != null && settingsPanel.activeSelf;
            bool pauseMenuOpen = pauseMenuPanel != null && pauseMenuPanel.activeSelf;
            if (paused && !pauseMenuOpen && !settingsOpen)
            {
                HideMobileButtonsOnly();
            }
            else if (!paused && !pauseMenuOpen && !settingsOpen)
            {
                ShowMobileButtonsOnly();
            }
        }
    }

    void OnDisable()
    {
        // Safety: never leave the game paused if this component is disabled.
        if (pauseWithTimeScale)
            Time.timeScale = 1f;

        if (muteAudioOnPause)
            AudioListener.pause = false;
    }

    void UpdatePauseIcon()
    {
        if (pauseIconObject != null || playIconObject != null)
        {
            if (pauseIconObject != null) pauseIconObject.SetActive(!isPaused);
            if (playIconObject != null) playIconObject.SetActive(isPaused);
            return;
        }

        if (pauseButtonIcon == null) return;
        if (isPaused)
        {
            if (playIconSprite != null) pauseButtonIcon.sprite = playIconSprite;
        }
        else
        {
            if (pauseIconSprite != null) pauseButtonIcon.sprite = pauseIconSprite;
        }
    }

    void SetMainButtonsVisible(bool visible)
    {
        if (pauseButton != null) pauseButton.SetActive(visible);
        if (settingsButton != null) settingsButton.SetActive(visible);
        if (runButton != null) runButton.SetActive(visible);
        if (dpadButton != null) dpadButton.SetActive(visible);
        // Note: gameInfoButton is controlled separately - only shows when paused
    }
    
    public void HideMobileButtonsOnly()
    {
        if (dpadButton != null) dpadButton.SetActive(false);
        if (runButton != null) runButton.SetActive(false);
        if (xButtonMobile != null) xButtonMobile.SetActive(false);
    }
    
    public void ShowMobileButtonsOnly()
    {
        if (dpadButton != null) dpadButton.SetActive(true);
        if (runButton != null) runButton.SetActive(true);
        if (xButtonMobile != null) xButtonMobile.SetActive(true);
    }
    
    public void ShowXButtonOnly()
    {
        if (xButtonMobile != null) xButtonMobile.SetActive(true);
    }
    
    public void HideXButtonOnly()
    {
        if (xButtonMobile != null) xButtonMobile.SetActive(false);
    }
    
    public void HideMainButtonsForGameInfo()
    {
        if (pauseButton != null) pauseButton.SetActive(false);
        if (settingsButton != null) settingsButton.SetActive(false);
        if (gameInfoButton != null) gameInfoButton.SetActive(false);
        if (runButton != null) runButton.SetActive(false);
        if (dpadButton != null) dpadButton.SetActive(false);
    }
    
    public void RestoreButtonsAfterGameInfo()
    {
        // Restore main buttons - game is still paused so they should be visible
        if (pauseButton != null) pauseButton.SetActive(true);
        if (settingsButton != null) settingsButton.SetActive(true);
        if (gameInfoButton != null) gameInfoButton.SetActive(true);
        if (runButton != null) runButton.SetActive(true);
        if (dpadButton != null) dpadButton.SetActive(true);
    }
    
    public void ShowXButtonForGameInfo()
    {
        if (xButtonMobile != null) xButtonMobile.SetActive(true);
    }
    
    public void HideXButtonAfterGameInfo()
    {
        if (xButtonMobile != null) xButtonMobile.SetActive(false);
    }
    
    public void HideUIForGameInfoCity()
    {
        HideMainButtonsForGameInfo();
        ShowXButtonForGameInfo();
        if (GhostPromptManager.Instance != null && GhostPromptManager.Instance.promptPanel != null)
            GhostPromptManager.Instance.promptPanel.SetActive(false);
    }
    
    public void HideUIForGameInfoForest()
    {
        HideMainButtonsForGameInfo();
        ShowXButtonForGameInfo();
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.timerText != null)
            GameFlowManager.Instance.timerText.gameObject.SetActive(false);
    }
    
    public void RestoreUIAfterGameInfoCity()
    {
        RestoreButtonsAfterGameInfo();
        HideXButtonAfterGameInfo();
        if (GhostPromptManager.Instance != null && GhostPromptManager.Instance.promptPanel != null)
            GhostPromptManager.Instance.promptPanel.SetActive(true);
    }
    
    public void RestoreUIAfterGameInfoForest()
    {
        RestoreButtonsAfterGameInfo();
        HideXButtonAfterGameInfo();
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.timerText != null)
            GameFlowManager.Instance.timerText.gameObject.SetActive(true);
    }

    void UpdateMainButtonsVisibilityByUIState()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            SetMainButtonsVisible(false);
            if (mobileControls != null) mobileControls.SetActive(false);
            return;
        }

        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            SetMainButtonsVisible(false);
            if (mobileControls != null) mobileControls.SetActive(false);
            return;
        }

        bool gameStarted = GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameStarted();
        bool gameInfoOpen = UIManager.Instance != null && UIManager.Instance.IsGameInfoActive();

        bool hideMainButtons =
            gameInfoOpen ||
            (GameManager.Instance != null && GameManager.Instance.isInteracting) ||
            TeleportEffect.IsTeleportingGlobal ||
            (UIManager.Instance != null &&
             ((UIManager.Instance.dialoguePanel != null && UIManager.Instance.dialoguePanel.activeSelf) ||
              (UIManager.Instance.choicePanel != null && UIManager.Instance.choicePanel.activeSelf))) ||
            (GhostPromptManager.Instance != null &&
             GhostPromptManager.Instance.promptPanel != null &&
             GhostPromptManager.Instance.promptPanel.activeSelf &&
             (gameStarted || !isPaused));

        SetMainButtonsVisible(!hideMainButtons);
    }

    void EnsureOverlayDoesNotBlockClicks()
    {
        if (pauseOverlay == null) return;

        var cg = pauseOverlay.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        var img = pauseOverlay.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.raycastTarget = false;
        }
    }

    void UpdateOverlay()
    {
        if (pauseOverlay == null) return;
        bool settingsOpen = settingsPanel != null && settingsPanel.activeSelf;
        bool editMode = LayoutEditorManager.Instance != null && LayoutEditorManager.Instance.IsEditMode();
        pauseOverlay.SetActive(isPaused || settingsOpen || editMode || forceBlur);
        EnsureOverlayDoesNotBlockClicks();
    }

    void UpdatePausedPrompt()
    {
        if (!showPausedPromptInCity) return;

        bool gameStarted = GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameStarted();
        if (gameStarted)
        {
            HidePausedPrompt();
            return;
        }

        // Never show "Game Paused" prompt when settings panel is open.
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            HidePausedPrompt();
            return;
        }

        if (isPaused)
        {
            if (GhostPromptManager.Instance != null &&
                GhostPromptManager.Instance.promptPanel != null &&
                GhostPromptManager.Instance.promptText != null)
            {
                GhostPromptManager.Instance.promptText.text = pausedPromptText;
                GhostPromptManager.Instance.promptPanel.SetActive(true);
            }
        }
        else
        {
            HidePausedPrompt();
        }
    }

    void HidePausedPrompt()
    {
        if (GhostPromptManager.Instance != null &&
            GhostPromptManager.Instance.promptPanel != null)
        {
            GhostPromptManager.Instance.promptPanel.SetActive(false);
        }
    }
}
