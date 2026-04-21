using System.Collections;
using UnityEngine;

public class GrandmaHomeDialogue : MonoBehaviour
{
    public static GrandmaHomeDialogue Instance;

    [Header("NPC Settings")]
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.X;
    public float inputCooldown = 0.3f;
    private float lastInputTime = 0f;

    [Header("Outside -> Home Dialogue")]
    [TextArea(2, 3)]
    public string welcomeHomeLine = "Hi dear, welcome home...";
    [TextArea(2, 3)]
    public string dayQuestionLine = "How was your day today?";
    [TextArea(2, 3)]
    public string[] outsideFollowupLines = new string[]
    {
        "You shouldn't stay outside for too long.",
        "Go upstairs to your room and get some rest."
    };
    public string outsideRepeatLine = "Go to your room upstairs and get some rest.";
    public string greatResponse = "That's good to hear...";
    public string badResponse = "That's so sad...";

    [Header("From Upstairs Dialogue")]
    [TextArea(2, 3)]
    public string[] upstairsLines = new string[]
    {
        "Hi, sweetheart... you're awake so soon?",
        "Did you sleep well? I hope you have a lovely day today.",
        "But don't wander too far from the city...",
        "Now go on, enjoy your day."
    };
    public string upstairsRepeatLine = "Have a nice day...";
    
    [Header("Gate Stop Auto-Walk (Upstairs Flow Only)")]
    public float gateWalkSpeed = 3f;
    public int gateWalkStepsRight = 2;
    public int gateWalkStepsUp = 2;

    private Transform player;
    private bool isInteracting = false;
    private bool waitingForMoodChoice = false;
    private bool waitingAfterMoodResponse = false;
    private bool usingOutsideFlow = true;
    private int lineIndex = 0;
    private bool moodChoiceShown = false;
    private bool dayQuestionShown = false;
    private int outsideFollowupIndex = -1;
    private GrandmaDialogueSource activeSource = GrandmaDialogueSource.None;
    private bool activeSourceIntroCompleted = false;
    private bool startFromGateStop = false;
    private bool useGateAutoWalkForThisConversation = false;
    private bool pendingGateAutoWalk = false;
    private bool isAutoWalking = false;

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (player == null) return;
        
        // Skip if Game Info panel is showing
        if (UIManager.Instance != null && UIManager.Instance.IsGameInfoActive()) return;

        float distance = Vector3.Distance(transform.position, player.position);
        // Allow continuing an already-started dialogue even if player is not near grandma
        // (needed when grandma auto-stops player at door).
        if (!isInteracting && distance > interactionDistance) return;

