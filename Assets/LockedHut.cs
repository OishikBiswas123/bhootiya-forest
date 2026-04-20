using UnityEngine;

public class LockedHut : MonoBehaviour
{
    [Header("Settings")]
    public float interactionDistance = 4f;
    public KeyCode interactKey = KeyCode.X;
    [TextArea(2, 3)]
    public string lockedMessage = "The door is locked from inside.";
    
    private Transform player;
    private bool playerInRange = false;
    private bool isShowingMessage = false;
    
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
            if (!playerInRange)
            {
                playerInRange = true;
            }
            
            // Only show message when X is pressed
            if (InputBridge.GetKeyDown(interactKey))
            {
                if (isShowingMessage)
                {
                    HidePrompt();
                }
                else
                {
                    ShowPrompt();
                }
            }
        }
        else
        {
            // Player left the area - close message if open
            if (playerInRange && isShowingMessage)
            {
                HidePrompt();
            }
            playerInRange = false;
        }
    }
    
    void ShowPrompt()
    {
        isShowingMessage = true;
        
        // Freeze player
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();
        
        // Show with hasMore=true so arrow shows and X works with delay to prevent accidental close
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDialogue(lockedMessage, false, true);
        }
    }
    
    void HidePrompt()
    {
        isShowingMessage = false;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }
        
        // Unfreeze player
        if (GameManager.Instance != null)
            GameManager.Instance.EndInteraction();
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
