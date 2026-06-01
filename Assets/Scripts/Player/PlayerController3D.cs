using UnityEngine;

public class PlayerController3D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool allowJump;

    private Rigidbody rb;
    private Collider playerCollider;
    private PlayerInputReader inputReader;
    private PlayerVisualAnimator visualAnimator;
    private Vector2 moveInput;
    private bool canMove = true;
    private float movementMultiplier = 1f;
    private float movementModifierEndsAt = -1f;

    public bool CanMove => canMove;
    public bool JumpEnabled => allowJump;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public Vector3 PlanarVelocity => rb == null ? Vector3.zero : new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    public float MovementAmount => canMove ? moveInput.magnitude : 0f;
    public bool IsGrounded => CheckGrounded();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.freezeRotation = true;

        playerCollider = GetComponent<Collider>();
        inputReader = GetComponent<PlayerInputReader>();
        visualAnimator = GetComponentInChildren<PlayerVisualAnimator>(true);
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
        if (inputReader != null) inputReader.JumpPressed += HandleJumpPressed;
    }

    private void OnDisable()
    {
        if (inputReader != null) inputReader.JumpPressed -= HandleJumpPressed;
    }

    private void Update()
    {
        if (movementModifierEndsAt >= 0f && Time.time >= movementModifierEndsAt)
        {
            movementMultiplier = 1f;
            movementModifierEndsAt = -1f;
        }

        if (!canMove || inputReader == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = inputReader.MoveInput;
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (rb == null || cameraTransform == null) return;
        if (moveInput.magnitude <= 0.1f) return;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = (forward * moveInput.y + right * moveInput.x).normalized;

        rb.MovePosition(rb.position + direction * moveSpeed * movementMultiplier * Time.fixedDeltaTime);
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
    }

    public void SetCanMove(bool state)
    {
        canMove = state;
        if (!state && rb != null) rb.linearVelocity = Vector3.zero;
    }

    public void ApplyTimedSpeedMultiplier(float multiplier, float duration)
    {
        movementMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        movementModifierEndsAt = Time.time + Mathf.Max(0f, duration);
    }

    public void ConfigureTraversal(bool jumpEnabled)
    {
        allowJump = jumpEnabled;
    }

    public void ConfigureLocomotion(float newMoveSpeed, float newRotationSpeed)
    {
        moveSpeed = Mathf.Max(0f, newMoveSpeed);
        rotationSpeed = Mathf.Max(0f, newRotationSpeed);
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    private void TryJump()
    {
        if (rb == null || !CheckGrounded()) return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        ResolveVisualAnimator()?.PlayJump();
    }

    private bool CheckGrounded()
    {
        int mask = groundMask.value == 0 ? Physics.DefaultRaycastLayers : groundMask.value;

        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            Vector3 origin = bounds.center;
            float distance = bounds.extents.y + groundCheckDistance;
            return Physics.Raycast(origin, Vector3.down, distance, mask, QueryTriggerInteraction.Ignore);
        }

        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.2f, mask, QueryTriggerInteraction.Ignore);
    }

    private void HandleJumpPressed()
    {
        if (allowJump) TryJump();
    }

    private PlayerVisualAnimator ResolveVisualAnimator()
    {
        if (visualAnimator == null) visualAnimator = GetComponentInChildren<PlayerVisualAnimator>(true);
        return visualAnimator;
    }
}
