using UnityEngine;

public class RandomDialogueNPC : MonoBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Random NPC";
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.X;
    
    [Header("Random Dialogues - 2 lines at a time")]
    [TextArea(2, 3)]
    public string[] dialogue1 = new string[]
    {
        "I once wrote code day and night,\nDebugging bugs till morning light.",
        "Now I twist jalebis, hot and sweet,\nLife turned my keyboard into a treat."
    };
    
    [TextArea(2, 3)]
    public string[] dialogue2 = new string[]
    {
        "I used to type with coffee in hand,\nFixing software across the land.",
        "But the job said, \"Goodbye, my friend,\"\nSo now I fry jalebis round the bend."
    };
    
    [TextArea(2, 3)]
    public string[] dialogue3 = new string[]
    {
        "From coding apps with logic so tight,\nTo frying jalebis golden and bright.",
        "Career went down, sugar went high,\nAt least my sweets will never crash or die."
    };
    
    [TextArea(2, 3)]
    public string[] dialogue4 = new string[]
    {
        "Earlier I coded, bugs in a line,\nNow I make jalebis—perfectly fine.",
        "Software failed, but syrup stayed true,\nNow every spiral is sweeter than blue screens too."
    };
    
    private string[][] allDialogues;
    
    private Transform player;
    private bool playerInRange = false;
    private bool isInteracting = false;
    private int currentDialogueIndex = 0;
    private int currentLineIndex = 0;
    private int dialogueCounter = 0; // Tracks which dialogue to show (0,1,2,3, then loops)
    private int dialogueStep = 0; // 0=question, 1=pause, 2=first lines, 3=second lines, 4=done
    
    void Start()
    {
        // Store all dialogues in array
        allDialogues = new string[][]
        {
            dialogue1,
            dialogue2,
            dialogue3,
            dialogue4
        };
        
        // Find player
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
            }
        }
    }
    
    void HandleInteraction()
    {
        if (!isInteracting)
        {
            StartDialogue();
        }
        else
        {
            AdvanceDialogue();
        }
    }
    
    void StartDialogue()
    {
        isInteracting = true;
        
        // Use dialogueCounter to show dialogues in order, then loop back
        currentDialogueIndex = dialogueCounter % allDialogues.Length;
        currentLineIndex = 0;
        dialogueStep = 0; // Start with question
        
        // Increase counter for next time
        dialogueCounter++;
        
        // Close any existing UI first
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
            UIManager.Instance.onDialogueClosed += OnUIManagerDialogueClosed;
            UIManager.Instance.onDialogueAdvance += OnUIManagerDialogueAdvance;
        }
        
        // Freeze player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartInteraction();
        }
        
        // Show the jalebi question first with arrow (there's more to come)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDialogue("Hi.. Do you want jalebies?", false, true);
        }
    }
    
    void AdvanceDialogue()
    {
        dialogueStep++;
        
        if (dialogueStep == 1)
        {
            // Show "..." pause with arrow (more to come)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue("...", false, true);
            }
        }
        else if (dialogueStep == 2)
        {
            // Show first lines with arrow (more to come)
            if (UIManager.Instance != null)
            {
                bool hasMore = allDialogues[currentDialogueIndex].Length > 1;
                UIManager.Instance.ShowDialogue(allDialogues[currentDialogueIndex][0], false, hasMore);
            }
        }
        else if (dialogueStep == 3)
        {
            // Show last lines - no arrow (this is the end)
            currentLineIndex++;
            if (currentLineIndex < allDialogues[currentDialogueIndex].Length)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowDialogue(allDialogues[currentDialogueIndex][currentLineIndex], false, true);
                }
            }
            else
            {
                EndDialogue();
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
        dialogueStep = 0;
        
        // Unsubscribe from dialogue callbacks
        if (UIManager.Instance != null)
        {
            UIManager.Instance.onDialogueClosed -= OnUIManagerDialogueClosed;
            UIManager.Instance.onDialogueAdvance -= OnUIManagerDialogueAdvance;
        }
        
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
    
    void OnUIManagerDialogueClosed()
    {
        if (isInteracting)
        {
            EndDialogue();
        }
    }
    
    void OnUIManagerDialogueAdvance()
    {
        if (isInteracting)
        {
            AdvanceDialogue();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
