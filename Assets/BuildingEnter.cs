using UnityEngine;

public class BuildingEnter : MonoBehaviour
{
    [Header("Building Settings")]
    public string buildingName = "House";
    public GameObject indoorObjects;
    public bool playerIsInside = false;
    
    [Header("Spawn Points")]
    public Transform indoorSpawnPoint;
    public Transform outdoorSpawnPoint;
    public Transform indoorDoorTrigger;
    public Transform outdoorDoorTrigger;
    
    [Header("Interaction")]
    public float interactionDistance = 2f;
    public float postTeleportCooldown = 0.3f;
    
    [Header("Audio (Optional)")]
    public AudioSource transitionAudioSource;
    
    [Header("Grandma Dialogue (Optional)")]
    public bool markGrandmaFromOutsideOnEnter = false;
    
    private Transform player;
    private bool isTeleporting = false;
    private bool wasInsideDoorRange = false;
    private bool wasOutsideDoorRange = false;
    private float nextCheckAllowedTime = 0f;

    void Awake()
    {
        // Safety: this script should live on the building/root door controller object.
        // If it gets attached on the indoor trigger marker by mistake, disable it to avoid double-teleport bugs.
        if (indoorDoorTrigger == transform && outdoorDoorTrigger != transform)
        {
            Debug.LogWarning("BuildingEnter disabled on trigger marker object: " + gameObject.name);
            enabled = false;
            return;
        }
    }
    
    void Start()
    {
        if (!enabled) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }
    
    void Update()
    {
        if (player == null || isTeleporting) return;
        if (Time.time < nextCheckAllowedTime) return;
        
        if (playerIsInside)
        {
            // Check for exit (indoor door)
            if (indoorDoorTrigger != null)
            {
                float indoorDist = Vector3.Distance(indoorDoorTrigger.position, player.position);
                bool inRange = indoorDist <= interactionDistance;
                
                if (inRange && !wasInsideDoorRange)
                {
                    if (GrandmaDialogueContext.PendingUpstairsCheckBeforeLeaving && GrandmaHomeDialogue.Instance != null)
                    {
                        // Pause exit and force grandma's downstairs reminder first.
                        if (GrandmaHomeDialogue.Instance.ForceStartUpstairsDialogue(true))
                        {
                            wasInsideDoorRange = true;
                            return;
                        }
                    }

                    ExitBuilding();
                }

                wasInsideDoorRange = inRange;
                wasOutsideDoorRange = false;
            }
        }
        else
        {
            // Check for enter (outdoor door if assigned, else object position)
            Vector3 outdoorCheckPos = (outdoorDoorTrigger != null) ? outdoorDoorTrigger.position : transform.position;
            float outdoorDist = Vector3.Distance(outdoorCheckPos, player.position);
            bool inRange = outdoorDist <= interactionDistance;
            
            if (inRange && !wasOutsideDoorRange)
            {
                EnterBuilding();
            }

            wasOutsideDoorRange = inRange;
            wasInsideDoorRange = false;
        }
    }
    
    void EnterBuilding()
    {
        if (isTeleporting) return;
        PlayTransitionSfx();
        StartCoroutine(EnterBuildingRoutine());
    }
    
    void ExitBuilding()
    {
        if (isTeleporting) return;

        // Hard gate: if downstairs grandma reminder is pending, do NOT let player exit yet.
        // This guarantees the stop happens inside before teleporting outside.
        if (playerIsInside && GrandmaDialogueContext.PendingUpstairsCheckBeforeLeaving)
        {
            if (GrandmaHomeDialogue.Instance != null)
            {
                if (GrandmaHomeDialogue.Instance.ForceStartUpstairsDialogue(true))
                {
                    wasInsideDoorRange = true;
                    nextCheckAllowedTime = Time.time + 0.15f;
                    return;
                }
            }
            
            // If grandma instance is missing for any reason, block exit to avoid wrong outside teleport.
            return;
        }

        PlayTransitionSfx();
        StartCoroutine(ExitBuildingRoutine());
    }
    