        if (InputBridge.GetKeyDown(interactKey))
        {
            if (Time.time - lastInputTime < inputCooldown) return;
            lastInputTime = Time.time;
            HandleInteraction();
        }
    }

    void HandleInteraction()
    {
        if (isAutoWalking)
        {
            return;
        }

        if (!isInteracting)
        {
            StartDialogue();
            return;
        }

        if (waitingForMoodChoice)
        {
            return; // Choice panel handles input
        }

        if (usingOutsideFlow)
        {
            HandleOutsideFlowAdvance();
        }
        else
        {
            HandleUpstairsFlowAdvance();
        }
    }

    void StartDialogue()
    {
        UIManager.Instance?.CloseDialogue();
        GameManager.Instance?.StartInteraction();

        isInteracting = true;
        waitingForMoodChoice = false;
        waitingAfterMoodResponse = false;
        lineIndex = 0;
        moodChoiceShown = false;
        dayQuestionShown = false;
        outsideFollowupIndex = -1;
        
        // Subscribe to dialogue callbacks
        if (UIManager.Instance != null)
        {
            UIManager.Instance.onDialogueClosed += OnUIManagerDialogueClosed;
            UIManager.Instance.onDialogueAdvance += OnUIManagerDialogueAdvance;
        }

        GrandmaDialogueSource src = GrandmaDialogueContext.Consume();
        if (src != GrandmaDialogueSource.None)
        {
            activeSource = src;
            activeSourceIntroCompleted = false;
        }
        else if (activeSource == GrandmaDialogueSource.None)
        {
            // Safe fallback if no source was marked yet.
            activeSource = GrandmaDialogueSource.FromOutside;
            activeSourceIntroCompleted = true;
        }
        
        usingOutsideFlow = (activeSource != GrandmaDialogueSource.FromUpstairs);
        if (!usingOutsideFlow)
        {
            GrandmaDialogueContext.MarkUpstairsDialogueHandled();
            useGateAutoWalkForThisConversation = startFromGateStop;
            pendingGateAutoWalk = useGateAutoWalkForThisConversation;
        }
        startFromGateStop = false;

        if (usingOutsideFlow)
        {
            if (activeSourceIntroCompleted)
            {
                UIManager.Instance?.ShowDialogue(outsideRepeatLine, false, true);
            }
            else
            {
                UIManager.Instance?.ShowDialogue(welcomeHomeLine, false, true);
            }
        }
        else
        {
            if (activeSourceIntroCompleted)
            {
                UIManager.Instance?.ShowDialogue(upstairsRepeatLine, false, true);
            }
            else
            {
                bool hasMore = upstairsLines != null && upstairsLines.Length > 1;
                if (upstairsLines != null && upstairsLines.Length > 0)
                {
                    UIManager.Instance?.ShowDialogue(upstairsLines[0], false, hasMore);
                }
                else
                {
                    EndDialogue();
                }
            }
        }
    }

    public bool ForceStartUpstairsDialogue()
    {
        return ForceStartUpstairsDialogue(false);
    }

    public bool ForceStartUpstairsDialogue(bool fromGateStop)
    {
        if (isInteracting) return true;

        startFromGateStop = fromGateStop;
        GrandmaDialogueContext.SetFromUpstairs();
        StartDialogue();
        return isInteracting;
    }

    void HandleOutsideFlowAdvance()
    {
        if (activeSourceIntroCompleted)
        {
            EndDialogue();
            return;
        }
        
        if (!dayQuestionShown)
        {
            dayQuestionShown = true;
            UIManager.Instance?.ShowDialogue(dayQuestionLine, false, true);
            return;
        }
        
        if (!moodChoiceShown)
        {
            moodChoiceShown = true;
            ShowMoodChoices();
            return;
        }

        if (waitingAfterMoodResponse)
        {
            waitingAfterMoodResponse = false;
            outsideFollowupIndex = 0;
            ShowOutsideFollowupLine();
            return;
        }

        if (outsideFollowupLines == null || outsideFollowupLines.Length == 0)
        {
            activeSourceIntroCompleted = true;
            EndDialogue();
            return;
        }

        outsideFollowupIndex++;
        if (outsideFollowupIndex >= outsideFollowupLines.Length)
        {
            activeSourceIntroCompleted = true;
            EndDialogue();
            return;
        }

        ShowOutsideFollowupLine();
    }

    void ShowOutsideFollowupLine()
    {
        if (outsideFollowupLines == null || outsideFollowupLines.Length == 0)
        {
            activeSourceIntroCompleted = true;
            EndDialogue();
            return;
        }

        bool hasMore = outsideFollowupIndex < outsideFollowupLines.Length - 1;
        hasMore = true;
        UIManager.Instance?.ShowDialogue(outsideFollowupLines[outsideFollowupIndex], false, hasMore);
    }

    void ShowMoodChoices()
    {
        waitingForMoodChoice = true;
        UIManager.Instance?.ShowDialogueWithChoices2Button("How was your day?", OnMoodGreat, OnMoodBad);
        UIManager.Instance?.SetChoiceButtonTexts("Great", "Bad");
    }

