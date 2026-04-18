using UnityEngine;
using System.Collections;

public class HospitalGateTrigger : MonoBehaviour
{
    [Header("Gate Settings")]
    public Transform destinationPoint;
    public string playerTag = "Player";
    public bool matchDestinationRotation = false;
    
    [Header("Audio (Optional)")]
    public AudioSource transitionAudioSource;

    [Header("Safety")]
    public float globalTeleportCooldown = 0.8f;

    private static float lastGlobalTeleportTime = -999f;
    private bool isTeleporting = false;

    void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;
        if (!other.CompareTag(playerTag)) return;
        if (destinationPoint == null) return;
        if (Time.time - lastGlobalTeleportTime < globalTeleportCooldown) return;

        StartCoroutine(TeleportPlayer(other.transform));
    }

    IEnumerator TeleportPlayer(Transform player)
    {
        isTeleporting = true;
        lastGlobalTeleportTime = Time.time;
        PlayTransitionSfx();

        GameManager.Instance?.StartInteraction();

        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeToBlack(() =>
            {
                player.position = destinationPoint.position;
                if (matchDestinationRotation)
                {
                    player.rotation = destinationPoint.rotation;
                }
            });
        }
        else
        {
            player.position = destinationPoint.position;
            if (matchDestinationRotation)
            {
                player.rotation = destinationPoint.rotation;
            }
            yield return null;
        }

        GameManager.Instance?.EndInteraction();
        isTeleporting = false;
    }

    void PlayTransitionSfx()
    {
        AudioSource src = transitionAudioSource != null ? transitionAudioSource : GetComponent<AudioSource>();
        if (src == null) return;
        if (src.clip == null) return;
        src.PlayOneShot(src.clip, AudioSettingsManager.GetSfxMultiplier());
    }
}
