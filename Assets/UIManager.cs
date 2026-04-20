using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    // Callback for when dialogue is closed (for NPCs to end their dialogue)
    public System.Action onDialogueClosed;
    // Callback for when player presses X to advance dialogue (for hasMore=true cases)
    public System.Action onDialogueAdvance;
    
    [Header("Dialogue Box")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    
    [Header("Continue Indicator")]
    public GameObject continueArrow; // Inverted triangle or arrow indicating more text
    public float arrowBounceSpeed = 2f;
    public float arrowBounceHeight = 5f;
    private bool hasMoreDialogue = false;
    private Vector2 arrowBasePosition;
    
    [Header("Choice Buttons (2-Button Mode)")]
    public GameObject choicePanel;
    public Button yesButton;
    public Button noButton;
    public TextMeshProUGUI yesButtonText;
    public TextMeshProUGUI noButtonText;
    
    [Header("Choice Buttons (4-Button Mode - Optional)")]
    public Button button3;
    public Button button4;
    public TextMeshProUGUI button3Text;
    public TextMeshProUGUI button4Text;

    [Header("Choice Panel Layout")]
    public bool autoResizeChoicePanel = false;
    public Vector2 choicePanelSize2 = new Vector2(200f, 80f);
    public Vector2 choicePanelPos2 = new Vector2(265f, 150f);
    public Vector2 choicePanelSize4 = new Vector2(800f, 300f);
    public Vector2 choicePanelPos4 = new Vector2(597f, 1120f);
    public bool autoRepositionButtons2 = true;
    public bool autoRepositionButtons4 = false;
    public bool align2ButtonPanelTo4Bottom = true;
    public bool autoFormatChoiceText = true;
    public float choiceTextMinSize = 18f;
    public float choiceTextMaxSize = 36f;
    public float twoButtonSidePadding = 20f;
    public float twoButtonVerticalPadding = 12f;
    public float twoButtonGap = 10f;
    public float fourButtonSidePadding = 20f;
    public float fourButtonVerticalPadding = 16f;
    public float fourButtonGap = 10f;
    
    private System.Action onYesClicked;
    private System.Action onNoClicked;
    private System.Action onButton3Clicked;
    private System.Action onButton4Clicked;
    private bool showingChoices = false;
    private Button currentSelectedButton;
    private int choiceCount = 2; // 2 or 4 buttons
    private bool isHorizontal2Button = false; // true for trees (left/right), false for NPC 4-button (up/down)
    
    // Scrollable 4-button list mode (used by office lift floor menu)
    private bool useScrollableChoices = false;
    private string[] scrollableOptions = null;
    private int scrollSelectedIndex = 0;
    private int scrollTopIndex = 0;
    private System.Action<int, string> onScrollableChoiceSelected;
    private bool promptClickSfxEnabledForCurrentDialogue = false;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    [Header("Game Info Panel")]
    public GameObject gameInfoPanel;
    public TextMeshProUGUI gameInfoBodyText;
    public TextMeshProUGUI gameInfoPageText;
    public GameObject gameInfoContinueArrow;
    private int gameInfoCurrentPage = 1;
    private const int totalGameInfoPages = 3;
    private bool gameInfoActive = false;
    
    [Header("UI SFX (Optional)")]
    public AudioSource uiSfxSource;
    public AudioClip choiceClickSfx;
    public AudioClip promptClickSfx;
    
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
    }
    
    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (choicePanel != null)
            choicePanel.SetActive(false);
        if (continueArrow != null)
            continueArrow.SetActive(false);
        if (gameInfoPanel != null)
            gameInfoPanel.SetActive(false);
        if (gameInfoContinueArrow != null)
            gameInfoContinueArrow.SetActive(false);
        if (uiSfxSource != null)
            uiSfxSource.ignoreListenerPause = true;
            
        // Store base position for arrow animation
        if (continueArrow != null)
        {
            RectTransform arrowRect = continueArrow.GetComponent<RectTransform>();
            if (arrowRect != null)
                arrowBasePosition = arrowRect.anchoredPosition;
        }
            
        // Setup button click events
        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesButtonClick);
        if (noButton != null)
            noButton.onClick.AddListener(OnNoButtonClick);
        if (button3 != null)
            button3.onClick.AddListener(OnButton3Click);
        if (button4 != null)
            button4.onClick.AddListener(OnButton4Click);
    }
    
