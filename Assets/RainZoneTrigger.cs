using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RainZoneTrigger : MonoBehaviour
{
    [Header("Zone")]
    public string zoneGroupId = "Forest_Backside_Rain";
    public string playerTag = "Player";

    [Header("Rain Target")]
    public GameObject rainRoot;

    private static readonly Dictionary<string, int> groupInsideCount = new Dictionary<string, int>();
    private static readonly Dictionary<string, GameObject> groupRainRoot = new Dictionary<string, GameObject>();
    private static readonly Dictionary<string, bool> groupRainActive = new Dictionary<string, bool>();
    private bool countedInside = false;
    private Collider zoneCollider;
    private Transform player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticState()
    {
        groupInsideCount.Clear();
        groupRainRoot.Clear();
        groupRainActive.Clear();
    }

    void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void Start()
    {
        zoneCollider = GetComponent<Collider>();
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) player = playerObj.transform;

        if (rainRoot != null)
        {
            groupRainRoot[zoneGroupId] = rainRoot;
            // Force OFF at startup so rain never begins before entering the zone.
            rainRoot.SetActive(false);
        }

        // Initialize group state to off at startup.
        groupRainActive[zoneGroupId] = false;
    }

    void Update()
    {
        if (zoneCollider == null || player == null) return;

        bool isInside = zoneCollider.bounds.Contains(player.position);
        if (isInside && !countedInside)
        {
            IncrementGroup();
        }
        else if (!isInside && countedInside)
        {
            DecrementGroup();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || countedInside) return;
        IncrementGroup();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag) || !countedInside) return;
        DecrementGroup();
    }

    void OnDisable()
    {
        if (!countedInside) return;
        DecrementGroup();
    }

    void IncrementGroup()
    {
        countedInside = true;

        int count = 0;
        groupInsideCount.TryGetValue(zoneGroupId, out count);
        count++;
        groupInsideCount[zoneGroupId] = count;

        SetGroupRainActive(zoneGroupId, true);
    }

    void DecrementGroup()
    {
        countedInside = false;

        int count = 0;
        groupInsideCount.TryGetValue(zoneGroupId, out count);
        count = Mathf.Max(0, count - 1);
        groupInsideCount[zoneGroupId] = count;

        if (count == 0)
        {
            SetGroupRainActive(zoneGroupId, false);
        }
    }

    static void SetGroupRainActive(string groupId, bool active)
    {
        bool currentActive = false;
        groupRainActive.TryGetValue(groupId, out currentActive);

        // No state change -> do nothing (prevents repeated prompts while moving across same rain group pieces).
        if (currentActive == active)
        {
            return;
        }

        groupRainActive[groupId] = active;

        GameObject root;
        if (groupRainRoot.TryGetValue(groupId, out root) && root != null)
        {
            root.SetActive(active);
        }

        if (GhostPromptManager.Instance != null)
        {
            if (active)
            {
                GhostPromptManager.Instance.ShowCustomPrompt("The rain begins to pour.", 2.2f);
            }
            else
            {
                GhostPromptManager.Instance.ShowCustomPrompt("The rain faded.", 2.2f);
            }
        }
    }
}
