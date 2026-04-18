using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    public const string KeyRunMode = "setting_runmode";
    [Header("Difficulty Buttons")]
    public Button easyButton;
    public Button midButton;
    public Button hardButton;

    [Header("Walk Fast Buttons")]
    public Button holdButton;
    public Button tapButton;

    [Header("Audio Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;

    [Header("Creator Link")]
    public Button creatorButton;
    public string creatorUrl = "https://oishikbiswas.vercel.app/";

    [Header("Layout Buttons")]
    public Button layoutCustomizeButton;
    public Button layoutDefaultButton;

    [Header("Reset Button")]
    public Button resetButton;
    public string resetPromptText = "All settings reset to default.";
    public float resetPromptDuration = 2f;

    [Header("Visuals")]
    [Range(0f, 1f)] public float inactiveAlpha = 0.45f;

    private Button activeButton;
    private Button activeRunButton;
    private PlayerMove.RunMode desiredRunMode = PlayerMove.RunMode.Hold;
    private bool runModeApplied = false;

    void Start()
    {
        if (easyButton != null) easyButton.onClick.AddListener(() => SetActiveButton(easyButton));
        if (midButton != null) midButton.onClick.AddListener(() => SetActiveButton(midButton));
        if (hardButton != null) hardButton.onClick.AddListener(() => SetActiveButton(hardButton));

        if (holdButton != null) holdButton.onClick.AddListener(() => SetActiveRunButton(holdButton));
        if (tapButton != null) tapButton.onClick.AddListener(() => SetActiveRunButton(tapButton));

        if (creatorButton != null)
            creatorButton.onClick.AddListener(OpenCreatorLink);

        if (layoutCustomizeButton != null)
            layoutCustomizeButton.onClick.AddListener(OpenLayoutCustomize);
        if (layoutDefaultButton != null)
            layoutDefaultButton.onClick.AddListener(SetLayoutDefault);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetAllSettings);

        LoadSavedSettings();

        // Default to Mid if nothing set.
        if (activeButton == null)
        {
            if (midButton != null) SetActiveButton(midButton);
            else if (easyButton != null) SetActiveButton(easyButton);
        }

        // Default to Hold if nothing set.
        if (activeRunButton == null)
        {
            if (holdButton != null) SetActiveRunButton(holdButton);
            else if (tapButton != null) SetActiveRunButton(tapButton);
        }

        InitSliders();
    }

    void OnEnable()
    {
        runModeApplied = false;
    }

    void Update()
    {
        // Apply run mode once player is available (handles init order).
        if (!runModeApplied && PlayerMove.Instance != null)
        {
            ApplyRunModeToPlayer();
            runModeApplied = true;
        }
    }

    void InitSliders()
    {
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(AudioSettingsManager.GetMusicVolume());
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioSettingsManager.GetSfxVolume());
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }
        if (uiSlider != null)
        {
            uiSlider.SetValueWithoutNotify(AudioSettingsManager.GetUiVolume());
            uiSlider.onValueChanged.AddListener(OnUiSliderChanged);
        }
    }

    void SetActiveButton(Button btn)
    {
        activeButton = btn;
        UpdateButtonAlpha(easyButton, easyButton == activeButton);
        UpdateButtonAlpha(midButton, midButton == activeButton);
        UpdateButtonAlpha(hardButton, hardButton == activeButton);

        if (GameFlowManager.Instance != null)
        {
            if (activeButton == easyButton) GameFlowManager.Instance.SetDifficultyEasy();
            else if (activeButton == midButton) GameFlowManager.Instance.SetDifficultyMid();
            else if (activeButton == hardButton) GameFlowManager.Instance.SetDifficultyHard();
        }
    }

    void SetActiveRunButton(Button btn)
    {
        activeRunButton = btn;
        UpdateButtonAlpha(holdButton, holdButton == activeRunButton);
        UpdateButtonAlpha(tapButton, tapButton == activeRunButton);

        if (activeRunButton == holdButton)
            desiredRunMode = PlayerMove.RunMode.Hold;
        else if (activeRunButton == tapButton)
            desiredRunMode = PlayerMove.RunMode.Tap;

        ApplyRunModeToPlayer();
        PlayerPrefs.SetInt(KeyRunMode, desiredRunMode == PlayerMove.RunMode.Hold ? 0 : 1);
    }

    void UpdateButtonAlpha(Button btn, bool isActive)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img == null) return;

        Color c = img.color;
        c.a = isActive ? 0f : inactiveAlpha;
        img.color = c;
    }

    void OnMusicSliderChanged(float value)
    {
        AudioSettingsManager.SetMusicVolume(value);
    }

    void OnSfxSliderChanged(float value)
    {
        AudioSettingsManager.SetSfxVolume(value);
    }

    void OnUiSliderChanged(float value)
    {
        AudioSettingsManager.SetUiVolume(value);
    }

    void ApplyRunModeToPlayer()
    {
        var player = PlayerMove.Instance;
        if (player == null) return;

        if (desiredRunMode == PlayerMove.RunMode.Hold)
            player.SetRunModeHold();
        else
            player.SetRunModeTap();
    }

    public void OpenCreatorLink()
    {
        if (!string.IsNullOrWhiteSpace(creatorUrl))
        {
            Application.OpenURL(creatorUrl);
        }
    }

    public void OpenLayoutCustomize()
    {
        if (LayoutEditorManager.Instance != null)
            LayoutEditorManager.Instance.EnterEditMode();
    }

    public void SetLayoutDefault()
    {
        if (LayoutEditorManager.Instance != null)
            LayoutEditorManager.Instance.ResetToDefault();
    }

    public void ResetAllSettings()
    {
        // Audio sliders to 50%
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(0.5f);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(0.5f);
        if (uiSlider != null) uiSlider.SetValueWithoutNotify(0.5f);

        AudioSettingsManager.SetMusicVolume(0.5f);
        AudioSettingsManager.SetSfxVolume(0.5f);
        AudioSettingsManager.SetUiVolume(0.5f);

        // Difficulty mid
        if (midButton != null) SetActiveButton(midButton);

        // Run mode hold
        if (holdButton != null) SetActiveRunButton(holdButton);

        // Layout reset
        if (LayoutEditorManager.Instance != null)
            LayoutEditorManager.Instance.ResetToDefaultWithPrompt(resetPromptText, resetPromptDuration);
        else if (GhostPromptManager.Instance != null)
        {
            var pauseCtrl = FindObjectOfType<PauseSettingsController>();
            if (pauseCtrl != null) pauseCtrl.SetForceBlur(true);
            GhostPromptManager.Instance.ShowCustomPrompt(resetPromptText, resetPromptDuration);
            StartCoroutine(ClearForceBlurAfter(resetPromptDuration));
        }

        PlayerPrefs.SetInt(GameFlowManager.KeyDifficulty, (int)GameFlowManager.Difficulty.Mid);
        PlayerPrefs.SetInt(KeyRunMode, 0);
    }

    IEnumerator ClearForceBlurAfter(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        var pauseCtrl = FindObjectOfType<PauseSettingsController>();
        if (pauseCtrl != null) pauseCtrl.SetForceBlur(false);
    }

    void LoadSavedSettings()
    {
        // Difficulty
        if (PlayerPrefs.HasKey(GameFlowManager.KeyDifficulty))
        {
            int d = PlayerPrefs.GetInt(GameFlowManager.KeyDifficulty);
            if (d == (int)GameFlowManager.Difficulty.Easy && easyButton != null) SetActiveButton(easyButton);
            else if (d == (int)GameFlowManager.Difficulty.Hard && hardButton != null) SetActiveButton(hardButton);
            else if (midButton != null) SetActiveButton(midButton);
        }

        // Run mode
        if (PlayerPrefs.HasKey(KeyRunMode))
        {
            int r = PlayerPrefs.GetInt(KeyRunMode, 0);
            if (r == 0 && holdButton != null) SetActiveRunButton(holdButton);
            else if (r == 1 && tapButton != null) SetActiveRunButton(tapButton);
        }
    }
}
