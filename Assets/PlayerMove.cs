using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public static PlayerMove Instance;
    
    public float walkSpeed = 10f;      // Doubled from 5f
    public float runSpeed = 20f;      // Doubled from 10f
    public float gridSize = 1f;
    public KeyCode runKey = KeyCode.Z;
    public float runToggleTapWindow = 0.2f; // Tap to toggle, hold for temporary run
    public enum RunMode { Hold, Tap }
    public RunMode runMode = RunMode.Hold;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    [Header("Idle Sprites - Drag from Project Window")]
    public Sprite idleDown;   // Front/Down (Direction 1)
    public Sprite idleUp;     // Back/Up (Direction 2)
    public Sprite idleLeft;   // Left (Direction 3)
    public Sprite idleRight;  // Right (Direction 4)

    private CharacterController controller;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private int currentDirection = 0;
    private int lastMovingDirection = 1; // Default facing front (Direction 1)
    
    public int LastFacingDirection => lastMovingDirection;
    private float lockedYPosition;
    private bool runToggledOn = false;
    private bool runKeyHeld = false;
    private float runKeyHeldTime = 0f;
    private bool runKeyUsedAsHold = false;
    private bool externalFreeze = false;
    public bool useIsWalkingParam = false;
    public bool useIsWalkingParamOnMobile = true;
    public bool forceDirectAnimatorOnMobile = true;
    private bool hasIsWalkingParam = false;
    private bool hasSpeedParam = false;
    private bool suppressMoveUntilRelease = false;
    private int idleStateHash;
    private int walkDownStateHash;
    private int walkUpStateHash;
    private int walkLeftStateHash;
    private int walkRightStateHash;
    // Mobile overrides removed: use the same animation flow as PC.

    void Awake()
    {
        // Ensure default is Hold even if Inspector had old value.
        runMode = RunMode.Hold;
        runToggledOn = false;
    }
 
    void Start()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetPosition = transform.position;
        lockedYPosition = transform.position.y; // Lock to initial Y position (ground level)
        lastMovingDirection = 1; // Default to facing front (down)
        
        // Set initial idle sprite
        if (idleDown != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = idleDown;
        }

        if (animator != null)
        {
            if (UseIsWalking())
            {
                hasIsWalkingParam = HasAnimatorParam("IsWalking", AnimatorControllerParameterType.Bool);
            }
            hasSpeedParam = HasAnimatorParam("Speed", AnimatorControllerParameterType.Float);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            idleStateHash = Animator.StringToHash("Idle");
            walkDownStateHash = Animator.StringToHash("walking");
            walkUpStateHash = Animator.StringToHash("WalkUP");
            walkLeftStateHash = Animator.StringToHash("WalkLeft");
            walkRightStateHash = Animator.StringToHash("WalkRight");
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        bool teleportFreeze = TeleportEffect.IsTeleportingGlobal;
        bool interactionFreeze = GameManager.Instance != null && GameManager.Instance.isInteracting;
        bool choiceFreeze = UIManager.Instance != null && UIManager.Instance.choicePanel != null && UIManager.Instance.choicePanel.activeSelf;

        UpdateRunInput();

        if (externalFreeze || interactionFreeze || teleportFreeze || choiceFreeze)
        {
            ResetMovementOnMobile();
        }

        if (externalFreeze || teleportFreeze)
        {
            if (isMoving)
            {
                isMoving = false;
                targetPosition = transform.position;
            }
            if (Application.isMobilePlatform && forceDirectAnimatorOnMobile)
            {
                if (animator != null)
                {
                    animator.enabled = false;
                    ShowIdleSprite(lastMovingDirection);
                }
                return;
            }
            if (animator != null)
            {
                if (UseIsWalking() && hasIsWalkingParam)
                {
                    animator.enabled = true;
                    SetIsWalking(false);
                    animator.SetInteger("Direction", lastMovingDirection);
                }
                else
                {
                    animator.enabled = false;
                    ShowIdleSprite(lastMovingDirection);
                }
            }
            return;
        }

        // Freeze player while choice panel is open (arrow keys are used for UI navigation).
        if (choiceFreeze)
        {
            if (isMoving)
            {
                isMoving = false;
                targetPosition = transform.position;
            }

            if (Application.isMobilePlatform && forceDirectAnimatorOnMobile)
            {
                if (animator != null)
                {
                    animator.enabled = false;
                    ShowIdleSprite(lastMovingDirection);
                }
            }
            else if (animator != null)
            {
                if (UseIsWalking() && hasIsWalkingParam)
                {
                    animator.enabled = true;
                    SetIsWalking(false);
                    animator.SetInteger("Direction", lastMovingDirection);
                }
                else
                {
                    animator.enabled = false;
                    ShowIdleSprite(lastMovingDirection);
                }
            }
            return;
        }

        // Freeze player during tree interaction
        if (interactionFreeze)
        {
            // Stop any current movement
            if (isMoving)
            {
                isMoving = false;
                targetPosition = transform.position;
            }
            
            // Show idle sprite when frozen
            if (Application.isMobilePlatform && forceDirectAnimatorOnMobile)
            {
                if (animator != null)
                {
                    animator.enabled = false;
                    ShowIdleSprite(lastMovingDirection);
                }
            }
            else if (animator != null)
            {
                if (UseIsWalking() && hasIsWalkingParam)
                {
                    animator.enabled = true;
                    SetIsWalking(false);
                    animator.SetInteger("Direction", lastMovingDirection);
                }
                else
                {
                    animator.enabled = false;
                    ShowIdleSprite(lastMovingDirection);
                }
            }
            
            return; // Don't process movement input
        }

        if (isMoving)
        {
            MoveToTarget();
        }
        else
        {
            HandleInput();
        }

        UpdateAnimationSpeed();
        ApplyMobileDirectAnimator();
        
        // Lock Y position to prevent floating when colliding with objects
        Vector3 pos = transform.position;
        pos.y = lockedYPosition;
        transform.position = pos;
    }
    
    void UpdateRunInput()
    {
        if (runMode == RunMode.Hold)
        {
            runToggledOn = false;
            runKeyHeld = InputBridge.GetKey(runKey);
            runKeyHeldTime = 0f;
            runKeyUsedAsHold = false;
            return;
        }

        if (InputBridge.GetKeyUp(runKey))
        {
            // Tap mode: toggle only after key is released.
            runToggledOn = !runToggledOn;
        }
    }
    
    bool IsRunning()
    {
        // Can't run while frozen or interacting
        if (externalFreeze)
            return false;
        
        // Check interaction freeze via GameManager
        if (GameManager.Instance != null && GameManager.Instance.isInteracting)
            return false;
        
        if (runMode == RunMode.Hold)
            return InputBridge.GetKey(runKey);
        return runToggledOn;
    }

    public void SetRunModeHold()
    {
        runMode = RunMode.Hold;
        runToggledOn = false;
    }

    public void SetRunModeTap()
    {
        runMode = RunMode.Tap;
    }

    public void SetExternalFreeze(bool freeze)
    {
        externalFreeze = freeze;
        if (freeze)
        {
            if (isMoving)
            {
                isMoving = false;
                targetPosition = transform.position;
            }
            if (animator != null) animator.enabled = false;
            ShowIdleSprite(lastMovingDirection);
        }
        else
        {
            // Reset run states when unfreezing to prevent auto-run
            runToggledOn = false;
            runKeyHeld = false;
            runKeyHeldTime = 0f;
            
            // If we unfreeze while already moving, force walking state back on.
            if (isMoving && animator != null)
            {
                animator.enabled = true;
                animator.SetInteger("Direction", currentDirection != 0 ? currentDirection : lastMovingDirection);
                if (UseIsWalking() && hasIsWalkingParam) SetIsWalking(true);
            }
        }
    }

    void HandleInput()
    {
        float h = InputBridge.GetAxisRaw("Horizontal");
        float v = InputBridge.GetAxisRaw("Vertical");

        if (Application.isMobilePlatform && suppressMoveUntilRelease)
        {
            if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
            {
                suppressMoveUntilRelease = false;
            }
            else
            {
                return;
            }
        }

        if (h != 0)
            v = 0;

        if (h != 0 || v != 0)
        {
            Vector3 moveDirection = new Vector3(h, 0, v);
            targetPosition = transform.position + (moveDirection * gridSize);
            targetPosition.y = lockedYPosition; // Lock target Y position

            if (Mathf.Abs(h) > Mathf.Abs(v))
            {
                currentDirection = (h > 0) ? 4 : 3;
            }
            else
            {
                currentDirection = (v > 0) ? 2 : 1;
            }
            
            // Always update direction and walking state when movement starts.
            lastMovingDirection = currentDirection;
            if (animator != null)
            {
                animator.enabled = true;
                animator.SetInteger("Direction", currentDirection);
                if (UseIsWalking() && hasIsWalkingParam) SetIsWalking(true);
                SetSpeedParam(IsRunning() ? 1f : 0.5f);
            }
            
            isMoving = true;
        }
    }
    
    void ShowIdleSprite(int direction)
    {
        if (spriteRenderer == null) return;
        
        if (direction == 1) // Down/Front
        {
            spriteRenderer.sprite = idleDown;
        }
        else if (direction == 2) // Up/Back
        {
            spriteRenderer.sprite = idleUp;
        }
        else if (direction == 3) // Left
        {
            spriteRenderer.sprite = idleLeft;
        }
        else if (direction == 4) // Right
        {
            spriteRenderer.sprite = idleRight;
        }
    }
    
    // Public method to set player facing direction (called by teleport)
    public void SetFacingDirection(int direction)
    {
        lastMovingDirection = direction;
        currentDirection = 0;
        isMoving = false;
        targetPosition = transform.position;
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetInteger("Direction", direction);
            SetIsWalking(false);
        }
        if (!UseIsWalking() || !hasIsWalkingParam) ShowIdleSprite(direction);
    }

    void MoveToTarget()
    {
        Vector3 move = targetPosition - transform.position;
        bool isRunning = IsRunning();
        float speed = isRunning ? runSpeed : walkSpeed;

        // Safety: if animator was disabled by some other script/frame, re-enable while moving.
        if (animator != null && !animator.enabled)
        {
            animator.enabled = true;
            animator.SetInteger("Direction", currentDirection != 0 ? currentDirection : lastMovingDirection);
        }
        // Only enable walking if NOT frozen AND actually moving (magnitude > 0.02f)
        bool isFrozen = GameManager.Instance != null && GameManager.Instance.isInteracting;
        bool hasChoiceOpen = UIManager.Instance != null && UIManager.Instance.choicePanel != null && UIManager.Instance.choicePanel.activeSelf;
        
        if (animator != null && UseIsWalking() && hasIsWalkingParam && !isFrozen && !hasChoiceOpen && move.magnitude >= 0.02f)
        {
            // Only set walking when actually moving (not at target position)
            SetIsWalking(true);
        }

        if (move.magnitude < 0.02f)
        {
            // Player reached target - stop moving
            transform.position = targetPosition;
            isMoving = false;
            
            // Stop movement and show idle
            if (animator != null)
            {
                if (UseIsWalking() && hasIsWalkingParam)
                {
                    animator.enabled = true;
                    SetIsWalking(false);
                    SetSpeedParam(0f);
                }
                else
                {
                    animator.enabled = false;
                    ShowIdleSprite(lastMovingDirection);
                }
            }
            
            ContinueIfKeyHeld();
        }
        else
        {
            // Keep moving, but clamp step to remaining distance to avoid end-point jitter/overshoot.
            float step = Mathf.Min(speed * Time.deltaTime, move.magnitude);
            Vector3 moveVec = move.normalized * step;
            controller.Move(moveVec);
        }
    }

    void UpdateAnimationSpeed()
    {
        // Only update animation speed when animator is enabled
        if (!animator.enabled) return;

        // Use lastMovingDirection so speed stays correct even when stopped
        int dir = isMoving ? currentDirection : lastMovingDirection;
        
        if (dir == 0)
        {
            animator.speed = 1f;
        }
        else
        {
            bool isRunning = IsRunning();
            animator.speed = isRunning ? 4f : 2f; // Doubled!
        }
    }

    // No mobile-specific LateUpdate or debug overlay.

    void ContinueIfKeyHeld()
    {
        float h = InputBridge.GetAxisRaw("Horizontal");
        float v = InputBridge.GetAxisRaw("Vertical");

        if (h != 0) v = 0;

        if (h != 0 || v != 0)
        {
            Vector3 moveDirection = new Vector3(h, 0, v);
            targetPosition = transform.position + (moveDirection * gridSize);
            targetPosition.y = lockedYPosition; // Lock target Y position

            if (Mathf.Abs(h) > Mathf.Abs(v))
            {
                currentDirection = (h > 0) ? 4 : 3;
            }
            else
            {
                currentDirection = (v > 0) ? 2 : 1;
            }

            lastMovingDirection = currentDirection;
            if (animator != null)
            {
                animator.enabled = true;
                animator.SetInteger("Direction", currentDirection);
                SetIsWalking(true);
                SetSpeedParam(IsRunning() ? 1f : 0.5f);
            }
            isMoving = true;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Stop only on real side walls/obstacles, not on floor/terrain contacts.
        Vector3 horizontalNormal = new Vector3(hit.normal.x, 0f, hit.normal.z);
        if (horizontalNormal.sqrMagnitude > 0.01f)
        {
            isMoving = false;
            targetPosition = transform.position;
            
            // Stop the walking animation and show idle sprite
            if (animator != null)
            {
                if (UseIsWalking() && hasIsWalkingParam)
                {
                    animator.enabled = true;
                    SetIsWalking(false);
                    SetSpeedParam(0f);
                }
                else
                {
                    animator.enabled = false;
                }
            }
            if (spriteRenderer != null)
            {
                if (!UseIsWalking() || !hasIsWalkingParam) ShowIdleSprite(lastMovingDirection);
            }
        }
    }

    bool HasAnimatorParam(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.type == type && p.name == paramName) return true;
        }
        return false;
    }

    void SetIsWalking(bool value)
    {
        if (!hasIsWalkingParam || animator == null) return;
        animator.SetBool("IsWalking", value);
    }

    bool UseIsWalking()
    {
        return useIsWalkingParam || (Application.isMobilePlatform && useIsWalkingParamOnMobile);
    }

    void SetSpeedParam(float value)
    {
        if (!hasSpeedParam || animator == null) return;
        animator.SetFloat("Speed", value);
    }

    void ApplyMobileDirectAnimator()
    {
        if (!forceDirectAnimatorOnMobile || !Application.isMobilePlatform) return;
        if (animator == null) return;

        if (!isMoving)
        {
            // Show directional idle sprite on mobile when stopped.
            animator.enabled = false;
            ShowIdleSprite(lastMovingDirection);
            return;
        }

        int desiredHash = walkDownStateHash;
        int dir = currentDirection != 0 ? currentDirection : lastMovingDirection;
        switch (dir)
        {
            case 1: desiredHash = walkDownStateHash; break;
            case 2: desiredHash = walkUpStateHash; break;
            case 3: desiredHash = walkLeftStateHash; break;
            case 4: desiredHash = walkRightStateHash; break;
            default: desiredHash = walkDownStateHash; break;
        }

        animator.enabled = true;
        var st = animator.GetCurrentAnimatorStateInfo(0);
        if (st.shortNameHash != desiredHash)
        {
            animator.Play(desiredHash, 0, 0f);
        }
    }
    
    // Force stop movement and animation on mobile when frozen/interacting
    void ResetMovementOnMobile()
    {
        if (!Application.isMobilePlatform || !forceDirectAnimatorOnMobile) return;
        isMoving = false;
        targetPosition = transform.position;
        suppressMoveUntilRelease = true;
        InputBridge.SetMove(Vector2.zero);
        if (animator != null)
        {
            animator.enabled = false;
            ShowIdleSprite(lastMovingDirection);
        }
    }

    // Y position is always locked to ground level - do not change
    // This prevents player from floating when teleporting
    public void SetLockedYPosition(float newY)
    {
        // Y stays locked at initial level - ignore changes
        // lockedYPosition = newY; // Disabled to keep player at ground level
    }
}
