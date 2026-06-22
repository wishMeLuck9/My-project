using UnityEngine;

public class PlayerController3D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float maxTurnDegreesPerSecond = 240f;
    [SerializeField] private float walkSpeedMultiplier = 0.55f;
    [SerializeField] private float groundAcceleration = 28f;
    [SerializeField] private float airAcceleration = 12f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private float groundCheckRadius = 0.34f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool allowJump;

    private Rigidbody rb;
    private Collider playerCollider;
    private PlayerInputReader inputReader;
    private PlayerVisualAnimator visualAnimator;
    private PlayerClimbController climbController;
    private Vector2 moveInput;
    private bool canMove = true;
    private float movementMultiplier = 1f;
    private float movementModifierEndsAt = -1f;
    private float externalImpulseControlLockedUntil = -1f;
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    public bool CanMove => canMove;
    public bool JumpEnabled => allowJump;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public Vector3 PlanarVelocity => rb == null ? Vector3.zero : new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    public bool IsWalking => canMove && inputReader != null && inputReader.WalkHeld;
    public bool IsRunning => canMove && moveInput.magnitude > 0.1f && !IsWalking;
    public float MovementAmount
    {
        get
        {
            if (!canMove || rb == null) return 0f;

            float planarSpeed = PlanarVelocity.magnitude;
            if (IsWalking)
            {
                float walkSpeed = Mathf.Max(0.01f, moveSpeed * movementMultiplier * walkSpeedMultiplier);
                return Mathf.Clamp01(planarSpeed / walkSpeed) * 0.4f;
            }

            float runSpeed = Mathf.Max(0.01f, moveSpeed * movementMultiplier);
            return Mathf.Clamp01(planarSpeed / runSpeed);
        }
    }
    public bool IsGrounded => CheckGrounded();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            ConfigurePhysicsBody(rb);
        }

        playerCollider = GetComponent<Collider>();
        inputReader = GetComponent<PlayerInputReader>();
        climbController = GetComponent<PlayerClimbController>();
        visualAnimator = GetComponentInChildren<PlayerVisualAnimator>(true);
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private static void ConfigurePhysicsBody(Rigidbody body)
    {
        body.isKinematic = false;
        body.useGravity = true;
        body.detectCollisions = true;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.WakeUp();
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

        if (Time.time < externalImpulseControlLockedUntil) return;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = (forward * moveInput.y + right * moveInput.x).normalized;
        float speed = moveSpeed * movementMultiplier * (IsWalking ? walkSpeedMultiplier : 1f);
        Vector3 velocity = rb.linearVelocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 targetVelocity = direction * speed;
        float acceleration = CheckGrounded() ? groundAcceleration : airAcceleration;
        planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(planarVelocity.x, velocity.y, planarVelocity.z);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion smoothedRotation = Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                1f - Mathf.Exp(-rotationSpeed * Time.fixedDeltaTime));
            rb.MoveRotation(Quaternion.RotateTowards(
                rb.rotation,
                smoothedRotation,
                maxTurnDegreesPerSecond * Time.fixedDeltaTime));
        }
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

    public void ApplyExternalImpulse(Vector3 impulse, float controlLockDuration = 0.18f)
    {
        if (rb == null || !canMove) return;

        rb.AddForce(impulse, ForceMode.VelocityChange);
        externalImpulseControlLockedUntil = Mathf.Max(
            externalImpulseControlLockedUntil,
            Time.time + Mathf.Max(0f, controlLockDuration));
    }

    public void ConfigureTraversal(bool jumpEnabled)
    {
        allowJump = jumpEnabled;
    }

    public void ConfigureLocomotion(float newMoveSpeed, float newRotationSpeed, float newMaxTurnDegreesPerSecond = -1f)
    {
        moveSpeed = Mathf.Max(0f, newMoveSpeed);
        rotationSpeed = Mathf.Max(0f, newRotationSpeed);
        if (newMaxTurnDegreesPerSecond >= 0f)
        {
            maxTurnDegreesPerSecond = newMaxTurnDegreesPerSecond;
        }
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
        ResolveVisualAnimator()?.PlayJump(IsRunning);
    }

    private bool CheckGrounded()
    {
        int mask = groundMask.value == 0 ? Physics.DefaultRaycastLayers : groundMask.value;

        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            float radius = Mathf.Min(groundCheckRadius, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f);
            Vector3 origin = bounds.center - Vector3.up * Mathf.Max(0f, bounds.extents.y - radius - 0.02f);
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                radius,
                Vector3.down,
                groundHits,
                groundCheckDistance + 0.04f,
                mask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = groundHits[i].collider;
                if (hit != null && hit.transform != transform && !hit.transform.IsChildOf(transform)) return true;
            }

            return false;
        }

        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.2f, mask, QueryTriggerInteraction.Ignore);
    }

    private void HandleJumpPressed()
    {
        if (ResolveClimbController()?.TryStartClimb() == true) return;
        if (!allowJump) return;
        TryJump();
    }

    private PlayerVisualAnimator ResolveVisualAnimator()
    {
        if (visualAnimator == null) visualAnimator = GetComponentInChildren<PlayerVisualAnimator>(true);
        return visualAnimator;
    }

    private PlayerClimbController ResolveClimbController()
    {
        if (climbController == null) climbController = GetComponent<PlayerClimbController>();
        return climbController;
    }
}