private float inputBufferTime = 0.2f;
    private float inputBuffer = 0f;
    [Header("Dialogue Advance")]
    public float dialogueAdvanceCooldown = 0.3f;
    private float lastAdvanceTime = 0f;
    private float dialogueShowTime = 0f; // Time when dialogue was shown - prevents immediate close
    private float dialogueCloseDelay = 0.3f; // Delay before X can close
    
    [Header("Choice Navigation")]
    public float navHoldDelay = 0.35f;
    public float navRepeatRate = 0.1f;
    private int navHeldDirection = 0; // -1 = up, 1 = down
    private float navRepeatTimer = 0f;
    private int lastAxisDir = 0;
    
    void Update()
    {
        if (inputBuffer > 0)
        {
            inputBuffer -= Time.deltaTime;
        }
        
        // Animate continue arrow bouncing
        if (continueArrow != null && continueArrow.activeSelf && hasMoreDialogue)
        {
            AnimateContinueArrow();
        }
        
        if (!showingChoices)
        {
            ResetNavigationRepeat();
            
            // Handle Game Info panel input
            if (gameInfoActive)
            {
                HandleGameInfoInput();
                return;
            }
            
            // Handle KEY FOUND special 2-line sequence only
            if (dialoguePanel != null && dialoguePanel.activeSelf && 
                dialogueText != null && dialogueText.text == "You found the KEY!")
            {
                if (InputBridge.GetKeyDown(KeyCode.X))
                {
                    dialogueText.text = "Please return to the hut to escape the forest.";
                    PlayPromptClickSfx();
                }
                return;
            }
            
            // Second line of key found - close dialogue and unfreeze game
            if (dialoguePanel != null && dialoguePanel.activeSelf && 
                dialogueText != null && dialogueText.text == "Please return to the hut to escape the forest.")
            {
                if (InputBridge.GetKeyDown(KeyCode.X))
                {
                    CloseDialogue();
                    if (GameManager.Instance != null)
                        GameManager.Instance.EndInteraction();
                    GhostAI[] ghosts = FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
                    foreach (GhostAI ghost in ghosts)
                    {
                        ghost.enabled = true;
                    }
                }
                return;
            }
            
            // Handle simple dialogue close (no choices, no hasMore) - close on X
            if (dialoguePanel != null && dialoguePanel.activeSelf && !hasMoreDialogue && !showingChoices)
            {
                if (InputBridge.GetKeyDown(KeyCode.X))
                {
                    CloseDialogue();
                    return;
                }
            }
            
            // Handle dialogue with arrow (hasMore=true) - close on X and unfreeze player
            if (dialoguePanel != null && dialoguePanel.activeSelf && hasMoreDialogue && !showingChoices)
            {
                if (InputBridge.GetKeyDown(KeyCode.X))
                {
                    // Don't close immediately - give time for dialogue to show
                    if (Time.time - dialogueShowTime > dialogueCloseDelay)
                    {
                        CloseDialogue();
                        if (GameManager.Instance != null)
                            GameManager.Instance.EndInteraction();
                    }
                }
                return;
            }
            
            // No active UI, exit
            return;
        }
        
        // Handle Game Info panel input (even when showing choices)
        if (gameInfoActive)
        {
            HandleGameInfoInput();
        }
        
        // Keyboard navigation
        HandleNavigation();
        
// Press X to select the highlighted button (with buffer to prevent immediate trigger)
        if (inputBuffer <= 0 && InputBridge.GetKeyDown(KeyCode.X))
        {
            SelectCurrentButton();
        }
    }
    
    public void SetInputCooldown(bool enable)
    {
        if (enable)
            inputBuffer = inputBufferTime;
        else
            inputBuffer = 0f;
    }
    
    void AnimateContinueArrow()
    {
        RectTransform arrowRect = continueArrow.GetComponent<RectTransform>();
        if (arrowRect != null)
        {
            // Bounce up and down using sine wave
            float bounce = Mathf.Sin(Time.time * arrowBounceSpeed) * arrowBounceHeight;
            arrowRect.anchoredPosition = arrowBasePosition + new Vector2(0, bounce);
        }
    }
    
    void HandleNavigation()
    {
        int direction = GetNavigationDirection();
        if (direction == 0) return;
        
        // Prompt-style click while moving selection up/down.
        PlayPromptClickSfx();
        
        if (choiceCount == 2)
        {
            NavigateTwoButtonLoop(direction);
        }
        else if (choiceCount == 4)
        {
            if (useScrollableChoices)
            {
                NavigateScrollableLoop(direction);
                return;
            }
            
            NavigateFourButtonLoop(direction);
        }
    }
    
    int GetNavigationDirection()
    {
        // Keyboard first (single step per key press, then slow repeat).
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            navHeldDirection = -1;
            navRepeatTimer = navHoldDelay;
            return -1;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            navHeldDirection = 1;
            navRepeatTimer = navHoldDelay;
            return 1;
        }

        if (navHeldDirection == -1 && Input.GetKey(KeyCode.UpArrow))
        {
            navRepeatTimer -= Time.deltaTime;
            if (navRepeatTimer <= 0f)
            {
                navRepeatTimer = navRepeatRate;
                return -1;
            }
            return 0;
        }
        if (navHeldDirection == 1 && Input.GetKey(KeyCode.DownArrow))
        {
            navRepeatTimer -= Time.deltaTime;
            if (navRepeatTimer <= 0f)
            {
                navRepeatTimer = navRepeatRate;
                return 1;
            }
            return 0;
        }

        // Mobile D-pad / axis (edge-trigger + slow repeat while held)
        float vAxis = InputBridge.GetAxisRaw("Vertical");
        int axisDir = (vAxis > 0.5f) ? -1 : (vAxis < -0.5f) ? 1 : 0;

        if (axisDir != 0)
        {
            if (axisDir != lastAxisDir)
            {
                lastAxisDir = axisDir;
                navHeldDirection = axisDir;
                navRepeatTimer = navHoldDelay;
                return axisDir;
            }

            navRepeatTimer -= Time.deltaTime;
            if (navRepeatTimer <= 0f)
            {
                navRepeatTimer = navRepeatRate;
                return axisDir;
            }
            return 0;
        }

        lastAxisDir = 0;
        ResetNavigationRepeat();
        return 0;
    }
    
    void ResetNavigationRepeat()
    {
        navHeldDirection = 0;
        navRepeatTimer = 0f;
    }
    
    void NavigateTwoButtonLoop(int direction)
    {
        // With 2 choices, up/down both just toggle.
        if (currentSelectedButton == yesButton)
            SelectButton(noButton);
        else
            SelectButton(yesButton);
    }
    
    void NavigateFourButtonLoop(int direction)
    {
        Button[] buttons = new Button[] { yesButton, noButton, button3, button4 };
        int currentIndex = 0;
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (currentSelectedButton == buttons[i])
            {
                currentIndex = i;
                break;
            }
        }
        
        int nextIndex = (currentIndex + direction + buttons.Length) % buttons.Length;
        SelectButton(buttons[nextIndex]);
    }
    
    void NavigateScrollableLoop(int direction)
    {
        if (scrollableOptions == null || scrollableOptions.Length == 0) return;
        
        int len = scrollableOptions.Length;
        scrollSelectedIndex = (scrollSelectedIndex + direction + len) % len;
        
        if (len <= 4)
        {
            scrollTopIndex = 0;
        }
        else
        {
            if (scrollSelectedIndex < scrollTopIndex)
            {
                scrollTopIndex = scrollSelectedIndex;
            }
            else if (scrollSelectedIndex > scrollTopIndex + 3)
            {
                scrollTopIndex = scrollSelectedIndex - 3;
            }
        }
        
        RefreshScrollableChoiceView();
    }
    
    void NavigateUp()
    {
        // Vertical navigation for 4 stacked buttons
        if (currentSelectedButton == noButton)
            SelectButton(yesButton);
        else if (currentSelectedButton == button3)
            SelectButton(noButton);
        else if (currentSelectedButton == button4)
            SelectButton(button3);
    }
    
    void NavigateDown()
    {
        // Vertical navigation for 4 stacked buttons
        if (currentSelectedButton == yesButton)
            SelectButton(noButton);
        else if (currentSelectedButton == noButton)
            SelectButton(button3);
        else if (currentSelectedButton == button3)
            SelectButton(button4);
    }
    
    void NavigateLeft()
    {
        // No horizontal movement in vertical layout
    }
    
    void NavigateRight()
    {
        // No horizontal movement in vertical layout
    }
    
    void SelectCurrentButton()
    {
        if (currentSelectedButton == yesButton)
        {
            OnYesButtonClick();
            showingChoices = false;
        }
        else if (currentSelectedButton == noButton)
        {
            OnNoButtonClick();
            showingChoices = false;
        }
        else if (currentSelectedButton == button3)
        {
            OnButton3Click();
            showingChoices = false;
        }
        else if (currentSelectedButton == button4)
        {
            OnButton4Click();
            showingChoices = false;
        }
    }
    
    public void SetInputBuffer()
    {
        inputBuffer = inputBufferTime;
    }
    
    void SetupButtonNavigation()
    {
        if (choiceCount == 2)
        {
            // Setup 2-button navigation
            if (yesButton != null && noButton != null)
            {
                Navigation yesNav = new Navigation();
                yesNav.mode = Navigation.Mode.Explicit;
                yesNav.selectOnRight = noButton;
                yesButton.navigation = yesNav;
                
                Navigation noNav = new Navigation();
                noNav.mode = Navigation.Mode.Explicit;
                noNav.selectOnLeft = yesButton;
                noButton.navigation = noNav;
            }
        }
        else
        {
            // Setup 4-button VERTICAL navigation (stacked vertically)
            if (yesButton != null)
            {
                Navigation nav = new Navigation();
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnDown = noButton;
                yesButton.navigation = nav;
            }
            
            if (noButton != null)
            {
                Navigation nav = new Navigation();
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = yesButton;
                nav.selectOnDown = button3;
                noButton.navigation = nav;
            }
            
            if (button3 != null)
            {
                Navigation nav = new Navigation();
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = noButton;
                nav.selectOnDown = button4;
                button3.navigation = nav;
            }
            
            if (button4 != null)
            {
                Navigation nav = new Navigation();
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = button3;
                button4.navigation = nav;
            }
        }
    }
    
    void SwitchSelection2Buttons()
    {
        if (currentSelectedButton == yesButton)
        {
            SelectButton(noButton);
        }
        else
        {
            SelectButton(yesButton);
        }
    }
    
    void SelectButton(Button button)
    {
        if (button == null) return;
        
        currentSelectedButton = button;
        
        // Highlight the selected button
        HighlightButton(yesButton, button == yesButton);
        HighlightButton(noButton, button == noButton);
        if (choiceCount == 4)
        {
            HighlightButton(button3, button == button3);
            HighlightButton(button4, button == button4);
        }
    }
    
    void HighlightButton(Button button, bool highlight)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        if (highlight)
        {
            colors.normalColor = Color.yellow; // Highlight color
            colors.highlightedColor = Color.yellow;
        }
        else
        {
            colors.normalColor = Color.white; // Normal color
            colors.highlightedColor = Color.white;
        }
        button.colors = colors;
    }
    
    public void ShowDialogue(string message, bool waitForInput)
    {
        Log("ShowDialogue (simple) called: " + message);
        promptClickSfxEnabledForCurrentDialogue = waitForInput;
        
        // Keep prompt click timing consistent across all scripts:
        // play when a new manual prompt line is shown.
        if (promptClickSfxEnabledForCurrentDialogue)
        {
            PlayPromptClickSfx();
        }
        
        if (dialoguePanel != null && dialogueText != null)
        {
            dialogueText.text = message;
            dialoguePanel.SetActive(true);
            
            // Hide choice buttons
            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
                Log("ChoicePanel hidden (simple dialogue)");
            }
                
            showingChoices = false;
        }
    }
    
    // New overload with hasMore parameter to show/hide continue arrow
    public void ShowDialogue(string message, bool waitForInput, bool hasMore)
    {
        ShowDialogue(message, waitForInput, hasMore, true);
    }

    // playPromptSfxOnShow=false is useful for auto-generated prompts.
    public void ShowDialogue(string message, bool waitForInput, bool hasMore, bool playPromptSfxOnShow)
    {
        ShowDialogue(message, waitForInput);
        // Enable prompt click only for dialogues advanced manually.
        // Auto-disappearing prompts (waitForInput=false, hasMore=false) stay silent.
        promptClickSfxEnabledForCurrentDialogue = waitForInput || hasMore;
        
        // If this is manual-via-hasMore only, play click once here.
        if (playPromptSfxOnShow && !waitForInput && hasMore)
        {
            PlayPromptClickSfx();
        }
        
        // Show or hide continue arrow based on hasMore
        hasMoreDialogue = hasMore;
        if (continueArrow != null)
        {
            continueArrow.SetActive(hasMore);
        }
        
        // Reset timing to prevent immediate close on first X press
        dialogueShowTime = Time.time;
    }
    
    // 2-button version for Trees - uses VERTICAL navigation (Up/Down arrows)
    public void ShowDialogueWithChoices2Button(string message, System.Action onYes, System.Action onNo)
    {
        useScrollableChoices = false;
        choiceCount = 2;
        isHorizontal2Button = false; // Vertical navigation for 2 buttons
        
        // Hide 3rd and 4th buttons completely
        if (button3 != null) button3.gameObject.SetActive(false);
        if (button4 != null) button4.gameObject.SetActive(false);
        
        // Make sure YES/NO buttons are visible
        if (yesButton != null) yesButton.gameObject.SetActive(true);
        if (noButton != null) noButton.gameObject.SetActive(true);
        
        // Clear any old text from button 3 and 4
        if (button3Text != null) button3Text.text = "";
        if (button4Text != null) button4Text.text = "";
        
        // Make panel smaller for 2 buttons
        ResizeChoicePanel(2);
        
        // Reposition YES/NO buttons to fit smaller panel (centered vertically)
        if (autoRepositionButtons2)
            RepositionButtonsFor2ButtonMode();
        
        onYesClicked = onYes;
        onNoClicked = onNo;
        
        ShowChoicesCommon(message);
    }
    
    // New 4-button version - uses VERTICAL navigation (Up/Down arrows)
    public void ShowDialogueWithChoices(string message, System.Action onYes, System.Action onNo, System.Action onBtn3, System.Action onBtn4)
    {
        useScrollableChoices = false;
        choiceCount = 4;
        isHorizontal2Button = false; // Vertical navigation for 4 buttons
        
        // Show all 4 buttons
        if (yesButton != null) yesButton.gameObject.SetActive(true);
        if (noButton != null) noButton.gameObject.SetActive(true);
        if (button3 != null) button3.gameObject.SetActive(true);
        if (button4 != null) button4.gameObject.SetActive(true);
        
        // Make panel bigger for 4 buttons
        ResizeChoicePanel(4);
        
        // Reposition all 4 buttons to their original positions
        if (autoRepositionButtons4)
            RepositionButtonsFor4ButtonMode();
        
        onYesClicked = onYes;
        onNoClicked = onNo;
        onButton3Clicked = onBtn3;
        onButton4Clicked = onBtn4;
        
        ShowChoicesCommon(message);
    }
    
    public void ShowScrollableChoices(string message, string[] options, System.Action<int, string> onChoiceSelected, int startIndex = 0)
    {
        if (options == null || options.Length == 0)
        {
            Debug.LogWarning("ShowScrollableChoices called with no options.");
            return;
        }
        
        useScrollableChoices = true;
        choiceCount = 4;
        isHorizontal2Button = false;
        scrollableOptions = options;
        onScrollableChoiceSelected = onChoiceSelected;
        int len = options.Length;
        scrollSelectedIndex = Mathf.Clamp(startIndex, 0, len - 1);
        if (len <= 4)
        {
            scrollTopIndex = 0;
        }
        else
        {
            scrollTopIndex = Mathf.Clamp(scrollSelectedIndex - 1, 0, Mathf.Max(0, len - 4));
        }
        
        if (yesButton != null) yesButton.gameObject.SetActive(true);
        if (noButton != null) noButton.gameObject.SetActive(true);
        if (button3 != null) button3.gameObject.SetActive(true);
        if (button4 != null) button4.gameObject.SetActive(true);
        
        ResizeChoicePanel(4);
        if (autoRepositionButtons4)
            RepositionButtonsFor4ButtonMode();
        
        onYesClicked = () => SelectScrollableOptionByOffset(0);
        onNoClicked = () => SelectScrollableOptionByOffset(1);
        onButton3Clicked = () => SelectScrollableOptionByOffset(2);
        onButton4Clicked = () => SelectScrollableOptionByOffset(3);
        
        ShowChoicesCommon(message);
        RefreshScrollableChoiceView();
    }
    
    void SelectScrollableOptionByOffset(int offset)
    {
        if (scrollableOptions == null) return;
        
        int index = scrollTopIndex + offset;
        if (index < 0 || index >= scrollableOptions.Length) return;
        
        onScrollableChoiceSelected?.Invoke(index, scrollableOptions[index]);
    }
    
    void RefreshScrollableChoiceView()
    {
        if (scrollableOptions == null || scrollableOptions.Length == 0) return;
        
        SetButtonLabelByOffset(0, scrollTopIndex + 0);
        SetButtonLabelByOffset(1, scrollTopIndex + 1);
        SetButtonLabelByOffset(2, scrollTopIndex + 2);
        SetButtonLabelByOffset(3, scrollTopIndex + 3);
        
        int selectedOffset = Mathf.Clamp(scrollSelectedIndex - scrollTopIndex, 0, 3);
        SelectButton(GetButtonByOffset(selectedOffset));
    }
    
    void SetButtonLabelByOffset(int offset, int optionIndex)
    {
        Button btn = GetButtonByOffset(offset);
        TextMeshProUGUI txt = GetTextByOffset(offset);
        if (btn == null || txt == null) return;
        
        bool valid = scrollableOptions != null && optionIndex >= 0 && optionIndex < scrollableOptions.Length;
        btn.gameObject.SetActive(valid);
        txt.text = valid ? scrollableOptions[optionIndex] : "";
    }
    
    Button GetButtonByOffset(int offset)
    {
        if (offset == 0) return yesButton;
        if (offset == 1) return noButton;
        if (offset == 2) return button3;
        return button4;
    }
    
    TextMeshProUGUI GetTextByOffset(int offset)
    {
        if (offset == 0) return yesButtonText;
        if (offset == 1) return noButtonText;
        if (offset == 2) return button3Text;
        return button4Text;
    }
    
    void ResizeChoicePanel(int buttonCount)
    {
        if (choicePanel == null) return;
        
        RectTransform panelRect = choicePanel.GetComponent<RectTransform>();
        if (panelRect == null) return;

        if (!autoResizeChoicePanel)
            return;
        
        if (buttonCount == 2)
        {
            // Same width/position as 4-button panel, only shorter height.
            float width = choicePanelSize4.x;
            float height = choicePanelSize2.y;
            panelRect.sizeDelta = new Vector2(width, height);
            if (align2ButtonPanelTo4Bottom)
            {
                float y = choicePanelPos4.y - (choicePanelSize4.y - height) * 0.5f;
                panelRect.anchoredPosition = new Vector2(choicePanelPos4.x, y);
            }
            else
            {
                panelRect.anchoredPosition = choicePanelPos2;
            }
        }
        else if (buttonCount == 4)
        {
            // Bigger panel for 4 buttons (NPC) - keep original position
            panelRect.sizeDelta = choicePanelSize4;
            panelRect.anchoredPosition = choicePanelPos4;
        }
    }
    
    void RepositionButtonsFor2ButtonMode()
    {
        // For trees: Position YES and NO inside the 2-button panel size.
        RectTransform panelRect = choicePanel != null ? choicePanel.GetComponent<RectTransform>() : null;
        float panelW = panelRect != null ? panelRect.sizeDelta.x : 200f;
        float panelH = panelRect != null ? panelRect.sizeDelta.y : 80f;
        float innerW = Mathf.Max(50f, panelW - (twoButtonSidePadding * 2f));
        float innerH = Mathf.Max(40f, panelH - (twoButtonVerticalPadding * 2f));
        float btnH = Mathf.Max(24f, (innerH - twoButtonGap) / 2f);
        float topY = (btnH / 2f) + (twoButtonGap / 2f);
        float bottomY = -topY;

        Vector2 btnSize = new Vector2(innerW, btnH);
        if (yesButton != null)
        {
            RectTransform yesRect = yesButton.GetComponent<RectTransform>();
            if (yesRect != null) 
            {
                yesRect.anchoredPosition = new Vector2(0f, topY);
                yesRect.sizeDelta = btnSize;
            }
        }
        if (noButton != null)
        {
            RectTransform noRect = noButton.GetComponent<RectTransform>();
            if (noRect != null) 
            {
                noRect.anchoredPosition = new Vector2(0f, bottomY);
                noRect.sizeDelta = btnSize;
            }
        }
    }
    
    void RepositionButtonsFor4ButtonMode()
    {
        // Dynamic layout based on panel size to avoid overlap on mobile.
        RectTransform panelRect = choicePanel != null ? choicePanel.GetComponent<RectTransform>() : null;
        float panelW = panelRect != null ? panelRect.sizeDelta.x : 800f;
        float panelH = panelRect != null ? panelRect.sizeDelta.y : 300f;
        float innerW = Mathf.Max(100f, panelW - (fourButtonSidePadding * 2f));
        float innerH = Mathf.Max(80f, panelH - (fourButtonVerticalPadding * 2f));
        float btnH = Mathf.Max(24f, (innerH - (fourButtonGap * 3f)) / 4f);

        float topY = (innerH / 2f) - (btnH / 2f);
        Vector2 btnSize = new Vector2(innerW, btnH);

        if (yesButton != null)
        {
            RectTransform yesRect = yesButton.GetComponent<RectTransform>();
            if (yesRect != null)
            {
                yesRect.anchoredPosition = new Vector2(0f, topY);
                yesRect.sizeDelta = btnSize;
            }
        }
        if (noButton != null)
        {
            RectTransform noRect = noButton.GetComponent<RectTransform>();
            if (noRect != null)
            {
                noRect.anchoredPosition = new Vector2(0f, topY - (btnH + fourButtonGap));
                noRect.sizeDelta = btnSize;
            }
        }
        if (button3 != null)
        {
            RectTransform btn3Rect = button3.GetComponent<RectTransform>();
            if (btn3Rect != null)
            {
                btn3Rect.anchoredPosition = new Vector2(0f, topY - 2f * (btnH + fourButtonGap));
                btn3Rect.sizeDelta = btnSize;
            }
        }
        if (button4 != null)
        {
            RectTransform btn4Rect = button4.GetComponent<RectTransform>();
            if (btn4Rect != null)
            {
                btn4Rect.anchoredPosition = new Vector2(0f, topY - 3f * (btnH + fourButtonGap));
                btn4Rect.sizeDelta = btnSize;
            }
        }
    }
    
    void ShowChoicesCommon(string message)
    {
        Log("ShowDialogueWithChoices called: " + message);
        
        if (dialoguePanel != null && dialogueText != null)
        {
            dialogueText.text = message;
            dialoguePanel.SetActive(true);
            
            if (choicePanel != null)
            {
                choicePanel.SetActive(true);
                Log("ChoicePanel activated");
            }
            else
            {
                Debug.LogError("ChoicePanel is NULL!");
            }
            
              // Setup navigation
              SetupButtonNavigation();
              
              if (autoFormatChoiceText)
                  ApplyChoiceTextFormatting();
            
            // Select first button by default
            SelectButton(yesButton);
            
            showingChoices = true;
            
            // Set input buffer to prevent X key from immediately triggering button
            SetInputBuffer();
            
            Log("ShowDialogueWithChoices complete - showingChoices=" + showingChoices);
        }
        else
        {
            Debug.LogError("dialoguePanel or dialogueText is NULL!");
        }
    }
    
    // Method to set custom text for 4 buttons
    public void SetChoiceButtonTexts(string text1, string text2, string text3, string text4)
    {
        if (yesButtonText != null) yesButtonText.text = text1;
        if (noButtonText != null) noButtonText.text = text2;
        if (button3Text != null) button3Text.text = text3;
        if (button4Text != null) button4Text.text = text4;
    }
    
    // Method to set custom text for 2 buttons
    public void SetChoiceButtonTexts(string text1, string text2)
    {
        if (yesButtonText != null) yesButtonText.text = text1;
        if (noButtonText != null) noButtonText.text = text2;
    }
    
