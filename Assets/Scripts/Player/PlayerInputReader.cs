using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionsAsset;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string attackActionName = "Attack";
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string pauseActionName = "Pause";

    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction sprintAction;
    private InputAction pauseAction;
    private bool isListening;
    private int lastJumpFrame = -1;
    private int lastAttackFrame = -1;
    private int lastInteractFrame = -1;
    private bool walkToggled;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool WalkHeld => IsWalkModeActive();
    public event Action JumpPressed;
    public event Action AttackPressed;
    public event Action InteractPressed;
    public event Action PausePressed;

    private void Awake()
    {
        ResolveActions();
    }

    private void OnEnable()
    {
        EnableActions();
    }

    private void OnDisable()
    {
        DisableActions();
    }

    private void Update()
    {
        UpdateMovementModeToggle();

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame) RaiseJump();
            if (Keyboard.current.eKey.wasPressedThisFrame) RaiseInteract();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            RaiseAttack();
        }
    }

    public void Configure(InputActionAsset newActionsAsset)
    {
        DisableActions();
        actionsAsset = newActionsAsset;
        ResolveActions();
        if (Application.isPlaying && isActiveAndEnabled) EnableActions();
    }

    private void ResolveActions()
    {
        playerMap = actionsAsset != null ? actionsAsset.FindActionMap(playerMapName, false) : null;
        moveAction = playerMap?.FindAction(moveActionName, false);
        lookAction = playerMap?.FindAction(lookActionName, false);
        jumpAction = playerMap?.FindAction(jumpActionName, false);
        attackAction = playerMap?.FindAction(attackActionName, false);
        interactAction = playerMap?.FindAction(interactActionName, false);
        sprintAction = playerMap?.FindAction(sprintActionName, false);
        pauseAction = playerMap?.FindAction(pauseActionName, false);
    }

    private void EnableActions()
    {
        if (!Application.isPlaying) return;
        if (isListening || playerMap == null) return;

        if (moveAction != null)
        {
            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
        }

        if (lookAction != null)
        {
            lookAction.performed += OnLook;
            lookAction.canceled += OnLook;
        }

        if (jumpAction != null) jumpAction.started += OnJump;
        if (attackAction != null) attackAction.started += OnAttack;
        if (interactAction != null) interactAction.started += OnInteract;
        if (pauseAction != null) pauseAction.started += OnPause;

        playerMap.Enable();
        isListening = true;
    }

    private void DisableActions()
    {
        if (!isListening) return;

        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
        }

        if (lookAction != null)
        {
            lookAction.performed -= OnLook;
            lookAction.canceled -= OnLook;
        }

        if (jumpAction != null) jumpAction.started -= OnJump;
        if (attackAction != null) attackAction.started -= OnAttack;
        if (interactAction != null) interactAction.started -= OnInteract;
        if (pauseAction != null) pauseAction.started -= OnPause;

        playerMap?.Disable();
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        isListening = false;
        walkToggled = false;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        RaiseJump();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (context.control?.device is Keyboard)
        {
            string controlName = context.control.name;
            if (controlName == "enter" || controlName == "numpadEnter") return;
        }

        RaiseAttack();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        RaiseInteract();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        PausePressed?.Invoke();
    }

    private void RaiseJump()
    {
        if (lastJumpFrame == Time.frameCount) return;
        lastJumpFrame = Time.frameCount;
        JumpPressed?.Invoke();
    }

    private void RaiseAttack()
    {
        if (lastAttackFrame == Time.frameCount) return;
        lastAttackFrame = Time.frameCount;
        AttackPressed?.Invoke();
    }

    private void RaiseInteract()
    {
        if (lastInteractFrame == Time.frameCount) return;
        lastInteractFrame = Time.frameCount;
        InteractPressed?.Invoke();
    }

    private void UpdateMovementModeToggle()
    {
        SettingsManager settings = SettingsManager.Instance;
        if (settings == null || !settings.ToggleRun)
        {
            walkToggled = false;
            return;
        }

        if (WasSprintPressedThisFrame())
        {
            walkToggled = !walkToggled;
        }
    }

    private bool IsWalkModeActive()
    {
        SettingsManager settings = SettingsManager.Instance;
        if (settings != null && settings.ToggleRun) return walkToggled;
        return IsSprintModifierPressed();
    }

    private bool IsSprintModifierPressed()
    {
        if (sprintAction != null && sprintAction.IsPressed()) return true;
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    private bool WasSprintPressedThisFrame()
    {
        if (sprintAction != null && sprintAction.WasPressedThisFrame()) return true;
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame);
    }
}
