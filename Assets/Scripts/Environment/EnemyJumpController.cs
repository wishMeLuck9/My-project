using System.Collections;
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
    [Header("Traversal Vault")]
    [SerializeField] private bool vaultEnabled = true;
    [SerializeField] private float vaultDistance = 2.35f;
    [SerializeField] private float vaultHeight = 1.1f;
    [SerializeField] private float vaultDuration = 0.42f;
    [SerializeField] private float vaultCooldown = 1.15f;
    [SerializeField] private float vaultLandingSampleRadius = 1.8f;
    [Header("Traversal Drop")]
    [SerializeField] private bool dropEnabled = true;
    [SerializeField] private float dropHeightThreshold = 0.75f;
    [SerializeField] private float dropDistance = 6.5f;
    [SerializeField] private float dropArcHeight = 0.35f;
    [SerializeField] private float dropDuration = 0.38f;
    [SerializeField] private float dropCooldown = 1.1f;
    [SerializeField] private float dropLandingSampleRadius = 4.5f;

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
    private float nextVaultTime;
    private float nextDropTime;
    private Coroutine vaultRoutine;
    private Coroutine dropRoutine;

    public bool JumpEnabled => jumpEnabled;
    public bool IsBusy => IsJumping || vaultRoutine != null || dropRoutine != null;

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
        if (!jumpEnabled || IsBusy || Time.time < nextJumpTime) return;

        if (agent != null) defaultBaseOffset = agent.baseOffset;
        else fallbackGroundY = transform.position.y;

        jumpStartedAt = Time.time;
        nextJumpTime = Time.time + jumpCooldown;
    }

    public bool TryVaultToward(Vector3 targetPosition)
    {
        if (!vaultEnabled || !jumpEnabled || IsBusy || Time.time < nextVaultTime) return false;

        Vector3 start = transform.position;
        Vector3 planar = targetPosition - start;
        planar.y = 0f;
        if (planar.sqrMagnitude <= 0.05f) return false;

        Vector3 direction = planar.normalized;
        float distance = Mathf.Min(vaultDistance, Mathf.Max(1f, planar.magnitude * 0.55f));
        Vector3 candidate = start + direction * distance;
        candidate.y = Mathf.Max(start.y, targetPosition.y);

        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, vaultLandingSampleRadius, NavMesh.AllAreas))
        {
            candidate = start + direction * Mathf.Min(1.2f, distance);
            if (!NavMesh.SamplePosition(candidate, out hit, vaultLandingSampleRadius, NavMesh.AllAreas)) return false;
        }

        nextVaultTime = Time.time + vaultCooldown;
        vaultRoutine = StartCoroutine(TraversalRoutine(hit.position, vaultDuration, vaultHeight, true));
        return true;
    }

    public bool TryDropToward(Vector3 targetPosition)
    {
        if (!dropEnabled || !jumpEnabled || IsBusy || Time.time < nextDropTime) return false;

        Vector3 start = transform.position;
        Vector3 planar = targetPosition - start;
        float verticalDrop = start.y - targetPosition.y;
        planar.y = 0f;
        if (verticalDrop < dropHeightThreshold || planar.sqrMagnitude <= 0.05f) return false;

        Vector3 direction = planar.normalized;
        if (!TryResolveDropLanding(start, direction, targetPosition, verticalDrop, planar.magnitude, out Vector3 landingPosition))
        {
            return false;
        }

        nextDropTime = Time.time + dropCooldown;
        dropRoutine = StartCoroutine(TraversalRoutine(landingPosition, dropDuration, dropArcHeight, false));
        return true;
    }

    public bool TryResolveTraversalToward(Vector3 targetPosition, bool pathTroubled)
    {
        if (!jumpEnabled || IsBusy) return false;

        Vector3 delta = targetPosition - transform.position;
        Vector3 planar = delta;
        planar.y = 0f;

        bool targetBelow = delta.y < -dropHeightThreshold && planar.sqrMagnitude <= dropDistance * dropDistance * 8f;
        if ((targetBelow || (pathTroubled && delta.y < -0.35f)) && TryDropToward(targetPosition))
        {
            return true;
        }

        bool targetAbove = delta.y > targetAirborneHeight && planar.sqrMagnitude <= vaultDistance * vaultDistance * 7f;
        if (targetAbove || pathTroubled)
        {
            return TryVaultToward(targetPosition);
        }

        return false;
    }

    private bool IsJumping => jumpStartedAt >= 0f && Time.time - jumpStartedAt < jumpDuration;

    private bool TryResolveDropLanding(
        Vector3 start,
        Vector3 direction,
        Vector3 targetPosition,
        float verticalDrop,
        float planarDistance,
        out Vector3 landingPosition)
    {
        landingPosition = targetPosition;
        float maxDistance = Mathf.Min(dropDistance, Mathf.Max(1.8f, planarDistance * 0.9f));
        float[] distanceChecks =
        {
            Mathf.Min(1.4f, maxDistance),
            Mathf.Min(2.4f, maxDistance),
            Mathf.Min(3.8f, maxDistance),
            maxDistance
        };

        for (int i = 0; i < distanceChecks.Length; i++)
        {
            Vector3 candidate = start + direction * distanceChecks[i];
            if (TryResolveGroundedDropPoint(candidate, verticalDrop, out landingPosition))
            {
                return true;
            }
        }

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit targetHit, dropLandingSampleRadius + 1.5f, NavMesh.AllAreas))
        {
            landingPosition = targetHit.position;
            return true;
        }

        return TryResolveGroundedDropPoint(targetPosition, verticalDrop, out landingPosition);
    }

    private bool TryResolveGroundedDropPoint(Vector3 candidate, float verticalDrop, out Vector3 landingPosition)
    {
        landingPosition = candidate;
        Vector3 rayOrigin = candidate + Vector3.up * 2.5f;
        float rayDistance = Mathf.Max(6f, verticalDrop + 8f);
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, rayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (groundHit.point.y > transform.position.y - dropHeightThreshold * 0.45f)
        {
            return false;
        }

        if (NavMesh.SamplePosition(groundHit.point, out NavMeshHit navHit, dropLandingSampleRadius, NavMesh.AllAreas))
        {
            landingPosition = navHit.position;
            return true;
        }

        landingPosition = groundHit.point;
        return true;
    }

    private IEnumerator TraversalRoutine(Vector3 landingPosition, float moveDuration, float arcHeight, bool isVault)
    {
        Vector3 start = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 direction = landingPosition - start;
        direction.y = 0f;
        Quaternion endRotation = direction.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : startRotation;

        bool hadAgent = agent != null && agent.enabled;
        bool wasStopped = hadAgent && agent.isStopped;
        if (hadAgent)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.08f, moveDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            Vector3 position = Vector3.Lerp(start, landingPosition, eased) + Vector3.up * (Mathf.Sin(eased * Mathf.PI) * arcHeight);
            transform.SetPositionAndRotation(position, Quaternion.Slerp(startRotation, endRotation, eased));
            yield return null;
        }

        transform.SetPositionAndRotation(landingPosition, endRotation);
        if (hadAgent)
        {
            if (!agent.Warp(landingPosition))
            {
                transform.position = landingPosition;
            }

            agent.isStopped = wasStopped;
        }

        if (isVault) vaultRoutine = null;
        else dropRoutine = null;
    }

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