public void CloseDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (choicePanel != null)
            choicePanel.SetActive(false);
        if (continueArrow != null)
            continueArrow.SetActive(false);
        
        // Hide all buttons to prevent ghost buttons
        if (yesButton != null) yesButton.gameObject.SetActive(false);
        if (noButton != null) noButton.gameObject.SetActive(false);
        if (button3 != null) button3.gameObject.SetActive(false);
        if (button4 != null) button4.gameObject.SetActive(false);
        
        // Reset state
        showingChoices = false;
        currentSelectedButton = null;
        hasMoreDialogue = false;
        
        // Clear cooldown so X can work normally
        inputBuffer = 0f;
        
        // Notify listeners that dialogue was closed
        onDialogueClosed?.Invoke();
    }
    
    void OnYesButtonClick()
    {
        PlayChoiceClickSfx();
        Log("UIManager: YES button clicked");
        if (choicePanel != null)
            choicePanel.SetActive(false);
        showingChoices = false;
        onYesClicked?.Invoke();
        Log("UIManager: onYesClicked invoked");
    }
    
    void OnNoButtonClick()
    {
        PlayChoiceClickSfx();
        Log("UIManager: NO button clicked");
        if (choicePanel != null)
            choicePanel.SetActive(false);
        showingChoices = false;
        onNoClicked?.Invoke();
        Log("UIManager: onNoClicked invoked");
    }
    
    void OnButton3Click()
    {
        PlayChoiceClickSfx();
        Log("UIManager: Button 3 clicked");
        if (choicePanel != null)
            choicePanel.SetActive(false);
        showingChoices = false;
        onButton3Clicked?.Invoke();
        Log("UIManager: onButton3Clicked invoked");
    }
    
    void OnButton4Click()
    {
        PlayChoiceClickSfx();
        Log("UIManager: Button 4 clicked");
        if (choicePanel != null)
            choicePanel.SetActive(false);
        showingChoices = false;
        onButton4Clicked?.Invoke();
        Log("UIManager: onButton4Clicked invoked");
    }

    void Log(string msg)
    {
        if (enableDebugLogs || GlobalDebugSettings.EnableAllLogs)
            Debug.Log(msg);
    }

    void PlayChoiceClickSfx()
    {
        PlayUiSfx(choiceClickSfx);
    }

    void PlayPromptClickSfx()
    {
        PlayUiSfx(promptClickSfx);
    }

    // External scripts (like intro flow) can call this when they consume X input.
    public void PlayPromptClickFromExternal()
    {
        PlayPromptClickSfx();
    }

    void ApplyChoiceTextFormatting()
    {
        ApplyChoiceTextFormatting(yesButtonText);
        ApplyChoiceTextFormatting(noButtonText);
        ApplyChoiceTextFormatting(button3Text);
        ApplyChoiceTextFormatting(button4Text);
    }

    void ApplyChoiceTextFormatting(TextMeshProUGUI txt)
    {
        if (txt == null) return;
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableAutoSizing = true;
        txt.fontSizeMin = choiceTextMinSize;
        txt.fontSizeMax = choiceTextMaxSize;
        txt.overflowMode = TextOverflowModes.Overflow;
        txt.enableWordWrapping = true;
    }

    void PlayUiSfx(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource src = uiSfxSource != null ? uiSfxSource : GetComponent<AudioSource>();
        if (src == null) return;
        src.PlayOneShot(clip, AudioSettingsManager.GetUiMultiplier());
    }
    
    // Game Info Methods
    public bool IsGameInfoActive()
    {
        return gameInfoActive;
    }
    
    public void ShowGameInfo()
    {
        bool isForest = GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameStarted();
        
        gameInfoCurrentPage = 1;
        gameInfoActive = true;
        UpdateGameInfoContent();
        
        if (gameInfoPanel != null)
            gameInfoPanel.SetActive(true);
        
        if (gameInfoContinueArrow != null)
            gameInfoContinueArrow.SetActive(true);
        
        if (PauseSettingsController.Instance != null)
        {
            if (isForest)
                PauseSettingsController.Instance.HideUIForGameInfoForest();
            else
                PauseSettingsController.Instance.HideUIForGameInfoCity();
        }
    }
    
    public void CloseGameInfo()
    {
        if (!gameInfoActive) return;
        
        bool isForest = GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameStarted();
        
        gameInfoActive = false;
        
        if (gameInfoPanel != null)
            gameInfoPanel.SetActive(false);
        
        if (gameInfoContinueArrow != null)
            gameInfoContinueArrow.SetActive(false);
        
        if (PauseSettingsController.Instance != null)
        {
            if (isForest)
                PauseSettingsController.Instance.RestoreUIAfterGameInfoForest();
            else
                PauseSettingsController.Instance.RestoreUIAfterGameInfoCity();
        }
    }
    
    void HandleGameInfoInput()
    {
        if (!gameInfoActive) return;
        
        // Close on Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseGameInfo();
            return;
        }
        
        if (InputBridge.GetKeyDown(KeyCode.X))
        {
            if (gameInfoCurrentPage < totalGameInfoPages)
            {
                gameInfoCurrentPage++;
                UpdateGameInfoContent();
                PlayPromptClickSfx();
            }
            else
            {
                CloseGameInfo();
            }
        }
    }
    
    void UpdateGameInfoContent()
    {
        if (gameInfoBodyText == null || gameInfoPageText == null)
            return;
        
        switch (gameInfoCurrentPage)
        {
            case 1:
                gameInfoBodyText.text = "Controls\nD-Pad – Move (Up / Down / Left / Right)\nX – Interact (Talk to NPCs, use lift, search trees)\nRun – Toggle run (Move faster)";
                gameInfoPageText.text = "1/3";
                break;
            case 2:
                gameInfoBodyText.text = "Objective\nArea 1 – Exploration Zone\n\nThis area is for free exploration.\nYou can walk around the town, visit buildings, and interact with NPCs.\nThere are no missions here.\n\nTo begin the main gameplay, head towards the forest corner and find the path to the next area.\n\nArea 2 – Main Gameplay Zone\n\nYour objective is simple:\nFind the hidden key in the forest\nSearch trees using X\nOnce you find the key, return to the hut to escape";
                gameInfoPageText.text = "2/3";
                break;
            case 3:
                gameInfoBodyText.text = "Help & Tips\n\nIn the forest, not every tree is useful.\nOnly certain trees contain the key.\n\nThere are many trees, but only 25 large trees can have the key\nFocus on big and noticeable trees instead of small ones\nPress X near a tree to search it\n\nImportant Tips\nUse Run to move faster between trees\nKeep track of where you've already searched\nThe key is hidden randomly…\nAfter finding the key, return to the hut immediately";
                gameInfoPageText.text = "3/3";
                break;
        }
        
        // Show arrow only if not on last page
        if (gameInfoContinueArrow != null)
            gameInfoContinueArrow.SetActive(gameInfoCurrentPage < totalGameInfoPages);
    }
}

