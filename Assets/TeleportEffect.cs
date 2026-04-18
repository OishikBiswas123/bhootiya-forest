using UnityEngine;
using System.Collections;

public class TeleportEffect : MonoBehaviour
{
    public static bool IsTeleportingGlobal = false;
    public float spinSpeed = 720f;
    public float flySpeed = 5f;
    public float flyHeight = 5f;
    public float fadeStartTime = 0.5f;
    public float fadeSpeed = 2f;
    [Header("Timing")]
    public float teleportOutDuration = 3f;
    public float teleportInDuration = 3f;
    public GameObject teleportParticles;
    
    [Header("Teleport Audio")]
    public AudioSource teleportAudioSource;
    public AudioClip teleportFlyClip; // Played forward on fly-out, reverse on fly-in
    [Range(0f, 1f)] public float teleportSfxVolume = 1f;
    
    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isTeleporting = false;
    
    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
    }
    
    public IEnumerator TeleportOut(System.Action onComplete)
    {
        if (isTeleporting) yield break;
        isTeleporting = true;
        IsTeleportingGlobal = true;

        PlayTeleportFlyForward();
        
        // Disable player movement during teleport
        if (PlayerMove.Instance != null)
        {
            PlayerMove.Instance.enabled = false;
            PlayerMove.Instance.SetFacingDirection(1); // Face front/down before teleport
        }
        
        float timer = 0f;
        float duration = Mathf.Max(0.1f, teleportOutDuration);
        float startY = transform.position.y;
        float targetY = startY + flyHeight;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            
            // Spin faster
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
            
            // Fly up higher
            float currentY = Mathf.Lerp(startY, targetY, easedT);
            transform.position = new Vector3(transform.position.x, currentY, transform.position.z);
            
            // Fade starts later (after 50% of animation)
            if (timer > fadeStartTime)
            {
                float fadeT = (timer - fadeStartTime) / Mathf.Max(0.01f, (duration - fadeStartTime));
                float alpha = Mathf.Lerp(1f, 0f, fadeT);
                SetAlpha(alpha);
            }
            
            yield return null;
        }
        
        // Hide completely
        SetAlpha(0f);
        gameObject.SetActive(false);
        
        isTeleporting = false;
        IsTeleportingGlobal = false;
        onComplete?.Invoke();
    }
    
    public IEnumerator TeleportIn(Vector3 targetPosition, System.Action onComplete)
    {
        if (isTeleporting) yield break;
        isTeleporting = true;
        IsTeleportingGlobal = true;
        
        // Set to target position but hidden and above
        transform.position = targetPosition;
        transform.position += Vector3.up * flyHeight; // Start from higher up
        gameObject.SetActive(true);
        SetAlpha(0f);
        
        // Disable player movement during teleport
        if (PlayerMove.Instance != null)
            PlayerMove.Instance.enabled = false;

        PlayTeleportFlyReverse();
        
        float timer = 0f;
        float duration = Mathf.Max(0.1f, teleportInDuration);
        float startY = targetPosition.y + flyHeight;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            
            // Spin faster
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
            
            // Fly down from higher
            float currentY = Mathf.Lerp(startY, targetPosition.y, easedT);
            transform.position = new Vector3(targetPosition.x, currentY, targetPosition.z);
            
            // Fade in starts later
            if (timer > fadeStartTime)
            {
                float fadeT = (timer - fadeStartTime) / Mathf.Max(0.01f, (duration - fadeStartTime));
                float alpha = Mathf.Lerp(0f, 1f, fadeT);
                SetAlpha(alpha);
            }
            
            yield return null;
        }
        
        // Ensure final position
        transform.position = targetPosition;
        
        // Reset rotation to face front (y rotation = 0)
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        
        SetAlpha(1f);
        
        // Re-enable player movement and face front
        if (PlayerMove.Instance != null)
        {
            PlayerMove.Instance.enabled = true;
            PlayerMove.Instance.SetFacingDirection(1); // Face front/down after teleport
        }
        
        // Force player to face front (idle down) after teleport
        PlayerMove playerMove = GetComponent<PlayerMove>();
        if (playerMove != null)
        {
            playerMove.SetFacingDirection(1);
        }
        
        isTeleporting = false;
        IsTeleportingGlobal = false;
        onComplete?.Invoke();
    }
    
    void SetAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null)
            {
                Color c = originalColors[i];
                c.a = alpha;
                renderers[i].material.color = c;
                
                // Handle transparency mode
                if (renderers[i].material.HasProperty("_Mode"))
                {
                    renderers[i].material.SetFloat("_Mode", 3); // Transparent
                }
                renderers[i].material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderers[i].material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderers[i].material.SetInt("_ZWrite", 0);
                renderers[i].material.DisableKeyword("_ALPHATEST_ON");
                renderers[i].material.EnableKeyword("_ALPHABLEND_ON");
                renderers[i].material.renderQueue = 3000;
            }
        }
    }

    void PlayTeleportFlyForward()
    {
        if (teleportFlyClip == null) return;
        AudioSource src = GetTeleportAudioSource();
        if (src == null) return;

        src.Stop();
        src.pitch = 1f;
        src.loop = false;
        src.clip = teleportFlyClip;
        src.volume = teleportSfxVolume * AudioSettingsManager.GetSfxMultiplier();
        src.time = 0f;
        src.Play();
    }

    void PlayTeleportFlyReverse()
    {
        if (teleportFlyClip == null) return;
        AudioSource src = GetTeleportAudioSource();
        if (src == null) return;

        src.Stop();
        src.loop = false;
        src.clip = teleportFlyClip;
        src.volume = teleportSfxVolume * AudioSettingsManager.GetSfxMultiplier();

        // Reverse playback by negative pitch.
        src.pitch = -1f;
        src.time = Mathf.Max(0f, teleportFlyClip.length - 0.01f);
        src.Play();
    }

    AudioSource GetTeleportAudioSource()
    {
        if (teleportAudioSource != null) return teleportAudioSource;

        teleportAudioSource = GetComponent<AudioSource>();
        if (teleportAudioSource == null)
        {
            teleportAudioSource = gameObject.AddComponent<AudioSource>();
            teleportAudioSource.playOnAwake = false;
            teleportAudioSource.loop = false;
            teleportAudioSource.spatialBlend = 0f;
        }

        return teleportAudioSource;
    }
}
