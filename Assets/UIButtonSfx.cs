using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSfx : MonoBehaviour, IPointerClickHandler
{
    public AudioSource sourceOverride;
    public AudioClip clickClip;
    public bool allowInLayoutEdit = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (LayoutEditorManager.Instance != null && LayoutEditorManager.Instance.IsEditMode() && !allowInLayoutEdit)
            return;

        AudioSource src = sourceOverride;
        if (src == null && UIManager.Instance != null)
            src = UIManager.Instance.uiSfxSource;
        if (src == null)
        {
            src = GetComponent<AudioSource>();
        }
        if (src == null || clickClip == null) return;
        src.ignoreListenerPause = true;
        src.PlayOneShot(clickClip, AudioSettingsManager.GetUiMultiplier());
    }
}
