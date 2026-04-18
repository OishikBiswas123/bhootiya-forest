using System.Collections;
using UnityEngine;

public class HomeGuideNPCDialogue : MonoBehaviour
{
    [Header("NPC Settings")]
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.X;
    public float inputCooldown = 0.3f;
    private float lastInputTime = 0f;

    [Header("Intro Lines")]
    [TextArea(2, 3)]
    public string[] introLines = new string[]
    {
        "I know this game might feel a little confusing…",
        "Let me guide you."
    };

    [Header("Choice Text")]
    public string option1Text = "What should I do now?";
    public string option2Text = "Send me to the game area";
    public string option3Text = "Any tips for survival?";
    public string option4Text = "No thanks, I'll manage";

    [Header("Replies")]
    [TextArea(2, 3)]
    public string option1Reply = "You can explore the area freely—walk around, visit places, and check inside buildings.";
    [TextArea(2, 3)]
    public string option2Reply = "I can skip the journey and take you straight to where the real game begins.";
    [TextArea(2, 3)]
    public string option3Reply = "Stay alert… and don't ignore anything that feels unusual.";
    [TextArea(2, 3)]
    public string option4Reply = "Alright… take your time and explore carefully.";

    [Header("Teleport")]
    public Transform citySpawnPoint;
    public float fadeHoldDuration = 3f;

    private Transform player;
    private bool isInteracting = false;
    private bool waitingToShowChoices = false;
    private bool showingChoices = false;
    private bool showingReply = false;
    private bool teleportAfterReply = false;
    private bool isTeleporting = false;
    private int currentIntroIndex = 0;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj != null) player = playerObj.transform;

        if (citySpawnPoint == null)
        {
            GameObject cityObj = GameObject.Find("CitySpawnPoint");
            if (cityObj != null) citySpawnPoint = cityObj.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > interactionDistance) return;
        if (isTeleporting) return;

        if (InputBridge.GetKeyDown(interactKey))
        {
            HandleInteraction();
        }
    }

    void HandleInteraction()
    {
        if (Time.time - lastInputTime < inputCooldown) return;
        lastInputTime = Time.time;
        
        if (!isInteracting)
        {
            StartDialogue();
            return;
        }

        if (showingChoices)
        {
            return;
        }

        if (waitingToShowChoices)
        {
            waitingToShowChoices = false;
            ShowChoices();
            return;
        }

        if (showingReply)
        {
            if (teleportAfterReply)
            {
                StartCoroutine(TeleportToGameArea());
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        AdvanceIntro();
    }

    void StartDialogue()
    {
        isInteracting = true;
        waitingToShowChoices = false;
        showingChoices = false;
        showingReply = false;
        teleportAfterReply = false;
        currentIntroIndex = 0;

        UIManager.Instance?.CloseDialogue();
        GameManager.Instance?.StartInteraction();

        if (UIManager.Instance != null)
        {
            bool hasMore = introLines.Length > 1;
            UIManager.Instance.ShowDialogue(introLines[0], false, hasMore);
        }
    }

    void AdvanceIntro()
    {
        currentIntroIndex++;
        if (currentIntroIndex < introLines.Length)
        {
            bool isLast = currentIntroIndex == introLines.Length - 1;
            UIManager.Instance?.ShowDialogue(introLines[currentIntroIndex], false, true);
            if (isLast)
            {
                waitingToShowChoices = true;
            }
            return;
        }

        ShowChoices();
    }

    void ShowChoices()
    {
        showingChoices = true;
        if (UIManager.Instance == null) return;

        UIManager.Instance.ShowDialogueWithChoices(
            "Choose what you want to know.",
            () => SelectOption(2),
            () => SelectOption(1),
            () => SelectOption(3),
            () => SelectOption(4)
        );
        UIManager.Instance.SetChoiceButtonTexts(option2Text, option1Text, option3Text, option4Text);
    }

    void SelectOption(int option)
    {
        showingChoices = false;
        showingReply = true;
        teleportAfterReply = false;

        string reply = option1Reply;
        switch (option)
        {
            case 1:
                reply = option1Reply;
                break;
            case 2:
                reply = option2Reply;
                teleportAfterReply = true;
                break;
            case 3:
                reply = option3Reply;
                break;
            case 4:
                reply = option4Reply;
                break;
        }

        UIManager.Instance?.CloseDialogue();
        UIManager.Instance?.ShowDialogue(reply, false, true);
    }

    IEnumerator TeleportToGameArea()
    {
        if (isTeleporting) yield break;
        isTeleporting = true;
        showingReply = false;

        if (player == null || citySpawnPoint == null)
        {
            isTeleporting = false;
            EndDialogue();
            yield break;
        }

        UIManager.Instance?.CloseDialogue();

        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeToBlackWithHold(() =>
            {
                player.position = citySpawnPoint.position;
                PlayerMove pm = player.GetComponent<PlayerMove>();
                if (pm != null)
                {
                    pm.SetFacingDirection(1);
                }
                AreaMusicManager.Instance?.RefreshZonesForPlayer(player);
            }, fadeHoldDuration);
        }
        else
        {
            player.position = citySpawnPoint.position;
            PlayerMove pm = player.GetComponent<PlayerMove>();
            if (pm != null)
            {
                pm.SetFacingDirection(1);
            }
            AreaMusicManager.Instance?.RefreshZonesForPlayer(player);
        }

        isTeleporting = false;
        EndDialogue();
    }

    void EndDialogue()
    {
        isInteracting = false;
        waitingToShowChoices = false;
        showingChoices = false;
        showingReply = false;
        teleportAfterReply = false;
        isTeleporting = false;
        currentIntroIndex = 0;

        UIManager.Instance?.CloseDialogue();
        GameManager.Instance?.EndInteraction();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
