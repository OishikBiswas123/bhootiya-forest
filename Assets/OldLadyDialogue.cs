using UnityEngine;

public class OldLadyDialogue : MonoBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Old Lady";
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.X;
    
    [Header("Dialogue Lines - 1 at a time")]
    [TextArea(2, 3)]
    public string[] dialogueLines = new string[]
    {
        "Child… don't go too deep into that forest.",
        "There's a place inside where even the wind feels wrong.",
        "They say a restless spirit lives there…",
        "Always watching, always waiting.",
        "And there's a strange force in those woods.",
        "It doesn't scare you away… It slowly pulls you in.",
        "Many walked in, thinking they'd return…",
        "But the forest swallowed them whole.",
        "So stay on the safe paths, child…",
        "And never go deeper than you should."
    };
    
    private Transform player;
    private bool playerInRange = false;
    private bool isInteracting = false;
    private int currentLineIndex = 0;
    
    void Start()
    {
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
        
        // Skip if Game Info panel is showing
        if (UIManager.Instance != null && UIManager.Instance.IsGameInfoActive()) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= interactionDistance)
        {
            if (!playerInRange && !isInteracting)
            {
                playerInRange = true;
            }
            
            if (InputBridge.GetKeyDown(interactKey))
            {
                HandleInteraction();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                if (!isInteracting)
                {
                    // No prompt to hide
                }
            }
        }
    }
    
    void HandleInteraction()
    {
        if (!isInteracting)
        {
            // Start dialogue
            StartDialogue();
        }
        else
        {
            // Continue to next line
            AdvanceDialogue();
        }
    }
    
    void StartDialogue()
    {
        isInteracting = true;
        currentLineIndex = 0;
        
        // Close any existing UI first
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }
        
        // Freeze player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartInteraction();
        }
        
        // Show first line with arrow (there are more lines)
        if (UIManager.Instance != null)
        {
            bool hasMore = dialogueLines.Length > 1;
            UIManager.Instance.ShowDialogue(dialogueLines[0], false, hasMore);
        }
    }
    
    void AdvanceDialogue()
    {
        currentLineIndex++;
        
        if (currentLineIndex < dialogueLines.Length)
        {
            // Show next line - check if it's the last one
            bool hasMore = (currentLineIndex < dialogueLines.Length - 1);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue(dialogueLines[currentLineIndex], false, hasMore);
            }
        }
        else
        {
            // End dialogue
            EndDialogue();
        }
    }
    
    void EndDialogue()
    {
        isInteracting = false;
        currentLineIndex = 0;
        
        // Hide dialogue
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }
        
        // Unfreeze player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndInteraction();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
