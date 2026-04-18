using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TitleScreenController : MonoBehaviour
{
    [Header("Scene")]
    public string nextSceneName = "SampleScene";

    [Header("Ghost")]
    public GameObject ghostPrefab;
    public float ghostMoveDuration = 2.0f;
    public float ghostStartViewportX = 1.2f; // right of screen
    public float ghostEndViewportX = -0.2f;  // left of screen
    public float ghostViewportY = 0.5f;      // middle
    public float ghostZDistance = 10f;        // distance in front of camera
    public int ghostSortingOrder = 1000;
    public string ghostSortingLayerName = "UI";
    public float ghostScale = 1f;
    public bool forceGhostOnScreen = true;
    public bool disableGhostAIOnTitle = true;
    public string ghostChaseBool = "isChasing";
    [Header("Ghost (UI Canvas)")]
    public bool useCanvasGhost = true;
    public RectTransform ghostRect;
    public Image ghostImage;
    public Sprite ghostUISprite;
    public Sprite[] ghostUIFrames;
    public float ghostUIFps = 12f;
    public float ghostCanvasY = 0f;
    public float ghostCanvasPadding = 80f;

    [Header("Logo")]
    public RectTransform logoRect;
    public float logoZoomDuration = 1.2f;
    public float logoStartScale = 0f;
    public float logoEndScale = 1.0f;
    public float holdAfterLogo = 0.5f;

    [Header("Title Text")]
    public bool showTitleText = true;
    public TMP_Text titleLine1;
    public TMP_Text titleLine2;
    public float textPopInDuration = 1.5f;
    public float textStagger = 0.05f;
    public float textHoldDuration = 2.0f;
    public float textPopOffset = 600f;
    public bool eraseTextWithGhost = true;
    public bool ghostMovesRightToLeft = true;
    public bool normalizePopInDurationPerLine = true;

    [Header("Loading Bar")]
    public bool showLoadingBar = true;
    public RectTransform loadingBarContainer;
    public Image loadingBarFill;
    public Image loadingBarBackground;
    public float loadingBarHeight = 20f;
    public float loadingBarWidth = 400f;
    public Color loadingBarFillColor = Color.white;
    public Color loadingBarBgColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public float loadingBarDuration = 2f;
    public float loadingBarDelay = 0.3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip titleTextAppearSfx;
    public AudioClip ghostAppearSfx;
    public AudioClip logoAppearSfx;
    public float sfxVolume = 1f;

    Camera cam;
    TMP_Text[] titleLines;
    Vector3[][] originalVerts;
    Color32[][] originalColors;

    void Start()
    {
        cam = Camera.main;
        if (logoRect != null)
        {
            logoRect.gameObject.SetActive(false);
            logoRect.localScale = Vector3.one * logoStartScale;
        }
        if (ghostRect != null)
        {
            ghostRect.gameObject.SetActive(false);
        }
        SetupTitleText();
        SetupLoadingBar();
        StartCoroutine(PlaySequence());
    }

    void SetupLoadingBar()
    {
        if (!showLoadingBar) return;
        
        if (loadingBarContainer != null)
        {
            loadingBarContainer.gameObject.SetActive(false);
            loadingBarContainer.sizeDelta = new Vector2(700f, 8f);
        }
        
        if (loadingBarBackground != null)
        {
            loadingBarBackground.color = loadingBarBgColor;
        }
        
        if (loadingBarFill != null)
        {
            loadingBarFill.gameObject.SetActive(false);
            loadingBarFill.color = loadingBarFillColor;
            Vector2 currentPos = loadingBarFill.rectTransform.anchoredPosition;
            loadingBarFill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            loadingBarFill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            loadingBarFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            loadingBarFill.rectTransform.anchoredPosition = new Vector2(2f, currentPos.y);
            loadingBarFill.rectTransform.sizeDelta = new Vector2(0f, 4f);
        }
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume * AudioSettingsManager.GetSfxMultiplier());
        }
    }

    IEnumerator AnimateLoadingBar(float duration)
    {
        if (!showLoadingBar) yield break;
        if (loadingBarContainer != null) loadingBarContainer.gameObject.SetActive(true);
        if (loadingBarFill != null) loadingBarFill.gameObject.SetActive(true);
        
        float maxWidth = 696f;
        float fillHeight = 4f;
        
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float currentWidth = maxWidth * progress;
            if (loadingBarFill != null)
            {
                loadingBarFill.rectTransform.sizeDelta = new Vector2(currentWidth, fillHeight);
            }
            yield return null;
        }
        if (loadingBarFill != null)
        {
            loadingBarFill.rectTransform.sizeDelta = new Vector2(maxWidth, fillHeight);
        }
    }

    IEnumerator PlaySequence()
    {
        // 0) Title text pop-in
        if (showTitleText)
        {
            PlaySfx(titleTextAppearSfx);
            yield return PlayTitleTextPopIn();
        }

        // 1) Spawn ghost and move across
        if (useCanvasGhost && ghostRect != null)
        {
            PlaySfx(ghostAppearSfx);
            yield return PlayGhostOnCanvas();
        }
        else if (ghostPrefab != null && cam != null)
        {
            float startX = ghostStartViewportX;
            float endX = ghostEndViewportX;
            float y = ghostViewportY;
            if (forceGhostOnScreen)
            {
                startX = 0.9f;
                endX = 0.1f;
                y = 0.55f;
            }
            Vector3 start = cam.ViewportToWorldPoint(new Vector3(startX, y, ghostZDistance));
            Vector3 end = cam.ViewportToWorldPoint(new Vector3(endX, y, ghostZDistance));
            GameObject ghost = Instantiate(ghostPrefab, start, Quaternion.identity);
            // Ensure visible on top.
            var sr = ghost.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = ghostSortingOrder;
                if (!string.IsNullOrEmpty(ghostSortingLayerName))
                    sr.sortingLayerName = ghostSortingLayerName;
                sr.enabled = true;
                sr.color = Color.white;
            }
            if (ghostScale > 0f)
                ghost.transform.localScale = Vector3.one * ghostScale;
            // Disable gameplay AI on title screen.
            if (disableGhostAIOnTitle)
            {
                var ai = ghost.GetComponent<GhostAI>();
                if (ai != null) ai.enabled = false;
                var rb = ghost.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
                var col = ghost.GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
            var anim = ghost.GetComponent<Animator>();
            if (anim != null && !string.IsNullOrEmpty(ghostChaseBool))
            {
                anim.SetBool(ghostChaseBool, true);
            }

            float t = 0f;
            while (t < ghostMoveDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / ghostMoveDuration);
                ghost.transform.position = Vector3.Lerp(start, end, p);
                yield return null;
            }

            Destroy(ghost);
        }
        else
        {
            // No ghost; wait same time to keep timing consistent
            yield return new WaitForSecondsRealtime(ghostMoveDuration);
        }

        // After ghost, hide title text
        if (showTitleText)
            SetTitleTextVisible(false);

        // 2) Logo zoom in
        if (logoRect != null)
        {
            logoRect.gameObject.SetActive(true);
            PlaySfx(logoAppearSfx);
            float t = 0f;
            while (t < logoZoomDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / logoZoomDuration);
                float s = Mathf.Lerp(logoStartScale, logoEndScale, p);
                logoRect.localScale = Vector3.one * s;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(logoZoomDuration);
        }

        if (holdAfterLogo > 0f)
            yield return new WaitForSecondsRealtime(holdAfterLogo);

        // Show and animate loading bar
        if (showLoadingBar)
        {
            if (loadingBarContainer != null) loadingBarContainer.gameObject.SetActive(true);
            if (loadingBarFill != null) loadingBarFill.gameObject.SetActive(true);
            
            yield return StartCoroutine(AnimateLoadingBar(loadingBarDuration));
            
            yield return new WaitForSecondsRealtime(loadingBarDelay);
        }

        // 3) Load game
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    IEnumerator PlayGhostOnCanvas()
    {
        RectTransform parent = ghostRect.parent as RectTransform;
        if (parent == null)
        {
            yield return new WaitForSeconds(ghostMoveDuration);
            yield break;
        }

        if (ghostImage != null)
        {
            if (ghostUISprite != null)
                ghostImage.sprite = ghostUISprite;
            if (ghostUIFrames != null && ghostUIFrames.Length > 0)
                ghostImage.sprite = ghostUIFrames[0];
        }

        float halfW = parent.rect.width * 0.5f;
        float ghostW = ghostRect.rect.width * 0.5f;
        float startX = halfW + ghostW + ghostCanvasPadding;
        float endX = -halfW - ghostW - ghostCanvasPadding;

        Vector2 start = new Vector2(startX, ghostCanvasY);
        Vector2 end = new Vector2(endX, ghostCanvasY);

        ghostRect.anchoredPosition = start;
        ghostRect.gameObject.SetActive(true);

        float t = 0f;
        while (t < ghostMoveDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / ghostMoveDuration);
            ghostRect.anchoredPosition = Vector2.Lerp(start, end, p);
            if (ghostImage != null && ghostUIFrames != null && ghostUIFrames.Length > 0 && ghostUIFps > 0f)
            {
                int idx = Mathf.FloorToInt(t * ghostUIFps) % ghostUIFrames.Length;
                ghostImage.sprite = ghostUIFrames[idx];
            }
            if (eraseTextWithGhost && showTitleText)
                UpdateTextEraseWithGhost();
            yield return null;
        }

        ghostRect.gameObject.SetActive(false);
    }

    void SetupTitleText()
    {
        if (!showTitleText) return;

        titleLines = new TMP_Text[2];
        titleLines[0] = titleLine1;
        titleLines[1] = titleLine2;

        originalVerts = new Vector3[2][];
        originalColors = new Color32[2][];

        for (int i = 0; i < titleLines.Length; i++)
        {
            var t = titleLines[i];
            if (t == null) continue;
            t.gameObject.SetActive(true);
            t.ForceMeshUpdate();
            var info = t.textInfo;
            var mesh = t.mesh;
            var verts = mesh.vertices;
            var cols = mesh.colors32;

            int idx = i;
            if (originalVerts[idx] == null || originalVerts[idx].Length != verts.Length)
            {
                originalVerts[idx] = new Vector3[verts.Length];
                originalColors[idx] = new Color32[cols.Length];
            }
            System.Array.Copy(verts, originalVerts[idx], verts.Length);
            System.Array.Copy(cols, originalColors[idx], cols.Length);
        }
    }

    IEnumerator PlayTitleTextPopIn()
    {
        if (titleLines == null) yield break;

        float maxChars = 0f;
        for (int i = 0; i < titleLines.Length; i++)
        {
            var t = titleLines[i];
            if (t == null) continue;
            t.ForceMeshUpdate();
            maxChars = Mathf.Max(maxChars, t.textInfo.characterCount);
        }

        float totalDuration = textPopInDuration + (maxChars * textStagger);
        float time = 0f;
        while (time < totalDuration)
        {
            time += Time.unscaledDeltaTime;
            // Two-line mode: line1 from left, line2 from right.
            if (titleLines[0] != null) AnimateTextPopIn(titleLines[0], 0, time, -1f, true);
            if (titleLines[1] != null) AnimateTextPopIn(titleLines[1], 1, time, 1f, false);
            yield return null;
        }

        if (textHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(textHoldDuration);
    }

    void AnimateTextPopIn(TMP_Text t, int lineIndex, float time, float dir, bool reverseOrder)
    {
        t.ForceMeshUpdate();
        var info = t.textInfo;
        var verts = t.mesh.vertices;
        var cols = t.mesh.colors32;
        float perCharStagger = normalizePopInDurationPerLine
            ? (textPopInDuration / Mathf.Max(1, info.characterCount))
            : textStagger;

        for (int c = 0; c < info.characterCount; c++)
        {
            int charIndex = reverseOrder ? (info.characterCount - 1 - c) : c;
            var ch = info.characterInfo[charIndex];
            if (!ch.isVisible) continue;

            int vIndex = ch.vertexIndex;
            float startTime = c * perCharStagger;
            float tt = Mathf.Clamp01((time - startTime) / Mathf.Max(0.01f, textPopInDuration));
            float offsetX = 0f;
            if (dir < 0f) offsetX = Mathf.Lerp(-textPopOffset, 0f, tt);
            else if (dir > 0f) offsetX = Mathf.Lerp(textPopOffset, 0f, tt);
            byte alpha = (byte)Mathf.RoundToInt(255f * tt);

            for (int i = 0; i < 4; i++)
            {
                verts[vIndex + i] = originalVerts[lineIndex][vIndex + i] + new Vector3(offsetX, 0f, 0f);
                var col = originalColors[lineIndex][vIndex + i];
                col.a = alpha;
                cols[vIndex + i] = col;
            }
        }

        t.mesh.vertices = verts;
        t.mesh.colors32 = cols;
        t.canvasRenderer.SetMesh(t.mesh);
    }

    void UpdateTextEraseWithGhost()
    {
        if (ghostRect == null) return;
        var ghostWorld = ghostRect.TransformPoint(Vector3.zero);
        Vector2 ghostScreen = RectTransformUtility.WorldToScreenPoint(null, ghostWorld);

        for (int i = 0; i < titleLines.Length; i++)
        {
            var t = titleLines[i];
            if (t == null) continue;
            t.ForceMeshUpdate();
            var info = t.textInfo;
            var verts = t.mesh.vertices;
            var cols = t.mesh.colors32;

            for (int c = 0; c < info.characterCount; c++)
            {
                var ch = info.characterInfo[c];
                if (!ch.isVisible) continue;

                int vIndex = ch.vertexIndex;
                // Compute character center in screen space
                Vector3 w0 = t.transform.TransformPoint(verts[vIndex]);
                Vector3 w2 = t.transform.TransformPoint(verts[vIndex + 2]);
                Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(null, (w0 + w2) * 0.5f);

                bool eaten = ghostMovesRightToLeft ? (screenCenter.x > ghostScreen.x) : (screenCenter.x < ghostScreen.x);
                byte alpha = eaten ? (byte)0 : (byte)255;

                for (int vi = 0; vi < 4; vi++)
                {
                    var col = cols[vIndex + vi];
                    col.a = alpha;
                    cols[vIndex + vi] = col;
                }
            }

            t.mesh.colors32 = cols;
            t.canvasRenderer.SetMesh(t.mesh);
        }
    }

    void SetTitleTextVisible(bool visible)
    {
        if (titleLines == null) return;
        foreach (var t in titleLines)
        {
            if (t != null) t.gameObject.SetActive(visible);
        }
    }
}
