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

        bool targetIsAirborne = target.position.y > targetGroundY + targetAirborneHeight;
        bool timedHop = hopWhileActive && Time.time >= nextActiveHopTime;

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
            targetGroundY = target.position.y;
            return;
        }

        targetGroundY = Mathf.Min(targetGroundY, target.position.y);
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
