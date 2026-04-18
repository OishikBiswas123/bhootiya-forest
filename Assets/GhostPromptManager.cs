using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GhostPromptManager : MonoBehaviour
{
    public static GhostPromptManager Instance;
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
[Header("UI References")]
    public GameObject promptPanel;
    public TMPro.TextMeshProUGUI promptText;
    
    [Header("First Time Prompts")]
    [TextArea(2, 3)]
    public string[] firstTimePrompts = new string[]
    {
        "A ghost has appeared in the forest...",
        "The forest is no longer safe. A ghost is hunting you...",
        "RUN..."
    };
    
    [Header("Random Prompts (for subsequent appearances)")]
    [TextArea(2, 3)]
    public string[] randomPrompts = new string[]
    {
        "It's back... and closer than before.",
        "You can feel its breath behind you.",
        "It found you again. Don't stop.",
        "The ghost is hunting you once more.",
        "You thought it was gone... it wasn't.",
        "You are not alone anymore.",
        "It's back... and closer than before.",
        "You can feel it right behind you.",
        "There is no escape."
    };
    
    private int currentPromptIndex = 0;
    private bool showingFirstTimePrompt = false;
    private bool isFirstGhostSpawn = true;
    private Coroutine activeCoroutine = null;

    [Header("Office 6th Floor Stay Prompts")]
    public bool enableOffice6thFloorPrompts = true;
    public float office6thFirstPromptDelay = 10f;
    public float office6thSecondPromptAfterFirstDelay = 12f;
    public string office6thPrompt1 = "Something feels wrong in this room...";
    public string office6thPrompt2 = "You feel like someone is watching you.";

    private Collider office6thZoneCollider;
    private Transform playerTransform;
    private float office6thStayTimer = 0f;
    private bool office6thFirstShown = false;
    private bool office6thSecondShown = false;
    
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
    }
    
    void Start()
    {
        // Hide prompt panel at start
        if (promptPanel != null)
            promptPanel.SetActive(false);

        ResolveOffice6thReferences();
    }
    
    void Update()
    {
        // Handle first time prompts with X button
        if (showingFirstTimePrompt && InputBridge.GetKeyDown(KeyCode.X))
        {
            ShowNextFirstTimePrompt();
        }

        UpdateOffice6thStayPrompts();
    }

    void ResolveOffice6thReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        
        GameObject zoneObj = GameObject.Find("Zone_Office_6thFloor");
        if (zoneObj == null) return;
        office6thZoneCollider = zoneObj.GetComponent<Collider>();
    }

    void UpdateOffice6thStayPrompts()
    {
        if (!enableOffice6thFloorPrompts) return;
        if (playerTransform == null || office6thZoneCollider == null)
        {
            // Late resolve in case objects load later.
            ResolveOffice6thReferences();
            return;
        }

        bool isInside = office6thZoneCollider.bounds.Contains(playerTransform.position);
        if (!isInside)
        {
            office6thStayTimer = 0f;
            office6thFirstShown = false;
            office6thSecondShown = false;
            return;
        }

        office6thStayTimer += Time.deltaTime;

        if (!office6thFirstShown && office6thStayTimer >= office6thFirstPromptDelay)
        {
            office6thFirstShown = true;
            ShowCustomPrompt(office6thPrompt1, 2.6f);
        }

        float secondAt = office6thFirstPromptDelay + office6thSecondPromptAfterFirstDelay;
        if (office6thFirstShown && !office6thSecondShown && office6thStayTimer >= secondAt)
        {
            office6thSecondShown = true;
            ShowCustomPrompt(office6thPrompt2, 2.6f);
        }
    }
    
    public void OnGhostSpawned()
    {
        if (isFirstGhostSpawn)
        {
            StartFirstTimePrompts();
        }
        else
        {
            ShowRandomPrompt();
        }
    }
    
    void StartFirstTimePrompts()
    {
        showingFirstTimePrompt = true;
        currentPromptIndex = 0;
        
        // Freeze game
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();
        
        // Stop ghost temporarily
        GhostAI[] ghosts = FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
        foreach (GhostAI ghost in ghosts)
        {
            ghost.enabled = false;
        }
        
        // Show first prompt
        if (promptPanel != null && promptText != null)
        {
            promptPanel.SetActive(true);
            promptText.text = firstTimePrompts[0];
        }
        
        Log("First ghost spawn - showing intro prompts");
    }
    
    void ShowNextFirstTimePrompt()
    {
        currentPromptIndex++;
        
        if (currentPromptIndex < firstTimePrompts.Length)
        {
            // Show next prompt
            if (promptText != null)
            {
                promptText.text = firstTimePrompts[currentPromptIndex];
            }
        }
        else
        {
            // All prompts shown - end first time sequence
            EndFirstTimePrompts();
        }
    }
    
    void EndFirstTimePrompts()
    {
        showingFirstTimePrompt = false;
        isFirstGhostSpawn = false; // Mark that first spawn is done
        
        // Hide panel
        if (promptPanel != null)
            promptPanel.SetActive(false);
        
        // Unfreeze game
        if (GameManager.Instance != null)
            GameManager.Instance.EndInteraction();
        
        // Re-enable ghost
        GhostAI[] ghosts = FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
        foreach (GhostAI ghost in ghosts)
        {
            ghost.enabled = true;
        }
        
        Log("First time prompts complete - game resumes");
    }
    
    void ShowRandomPrompt()
    {
        // Cancel any existing random prompt
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        
        // Pick random prompt
        int randomIndex = Random.Range(0, randomPrompts.Length);
        string selectedPrompt = randomPrompts[randomIndex];
        
        // Show prompt without pausing game
        activeCoroutine = StartCoroutine(ShowPromptBriefly(selectedPrompt));
    }
    
    IEnumerator ShowPromptBriefly(string message)
    {
        if (promptPanel != null && promptText != null)
        {
            promptText.text = message;
            promptPanel.SetActive(true);
            
            // Show for 2.5 seconds without pausing game
            yield return new WaitForSeconds(2.5f);
            
            // Only hide if we're not showing first time prompts
            if (!showingFirstTimePrompt)
            {
                promptPanel.SetActive(false);
            }
        }
        
        activeCoroutine = null;
    }
    
    // Call this to reset first spawn (for testing)
    public void ResetFirstSpawn()
    {
        isFirstGhostSpawn = true;
    }
    
    // Hide prompt (called by other managers)
    public void ShowDeathPrompt()
    {
        ShowDeathPrompt("THE GHOST FOUND YOU!");
    }
    
    public void ShowDeathPrompt(string message)
    {
        ShowCustomPrompt(message, 5f);
    }
    
    public void HideNearbyPrompt()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
        
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    // Show a custom nearby prompt for a short duration.
    // Does not interrupt the first-ghost multi-step prompt sequence.
    public void ShowCustomPrompt(string message, float duration = 2.5f)
    {
        if (showingFirstTimePrompt) return;
        if (string.IsNullOrEmpty(message)) return;

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        activeCoroutine = StartCoroutine(ShowCustomPromptRoutine(message, duration));
    }

    IEnumerator ShowCustomPromptRoutine(string message, float duration)
    {
        if (promptPanel != null && promptText != null)
        {
            promptText.text = message;
            promptPanel.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(0.1f, duration));

            if (!showingFirstTimePrompt)
            {
                promptPanel.SetActive(false);
            }
        }

        activeCoroutine = null;
    }

    void Log(string message)
    {
        if (enableDebugLogs || GlobalDebugSettings.EnableAllLogs)
        {
            Debug.Log(message);
        }
    }
}

