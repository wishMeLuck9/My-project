using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController3D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private Collider playerCollider;
    private Vector2 moveInput;
    private bool canMove = true;

    public bool CanMove => canMove;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.freezeRotation = true;

        playerCollider = GetComponent<Collider>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (!canMove || Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = 0;
        float vertical = 0;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1;

        moveInput = new Vector2(horizontal, vertical);

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryJump();
        }
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

        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
    }

    public void SetCanMove(bool state)
    {
        canMove = state;
        if (!state && rb != null) rb.linearVelocity = Vector3.zero;
    }

    private void TryJump()
    {
        if (rb == null || !IsGrounded()) return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private bool IsGrounded()
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
}