void OnMoodGreat()
    {
        waitingForMoodChoice = false;
        waitingAfterMoodResponse = true;
        UIManager.Instance?.ShowDialogue(greatResponse, false, true);
    }
    
    void OnMoodBad()
    {
        waitingForMoodChoice = false;
        waitingAfterMoodResponse = true;
        UIManager.Instance?.ShowDialogue(badResponse, false, true);
    }

    void HandleUpstairsFlowAdvance()
    {
        if (isAutoWalking)
        {
            return;
        }

        if (activeSourceIntroCompleted)
        {
            EndDialogue();
            return;
        }

        if (pendingGateAutoWalk && lineIndex == 0)
        {
            StartCoroutine(DoGateAutoWalkThenContinue());
            return;
        }
        
        if (upstairsLines == null || upstairsLines.Length == 0)
        {
            activeSourceIntroCompleted = true;
            EndDialogue();
            return;
        }

        lineIndex++;
        if (lineIndex >= upstairsLines.Length)
        {
            activeSourceIntroCompleted = true;
            EndDialogue();
            return;
        }

        // Always show arrow so player can see this line and press X to advance
        bool hasMore = lineIndex < upstairsLines.Length - 1;
        hasMore = true;
        UIManager.Instance?.ShowDialogue(upstairsLines[lineIndex], false, hasMore);
    }
    
    IEnumerator DoGateAutoWalkThenContinue()
    {
        pendingGateAutoWalk = false;
        isAutoWalking = true;

        if (player == null)
        {
            isAutoWalking = false;
            AdvanceToNextUpstairsLineAfterAutoWalk();
            yield break;
        }

        PlayerMove pm = player.GetComponent<PlayerMove>();
        Animator anim = player.GetComponent<Animator>();
        bool previousEnabled = pm != null && pm.enabled;
        bool previousAnimEnabled = anim != null && anim.enabled;
        float previousAnimSpeed = anim != null ? anim.speed : 1f;
        float stepSize = (pm != null) ? Mathf.Max(0.1f, pm.gridSize) : 1f;
        float moveSpeed = Mathf.Max(0.1f, gateWalkSpeed);

        if (pm != null)
            pm.enabled = false;

        if (anim != null)
        {
            anim.enabled = true;
            anim.speed = 2f;
        }

        // 2 steps to the right
        if (pm != null)
        {
            pm.SetFacingDirection(4); // This sets direction but disables animator.
            if (anim != null) anim.enabled = true; // Re-enable animator for walking frames.
        }
        if (anim != null) anim.SetInteger("Direction", 4);
        Vector3 targetRight = player.position + new Vector3(stepSize * gateWalkStepsRight, 0f, 0f);
        yield return MovePlayerTo(targetRight, moveSpeed);

        // 2 steps up
        if (pm != null)
        {
            pm.SetFacingDirection(2); // This sets direction but disables animator.
            if (anim != null) anim.enabled = true; // Re-enable animator for walking frames.
        }
        if (anim != null) anim.SetInteger("Direction", 2);
        Vector3 targetUp = player.position + new Vector3(0f, 0f, stepSize * gateWalkStepsUp);
        yield return MovePlayerTo(targetUp, moveSpeed);

        if (pm != null)
        {
            pm.SetFacingDirection(2);
            pm.enabled = previousEnabled;
        }
        
        if (anim != null)
        {
            anim.speed = previousAnimSpeed;
            anim.enabled = previousAnimEnabled;
        }

        isAutoWalking = false;
        AdvanceToNextUpstairsLineAfterAutoWalk();
    }

    IEnumerator MovePlayerTo(Vector3 target, float speed)
    {
        // Keep original Y to avoid vertical drift.
        float fixedY = player.position.y;
        target.y = fixedY;

        while (Vector3.Distance(player.position, target) > 0.01f)
        {
            Vector3 next = Vector3.MoveTowards(player.position, target, speed * Time.deltaTime);
            next.y = fixedY;
            player.position = next;
            yield return null;
        }

        Vector3 finalPos = target;
        finalPos.y = fixedY;
        player.position = finalPos;
    }

    void AdvanceToNextUpstairsLineAfterAutoWalk()
    {
        if (upstairsLines == null || upstairsLines.Length == 0)
        {
            activeSourceIntroCompleted = true;
            EndDialogue();
            return;
        }

        lineIndex = 1;
        if (lineIndex >= upstairsLines.Length)
        {
            activeSourceIntroCompleted = true;
            EndDialogue();
            return;
        }

        bool hasMore = lineIndex < upstairsLines.Length - 1;
        hasMore = true;
        UIManager.Instance?.ShowDialogue(upstairsLines[lineIndex], false, hasMore);
    }

    void EndDialogue()
    {
        isInteracting = false;
        waitingForMoodChoice = false;
        waitingAfterMoodResponse = false;
        lineIndex = 0;
        moodChoiceShown = false;
        dayQuestionShown = false;
        outsideFollowupIndex = -1;
        startFromGateStop = false;
        
        // Unsubscribe from dialogue callbacks
        if (UIManager.Instance != null)
        {
            UIManager.Instance.onDialogueClosed -= OnUIManagerDialogueClosed;
            UIManager.Instance.onDialogueAdvance -= OnUIManagerDialogueAdvance;
        }
    }
    
    void OnUIManagerDialogueClosed()
    {
        // Dialogue was closed externally - end our interaction
        if (isInteracting)
        {
            EndDialogue();
        }
    }
    
    void OnUIManagerDialogueAdvance()
    {
        // Player pressed X to advance dialogue
        if (isInteracting)
        {
            if (isAutoWalking)
            {
                return;
            }

            if (usingOutsideFlow)
            {
                HandleOutsideFlowAdvance();
            }
            else
            {
                HandleUpstairsFlowAdvance();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
