using UnityEngine;
using System.Collections;

public class OfficeLiftController : MonoBehaviour
{
    [Header("Lift Interaction")]
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.X;

    [Header("Lift Timing")]
    public float liftArrivalDelay = 3f;   // Wait before lift arrives
    public float enterLiftSettleDelay = 1f; // Wait after entering lift
    public float floorPromptDelay = 1f;     // Additional wait before floor menu
    public float floorTravelDelay = 2f;     // Wait after selecting a floor before teleport
    public float restrictedMessageDuration = 1.5f;

    [Header("Lift Audio (Optional)")]
    public AudioSource liftAudioSource;
    public AudioClip liftArrivingClip;   // Plays while player waits for lift
    public AudioClip liftDingClip;       // Plays on enter lift and exit lift
    [Range(0.1f, 1f)] public float bgmDuckMultiplier = 0.6f;
    public float bgmDuckFadeDuration = 0.15f;
    private int activeBgmDuckRequests = 0;

    [Header("Floor Destination Points")]
    public Transform groundFloorPoint;
    public Transform firstFloorPoint;
    public Transform secondFloorPoint;

    [Header("Current Floor Index (0=Ground, 1=1st, 6=6th)")]
    [Range(0, 6)]
    public int currentFloorIndex = 0;

    [Header("Optional: Lift Entry Point")]
    public Transform liftInsidePoint;     // If assigned, player is moved here before floor selection

    private Transform player;
    private bool isProcessing = false;
    private bool playerInRange = false;
    
    private readonly string[] floorOptions = new string[]
    {
        "Ground Floor",
        "1st Floor",
        "2nd Floor",
        "3rd Floor",
        "4th Floor",
        "5th Floor",
        "6th Floor"
    };

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (player == null || isProcessing) return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionDistance;

