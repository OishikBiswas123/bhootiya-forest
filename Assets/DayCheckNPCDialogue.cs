using UnityEngine;

public class DayCheckNPCDialogue : MonoBehaviour
{
    [Header("NPC Settings")]
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.X;
    public float inputCooldown = 0.3f;
    private float lastInputTime = 0f;

    [Header("Main Dialogue")]
    [TextArea(2, 3)]
    public string greetingLine = "Hi! How's your day going?";

    [Header("Choices")]
    public string choice1 = "I'm doing good.";
    public string choice2 = "Not so well.";
    public string choice3 = "Just surviving.";
    public string choice4 = "I don't want to talk.";

    [Header("Replies")]
    [TextArea(2, 3)]
    public string reply1 = "That's great to hear! Hope it stays that way.";
    [TextArea(2, 3)]
    public string reply2 = "I'm sorry to hear that. I hope things get better soon.";
    [TextArea(2, 3)]
    public string reply3 = "Fair enough... some days are like that.";
    [TextArea(2, 3)]
    public string reply4 = "That's okay. Take care.";

    private Transform player;
    private bool isInteracting = false;
    private bool waitingToShowChoices = false;
    private bool waitingForChoice = false;
    private bool waitingToClose = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (player == null) return;
        
        // Skip if Game Info panel is showing
        if (UIManager.Instance != null && UIManager.Instance.IsGameInfoActive()) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > interactionDistance) return;

if (!InputBridge.GetKeyDown(interactKey)) return;
        
        if (Time.time - lastInputTime < inputCooldown) return;
        lastInputTime = Time.time;
        
        if (!isInteracting)
        {
            StartDialogue();
            return;
        }
        
        if (waitingToShowChoices)
        {
            waitingToShowChoices = false;
            ShowChoices();
            return;
        }
        
        if (waitingForChoice)
        {
            return;
        }

        if (waitingToClose)
        {
            EndDialogue();
        }
    }

    void StartDialogue()
    {
        isInteracting = true;
        waitingToShowChoices = true;
        waitingForChoice = false;
        waitingToClose = false;

        UIManager.Instance?.CloseDialogue();
        GameManager.Instance?.StartInteraction();
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.onDialogueClosed += OnUIManagerDialogueClosed;
            UIManager.Instance.onDialogueAdvance += OnUIManagerDialogueAdvance;
        }
        
        UIManager.Instance?.ShowDialogue(greetingLine, false, true);
    }
    
    void ShowChoices()
    {
        waitingForChoice = true;
        UIManager.Instance?.ShowDialogueWithChoices(
            greetingLine,
            OnChoice1,
            OnChoice2,
            OnChoice3,
            OnChoice4
        );
        UIManager.Instance?.SetChoiceButtonTexts(choice1, choice2, choice3, choice4);
    }

    void OnChoice1()
    {
        ShowReply(reply1);
    }

    void OnChoice2()
    {
        ShowReply(reply2);
    }

    void OnChoice3()
    {
        ShowReply(reply3);
    }

    void OnChoice4()
    {
        ShowReply(reply4);
    }

    void ShowReply(string reply)
    {
        waitingForChoice = false;
        waitingToClose = true;
        UIManager.Instance?.ShowDialogue(reply, false, true);
    }

void EndDialogue()
    {
        isInteracting = false;
        waitingToShowChoices = false;
        waitingForChoice = false;
        waitingToClose = false;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.onDialogueClosed -= OnUIManagerDialogueClosed;
            UIManager.Instance.onDialogueAdvance -= OnUIManagerDialogueAdvance;
        }
        
        UIManager.Instance?.CloseDialogue();
        GameManager.Instance?.EndInteraction();
    }
    
    void OnUIManagerDialogueClosed()
    {
        Debug.Log("DayCheckNPC: OnUIManagerDialogueClosed called");
        if (isInteracting)
        {
            EndDialogue();
        }
    }
    
    void OnUIManagerDialogueAdvance()
    {
        Debug.Log("DayCheckNPC: OnUIManagerDialogueAdvance called");
        if (isInteracting)
        {
            EndDialogue();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
