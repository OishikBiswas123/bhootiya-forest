using UnityEngine;
using System.Collections;
using TMPro;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    public enum Difficulty
    {
        Easy,
        Mid,
        Hard
    }

    public static Difficulty CurrentDifficulty = Difficulty.Mid;
    public const string KeyDifficulty = "setting_difficulty";

    [Header("Game Settings")]
    public float gameTimeLimit = 240f; // 4 minutes in seconds
    
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public GameObject darkScreenPanel; // Pitch black screen behind game over

    [Header("Game State SFX (Optional)")]
    public AudioSource gameStateAudioSource;
    public AudioClip gameBeginClip;
    public AudioClip gameOverClip;
    public AudioClip victoryClip;
    
    [Header("Post Victory Flow")]
    public Transform citySpawnPoint;
    public Transform homeOutdoorSpawnPoint;
    public float victoryReturnPromptDelay = 1f;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private float currentTime;
    private bool gameStarted = false;
    private bool gameEnded = false;
    private bool waitingForVictoryChoiceInput = false;
    private bool showingVictoryChoices = false;
    private bool endPanelMusicMuted = false;
    
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

        if (PlayerPrefs.HasKey(KeyDifficulty))
        {
            int val = PlayerPrefs.GetInt(KeyDifficulty);
            if (val < 0) val = 0;
            if (val > 2) val = 2;
            CurrentDifficulty = (Difficulty)val;
        }
    }
    
    void Start()
    {
        // Hide game over panel at start
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        // Hide timer at start (not in game yet)
        if (timerText != null)
            timerText.gameObject.SetActive(false);
        
        // Hide dark screen at start
        if (darkScreenPanel != null)
        {
            darkScreenPanel.SetActive(false);
            CanvasGroup cg = darkScreenPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = darkScreenPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        if (citySpawnPoint == null)
        {
            GameObject cityObj = GameObject.Find("CitySpawnPoint");
            if (cityObj != null) citySpawnPoint = cityObj.transform;
        }

        if (homeOutdoorSpawnPoint == null)
        {
            GameObject homeOutObj = GameObject.Find("Outdoorspawn point");
            if (homeOutObj != null) homeOutdoorSpawnPoint = homeOutObj.transform;
        }

    }
    
    void Update()
    {
        if (gameStarted && !gameEnded)
        {
            // Countdown timer
            currentTime -= Time.deltaTime;
            
            if (currentTime <= 0)
            {
                currentTime = 0;
                LoseGame();
            }
            
            UpdateTimerDisplay();
        }

        if (waitingForVictoryChoiceInput && !showingVictoryChoices && InputBridge.GetKeyDown(KeyCode.X))
        {
            ShowVictoryReturnChoices();
        }
    }
    
    public void StartActualGame()
    {
        Log("GameFlowManager.StartActualGame() CALLED!");
        
        gameStarted = true;
        gameTimeLimit = GetTimeLimitForDifficulty(CurrentDifficulty);
        currentTime = gameTimeLimit;
        
        Log("ACTUAL GAME STARTED! gameStarted=" + gameStarted + ", Timer: " + gameTimeLimit + " seconds");

        if (GhostPromptManager.Instance != null)
        {
            GhostPromptManager.Instance.ResetFirstSpawn();
        }
        
// Make sure timer text is visible
        if (timerText != null)
        {
            Log("Timer text found - enabling it!");
            timerText.gameObject.SetActive(true);
            timerText.color = Color.white;
        }
        else
        {
            Debug.LogError("Timer text is NULL!");
        }
        
        // Start ghost spawning
        GhostSpawner spawner = FindFirstObjectByType<GhostSpawner>();
        if (spawner != null)
        {
            Log("Ghost spawner found - calling StartSpawning()");
            spawner.StartSpawning();
        }
        else
        {
            Debug.LogError("Ghost spawner is NULL!");
        }
    }

    public void SetDifficultyEasy()
    {
        CurrentDifficulty = Difficulty.Easy;
        PlayerPrefs.SetInt(KeyDifficulty, (int)CurrentDifficulty);
    }

    public void SetDifficultyMid()
    {
        CurrentDifficulty = Difficulty.Mid;
        PlayerPrefs.SetInt(KeyDifficulty, (int)CurrentDifficulty);
    }

    public void SetDifficultyHard()
    {
        CurrentDifficulty = Difficulty.Hard;
        PlayerPrefs.SetInt(KeyDifficulty, (int)CurrentDifficulty);
    }

    public bool IsHardMode()
    {
        return CurrentDifficulty == Difficulty.Hard;
    }

    float GetTimeLimitForDifficulty(Difficulty diff)
    {
        switch (diff)
        {
            case Difficulty.Easy:
                return 600f; // 10 minutes
            case Difficulty.Mid:
                return 300f; // 5 minutes
            case Difficulty.Hard:
                return 300f; // 5 minutes
            default:
                return 300f;
        }
    }
    
    public void ShowGameBeginsScreen()
    {
        ShowDarkScreen();

        if (gameOverPanel != null && gameOverText != null)
        {
            gameOverPanel.SetActive(true);
            gameOverText.text = "GAME BEGINS\n\nFind the key before time runs out...";
        }
        PlayGameStateSfx(gameBeginClip);
    }
    
    public void HideGameBeginsScreen()
    {
        Log("HideGameBeginsScreen called!");
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        StartCoroutine(FadeOutDarkScreenOnly(0.8f));
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            
            // Change color when time is low
            if (currentTime < 60f)
            {
                timerText.color = Color.red;
            }
        }
    }
    
    public void WinGame()
    {
        if (gameEnded) return;
        
        gameEnded = true;
        Log("GAME WON! You escaped with the key!");
        PlayGameStateSfx(victoryClip);
        ShowDarkScreen();
        
        // Hide timer
        if (timerText != null)
            timerText.gameObject.SetActive(false);
        
        // Show win screen
        if (gameOverPanel != null && gameOverText != null)
        {
            MuteBgmForEndPanel();
            gameOverPanel.SetActive(true);
            gameOverText.text = "VICTORY!\n\nYou escaped the forest with the key!\n\nTime Remaining: " + timerText.text;
        }
        
        // Freeze everything
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();
        
        // Stop ghost
        GhostAI[] ghosts = FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
        foreach (GhostAI ghost in ghosts)
        {
            ghost.enabled = false;
        }
        
        // Auto-close victory screen after 6 seconds and reset game
        Invoke("HideVictoryAndReset", 6f);
    }
    
    void HideVictoryAndReset()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        RestoreBgmAfterEndPanel();
        
        // Reset game
        currentTime = gameTimeLimit;
        gameStarted = false;
        gameEnded = false;
        
        if (KeyFoundManager.Instance != null)
            KeyFoundManager.Instance.ResetKey();
        
        // Destroy ghosts
        GhostAI[] ghosts = FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
        foreach (GhostAI ghost in ghosts)
        {
            Destroy(ghost.gameObject);
        }
        
        if (TreeManager.Instance != null)
            TreeManager.Instance.ResetAllTrees();
        
        GhostSpawner spawner = FindFirstObjectByType<GhostSpawner>();
        if (spawner != null)
            spawner.ResetSpawner();
        
        // Land at city spawn after victory.
        RespawnAtCitySpawn();
        
        StartCoroutine(FadeOutDarkScreenOnly(0.8f));
        StartCoroutine(BeginPostVictoryReturnHomePrompt());
        Log("Game reset after victory!");
    }
    
    void LoseGame()
    {
        if (gameEnded) return;
        
        gameEnded = true;
        Log("GAME LOST! Time ran out!");
        StartCoroutine(HandleTimeOutSequence());
    }
    
    public void LoseGameToGhost()
    {
        if (gameEnded) return;
        
        gameEnded = true;
        Log("GAME LOST! Caught by ghost!");
        PlayGameStateSfx(gameOverClip);
        
        // Hide timer
        if (timerText != null)
            timerText.gameObject.SetActive(false);
        
        // Show lose screen
        if (gameOverPanel != null && gameOverText != null)
        {
            MuteBgmForEndPanel();
            gameOverPanel.SetActive(true);
            gameOverText.text = "GAME OVER\n\nYou became another spirit in the forest...";
        }
        
        // Freeze everything
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();
    }
    
