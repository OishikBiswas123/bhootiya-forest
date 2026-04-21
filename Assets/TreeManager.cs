using UnityEngine;
using System.Collections.Generic;

public class TreeManager : MonoBehaviour
{
    public static TreeManager Instance;
    
    [Header("Interaction Settings")]
    public float interactionDistance = 12f;
    public KeyCode interactKey = KeyCode.X;
    public float inputCooldown = 0.3f;
    
    [Header("Tree References")]
    public List<GameObject> trees = new List<GameObject>();
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool revealKeyTreeInConsole = true;
    public bool forceFixedKeyTreeForTesting = true;
    public int fixedKeyTreeIndex = 23;
    
    private List<GameObject> originalTrees = new List<GameObject>();
    private Transform player;
    private GameObject currentTree = null;
    private int currentTreeIndex = -1;
    private int treePhase = 0;
    private float waitTime = 0f;
    private int keyTreeIndex = -1;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        Log("TREEMANAGER STARTING! Trees count: " + trees.Count);
        
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) p = GameObject.Find("player");
        if (p == null) p = GameObject.Find("Player");
        player = p?.transform;
        
        originalTrees = new List<GameObject>(trees);
        
        if (trees.Count > 0)
        {
            if (forceFixedKeyTreeForTesting && fixedKeyTreeIndex >= 0 && fixedKeyTreeIndex < trees.Count)
            {
                keyTreeIndex = fixedKeyTreeIndex;
            }
            else
            {
                keyTreeIndex = Random.Range(0, trees.Count);
            }
            Log("TREEMANAGER: KEY IS IN TREE INDEX: " + keyTreeIndex);
            RevealKeyTreeForTesting();
        }
    }
    
    private float checkTimer = 0f;
    private const float CHECK_INTERVAL = 0.2f;
    private float lastInputTime = 0f;
    
void Update()
    {
        if (player == null || trees.Count == 0) return;
        
        // Skip if Game Info panel is showing
        if (UIManager.Instance != null && UIManager.Instance.IsGameInfoActive()) return;
        
        // Handle wait timer for "..." delay (using Update instead of Invoke for better control)
        if (treePhase == 3 && waitTime > 0)
        {
            waitTime -= Time.deltaTime;
            if (waitTime <= 0)
            {
                // Time's up - show result, keep player frozen
                if (GameManager.Instance != null)
                    GameManager.Instance.StartInteraction();
                UIManager.Instance.ShowDialogue("There is nothing inside.", true, false);
                treePhase = 4;
            }
        }
        
        if (currentTree != null && InputBridge.GetKeyDown(interactKey))
        {
            HandleClick();
            return;
        }
        
        checkTimer += Time.deltaTime;
        if (checkTimer < CHECK_INTERVAL) return;
        checkTimer = 0f;
        
        GameObject nearest = null;
        float nearestDist = float.MaxValue;
        int nearestIndex = -1;
        
        for (int i = 0; i < trees.Count; i++)
        {
            if (trees[i] == null) continue;
            float d = Vector3.Distance(trees[i].transform.position, player.position);
            
            if (d < nearestDist && d <= interactionDistance)
            {
                nearestDist = d;
                nearest = trees[i];
                nearestIndex = i;
            }
        }
        
        if (nearest != null && nearest != currentTree)
        {
            if (currentTree != null)
            {
                ResetInteraction();
            }
            
            currentTree = nearest;
            currentTreeIndex = nearestIndex;
            Log("TREEMANAGER: Now near tree " + currentTree.name + " at distance " + nearestDist.ToString("F1"));
        }
        else if (nearest == null && currentTree != null)
        {
            Log("TREEMANAGER: Moved away from trees");
            ResetInteraction();
        }
    }
    
    void HandleClick()
    {
        // Don't allow interaction if game is over
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameEnded()) return;
        
        if (Time.time - lastInputTime < inputCooldown) return;
        lastInputTime = Time.time;
        
        Log("HandleClick called! treePhase=" + treePhase + ", currentTree=" + (currentTree != null ? currentTree.name : "null"));
        
        if (UIManager.Instance == null) 
        {
            Debug.LogError("UIManager is null!");
            return;
        }
        
        if (treePhase == 0)
        {
            Log("Phase 0: Showing hole message");
            treePhase = 1;
            GameManager.Instance.StartInteraction();
            UIManager.Instance.ShowDialogue("There is a small hole in the tree trunk.", true, true);
        }
        else if (treePhase == 1)
        {
            Log("Phase 1: Showing YES/NO choices");
            treePhase = 2;
            UIManager.Instance.ShowDialogueWithChoices2Button("Do you want to check it?", OnYes, OnNo);
            UIManager.Instance.SetChoiceButtonTexts("YES", "NO");
        }
        else if (treePhase == 4)
        {
            Log("Phase 4: Closing");
            ResetInteraction();
        }
        else if (treePhase == 0 || treePhase == 1 || treePhase == 4)
        {
            // Close any open dialogue - result shown
            CancelInvoke("ShowNoKeyResult");
            UIManager.Instance.CloseDialogue();
            GameManager.Instance.EndInteraction();
            treePhase = 0;
        }
    }
    
    void OnYes()
    {
        if (treePhase != 2) return;
        
        bool isKeyTree = (currentTreeIndex == keyTreeIndex);
        
        // Reset tracking but keep showing UI
        currentTree = null;
        currentTreeIndex = -1;
        treePhase = 3;
        
        if (isKeyTree)
        {
            // Key tree - close choice panel, show search, then key animation
            UIManager.Instance.CloseDialogue();
            KeyFoundManager.Instance.FoundKey();
        }
        else
        {
            // Non-key: keep player frozen through the full search/result flow.
            if (GameManager.Instance != null)
                GameManager.Instance.StartInteraction();

            UIManager.Instance.ShowDialogue("...", true, false);
            waitTime = 1.5f;
        }
    }
    
    void OnNo()
    {
        if (treePhase != 2) return;
        
        // Close and reset
        UIManager.Instance.CloseDialogue();
        GameManager.Instance.EndInteraction();
        treePhase = 0;
        waitTime = 0f;
        currentTree = null;
        currentTreeIndex = -1;
    }
    
    void ResetInteraction()
    {
        treePhase = 0;
        waitTime = 0f;
        currentTree = null;
        currentTreeIndex = -1;
        UIManager.Instance.CloseDialogue();
        GameManager.Instance.EndInteraction();
    }
    
    public void ResetAllTrees()
    {
        trees = new List<GameObject>(originalTrees);
        ResetInteraction();
        
        if (trees.Count > 0)
        {
            if (forceFixedKeyTreeForTesting && fixedKeyTreeIndex >= 0 && fixedKeyTreeIndex < trees.Count)
            {
                keyTreeIndex = fixedKeyTreeIndex;
            }
            else
            {
                keyTreeIndex = Random.Range(0, trees.Count);
            }
            Log("KEY IS IN NEW TREE INDEX: " + keyTreeIndex);
            RevealKeyTreeForTesting();
        }
        
        Log("All trees reset - " + trees.Count + " trees available");
    }

    void Log(string msg)
    {
        if (enableDebugLogs || GlobalDebugSettings.EnableAllLogs)
            Debug.Log(msg);
    }

    void RevealKeyTreeForTesting()
    {
        if (!revealKeyTreeInConsole) return;
        if (keyTreeIndex < 0 || keyTreeIndex >= trees.Count) return;
        if (trees[keyTreeIndex] == null) return;
    }
}
