using System.Collections.Generic;
using UnityEngine;

public class AreaMusicManager : MonoBehaviour
{
    public static AreaMusicManager Instance;

    [Header("Music Sources Root (optional)")]
    public Transform musicRoot;

    [Header("Fade Settings")]
    public float fadeInDuration = 1.8f;
    public float fadeOutDuration = 0.8f;
    public float activeVolume = 1f;
    public float userMusicVolume = 1f;
    public float externalVolumeMultiplier = 1f;

    private class ActiveZoneState
    {
        public string zoneKey;
        public string zoneId;
        public AudioSource source;
        public int priority;
        public float zoneVolumeMultiplier;
        public int insideCount;
        public int lastEnterOrder;
    }

    private readonly Dictionary<string, ActiveZoneState> activeZones = new Dictionary<string, ActiveZoneState>();
    private readonly List<AudioSource> allSources = new List<AudioSource>();
    private int enterOrderCounter = 0;
    private AudioSource currentSource;
    private AudioSource desiredSource;
    private AudioSource pendingSource;
    private AudioSource fadingOutSource;
    private bool isTransitioning = false;
    private AudioSource lastActiveNonNullSource;
    private float currentZoneVolumeMultiplier = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CacheSources();
        PrepareSources();

        userMusicVolume = AudioSettingsManager.GetMusicMultiplier();
    }

    void CacheSources()
    {
        allSources.Clear();

        if (musicRoot != null)
        {
            AudioSource[] sources = musicRoot.GetComponentsInChildren<AudioSource>(true);
            allSources.AddRange(sources);
            return;
        }

        AudioSource[] localSources = GetComponentsInChildren<AudioSource>(true);
        allSources.AddRange(localSources);
    }

    void PrepareSources()
    {
        for (int i = 0; i < allSources.Count; i++)
        {
            AudioSource src = allSources[i];
            if (src == null) continue;

            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;

            if (!src.isPlaying && src.clip != null)
            {
                src.Play();
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < allSources.Count; i++)
        {
            AudioSource src = allSources[i];
            if (src == null) continue;

            float target = (src == currentSource) ? (activeVolume * userMusicVolume * currentZoneVolumeMultiplier * externalVolumeMultiplier) : 0f;
            bool fadingIn = target > src.volume;
            float duration = fadingIn ? fadeInDuration : fadeOutDuration;
            float speed = activeVolume / Mathf.Max(0.01f, duration);
            src.volume = Mathf.MoveTowards(src.volume, target, speed * Time.deltaTime);
        }

        if (isTransitioning && fadingOutSource != null && fadingOutSource.volume <= 0.001f)
        {
            ActivatePendingSource();
        }
    }

    public void EnterZone(string zoneId, AudioSource source, int priority, float zoneVolumeMultiplier = 1f)
    {
        if (source == null) return;

        string key = BuildZoneKey(zoneId, source);
        ActiveZoneState state;
        if (!activeZones.TryGetValue(key, out state))
        {
            state = new ActiveZoneState
            {
                zoneKey = key,
                zoneId = zoneId,
                source = source,
                priority = priority,
                zoneVolumeMultiplier = Mathf.Clamp(zoneVolumeMultiplier, 0f, 2f),
                insideCount = 0,
                lastEnterOrder = 0
            };
            activeZones[key] = state;
        }
        else
        {
            state.zoneVolumeMultiplier = Mathf.Clamp(zoneVolumeMultiplier, 0f, 2f);
        }

        state.insideCount++;
        enterOrderCounter++;
        state.lastEnterOrder = enterOrderCounter;

        if (!allSources.Contains(source))
        {
            allSources.Add(source);
            source.volume = 0f;
            if (!source.isPlaying && source.clip != null)
            {
                source.Play();
            }
        }

        RecalculateCurrentSource();
    }

    public void ExitZone(string zoneId, AudioSource source)
    {
        if (source == null) return;

        string key = BuildZoneKey(zoneId, source);
        ActiveZoneState state;
        if (!activeZones.TryGetValue(key, out state)) return;

        state.insideCount--;
        if (state.insideCount <= 0)
        {
            activeZones.Remove(key);
        }

        RecalculateCurrentSource();
    }

    void RecalculateCurrentSource()
    {
        ActiveZoneState best = null;

        foreach (KeyValuePair<string, ActiveZoneState> kvp in activeZones)
        {
            ActiveZoneState state = kvp.Value;
            if (state == null || state.insideCount <= 0 || state.source == null) continue;

            if (best == null)
            {
                best = state;
                continue;
            }

            if (state.priority > best.priority)
            {
                best = state;
            }
            else if (state.priority == best.priority && state.lastEnterOrder > best.lastEnterOrder)
            {
                best = state;
            }
        }

        desiredSource = (best != null) ? best.source : null;
        currentZoneVolumeMultiplier = (best != null) ? Mathf.Clamp(best.zoneVolumeMultiplier, 0f, 2f) : 1f;

        if (desiredSource == currentSource && !isTransitioning)
        {
            return;
        }

        // If already transitioning, only update pending target.
        // Do not reset fadingOutSource/current transition state.
        if (isTransitioning)
        {
            pendingSource = desiredSource;
            return;
        }

        // If nothing is currently playing, switch immediately to desired.
        if (currentSource == null && !isTransitioning)
        {
            if (desiredSource != null)
            {
                currentSource = desiredSource;
                if (!currentSource.isPlaying && currentSource.clip != null)
                {
                    currentSource.Play();
                }
                // Restart only when switching to a different music source.
                // If re-entering the same source (e.g., lift <-> office with same music), continue playback.
                if (lastActiveNonNullSource == null || currentSource != lastActiveNonNullSource)
                {
                    currentSource.time = 0f;
                }
                lastActiveNonNullSource = currentSource;
            }
            return;
        }

        // Sequential fade: fade out old first, then fade in new (no overlap).
        pendingSource = desiredSource;
        fadingOutSource = currentSource;
        currentSource = null;
        isTransitioning = true;
    }

    void ActivatePendingSource()
    {
        isTransitioning = false;
        fadingOutSource = null;

        if (pendingSource == null)
        {
            return;
        }

        currentSource = pendingSource;
        pendingSource = null;

        if (!currentSource.isPlaying && currentSource.clip != null)
        {
            currentSource.Play();
        }
        if (lastActiveNonNullSource == null || currentSource != lastActiveNonNullSource)
        {
            currentSource.time = 0f;
        }
        lastActiveNonNullSource = currentSource;
        currentSource.volume = 0f;
    }

    public void StopAllMusic()
    {
        pendingSource = null;
        desiredSource = null;
        currentSource = null;
        isTransitioning = false;
        fadingOutSource = null;

        for (int i = 0; i < allSources.Count; i++)
        {
            AudioSource src = allSources[i];
            if (src == null) continue;
            src.volume = 0f;
        }
    }

    public void RefreshZonesForPlayer(Transform player)
    {
        if (player == null) return;

        // Reset active state and silence all, then rebuild from current player position.
        activeZones.Clear();
        pendingSource = null;
        desiredSource = null;
        currentSource = null;
        isTransitioning = false;
        fadingOutSource = null;

        for (int i = 0; i < allSources.Count; i++)
        {
            AudioSource src = allSources[i];
            if (src == null) continue;
            src.volume = 0f;
        }

        AreaMusicZoneTrigger[] zones = FindObjectsByType<AreaMusicZoneTrigger>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            AreaMusicZoneTrigger z = zones[i];
            if (z == null || !z.isActiveAndEnabled) continue;
            if (z.targetMusicSource == null) continue;

            Collider c = z.GetComponent<Collider>();
            if (c == null || !c.enabled) continue;

            if (c.bounds.Contains(player.position))
            {
                EnterZone(z.zoneId, z.targetMusicSource, z.priority, z.zoneVolumeMultiplier);
            }
        }
    }

    string BuildZoneKey(string zoneId, AudioSource source)
    {
        string safeZoneId = string.IsNullOrEmpty(zoneId) ? "Zone" : zoneId;
        return safeZoneId + "|" + source.GetInstanceID();
    }

    public void SetExternalVolumeMultiplier(float multiplier)
    {
        externalVolumeMultiplier = Mathf.Clamp01(multiplier);
    }

    public void SetUserMusicVolume(float volume)
    {
        userMusicVolume = Mathf.Clamp01(volume);
    }
}
