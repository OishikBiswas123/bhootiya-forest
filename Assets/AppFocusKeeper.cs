using UnityEngine;

// Prevent unwanted auto-pause when notification bar/calls steal focus.
public class AppFocusKeeper : MonoBehaviour
{
    void Awake()
    {
        Application.runInBackground = true;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;
        RestoreIfNotUserPaused();
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused) return;
        // If app loses focus, keep running unless the user explicitly paused.
        RestoreIfNotUserPaused();
    }

    void RestoreIfNotUserPaused()
    {
        var pauseCtrl = FindObjectOfType<PauseSettingsController>();
        if (pauseCtrl != null && pauseCtrl.IsPaused())
            return;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (PlayerMove.Instance != null)
            PlayerMove.Instance.SetExternalFreeze(false);
    }
}