public void HideGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    
    public void ShowGameOverAtLocation()
    {
        // Hide timer
        if (timerText != null)
            timerText.gameObject.SetActive(false);
        
        PlayGameStateSfx(gameOverClip);
        
        // Show dark screen
        ShowDarkScreen();
        
        if (gameOverPanel != null && gameOverText != null)
        {
            MuteBgmForEndPanel();
            gameOverPanel.SetActive(true);
            gameOverText.text = "GAME OVER\n\nYou became another spirit in the forest...";
        }
    }
    
public void ShowDarkScreen()
    {
        if (darkScreenPanel != null)
        {
            darkScreenPanel.SetActive(true);
            CanvasGroup cg = darkScreenPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = darkScreenPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }
    }

    IEnumerator FadeOutDarkScreenOnly(float duration)
    {
        CanvasGroup cg = darkScreenPanel?.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            if (darkScreenPanel != null) darkScreenPanel.SetActive(false);
            yield break;
        }

        float d = Mathf.Max(0.05f, duration);
        float startAlpha = cg.alpha;
        float t = 0f;

        while (t < d)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, t / d);
            yield return null;
        }

        cg.alpha = 0f;
        if (darkScreenPanel != null) darkScreenPanel.SetActive(false);
    }
    
    public void ShowDarkScreenOnly()
    {
        ShowDarkScreen();
    }
    
    public void StartTeleportHome()
    {
        // Respawn player at city spawn (same post-flow as victory)
        RespawnAtCitySpawn();
        
        // After respawn, fade out dark screen
        Invoke("FadeOutDarkScreen", 1f);
    }
    
    void FadeOutDarkScreen()
    {
        StartCoroutine(FadeDarkScreenAndReset());
    }
    
    IEnumerator FadeDarkScreenAndReset()
    {
        CanvasGroup cg = darkScreenPanel?.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float duration = 3f; // Slower fade
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, timer / duration);
                yield return null;
            }
            
            cg.alpha = 0f;
            darkScreenPanel.SetActive(false);
        }
        
        // Now reset the game
        StartCoroutine(ResetGameAfterDelay());
    }
    
