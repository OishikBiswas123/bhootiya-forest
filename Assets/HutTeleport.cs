using UnityEngine;
using System.Collections;

public class HutTeleport : MonoBehaviour
{
    public float interactionDistance = 5f;
    public KeyCode interactKey = KeyCode.X;
    public int hutID = 1;
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    [Header("Destination")]
    public Transform destinationPoint;
    
    private Transform player;
    private bool playerInRange = false;
    private int hut1Phase = 0;
    [Header("Screen Timing")]
    public float gameBeginScreenMinDuration = 3f;
    [Header("Intro Prompt Timing")]
    public float firstForestPromptLockSeconds = 2f;
    [Header("Input Timing")]
    public float interactCooldownAfterChoice = 0.25f;
    
    // Forest intro prompts
    private string[] forestIntroPrompts = new string[]
    {
        "You have been pulled here by a mysterious force…",
        "This is a deep, haunted forest.",
        "The hut behind you is locked.",
        "Somewhere in the forest lies the lost key.",
        "Find it… and be careful of what lurks nearby."
    };
    private int forestPromptIndex = -1; // -1 = not started yet
    private float nextInteractAllowedTime = 0f;
    private bool isTeleporting = false;
    private float forestPromptInputUnlockTime = 0f;
    
    void Start()
    {
        hut1Phase = 0;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }
    
