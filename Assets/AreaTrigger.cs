using UnityEngine;

public class AreaTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string triggerMessage = "There is a strange feeling in this air...";
    public float cooldownTime = 30f; // Seconds before showing again
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private bool canTrigger = true;
    private float cooldownTimer = 0f;
    private bool playerInTrigger = false;
    
    void Update()
    {
        // Countdown cooldown
        if (!canTrigger)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                canTrigger = true;
                cooldownTimer = 0f;
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && canTrigger && !playerInTrigger)
        {
            playerInTrigger = true;
            
            // Show the message (no arrow - single message)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue(triggerMessage, false, false);
            }
            
            // Close message after 2.5 seconds
            Invoke("CloseMessage", 2.5f);
            
            // Start cooldown
            canTrigger = false;
            cooldownTimer = cooldownTime;
            
            Log("Area trigger activated! Cooldown started: " + cooldownTime + " seconds");
        }
    }
    
    void CloseMessage()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw the trigger area in editor
        Gizmos.color = Color.magenta;
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.DrawWireCube(transform.position + box.center, box.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(5f, 3f, 5f));
        }
    }

    void Log(string message)
    {
        if (enableDebugLogs || GlobalDebugSettings.EnableAllLogs)
        {
            Debug.Log(message);
        }
    }
}

