using UnityEngine;

public class GhostSpawner : MonoBehaviour
{
    public static GhostSpawner Instance;
    public GameObject ghostPrefab;
    public float minSpawnTime = 20f;
    public float maxSpawnTime = 25f;
    
    [Header("Ghost SFX")]
    public AudioSource ghostSpawnAudioSource;
    public AudioClip firstSpawnSfx;
    public AudioClip[] chaseSpawnSfx;
    [Range(0f, 1f)] public float spawnSfxVolume = 1f;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private float spawnTimer = 0f;
    private float nextSpawnTime;
    private GameObject currentGhost;
    private bool spawningEnabled = false;
    private bool firstSpawnDone = false;
    private int lastChaseClipIndex = -1;
    
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        SetNextSpawnTime();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    public void StartSpawning()
    {
        spawningEnabled = true;
        Log("Ghost spawning enabled!");
    }
    
    void Update()
    {
        // Don't spawn ghost until player confirmed to start
        if (!spawningEnabled)
        {
            return; // Wait for player to start the game
        }
        
        // Don't spawn ghost until game has started
        if (GameFlowManager.Instance == null || !GameFlowManager.Instance.IsGameStarted())
        {
            return; // Wait for game to start
        }
        
        // Don't spawn ghost during interactions/dialogs/prompts
        if (ShouldPauseGhostForUI())
        {
            return; // Pause timer during interaction
        }
        
        // Only count timer if there's no ghost currently active
        if (currentGhost == null)
        {
            spawnTimer += Time.deltaTime;
            
            if (spawnTimer >= nextSpawnTime)
            {
                SpawnGhost();
            }
        }
    }
    
void SpawnGhost()
    {
        if (ghostPrefab != null)
        {
            currentGhost = Instantiate(ghostPrefab);
            spawnTimer = 0f;
            SetNextSpawnTime();
            
            // Flag for first spawn (front) vs respawn (behind)
            GhostAI ghostAI = currentGhost.GetComponent<GhostAI>();
            if (ghostAI != null)
            {
                ghostAI.isFirstSpawn = !firstSpawnDone;
            }
            
            // Trigger ghost prompt
            if (GhostPromptManager.Instance != null)
            {
                GhostPromptManager.Instance.OnGhostSpawned();
            }
            
            PlayGhostSpawnSfx();
        }
    }

    public void ForceSpawnForTimeout(bool forceFirstSpawnSound = true)
    {
        if (ghostPrefab == null) return;

        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }

        currentGhost = Instantiate(ghostPrefab);

        if (forceFirstSpawnSound && firstSpawnSfx != null)
        {
            AudioSource src = GetGhostSpawnAudioSource();
            if (src != null)
            {
                src.PlayOneShot(firstSpawnSfx, spawnSfxVolume * AudioSettingsManager.GetSfxMultiplier());
            }
            firstSpawnDone = true;
            return;
        }

        PlayGhostSpawnSfx();
    }
    
    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);
    }
    
    public void ResetSpawner()
    {
        spawnTimer = 0f;
        SetNextSpawnTime();
        currentGhost = null;
        spawningEnabled = false;
        Log("Ghost spawner reset");
    }

    void Log(string message)
    {
        if (enableDebugLogs || GlobalDebugSettings.EnableAllLogs)
        {
            Debug.Log(message);
        }
    }

    bool ShouldPauseGhostForUI()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsHardMode())
            return false;

        if (GameManager.Instance != null && GameManager.Instance.isInteracting)
            return true;

        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.dialoguePanel != null && UIManager.Instance.dialoguePanel.activeSelf)
                return true;
            if (UIManager.Instance.choicePanel != null && UIManager.Instance.choicePanel.activeSelf)
                return true;
        }

        if (GhostPromptManager.Instance != null)
        {
            if (GhostPromptManager.Instance.promptPanel != null && GhostPromptManager.Instance.promptPanel.activeSelf)
                return true;
        }

        return false;
    }

    void PlayGhostSpawnSfx()
    {
        AudioClip clipToPlay = null;
        if (!firstSpawnDone && firstSpawnSfx != null)
        {
            clipToPlay = firstSpawnSfx;
            firstSpawnDone = true;
        }
        else
        {
            clipToPlay = GetNextChaseClipNonRepeating();
            firstSpawnDone = true;
        }

        if (clipToPlay == null) return;
        AudioSource src = GetGhostSpawnAudioSource();
        if (src == null) return;
        src.PlayOneShot(clipToPlay, spawnSfxVolume * AudioSettingsManager.GetSfxMultiplier());
    }

    AudioClip GetNextChaseClipNonRepeating()
    {
        if (chaseSpawnSfx == null || chaseSpawnSfx.Length == 0)
            return null;

        if (chaseSpawnSfx.Length == 1)
        {
            lastChaseClipIndex = 0;
            return chaseSpawnSfx[0];
        }

        int idx = lastChaseClipIndex;
        int guard = 0;
        while (idx == lastChaseClipIndex && guard < 20)
        {
            idx = Random.Range(0, chaseSpawnSfx.Length);
            guard++;
        }

        if (idx < 0 || idx >= chaseSpawnSfx.Length)
            idx = 0;

        lastChaseClipIndex = idx;
        return chaseSpawnSfx[idx];
    }

    AudioSource GetGhostSpawnAudioSource()
    {
        if (ghostSpawnAudioSource != null)
            return ghostSpawnAudioSource;

        ghostSpawnAudioSource = GetComponent<AudioSource>();
        if (ghostSpawnAudioSource == null)
        {
            ghostSpawnAudioSource = gameObject.AddComponent<AudioSource>();
        }

        ghostSpawnAudioSource.playOnAwake = false;
        ghostSpawnAudioSource.loop = false;
        ghostSpawnAudioSource.spatialBlend = 0f; // 2D so spawn cue is always audible
        return ghostSpawnAudioSource;
    }

    public void OnGhostDespawned()
    {
        // Ghost disappeared - reset so next spawn can happen
        currentGhost = null;
        spawnTimer = 0f;
        SetNextSpawnTime();
    }
    
    public void PlayCatchSfx()
    {
        // SFX when ghost catches player - can be added later
    }
    
    public void PlayDeathLine2Sfx()
    {
        // SFX for second death line - can be added later
    }
    
    public void StopGhostSpawnSfx()
    {
        if (ghostSpawnAudioSource != null && ghostSpawnAudioSource.isPlaying)
        {
            ghostSpawnAudioSource.Stop();
        }
    }
}

