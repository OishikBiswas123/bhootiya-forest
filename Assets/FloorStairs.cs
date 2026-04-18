using UnityEngine;
using System.Collections;

public class FloorStairs : MonoBehaviour
{
    [Header("Stairs Settings")]
    public string floorName = "Upstairs";
    public Transform destinationPoint;
    public GameObject currentFloor;
    public GameObject destinationFloor;
    public bool goingUp = true;
    
    [Header("Audio (Optional)")]
    public AudioSource transitionAudioSource;
    
    [Header("Grandma Dialogue (Optional)")]
    public bool markGrandmaFromUpstairs = false;
    
    private Transform player;
    private bool isTeleporting = false;
    
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTeleporting && destinationPoint != null)
        {
            StartCoroutine(TeleportToFloor());
        }
    }
    
    IEnumerator TeleportToFloor()
    {
        isTeleporting = true;
        PlayTransitionSfx();
        
        // Freeze player
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();
        
        // Fade to black, switch floors, fade back
        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeToBlack(() => {
                if (player != null)
                {
                    Vector3 newPos = destinationPoint.position;
                    newPos.y = player.position.y;
                    player.position = newPos;
                    
                    PlayerMove pm = player.GetComponent<PlayerMove>();
                    if (pm != null)
                        pm.SetFacingDirection(3); // Face left after stair landing
                    
                    if (markGrandmaFromUpstairs)
                        GrandmaDialogueContext.SetFromUpstairs();
                }
            });
        }
        else
        {
            if (player != null)
            {
                Vector3 newPos = destinationPoint.position;
                newPos.y = player.position.y;
                player.position = newPos;
                
                PlayerMove pm = player.GetComponent<PlayerMove>();
                if (pm != null)
                    pm.SetFacingDirection(3); // Face left after stair landing
                
                if (markGrandmaFromUpstairs)
                    GrandmaDialogueContext.SetFromUpstairs();
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        // Unfreeze player
        if (GameManager.Instance != null)
            GameManager.Instance.EndInteraction();
        
        isTeleporting = false;
    }
    
    void PlayTransitionSfx()
    {
        AudioSource src = transitionAudioSource != null ? transitionAudioSource : GetComponent<AudioSource>();
        if (src == null) return;
        if (src.clip == null) return;
        src.PlayOneShot(src.clip, AudioSettingsManager.GetSfxMultiplier());
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = goingUp ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(2f, 0.5f, 2f));
        
        if (destinationPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destinationPoint.position);
            Gizmos.DrawWireSphere(destinationPoint.position, 0.5f);
        }
    }
}
