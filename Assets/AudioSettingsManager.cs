using UnityEngine;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance;
    public const string KeyMusic = "setting_music";
    public const string KeySfx = "setting_sfx";
    public const string KeyUi = "setting_ui";

    [Header("Default Volumes (0-1)")]
    [Range(0f, 1f)] public float musicVolume = 0.5f; // slider value
    [Range(0f, 1f)] public float sfxVolume = 0.5f;   // slider value
    [Range(0f, 1f)] public float uiVolume = 0.5f;    // slider value

    private static float musicVol = 0.5f; // slider value
    private static float sfxVol = 0.5f;   // slider value
    private static float uiVol = 0.5f;    // slider value

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Load saved values if present.
        if (PlayerPrefs.HasKey(KeyMusic)) musicVolume = PlayerPrefs.GetFloat(KeyMusic);
        if (PlayerPrefs.HasKey(KeySfx)) sfxVolume = PlayerPrefs.GetFloat(KeySfx);
        if (PlayerPrefs.HasKey(KeyUi)) uiVolume = PlayerPrefs.GetFloat(KeyUi);

        SetMusicVolume(musicVolume);
        SetSfxVolume(sfxVolume);
        SetUiVolume(uiVolume);
    }

    public static float GetMusicVolume()
    {
        return musicVol;
    }

    public static float GetSfxVolume()
    {
        return sfxVol;
    }

    public static float GetUiVolume()
    {
        return uiVol;
    }

    // Multiplier: slider 0.5 == current loudness (1.0x), slider 1.0 == 2.0x
    public static float GetMusicMultiplier()
    {
        return Mathf.Clamp01(musicVol) * 2f;
    }

    public static float GetSfxMultiplier()
    {
        return Mathf.Clamp01(sfxVol) * 2f;
    }

    public static float GetUiMultiplier()
    {
        return Mathf.Clamp01(uiVol) * 2f;
    }

    public static void SetMusicVolume(float v)
    {
        musicVol = Mathf.Clamp01(v);
        if (Instance != null) Instance.musicVolume = musicVol;
        if (AreaMusicManager.Instance != null)
            AreaMusicManager.Instance.SetUserMusicVolume(GetMusicMultiplier());
        PlayerPrefs.SetFloat(KeyMusic, musicVol);
    }

    public static void SetSfxVolume(float v)
    {
        sfxVol = Mathf.Clamp01(v);
        if (Instance != null) Instance.sfxVolume = sfxVol;
        PlayerPrefs.SetFloat(KeySfx, sfxVol);
    }

    public static void SetUiVolume(float v)
    {
        uiVol = Mathf.Clamp01(v);
        if (Instance != null) Instance.uiVolume = uiVol;
        PlayerPrefs.SetFloat(KeyUi, uiVol);
    }
}
