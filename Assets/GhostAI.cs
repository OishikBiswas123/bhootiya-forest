using UnityEngine;
using System.Collections;

public class GhostAI : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;
    public float catchRadius = 0.5f;
    public float heightOffset = 0f;
    public float inputBuffer = 0.2f;
    
    [Header("Catching")]
    public float chaseDuration = 15f;
    public float promptFadeDuration = 7f;
    public float fadeDelayAfterSecondPrompt = 0.5f; // Delay before fade starts
    public float fadeSpeed = 1f; // How fast the black screen fades in
    
    [Header("Death Prompts")]
    public string firstDeathPrompt = "You were caught by the ghost...";
    public string secondDeathPrompt = "The ghost broke your head and ate you...";
    public float firstPromptHoldTime = 3f; // Must wait 3 seconds before X works
    public AudioSource audioSource;
    public AudioClip firstDeathSound;
    public AudioClip secondDeathSound;
    
    private Transform player;
    private Animator animator;
    private bool isCatching = false;
    private float catchTimer = 0f;
    private float chaseTime = 0f;
    private int deathPhase = 0; // 0=waiting, 1=first prompt, 2=second prompt, 3=waiting for fade
    private bool chaseAnimPausedByUI = false;
    
    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        if (animator != null)
            animator.SetBool("isChasing", true);
        
        // Original spawn: in FRONT of player
        if (player != null)
        {
            Vector3 spawnPos = player.position + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(8f, 15f));
            spawnPos.y = player.position.y + heightOffset;
            transform.position = spawnPos;
        }
    }
    
    void Update()
    {
        if (inputBuffer > 0)
            inputBuffer -= Time.deltaTime;
        
        // Handle death sequence when catching player
        if (isCatching)
        {
            UpdateDeathSequence();
            return;
        }
        
        // Track chase time - auto disappear after chaseDuration
        chaseTime += Time.deltaTime;
        if (chaseTime >= chaseDuration)
        {
            DisappearGhost();
            return;
        }
        
        if (ShouldPauseGhostForUI())
        {
            if (!chaseAnimPausedByUI && animator != null)
            {
                animator.SetBool("isChasing", false);
                chaseAnimPausedByUI = true;
            }
            return;
        }
        else if (chaseAnimPausedByUI && animator != null)
        {
            animator.SetBool("isChasing", true);
            chaseAnimPausedByUI = false;
        }
        
        if (animator != null)
            animator.SetBool("isChasing", true);
        
        ChasePlayer();
    }
    
    void UpdateDeathSequence()
    {
        catchTimer += Time.deltaTime;
        
        // Phase 1: First death prompt
        if (deathPhase == 0)
        {
            if (catchTimer >= 0.1f && !gameOverShown)
            {
                deathPhase = 1;
                UIManager.Instance?.ShowDialogue(firstDeathPrompt, true, false);
                if (audioSource != null && firstDeathSound != null)
                    audioSource.PlayOneShot(firstDeathSound);
                gameOverShown = true;
                GameManager.Instance?.StartInteraction();
                catchTimer = 0f;
            }
        }
        // Phase 2: Must wait 3 seconds before X can proceed
        else if (deathPhase == 1)
        {
            if (catchTimer >= firstPromptHoldTime && InputBridge.GetKeyDown(KeyCode.X))
            {
                deathPhase = 2;
                UIManager.Instance?.ShowDialogue(secondDeathPrompt, true, false);
                if (audioSource != null && secondDeathSound != null)
                    audioSource.PlayOneShot(secondDeathSound);
                catchTimer = 0f;
            }
        }
        // Phase 3: Auto-close second prompt after 2 seconds, then start fade
        else if (deathPhase == 2)
        {
            if (catchTimer >= 2f)
            {
                UIManager.Instance?.CloseDialogue();
                deathPhase = 3;
                catchTimer = 0f;
            }
        }
        // Phase 4: Fade to black slowly
        else if (deathPhase == 3)
        {
            if (catchTimer >= fadeDelayAfterSecondPrompt)
            {
                deathPhase = 4;
                GameFlowManager.Instance?.StartDarkScreenFade();
            }
        }
        // Phase 5: After fade completes, show game over panel
        else if (deathPhase == 4)
        {
            if (catchTimer >= promptFadeDuration)
            {
                deathPhase = 5;
                GameFlowManager.Instance?.ShowGameOverAtLocation();
                catchTimer = 0f;
            }
        }
        // Phase 6: Game over shows for 3 seconds, then teleport home
        else if (deathPhase == 5)
        {
            if (catchTimer >= 3f)
            {
                deathPhase = 6;
                // Close game over panel before teleport
                if (GameFlowManager.Instance != null)
                    GameFlowManager.Instance.HideGameOverPanel();
                
                UIManager.Instance?.CloseDialogue();
                GameFlowManager.Instance?.StartTeleportHome();
            }
        }
    }
    
    bool gameOverShown = false;
    
    void ShowGameOverAfterFade()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ShowGameOverAtLocation();
    }
    
    void ChasePlayer()
    {
        if (player == null) return;
        
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }
    
    bool ShouldPauseGhostForUI()
    {
        // Always pause during death sequence
        if (isCatching) return true;
        
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
        
        if (GhostPromptManager.Instance != null && GhostPromptManager.Instance.promptPanel != null && GhostPromptManager.Instance.promptPanel.activeSelf)
            return true;
        
        return false;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCatching)
        {
            Debug.Log("Ghost caught player!");
            GhostSpawner.Instance?.StopGhostSpawnSfx();
            isCatching = true;
            catchTimer = 0f;
            deathPhase = 0;
            gameOverShown = false;
            chaseTime = 0f; // Reset chase time so ghost doesn't disappear while catching
            
            // Freezes player
            if (GameManager.Instance != null)
                GameManager.Instance.StartInteraction();
        }
    }
    
    void DisappearGhost()
    {
        if (animator != null)
            animator.SetBool("isChasing", false);
        
        // Notify spawner that ghost is gone
        if (GhostSpawner.Instance != null)
            GhostSpawner.Instance.OnGhostDespawned();
        
        // Destroy this ghost
        Destroy(gameObject);
    }
}