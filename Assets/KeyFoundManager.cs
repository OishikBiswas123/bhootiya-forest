using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class KeyFoundManager : MonoBehaviour
{
    public static KeyFoundManager Instance;
    
    [Header("UI References")]
    public GameObject keyAnimationPanel;
    public Image keyImage;
    public GameObject keyPromptPanel;
    public TextMeshProUGUI keyPromptText;
    
    [Header("Manual Animation Sprites (Backup)")]
    public Sprite[] keySprites; // Drag all 4 key frames here as backup
    
    [Header("Animation Settings")]
    public float zoomInDuration = 0.5f;
    public float stayDuration = 2f;
    public float zoomOutDuration = 0.5f;
    public float finalPromptDuration = 4f;
    
    [Header("Key Found SFX")]
    public AudioSource keyFoundAudioSource;
    public AudioClip keyFoundClip;
    [Range(0f, 1f)] public float keyFoundVolume = 1f;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private bool hasKey = false;
    private bool showingKeyAnimation = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Hide panels immediately in Awake
        if (keyAnimationPanel != null)
            keyAnimationPanel.SetActive(false);
        if (keyPromptPanel != null)
            keyPromptPanel.SetActive(false);
    }
    
    void Start()
    {
        // Hide panels at start (redundant but safe)
        if (keyAnimationPanel != null)
            keyAnimationPanel.SetActive(false);
        if (keyPromptPanel != null)
            keyPromptPanel.SetActive(false);
    }
    
    void Update()
    {
        // Press X to dismiss the final prompt
        if (hasKey && InputBridge.GetKeyDown(KeyCode.X) && keyPromptPanel.activeSelf && Time.time >= keyPromptInputUnlockTime)
        {
            HideKeyPrompt();
        }
    }
    
    public void FoundKey()
    {
        if (hasKey) return; // Already have key
        
        hasKey = true;
        Log("FoundKey() called - starting animation");
        StartCoroutine(PlayKeyFoundSequence());
    }
    
    IEnumerator PlayKeyFoundSequence()
    {
        showingKeyAnimation = true;
        Log("=== STARTING KEY ANIMATION SEQUENCE ===");
        Log("showingKeyAnimation set to: " + showingKeyAnimation);
        
        PlayKeyFoundSfx();
        
        // Check if UI elements are assigned
        if (keyAnimationPanel == null)
        {
            Debug.LogError("Key Animation Panel is NULL!");
            yield break;
        }
        if (keyImage == null)
        {
            Debug.LogError("Key Image is NULL!");
            yield break;
        }
        Log("UI elements are assigned correctly");
        
        // Close any open dialogues first
        if (UIManager.Instance != null)
            UIManager.Instance.CloseDialogue();
        
        // Hide ghost prompts
        if (GhostPromptManager.Instance != null)
            GhostPromptManager.Instance.HideNearbyPrompt();
        
        // Freeze game
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();
        
        // Freeze any ghost
        GhostAI[] ghosts = FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
        foreach (GhostAI ghost in ghosts)
        {
            ghost.enabled = false;
        }
        
        // Show key animation panel
        if (keyAnimationPanel != null && keyImage != null)
        {
            Log("Activating key animation panel");
            
            // Check if keyImage has a sprite
            if (keyImage.sprite == null)
            {
                Debug.LogError("KeyImage has no sprite assigned! Please drag a key sprite into the Source Image field.");
            }
            else
            {
                Log("Key sprite assigned: " + keyImage.sprite.name);
            }
            
            // Check for Animator
            Animator anim = keyImage.GetComponent<Animator>();
            if (anim == null)
            {
                Debug.LogWarning("KeyImage has no Animator component!");
            }
            else
            {
                Log("Animator found on KeyImage");
                
                // Get animation clip name
                if (anim.runtimeAnimatorController != null)
                {
                    var clips = anim.runtimeAnimatorController.animationClips;
                    if (clips.Length > 0)
                    {
                        string clipName = clips[0].name;
                        Log("Playing animation: " + clipName);
                        anim.Play(clipName, 0, 0f);
                    }
                    else
                    {
                        Debug.LogWarning("No animation clips found!");
                    }
                }
                else
                {
                    Debug.LogWarning("No runtime animator controller!");
                }
            }
            
            keyAnimationPanel.SetActive(true);
            
            // Start from center, scale 0
            keyImage.rectTransform.anchoredPosition = Vector2.zero;
            keyImage.rectTransform.localScale = Vector3.zero;
            
            // Enable animator if disabled
            if (anim != null && !anim.enabled)
            {
                anim.enabled = true;
            }
            
            // Start animation - FORCE manual animation for now since animator isn't working
            bool usingAnimator = false;
            
            // Skip animator and use manual animation always
            Log("Forcing manual sprite animation");
            usingAnimator = false;
            
            // If no animator, use manual sprite swapping
            bool usingManualAnimation = !usingAnimator && keySprites != null && keySprites.Length > 0;
            Log("usingAnimator=" + usingAnimator + ", usingManualAnimation=" + usingManualAnimation + ", keySprites.Length=" + (keySprites != null ? keySprites.Length : 0));
            if (usingManualAnimation)
            {
                Log("ABOUT TO START MANUAL ANIMATION COROUTINE");
                StartCoroutine(ManualSpriteAnimation());
                Log("MANUAL ANIMATION COROUTINE STARTED");
            }
            else
            {
                Debug.LogWarning("NOT using manual animation! usingManualAnimation=" + usingManualAnimation);
            }
            
            Log("Key image scale set to 0, starting zoom in");
            
            // Zoom in
            float elapsed = 0;
            while (elapsed < zoomInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / zoomInDuration;
                keyImage.rectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 2f, t);
                yield return null;
            }
            keyImage.rectTransform.localScale = Vector3.one * 2f;
            
            // Stay for a moment
            yield return new WaitForSeconds(stayDuration);
            
            // Zoom out
            elapsed = 0;
            while (elapsed < zoomOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / zoomOutDuration;
                keyImage.rectTransform.localScale = Vector3.Lerp(Vector3.one * 2f, Vector3.zero, t);
                yield return null;
            }
            
            // Stop animation
            if (anim != null && usingAnimator)
            {
                anim.StopPlayback();
            }
            
            keyAnimationPanel.SetActive(false);
        }
        
        // Show final prompt
        if (keyPromptPanel != null && keyPromptText != null)
        {
            keyPromptPanel.SetActive(true);
            keyPromptText.text = "You found the KEY!\n\nPlease return to the HUT to escape from the forest.";
        }
        keyPromptInputUnlockTime = Time.time + 1f;
        
        showingKeyAnimation = false;
    }
    
    void HideKeyPrompt()
    {
        if (keyPromptPanel != null)
            keyPromptPanel.SetActive(false);
        
        // Unfreeze game
        if (GameManager.Instance != null)
            GameManager.Instance.EndInteraction();
        
        // Re-enable ghosts
        GhostAI[] ghosts = FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
        foreach (GhostAI ghost in ghosts)
        {
            ghost.enabled = true;
        }
    }
    
    IEnumerator ManualSpriteAnimation()
    {
        Log("=== ManualSpriteAnimation COROUTINE STARTED ===");
        Log("showingKeyAnimation=" + showingKeyAnimation + ", keySprites.Length=" + keySprites.Length);
        
        float frameTime = 0.25f; // Change sprite every 0.25 seconds
        int currentFrame = 0;
        int loopCount = 0;
        
        while (showingKeyAnimation && keySprites.Length > 0)
        {
            loopCount++;
            Log("LOOP #" + loopCount + ": showingKeyAnimation=" + showingKeyAnimation);
            
            if (keyImage != null)
            {
                if (keySprites[currentFrame] != null)
                {
                    Log("Changing to sprite: " + currentFrame + " - " + keySprites[currentFrame].name);
                    keyImage.sprite = keySprites[currentFrame];
                    currentFrame = (currentFrame + 1) % keySprites.Length;
                }
                else
                {
                    Debug.LogError("keySprites[" + currentFrame + "] is NULL!");
                }
            }
            else
            {
                Debug.LogWarning("keyImage is null in ManualSpriteAnimation!");
            }
            
            Log("Waiting for " + frameTime + " seconds...");
            yield return new WaitForSeconds(frameTime);
        }
        
        Log("=== ManualSpriteAnimation ENDED === showingKeyAnimation=" + showingKeyAnimation + ", loopCount=" + loopCount);
    }

    public bool HasKey()
    {
        return hasKey;
    }
    
    public void ResetKey()
    {
        hasKey = false;
        showingKeyAnimation = false;
        
        // Hide any active panels
        if (keyAnimationPanel != null)
            keyAnimationPanel.SetActive(false);
        if (keyPromptPanel != null)
            keyPromptPanel.SetActive(false);
            
        Log("Key reset - player no longer has key");
    }

    void Log(string message)
    {
        if (enableDebugLogs || GlobalDebugSettings.EnableAllLogs)
        {
            Debug.Log(message);
        }
    }

    private float keyPromptInputUnlockTime = 0f;


    void PlayKeyFoundSfx()
    {
        if (keyFoundClip == null) return;
        AudioSource src = keyFoundAudioSource != null ? keyFoundAudioSource : GetComponent<AudioSource>();
        if (src == null) return;
        src.PlayOneShot(keyFoundClip, keyFoundVolume * AudioSettingsManager.GetSfxMultiplier());
    }
}


