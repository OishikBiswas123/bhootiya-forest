using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public bool isInteracting = false;
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
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
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public void StartInteraction()
    {
        isInteracting = true;
        Log("Interaction started - Player and Ghost frozen");
    }
    
    public void EndInteraction()
    {
        isInteracting = false;
        Log("GameManager: Interaction ended - isInteracting=" + isInteracting);
    }

    void Log(string message)
    {
        if (enableDebugLogs || GlobalDebugSettings.EnableAllLogs)
        {
            Debug.Log(message);
        }
    }
}

