using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaMusicZoneTrigger : MonoBehaviour
{
    [Header("Zone Settings")]
    public string zoneId = "City_Main";
    public int priority = 0;
    [Range(0f, 2f)] public float zoneVolumeMultiplier = 1f;
    public AudioSource targetMusicSource;
    public string playerTag = "Player";

    void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (AreaMusicManager.Instance == null) return;

        AreaMusicManager.Instance.EnterZone(zoneId, targetMusicSource, priority, zoneVolumeMultiplier);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (AreaMusicManager.Instance == null) return;

        AreaMusicManager.Instance.ExitZone(zoneId, targetMusicSource);
    }
}
