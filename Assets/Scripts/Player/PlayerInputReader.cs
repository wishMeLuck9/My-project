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

    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private bool isListening;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public event Action JumpPressed;
    public event Action AttackPressed;
    public event Action InteractPressed;

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

        if (jumpAction != null) jumpAction.performed += OnJump;
        if (attackAction != null) attackAction.performed += OnAttack;
        if (interactAction != null) interactAction.started += OnInteract;

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

        if (jumpAction != null) jumpAction.performed -= OnJump;
        if (attackAction != null) attackAction.performed -= OnAttack;
        if (interactAction != null) interactAction.started -= OnInteract;

        playerMap?.Disable();
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        isListening = false;
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
        JumpPressed?.Invoke();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (context.control?.device is Keyboard)
        {
            string controlName = context.control.name;
            if (controlName == "enter" || controlName == "numpadEnter") return;
        }

        AttackPressed?.Invoke();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        InteractPressed?.Invoke();
    }
}