public void FadeDarkScreenAndRespawn()
    {
        StartCoroutine(FadeDarkScreen());
    }
    
    public void StartDarkScreenFade()
    {
        StartCoroutine(FadeDarkScreenIn(7f));
    }
    
    IEnumerator FadeDarkScreenIn(float duration)
    {
        if (darkScreenPanel != null)
        {
            darkScreenPanel.SetActive(true);
            CanvasGroup cg = darkScreenPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = darkScreenPanel.AddComponent<CanvasGroup>();
            
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, timer / duration);
                yield return null;
            }
            cg.alpha = 1f;
        }
    }
    
    IEnumerator FadeDarkScreen()
    {
        CanvasGroup cg = darkScreenPanel?.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float duration = 2f;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, timer / duration);
                yield return null;
            }
            
            cg.alpha = 0f;
            darkScreenPanel.SetActive(false);
        }
    }
    
    public void HideGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        RestoreBgmAfterEndPanel();
    }
    
    public bool IsGameStarted()
    {
        return gameStarted;
    }
    
    public bool IsGameEnded()
    {
        return gameEnded;
    }

    public void RestartForestFromPause()
    {
        gameEnded = false;
        gameStarted = false;
        currentTime = gameTimeLimit;

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (KeyFoundManager.Instance != null)
            KeyFoundManager.Instance.ResetKey();

        GhostAI[] ghosts = FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
        foreach (GhostAI ghost in ghosts)
        {
            Destroy(ghost.gameObject);
        }

        if (TreeManager.Instance != null)
            TreeManager.Instance.ResetAllTrees();

        GhostSpawner spawner = FindFirstObjectByType<GhostSpawner>();
        if (spawner != null)
            spawner.ResetSpawner();

        HutTeleport[] huts = FindObjectsByType<HutTeleport>(FindObjectsSortMode.None);
        foreach (HutTeleport hut in huts)
        {
            if (hut != null && hut.hutID == 1)
            {
                hut.StartForestLandingOnly();
                return;
            }
        }
    }
    
    public void RespawnAtHome()
    {
        Log("Respawning player at home...");
        
        // Find the home hut (hutID = 1)
        HutTeleport[] huts = FindObjectsByType<HutTeleport>(FindObjectsSortMode.None);
        Transform homeSpawnPoint = null;
        
        foreach (HutTeleport hut in huts)
        {
            if (hut.hutID == 1) // Home hut
            {
                // Find the destination point for the home hut (where player should appear)
                // We need to find the other hut that points to home
                foreach (HutTeleport otherHut in huts)
                {
                    if (otherHut.hutID == 2 && otherHut.destinationPoint != null) // Forest hut
                    {
                        homeSpawnPoint = otherHut.destinationPoint;
                        break;
                    }
                }
                break;
            }
        }
        
        // Teleport player to home (instant - no effect, dark screen will hide after)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && homeSpawnPoint != null)
        {
            player.transform.position = homeSpawnPoint.position;
            
            // Face player front to camera (idle down)
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerMove.SetFacingDirection(1);
            }
            
            Log("Player respawned at home position: " + homeSpawnPoint.position);
        }
        else
        {
            Debug.LogError("Could not respawn player - player or spawn point not found!");
        }
        
        // Don't reset game yet - wait for dark screen to fade first
        // Reset will happen after dark screen fades
    }

    void RespawnAtCitySpawn()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (citySpawnPoint != null)
        {
            player.transform.position = citySpawnPoint.position;
        }
        else
        {
            Debug.LogError("CitySpawnPoint is not assigned/found on GameFlowManager.");
        }

        PlayerMove playerMove = player.GetComponent<PlayerMove>();
        if (playerMove != null)
        {
            playerMove.SetFacingDirection(1);
        }

        AreaMusicManager.Instance?.RefreshZonesForPlayer(player.transform);
    }
    
    System.Collections.IEnumerator ResetGameAfterDelay()
    {
        yield return null;
        
        // Reset timer
        currentTime = gameTimeLimit;
        gameStarted = false;
        gameEnded = false;
        
        // Reset key
        if (KeyFoundManager.Instance != null)
        {
            KeyFoundManager.Instance.ResetKey();
        }
        
        // Destroy any existing ghosts
        GhostAI[] ghosts = FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
        foreach (GhostAI ghost in ghosts)
        {
            Destroy(ghost.gameObject);
        }
        
        // Reset tree interactions
        if (TreeManager.Instance != null)
        {
            TreeManager.Instance.ResetAllTrees();
        }
        
        // Reset ghost spawner
        GhostSpawner spawner = FindFirstObjectByType<GhostSpawner>();
        if (spawner != null)
        {
            spawner.ResetSpawner();
        }
        
        // Face player front to camera (idle down)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerMove.SetFacingDirection(1);
            }
        }
        
        StartCoroutine(BeginPostVictoryReturnHomePrompt());
        Log("Game reset complete. Waiting for post-return choice.");
    }

    IEnumerator BeginPostVictoryReturnHomePrompt()
    {
        waitingForVictoryChoiceInput = false;
        showingVictoryChoices = false;

        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();

        yield return new WaitForSeconds(Mathf.Max(0f, victoryReturnPromptDelay));

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDialogue("Do you want to go back home ?", false, true, false);
        }

        waitingForVictoryChoiceInput = true;
    }

    void ShowVictoryReturnChoices()
    {
        waitingForVictoryChoiceInput = false;
        showingVictoryChoices = true;

        if (UIManager.Instance == null)
        {
            if (GameManager.Instance != null) GameManager.Instance.EndInteraction();
            showingVictoryChoices = false;
            return;
        }

        UIManager.Instance.ShowDialogueWithChoices2Button(
            "Do you want to go back home ?",
            OnVictoryGoHomeSelected,
            OnVictoryStayHereSelected
        );
        UIManager.Instance.SetChoiceButtonTexts("Go Home", "Stay Here");
    }

    void OnVictoryStayHereSelected()
    {
        showingVictoryChoices = false;
        UIManager.Instance?.CloseDialogue();
        if (GameManager.Instance != null)
            GameManager.Instance.EndInteraction();
    }

    void OnVictoryGoHomeSelected()
    {
        showingVictoryChoices = false;
        StartCoroutine(TeleportHomeOutdoorAfterVictory());
    }

