using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Oishik Biswas";
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.X;
    public float inputCooldown = 0.3f;
    private float lastInputTime = 0f;
    
    [Header("Initial Dialogue Lines")]
    [TextArea(2, 3)]
    public string[] introLines = new string[]
    {
        "Hi, my name is Oishik Biswas, the developer of this game.",
        "The building behind me is still under construction, so it's closed for now. So, Feel free to look all around.",
        "Do you have any other queries?"
    };
    
    [Header("Question Choices")]
    public string[] questions = new string[]
    {
        "What building is this?",
        "How to play this game?",
        "How did you make this game?",
        "No thanks, I'm good."
    };
    
    [Header("Answers - Split into lines (show one at a time)")]
    [TextArea(2, 3)]
    public string[] answer1Lines = new string[]
    {
        "Oh, this place is still under construction.",
        "I'm working on some great features inside.",
        "You'll get an update once it's finished.",
        "Let it be a surprise until then."
    };
    
    [TextArea(2, 3)]
    public string[] answer2Lines = new string[]
    {
        "This place has two areas.",
        "The first one is peaceful—you can explore the town and the forest at your own pace.",
        "But the real story lies beyond.",
        "If you're curious, ask the old lady near the pond… she might guide you."
    };
    
    [TextArea(2, 3)]
    public string[] answer3Lines = new string[]
    {
        "I created it in Unity.",
        "It took me about 15–20 days to build.",
        "It's my very first game, so it may seem a bit simple…",
        "But I hope you enjoy exploring the world I've made."
    };
    
    [Header("Repeat Question")]
    [TextArea(2, 3)]
    public string repeatQuestion = "Do you have any other queries?";
    
    private Transform player;
    private bool playerInRange = false;
    private bool isInteracting = false;
    private int currentLineIndex = 0;
    private bool showingChoices = false;
    private bool waitingToShowChoices = false;
    private bool inAnswerPhase = false;
    private int currentAnswerLineIndex = 0;
    private string[] currentAnswerLines = null;
    
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= interactionDistance)
        {
            if (!playerInRange && !isInteracting)
            {
                playerInRange = true;
                // No approach prompt - player figures it out
            }
            
            if (InputBridge.GetKeyDown(interactKey))
            {
                HandleInteraction();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                if (!isInteracting)
                {
                    // No prompt to hide
                }
            }
        }
    }
    
    void HandleInteraction()
    {
        // Prevent rapid X clicks
        if (Time.time - lastInputTime < inputCooldown) return;
        lastInputTime = Time.time;
        
        if (!isInteracting)
        {
            // Start interaction
            StartDialogue();
        }
        else if (inAnswerPhase)
        {
            // Show next line of answer or return to choices
            AdvanceAnswerLine();
        }
        else if (waitingToShowChoices)
        {
            waitingToShowChoices = false;
            ShowChoices();
        }
        else if (showingChoices)
        {
            // Do nothing - waiting for button selection
        }
        else
        {
            // Continue to next intro line
            AdvanceDialogue();
        }
    }
    
    void StartDialogue()
    {
        isInteracting = true;
        currentLineIndex = 0;
        showingChoices = false;
        waitingToShowChoices = false;
        inAnswerPhase = false;
        currentAnswerLineIndex = 0;
        currentAnswerLines = null;
        
        // Close any existing UI first (in case tree or other dialogue is showing)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }
        
        // Freeze player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartInteraction();
        }
        
        // Show first intro line with arrow (there are more lines)
        if (UIManager.Instance != null)
        {
            bool hasMore = introLines.Length > 1;
            UIManager.Instance.ShowDialogue(introLines[0], false, hasMore);
        }
    }
    
    void AdvanceDialogue()
    {
        currentLineIndex++;
        
        if (currentLineIndex < introLines.Length)
        {
            // Show next intro line; if this is last line, wait one more X before showing choices.
            bool isLastIntroLine = (currentLineIndex == introLines.Length - 1);
            bool hasMore = isLastIntroLine ? true : (currentLineIndex < introLines.Length - 1);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue(introLines[currentLineIndex], false, hasMore);
            }
            
            if (isLastIntroLine)
            {
                waitingToShowChoices = true;
            }
        }
    }
    
    void ShowChoices()
    {
        showingChoices = true;
        
        if (UIManager.Instance != null)
        {
            // Map questions to buttons: Questions 1,2,3 on top (buttons 1,2,3), Exit on bottom (button 4)
            // Button order from top to bottom: Q1, Q2, Q3, Exit
            UIManager.Instance.ShowDialogueWithChoices(
                repeatQuestion,
                () => OnQuestionSelected(0),  // Button 1 (top) - Question 1
                () => OnQuestionSelected(1),  // Button 2 - Question 2
                () => OnQuestionSelected(2),  // Button 3 - Question 3
                () => OnQuestionSelected(3)   // Button 4 (bottom) - Exit
            );
            
            // Update button texts - Q1, Q2, Q3 on top buttons, Exit on bottom
            UIManager.Instance.SetChoiceButtonTexts(questions[0], questions[1], questions[2], questions[3]);
        }
    }
    
    void OnQuestionSelected(int questionIndex)
    {
        if (questionIndex == 3) // "No thanks, I'm good."
        {
            EndDialogue();
        }
        else
        {
            // Start showing the answer line by line
            showingChoices = false;
            inAnswerPhase = true;
            currentAnswerLineIndex = 0;
            
            // Get the appropriate answer lines
            switch (questionIndex)
            {
                case 0:
                    currentAnswerLines = answer1Lines;
                    break;
                case 1:
                    currentAnswerLines = answer2Lines;
                    break;
                case 2:
                    currentAnswerLines = answer3Lines;
                    break;
            }
            
            // Show first line of the answer
            if (currentAnswerLines != null && currentAnswerLines.Length > 0 && UIManager.Instance != null)
            {
                UIManager.Instance.CloseDialogue();
                bool hasMore = currentAnswerLines.Length > 1;
                UIManager.Instance.ShowDialogue(currentAnswerLines[0], false, hasMore);
            }
        }
    }
    
    void AdvanceAnswerLine()
    {
        currentAnswerLineIndex++;
        
        if (currentAnswerLines != null && currentAnswerLineIndex < currentAnswerLines.Length)
        {
            // Show next line of the answer - check if it's the last one
            bool hasMore = (currentAnswerLineIndex < currentAnswerLines.Length - 1);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue(currentAnswerLines[currentAnswerLineIndex], false, hasMore);
            }
        }
        else
        {
            // Finished all lines, return to choices
            ReturnToChoices();
        }
    }
    
    void ReturnToChoices()
    {
        inAnswerPhase = false;
        currentAnswerLineIndex = 0;
        currentAnswerLines = null;
        ShowChoices();
    }
    
    void EndDialogue()
    {
        isInteracting = false;
        currentLineIndex = 0;
        showingChoices = false;
        waitingToShowChoices = false;
        inAnswerPhase = false;
        currentAnswerLineIndex = 0;
        currentAnswerLines = null;
        
        // Hide dialogue
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseDialogue();
        }
        
        // Unfreeze player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndInteraction();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
