using UnityEngine;

public class FruitTreeNPCDialogue : MonoBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Fruit NPC";
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.X;
    public float inputCooldown = 0.3f;
    private float lastInputTime = 0f;

    [Header("Dialogue Lines - One line per click")]
    [TextArea(2, 3)]
    public string[] dialogueLines = new string[]
    {
        "The tree behind me has juicy, tasty fruit.",
        "If you can reach it, you may eat and enjoy it."
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
                if (Time.time - lastInputTime < inputCooldown) return;
                lastInputTime = Time.time;
                
                HandleInteraction();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                if (isInteracting)
                {
                    EndDialogue();
                }
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
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        isInteracting = true;
        currentLineIndex = 0;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartInteraction();
        }

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
            bool hasMore = currentLineIndex < dialogueLines.Length - 1;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue(dialogueLines[currentLineIndex], false, hasMore);
            }
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isInteracting = false;
        currentLineIndex = 0;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndInteraction();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