IEnumerator TeleportHomeOutdoorAfterVictory()
    {
        UIManager.Instance?.CloseDialogue();
        
        // Restore BGM before teleporting
        RestoreBgmAfterEndPanel();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || homeOutdoorSpawnPoint == null)
        {
            Debug.LogError("Cannot teleport home after victory: player or Outdoorspawn point missing.");
            if (GameManager.Instance != null) GameManager.Instance.EndInteraction();
            yield break;
        }

        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeToBlack(() =>
            {
                player.transform.position = homeOutdoorSpawnPoint.position;
            });
        }
        else
        {
            player.transform.position = homeOutdoorSpawnPoint.position;
        }

AreaMusicManager.Instance?.RefreshZonesForPlayer(player.transform);

        PlayerMove playerMove = player.GetComponent<PlayerMove>();
        if (playerMove != null)
        {
            playerMove.SetFacingDirection(1);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.EndInteraction();

        // Force reset animation state after teleport to prevent walking animation glitch
        PlayerMove pm = player.GetComponent<PlayerMove>();
        Animator anim = player.GetComponent<Animator>();
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        
        if (pm != null)
        {
            pm.SetFacingDirection(1);
        }
        
        if (anim != null)
        {
            anim.SetBool("IsWalking", false);
            anim.SetInteger("Direction", 1);
        }
        
        if (pm != null && sr != null)
        {
            sr.sprite = pm.idleDown;
            pm.SetLockedYPosition(player.transform.position.y);
        }
    }

    void Log(string message)
    {
        if (enableDebugLogs || GlobalDebugSettings.EnableAllLogs)
        {
            Debug.Log(message);
        }
    }

    void PlayGameStateSfx(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource src = gameStateAudioSource != null ? gameStateAudioSource : GetComponent<AudioSource>();
        if (src == null) return;
        src.PlayOneShot(clip, AudioSettingsManager.GetSfxMultiplier());
    }

    IEnumerator HandleTimeOutSequence()
    {
        // Hide timer
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        // Freeze player immediately
        if (GameManager.Instance != null)
            GameManager.Instance.StartInteraction();

        // Spawn ghost instantly with first spawn sound.
        GhostSpawner spawner = FindFirstObjectByType<GhostSpawner>();
        if (spawner != null)
        {
            spawner.ForceSpawnForTimeout(true);
        }

        // Prompt while fading.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDialogue("You are out of time..", false, false);
        }
        
        // Keep timeout prompt visible briefly before fade starts.
        yield return new WaitForSeconds(4f);
        yield return StartCoroutine(FadeToBlackOnly(2f));

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }

        PlayGameStateSfx(gameOverClip);

        // Show simplified game over panel (without "time ran out" line).
        if (gameOverPanel != null && gameOverText != null)
        {
            MuteBgmForEndPanel();
            gameOverPanel.SetActive(true);
            gameOverText.text = "GAME OVER\n\nYou ran out of time... and the ghost ran out of patience.";
        }

        // Keep panel for 4 seconds, then teleport back via same game-over return flow.
        yield return new WaitForSeconds(4f);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        RestoreBgmAfterEndPanel();

        StartTeleportHome();
    }

    IEnumerator FadeToBlackOnly(float duration)
    {
        if (darkScreenPanel == null) yield break;

        darkScreenPanel.SetActive(true);
        CanvasGroup cg = darkScreenPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = darkScreenPanel.AddComponent<CanvasGroup>();

        float d = Mathf.Max(0.05f, duration);
        float startAlpha = cg.alpha;
        float t = 0f;

        while (t < d)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 1f, t / d);
            yield return null;
        }

        cg.alpha = 1f;
    }

    void MuteBgmForEndPanel()
    {
        if (endPanelMusicMuted) return;
        endPanelMusicMuted = true;
        AreaMusicManager.Instance?.SetExternalVolumeMultiplier(0f);
    }

    void RestoreBgmAfterEndPanel()
    {
        if (!endPanelMusicMuted) return;
        endPanelMusicMuted = false;
        AreaMusicManager.Instance?.SetExternalVolumeMultiplier(1f);
    }
}


