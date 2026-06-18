using UnityEngine;
using UnityEngine.AI;

public class EnemyJumpController : MonoBehaviour
{
    [SerializeField] private bool jumpEnabled = true;
    [SerializeField] private float jumpHeight = 0.9f;
    [SerializeField] private float jumpDuration = 0.45f;
    [SerializeField] private float jumpCooldown = 1.35f;
    [SerializeField] private float triggerDistance = 8f;
    [SerializeField] private float targetAirborneHeight = 0.35f;
    [SerializeField] private float activeHopInterval = 2.4f;

    private NavMeshAgent agent;
    private Transform trackedTarget;
    private PlayerController3D trackedPlayerController;
    private Rigidbody trackedTargetBody;
    private float targetGroundY;
    private float defaultBaseOffset;
    private float fallbackGroundY;
    private float jumpStartedAt = -1f;
    private float nextJumpTime;
    private float nextActiveHopTime;

    public bool JumpEnabled => jumpEnabled;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        defaultBaseOffset = agent != null ? agent.baseOffset : 0f;
        fallbackGroundY = transform.position.y;
        nextActiveHopTime = Time.time + Random.Range(activeHopInterval * 0.45f, activeHopInterval);
    }

    public void Configure(bool enabled)
    {
        jumpEnabled = enabled;
    }

    public void TickAutoJump(Transform target, bool canAct, bool hopWhileActive)
    {
        TrackTargetGround(target);

        if (!jumpEnabled || !canAct || target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > triggerDistance * triggerDistance) return;

        bool targetIsAirborne = IsTargetJumpingAboveGround(target);
        bool timedHop = hopWhileActive
                         && Time.time >= nextActiveHopTime
                         && target.position.y <= transform.position.y + targetAirborneHeight;

        if (targetIsAirborne || timedHop)
        {
            TryJump();
            nextActiveHopTime = Time.time + activeHopInterval;
        }
    }

    public void TryJump()
    {
        if (!jumpEnabled || IsJumping || Time.time < nextJumpTime) return;

        if (agent != null) defaultBaseOffset = agent.baseOffset;
        else fallbackGroundY = transform.position.y;

        jumpStartedAt = Time.time;
        nextJumpTime = Time.time + jumpCooldown;
    }

    private bool IsJumping => jumpStartedAt >= 0f && Time.time - jumpStartedAt < jumpDuration;

    private void LateUpdate()
    {
        if (jumpStartedAt < 0f) return;

        float elapsed = Time.time - jumpStartedAt;
        if (elapsed >= jumpDuration)
        {
            jumpStartedAt = -1f;
            ApplyVerticalOffset(0f);
            return;
        }

        float normalizedTime = Mathf.Clamp01(elapsed / jumpDuration);
        float arc = Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;
        ApplyVerticalOffset(arc);
    }

    private void TrackTargetGround(Transform target)
    {
        if (target == null) return;

        if (trackedTarget != target)
        {
            trackedTarget = target;
            trackedPlayerController = target.GetComponent<PlayerController3D>();
            trackedTargetBody = target.GetComponent<Rigidbody>();
            targetGroundY = target.position.y;
            return;
        }

        if (trackedPlayerController == null)
        {
            trackedPlayerController = target.GetComponent<PlayerController3D>();
        }

        if (trackedTargetBody == null)
        {
            trackedTargetBody = target.GetComponent<Rigidbody>();
        }

        // Important: do not keep the lowest Y forever.
        // When the player runs uphill, their Y position naturally increases while still grounded.
        // The old code treated that as if the player was airborne, so the guardian kept hopping.
        bool targetIsGrounded = trackedPlayerController == null || trackedPlayerController.IsGrounded;
        if (targetIsGrounded || target.position.y < targetGroundY)
        {
            targetGroundY = target.position.y;
        }
    }

    private bool IsTargetJumpingAboveGround(Transform target)
    {
        if (target == null) return false;

        bool aboveTrackedGround = target.position.y > targetGroundY + targetAirborneHeight;
        if (!aboveTrackedGround) return false;

        if (trackedPlayerController == null) return true;
        if (trackedPlayerController.IsGrounded) return false;

        return trackedTargetBody == null || trackedTargetBody.linearVelocity.y > 0.05f;
    }

    private void ApplyVerticalOffset(float offset)
    {
        if (agent != null)
        {
            agent.baseOffset = defaultBaseOffset + offset;
            return;
        }

        Vector3 position = transform.position;
        position.y = fallbackGroundY + offset;
        transform.position = position;
    }
}