    void PlayTransitionSfx()
    {
        AudioSource src = transitionAudioSource != null ? transitionAudioSource : GetComponent<AudioSource>();
        if (src == null) return;
        if (src.clip == null) return;
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.ignoreListenerPause = true;
        if (src.isPlaying)
        {
            src.Stop();
        }
        src.PlayOneShot(src.clip, AudioSettingsManager.GetSfxMultiplier());
    }
    
    System.Collections.IEnumerator EnterBuildingRoutine()
    {
        isTeleporting = true;
        
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();
        
        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeToBlack(() => {
                playerIsInside = true;
                GrandmaDialogueContext.SetFromOutside();
                
                if (indoorSpawnPoint != null && player != null)
                {
                    Vector3 newPos = indoorSpawnPoint.position;
                    newPos.y = player.position.y;
                    player.position = newPos;
                }
            });
        }
        else
        {
            playerIsInside = true;
            GrandmaDialogueContext.SetFromOutside();
            
            if (indoorSpawnPoint != null && player != null)
            {
                Vector3 newPos = indoorSpawnPoint.position;
                newPos.y = player.position.y;
                player.position = newPos;
            }
        }
        
        // Prevent immediate bounce and keep enter/exit latches independent.
        nextCheckAllowedTime = Time.time + postTeleportCooldown;
        wasOutsideDoorRange = false;
        if (player != null && indoorDoorTrigger != null)
        {
            wasInsideDoorRange = Vector3.Distance(indoorDoorTrigger.position, player.position) <= interactionDistance;
        }
        else
        {
            wasInsideDoorRange = false;
        }
        
        if (GameManager.Instance != null)
            GameManager.Instance.EndInteraction();
        
        isTeleporting = false;
    }
    
    System.Collections.IEnumerator ExitBuildingRoutine()
    {
        // Extra safeguard: never allow outside teleport while grandma check is pending.
        if (playerIsInside && GrandmaDialogueContext.PendingUpstairsCheckBeforeLeaving)
        {
            if (GrandmaHomeDialogue.Instance != null)
            {
                GrandmaHomeDialogue.Instance.ForceStartUpstairsDialogue(true);
            }
            isTeleporting = false;
            yield break;
        }

        isTeleporting = true;
        
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();
        
        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeToBlack(() => {
                playerIsInside = false;
                
                if (outdoorSpawnPoint != null && player != null)
                {
                    Vector3 newPos = outdoorSpawnPoint.position;
                    newPos.y = player.position.y;
                    player.position = newPos;
                }
            });
        }
        else
        {
            playerIsInside = false;
            
            if (outdoorSpawnPoint != null && player != null)
            {
                Vector3 newPos = outdoorSpawnPoint.position;
                newPos.y = player.position.y;
                player.position = newPos;
            }
        }
        
        // Prevent immediate bounce and keep enter/exit latches independent.
        nextCheckAllowedTime = Time.time + postTeleportCooldown;
        wasInsideDoorRange = false;
        if (player != null)
        {
            Vector3 outdoorCheckPos = (outdoorDoorTrigger != null) ? outdoorDoorTrigger.position : transform.position;
            wasOutsideDoorRange = Vector3.Distance(outdoorCheckPos, player.position) <= interactionDistance;
        }
        else
        {
            wasOutsideDoorRange = false;
        }
        
        if (GameManager.Instance != null)
            GameManager.Instance.EndInteraction();
        
        isTeleporting = false;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        if (indoorSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(indoorSpawnPoint.position, 0.5f);
        }
        
        if (outdoorSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(outdoorSpawnPoint.position, 0.5f);
        }
        
        if (indoorDoorTrigger != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(indoorDoorTrigger.position, interactionDistance);
        }
        
        if (outdoorDoorTrigger != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(outdoorDoorTrigger.position, interactionDistance);
        }
    }
}