        if (playerInRange && InputBridge.GetKeyDown(interactKey))
        {
            StartCoroutine(BeginLiftFlow());
        }
    }

    IEnumerator BeginLiftFlow()
    {
        isProcessing = true;
        GameManager.Instance?.StartInteraction();
        UIManager.Instance?.CloseDialogue();

        UIManager.Instance?.ShowDialogue("Please wait... Lift is arriving.", false, false);
        PlaySfxWithMusicDuck(liftArrivingClip);
        yield return new WaitForSeconds(liftArrivalDelay);

        if (liftInsidePoint != null && player != null)
        {
            if (FadeManager.Instance != null)
            {
                yield return FadeManager.Instance.FadeToBlack(() =>
                {
                    player.position = liftInsidePoint.position;
                });
            }
            else
            {
                player.position = liftInsidePoint.position;
            }
        }

        PlaySfxWithMusicDuck(liftDingClip);

        UIManager.Instance?.ShowDialogue("You entered the lift.", false, false);
        yield return new WaitForSeconds(enterLiftSettleDelay);

        // Face player toward camera (idle down) before showing floor options
        PlayerMove playerMove = player != null ? player.GetComponent<PlayerMove>() : null;
        if (playerMove != null)
        {
            playerMove.SetFacingDirection(1);
        }

        yield return new WaitForSeconds(floorPromptDelay);

        ShowFloorSelection();
    }

    void ShowFloorSelection()
    {
        if (UIManager.Instance == null)
        {
            EndLiftFlow();
            return;
        }

        UIManager.Instance.ShowScrollableChoices(
            "Which floor do you want to go?",
            floorOptions,
            OnFloorSelected,
            currentFloorIndex
        );
    }

    void OnFloorSelected(int index, string label)
    {
        if (index == 0)
        {
            StartCoroutine(MoveToFloor(groundFloorPoint, true));
            return;
        }
        if (index == 1)
        {
            StartCoroutine(MoveToFloor(firstFloorPoint, true));
            return;
        }
        if (index == 2)
        {
            if (UIManager.Instance != null)
                StartCoroutine(ShowRestrictedMessageThenReturn(label));
            return;
        }
        if (index == 6)
        {
            StartCoroutine(MoveToFloor(secondFloorPoint, false));
            return;
        }
        
        if (UIManager.Instance != null)
            StartCoroutine(ShowRestrictedMessageThenReturn(label));
    }

    IEnumerator ShowRestrictedMessageThenReturn(string label)
    {
        UIManager.Instance?.ShowDialogue("You cannot go to " + label + " right now. A meeting is in progress.", false, false);
        yield return new WaitForSeconds(restrictedMessageDuration);
        ShowFloorSelection();
    }

    IEnumerator MoveToFloor(Transform destination, bool faceDownAfterTeleport)
    {
        UIManager.Instance?.CloseDialogue();

        if (destination == null || player == null)
        {
            UIManager.Instance?.ShowDialogue("Destination not set for this floor.", false, false);
            EndLiftFlow();
            yield break;
        }

        float travelHold = floorTravelDelay;
        if (liftArrivingClip != null && liftArrivingClip.length > 0f)
        {
            travelHold = liftArrivingClip.length;
        }

        // Keep BGM ducked continuously from movement sound until ding completes.
        BeginBgmDuck();
        PlaySfxNoDuck(liftArrivingClip);

        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeToBlackWithHold(() =>
            {
                player.position = destination.position;
            }, travelHold);
        }
        else
        {
            yield return new WaitForSeconds(Mathf.Max(0f, travelHold));
            player.position = destination.position;
        }

        if (faceDownAfterTeleport)
        {
            PlayerMove playerMove = player != null ? player.GetComponent<PlayerMove>() : null;
            if (playerMove != null)
            {
                playerMove.SetFacingDirection(1);
            }
        }

        PlaySfxNoDuck(liftDingClip);
        float dingWait = (liftDingClip != null && liftDingClip.length > 0f) ? liftDingClip.length : 0.8f;
        yield return new WaitForSeconds(dingWait);
        EndBgmDuck();

        EndLiftFlow();
    }

    void EndLiftFlow()
    {
        isProcessing = false;
        GameManager.Instance?.EndInteraction();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        if (groundFloorPoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(groundFloorPoint.position, 0.3f);
        }

        if (firstFloorPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(firstFloorPoint.position, 0.3f);
        }

        if (secondFloorPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(secondFloorPoint.position, 0.3f);
        }
    }

    void PlaySfxWithMusicDuck(AudioClip clip)
    {
        if (clip == null) return;
        StartCoroutine(PlaySfxWithMusicDuckRoutine(clip));
    }

    void PlaySfxNoDuck(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource src = liftAudioSource != null ? liftAudioSource : GetComponent<AudioSource>();
        if (src == null) return;
        src.PlayOneShot(clip, AudioSettingsManager.GetSfxMultiplier());
    }

    IEnumerator PlaySfxWithMusicDuckRoutine(AudioClip clip)
    {
        AudioSource src = liftAudioSource != null ? liftAudioSource : GetComponent<AudioSource>();
        if (src == null) yield break;

        activeBgmDuckRequests++;
        AreaMusicManager.Instance?.SetExternalVolumeMultiplier(bgmDuckMultiplier);

        src.PlayOneShot(clip, AudioSettingsManager.GetSfxMultiplier());
        float wait = clip.length > 0f ? clip.length : 0.8f;
        yield return new WaitForSeconds(wait);

        activeBgmDuckRequests = Mathf.Max(0, activeBgmDuckRequests - 1);
        if (activeBgmDuckRequests == 0)
        {
            if (bgmDuckFadeDuration <= 0f)
            {
                AreaMusicManager.Instance?.SetExternalVolumeMultiplier(1f);
            }
            else
            {
                float t = 0f;
                while (t < bgmDuckFadeDuration)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / bgmDuckFadeDuration);
                    float mul = Mathf.Lerp(bgmDuckMultiplier, 1f, k);
                    AreaMusicManager.Instance?.SetExternalVolumeMultiplier(mul);
                    yield return null;
                }

                AreaMusicManager.Instance?.SetExternalVolumeMultiplier(1f);
            }
        }
    }

    void BeginBgmDuck()
    {
        activeBgmDuckRequests++;
        AreaMusicManager.Instance?.SetExternalVolumeMultiplier(bgmDuckMultiplier);
    }

    void EndBgmDuck()
    {
        activeBgmDuckRequests = Mathf.Max(0, activeBgmDuckRequests - 1);
        if (activeBgmDuckRequests == 0)
        {
            StartCoroutine(RestoreBgmAfterDuck());
        }
    }

    IEnumerator RestoreBgmAfterDuck()
    {
        if (bgmDuckFadeDuration <= 0f)
        {
            AreaMusicManager.Instance?.SetExternalVolumeMultiplier(1f);
            yield break;
        }

        float t = 0f;
        while (t < bgmDuckFadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / bgmDuckFadeDuration);
            float mul = Mathf.Lerp(bgmDuckMultiplier, 1f, k);
            AreaMusicManager.Instance?.SetExternalVolumeMultiplier(mul);
            yield return null;
        }

        AreaMusicManager.Instance?.SetExternalVolumeMultiplier(1f);
    }
}