    void Update()
    {
        if (player == null) return;
        if (isTeleporting) return;
        
        // Handle forest intro prompts
        if (hutID == 1 && forestPromptIndex >= 0)
        {
            if (Time.time >= forestPromptInputUnlockTime && InputBridge.GetKeyDown(interactKey))
            {
                HandleForestIntro();
            }
            return;
        }
        
        // Skip the showingGameBegins check - it's now automatic
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= interactionDistance)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowTeleportPrompt();
            }
            
            if (Time.time >= nextInteractAllowedTime && InputBridge.GetKeyDown(interactKey))
            {
                TryTeleport();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                HideTeleportPrompt();
            }
        }
    }
    
    void HandleForestIntro()
    {
        if (isTeleporting) return;

        // Close current dialogue first
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }
        
        // Prevent stacked delayed calls from rapid input
        CancelInvoke("ShowNextPrompt");
        
        forestPromptIndex++;
        
        if (forestPromptIndex < forestIntroPrompts.Length)
        {
            // Show next prompt after a brief delay
            Invoke("ShowNextPrompt", 0.1f);
        }
        else
        {
            // All prompts shown - show game begins screen for 3 seconds then auto-start
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ShowGameBeginsScreen();
            }
            
            // Keep game-begins screen at least as long as the SFX (3s by default).
            Invoke("AutoStartGame", Mathf.Max(3f, gameBeginScreenMinDuration));
        }
    }
    
    void ShowNextPrompt()
    {
        if (forestPromptIndex < forestIntroPrompts.Length && UIManager.Instance != null)
        {
            bool hasMore = true;
            UIManager.Instance.ShowDialogue(forestIntroPrompts[forestPromptIndex], false, hasMore);
        }
    }
    
    void AutoStartGame()
    {
        Log("AutoStartGame CALLED!");
        
        forestPromptIndex = -1;
        
        // Hide game begins screen
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.HideGameBeginsScreen();
        }
        
        // Unfreeze player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndInteraction();
        }
        
        // Start the actual game (timer + ghost spawning)
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartActualGame();
        }
        
        Log("AutoStartGame COMPLETE!");
    }
    
    // Legacy method - not used anymore but kept for compatibility
    void StartActualGame()
    {
        AutoStartGame();
    }
    
    void ShowTeleportPrompt()
    {
        if (hutID == 1)
        {
            hut1Phase = 0;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue("You feel a mysterious energy near the hut.", false, false);
            }
        }
        else
        {
            if (KeyFoundManager.Instance != null && KeyFoundManager.Instance.HasKey())
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowDialogue("You have the KEY! Press X to unlock and escape!", false, false);
                }
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowDialogue("The door is locked. You need a KEY to escape. Find it in the forest!", false, false);
                }
            }
        }
    }
    
    void HideTeleportPrompt()
    {
        // Don't reset during forest intro sequence
        if (forestPromptIndex >= 0)
        {
            return;
        }
        
        hut1Phase = 0;
        forestPromptIndex = -1;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }
    }
    
    void TryTeleport()
    {
        if (hutID == 1)
        {
            if (hut1Phase == 0)
            {
                hut1Phase = 1;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowDialogue("The air around this hut feels... unsettling.", false, true);
                }
            }
            else if (hut1Phase == 1)
            {
                hut1Phase = 0;
                TeleportPlayer();
                
                // DON'T show prompts here - they will be shown after teleport effect completes
            }
        }
        else
        {
            if (KeyFoundManager.Instance != null && KeyFoundManager.Instance.HasKey())
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowDialogueWithChoices2Button("Do you want to use the KEY you found in the forest to escape?", OnYesUseKey, OnNoUseKey);
                }
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowDialogue("The door won't budge. Find the KEY first!", true, false);
                }
            }
        }
    }
    
    void TeleportPlayer()
    {
        if (destinationPoint != null && player != null)
        {
            StartCoroutine(DoTeleportWithEffect());
        }
    }

    public void StartForestLandingOnly()
    {
        if (destinationPoint == null || player == null) return;
        StartCoroutine(DoTeleportInOnly());
    }
    
    IEnumerator DoTeleportWithEffect()
    {
        isTeleporting = true;
        nextInteractAllowedTime = Time.time + 10f;
        CancelInvoke("ShowNextPrompt");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }

        TeleportEffect effect = player.GetComponent<TeleportEffect>();
        if (effect == null)
        {
            effect = player.gameObject.AddComponent<TeleportEffect>();
        }
        
        // Teleport out
        yield return effect.TeleportOut(() => { });
        
        // Move to destination (invisible)
        
        // Teleport in
        yield return effect.TeleportIn(destinationPoint.position, () => { });
        
        Log("Teleported to: " + destinationPoint.name);
        
        // NOW show forest intro prompts (after landing)
        if (hutID == 1)
        {
            // Freeze player during forest intro
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartInteraction();
            }
            
            // Start forest intro prompts
            forestPromptIndex = 0;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue(forestIntroPrompts[0], false);
            }
            forestPromptInputUnlockTime = Time.time + Mathf.Max(0f, firstForestPromptLockSeconds);
        }

        isTeleporting = false;
        nextInteractAllowedTime = Time.time + 0.25f;
    }

    IEnumerator DoTeleportInOnly()
    {
        isTeleporting = true;
        nextInteractAllowedTime = Time.time + 10f;
        CancelInvoke("ShowNextPrompt");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }

        TeleportEffect effect = player.GetComponent<TeleportEffect>();
        if (effect == null)
        {
            effect = player.gameObject.AddComponent<TeleportEffect>();
        }

        yield return effect.TeleportIn(destinationPoint.position, () => { });

        Log("Teleported (landing only) to: " + destinationPoint.name);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartInteraction();
        }

        forestPromptIndex = 0;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDialogue(forestIntroPrompts[0], false);
        }
        forestPromptInputUnlockTime = Time.time + Mathf.Max(0f, firstForestPromptLockSeconds);

        isTeleporting = false;
        nextInteractAllowedTime = Time.time + 0.25f;
    }
    
    void OnYesUseKey()
    {
        nextInteractAllowedTime = Time.time + interactCooldownAfterChoice;

        // Close prompt/choices first so UI doesn't remain visible during teleport.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }

        // Use same landing destination as game-over flow.
        Transform homeDestination = GetGameOverLandingDestination();
        
        if (homeDestination != null)
        {
            StartCoroutine(DoVictoryTeleport(homeDestination.position));
        }
        else
        {
            // Fallback to instant teleport
            TeleportPlayer();
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.WinGame();
            }
        }
    }
    
    Transform GetGameOverLandingDestination()
    {
        HutTeleport[] huts = FindObjectsByType<HutTeleport>(FindObjectsSortMode.None);

        // Match GameFlowManager.RespawnAtHome logic:
        // use hutID==2 destinationPoint as final home landing.
        foreach (HutTeleport hut in huts)
        {
            if (hut.hutID == 2 && hut.destinationPoint != null)
            {
                return hut.destinationPoint;
            }
        }
        return null;
    }
    
    IEnumerator DoVictoryTeleport(Vector3 targetPos)
    {
        // Freeze player
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();
        
        TeleportEffect effect = player.GetComponent<TeleportEffect>();
        if (effect == null)
        {
            effect = player.gameObject.AddComponent<TeleportEffect>();
        }
        
        // Teleport out
        yield return effect.TeleportOut(() => { });
        
        // Teleport in at home
        yield return effect.TeleportIn(targetPos, () => { });
        
        // Now show victory
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.WinGame();
        }
    }
    
    void OnNoUseKey()
    {
        nextInteractAllowedTime = Time.time + interactCooldownAfterChoice;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }
        Log("Player chose not to use key yet");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }

    void Log(string message)
    {
        if (enableDebugLogs || GlobalDebugSettings.EnableAllLogs)
        {
            Debug.Log(message);
        }
    }
}

