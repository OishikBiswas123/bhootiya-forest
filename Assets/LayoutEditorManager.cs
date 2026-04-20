using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayoutEditorManager : MonoBehaviour
{
    public static LayoutEditorManager Instance;

    [System.Serializable]
    public class LayoutItem
    {
        public string id;
        public RectTransform rect;
        [HideInInspector] public Vector2 defaultPos;
        [HideInInspector] public Vector3 defaultScale;
    }

    [Header("Layout Items")]
    public LayoutItem[] items;

    [Header("UI")]
    public GameObject settingsPanel;
    public GameObject blurPanel;
    public GameObject exitButton;

    [Header("Prompt")]
    public string defaultPromptText = "Layout changed to default";
    public float defaultPromptDuration = 2f;

    [Header("Persistence")]
    public bool loadSavedLayout = true;

    private bool editMode = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        CacheDefaults();
        if (loadSavedLayout)
            LoadLayout();
        SetEditMode(false);
    }

    void CacheDefaults()
    {
        if (items == null) return;
        foreach (var it in items)
        {
            if (it == null || it.rect == null) continue;
            it.defaultPos = it.rect.anchoredPosition;
            it.defaultScale = it.rect.localScale;
        }
    }

    public bool IsEditMode()
    {
        return editMode;
    }

    void SetEditMode(bool value)
    {
        editMode = value;
        if (blurPanel != null)
        {
            blurPanel.SetActive(editMode);
            if (editMode) blurPanel.transform.SetAsFirstSibling(); // keep blur behind buttons
        }
        if (exitButton != null)
        {
            exitButton.SetActive(editMode);
            if (editMode) exitButton.transform.SetAsLastSibling(); // keep exit on top
        }
        if (editMode && settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void EnterEditMode()
    {
        ShowAllLayoutItems();
        SetEditMode(true);
        SetItemButtonsInteractable(false);
    }
    
    void ShowAllLayoutItems()
    {
        if (items == null) return;
        foreach (var it in items)
        {
            if (it != null && it.rect != null && it.rect.gameObject != null)
            {
                it.rect.gameObject.SetActive(true);
            }
        }
    }

    public void ExitEditMode()
    {
        SaveLayout();
        SetEditMode(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        SetItemButtonsInteractable(true);
    }

    public void ResetToDefault()
    {
        ResetToDefaultInternal();
        StartCoroutine(ShowDefaultPromptThenReopen(defaultPromptText, defaultPromptDuration));
    }

    public void ResetToDefaultWithPrompt(string promptText, float duration)
    {
        ResetToDefaultInternal();
        StartCoroutine(ShowDefaultPromptThenReopen(promptText, duration));
    }

    void ResetToDefaultInternal()
    {
        if (items == null) return;
        foreach (var it in items)
        {
            if (it == null || it.rect == null) continue;
            it.rect.anchoredPosition = it.defaultPos;
            it.rect.localScale = it.defaultScale;
        }
        SaveLayout();
    }

    void SetItemButtonsInteractable(bool enabled)
    {
        if (items == null) return;
        foreach (var it in items)
        {
            if (it == null || it.rect == null) continue;
            var btn = it.rect.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.enabled = enabled;
        }
    }

    IEnumerator ShowDefaultPromptThenReopen(string promptText, float duration)
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Ensure blur is visible during the prompt.
        if (blurPanel != null) blurPanel.SetActive(true);
        var pauseCtrl = FindObjectOfType<PauseSettingsController>();
        if (pauseCtrl != null) pauseCtrl.SetForceBlur(true);

        if (GhostPromptManager.Instance != null)
            GhostPromptManager.Instance.ShowCustomPrompt(promptText, duration);

        // Use unscaled time so it still works while paused.
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (settingsPanel != null) settingsPanel.SetActive(true);

        // Keep blur on after returning to settings.
        if (blurPanel != null) blurPanel.SetActive(true);
        if (pauseCtrl != null) pauseCtrl.SetForceBlur(false);
    }

    void SaveLayout()
    {
        if (items == null) return;
        foreach (var it in items)
        {
            if (it == null || it.rect == null || string.IsNullOrEmpty(it.id)) continue;
            PlayerPrefs.SetFloat(it.id + "_x", it.rect.anchoredPosition.x);
            PlayerPrefs.SetFloat(it.id + "_y", it.rect.anchoredPosition.y);
            PlayerPrefs.SetFloat(it.id + "_sx", it.rect.localScale.x);
            PlayerPrefs.SetFloat(it.id + "_sy", it.rect.localScale.y);
        }
        PlayerPrefs.Save();
    }

    void LoadLayout()
    {
        if (items == null) return;
        foreach (var it in items)
        {
            if (it == null || it.rect == null || string.IsNullOrEmpty(it.id)) continue;
            if (PlayerPrefs.HasKey(it.id + "_x"))
            {
                float x = PlayerPrefs.GetFloat(it.id + "_x");
                float y = PlayerPrefs.GetFloat(it.id + "_y");
                float sx = PlayerPrefs.GetFloat(it.id + "_sx", it.rect.localScale.x);
                float sy = PlayerPrefs.GetFloat(it.id + "_sy", it.rect.localScale.y);
                it.rect.anchoredPosition = new Vector2(x, y);
                it.rect.localScale = new Vector3(sx, sy, it.rect.localScale.z);
            }
        }
    }
}
