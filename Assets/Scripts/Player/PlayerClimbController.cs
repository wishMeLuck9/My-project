using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController3D))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerClimbController : MonoBehaviour
{
    [SerializeField] private LayerMask climbMask = ~0;
    [SerializeField] private float probeDistance = 0.95f;
    [SerializeField] private float minClimbHeight = 0.55f;
    [SerializeField] private float maxClimbHeight = 2.4f;
    [SerializeField] private float topProbeHeight = 2.75f;
    [SerializeField] private float topProbeForwardOffset = 0.55f;
    [SerializeField] private float landingInset = 0.55f;
    [SerializeField] private float climbDuration = 0.55f;

    private readonly Collider[] clearanceHits = new Collider[12];
    private PlayerController3D player;
    private PlayerVisualAnimator visualAnimator;
    private Rigidbody body;
    private Collider playerCollider;
    private Coroutine climbRoutine;
    private bool isClimbing;
    private bool hasStoredBodyState;
    private bool storedKinematic;
    private bool storedGravity;

    public bool IsClimbing => isClimbing;

    private void Awake()
    {
        player = GetComponent<PlayerController3D>();
        body = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        visualAnimator = GetComponentInChildren<PlayerVisualAnimator>(true);
    }

    private void OnDisable()
    {
        if (climbRoutine != null)
        {
            StopCoroutine(climbRoutine);
            climbRoutine = null;
        }

        if (isClimbing) RestoreMovement();
    }

    public bool TryStartClimb()
    {
        if (isClimbing || player == null || body == null || playerCollider == null) return false;
        if (!player.CanMove || !player.IsGrounded) return false;
        if (!TryFindClimbTarget(out Vector3 targetPosition, out Quaternion targetRotation)) return false;

        climbRoutine = StartCoroutine(ClimbRoutine(targetPosition, targetRotation));
        return true;
    }

    private bool TryFindClimbTarget(out Vector3 targetPosition, out Quaternion targetRotation)
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        Bounds bounds = playerCollider.bounds;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f) return false;
        forward.Normalize();

        float bodyRadius = Mathf.Max(0.15f, Mathf.Min(bounds.extents.x, bounds.extents.z));
        if (!TryFindWallHit(bounds, forward, bodyRadius, out RaycastHit wallHit)) return false;

        Vector3 topOrigin = wallHit.point + forward * topProbeForwardOffset + Vector3.up * topProbeHeight;
        float topProbeDistance = topProbeHeight + 0.35f;
        if (!Physics.Raycast(topOrigin, Vector3.down, out RaycastHit topHit, topProbeDistance, climbMask, QueryTriggerInteraction.Ignore)) return false;
        if (topHit.collider == null || IsSelfCollider(topHit.collider)) return false;
        if (Vector3.Dot(topHit.normal, Vector3.up) < 0.65f) return false;

        float climbHeight = topHit.point.y - bounds.min.y;
        if (climbHeight < minClimbHeight || climbHeight > maxClimbHeight) return false;

        Vector3 landingPoint = topHit.point + forward * landingInset;
        Vector3 bottomToRootOffset = transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        targetPosition = new Vector3(landingPoint.x, topHit.point.y, landingPoint.z) + bottomToRootOffset;
        targetRotation = Quaternion.LookRotation(forward, Vector3.up);

        if (!ExteriorBoundaryController.TryValidateTargetPosition(targetPosition, out _, true)) return false;
        return HasClearanceAt(targetPosition);
    }

    private bool TryFindWallHit(Bounds bounds, Vector3 forward, float bodyRadius, out RaycastHit wallHit)
    {
        const int ProbeCount = 5;
        float topOffset = Mathf.Min(maxClimbHeight, bounds.size.y * 0.95f);
        for (int i = 0; i < ProbeCount; i++)
        {
            float t = ProbeCount == 1 ? 0f : i / (float)(ProbeCount - 1);
            float yOffset = Mathf.Lerp(minClimbHeight, topOffset, t);
            Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + yOffset, bounds.center.z)
                + forward * (bodyRadius + 0.03f);

            if (!Physics.Raycast(origin, forward, out wallHit, probeDistance, climbMask, QueryTriggerInteraction.Ignore)) continue;
            if (wallHit.collider == null || IsSelfCollider(wallHit.collider)) continue;
            if (Vector3.Dot(wallHit.normal, Vector3.up) > 0.35f) continue;
            return true;
        }

        wallHit = default;
        return false;
    }

    private bool HasClearanceAt(Vector3 targetRootPosition)
    {
        GetTargetCapsule(targetRootPosition, out Vector3 bottom, out Vector3 top, out float radius);
        int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, clearanceHits, climbMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = clearanceHits[i];
            if (hit == null || IsSelfCollider(hit)) continue;
            return false;
        }

        return true;
    }

    private void GetTargetCapsule(Vector3 targetRootPosition, out Vector3 bottom, out Vector3 top, out float radius)
    {
        if (playerCollider is CapsuleCollider capsule)
        {
            Vector3 scale = capsule.transform.lossyScale;
            radius = Mathf.Max(0.05f, capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) - 0.03f);
            float capsuleHeight = Mathf.Max(radius * 2f, capsule.height * Mathf.Abs(scale.y));
            Vector3 center = targetRootPosition + transform.rotation * capsule.center;
            float halfLine = Mathf.Max(0f, capsuleHeight * 0.5f - radius);
            bottom = center + Vector3.up * (-halfLine + 0.08f);
            top = center + Vector3.up * halfLine;
            return;
        }

        Bounds bounds = playerCollider.bounds;
        radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) - 0.03f);
        float fallbackHeight = Mathf.Max(radius * 2f, bounds.size.y);
        Vector3 centerOffset = bounds.center - transform.position;
        Vector3 centerAtTarget = targetRootPosition + centerOffset;
        float halfLineFallback = Mathf.Max(0f, fallbackHeight * 0.5f - radius);
        bottom = centerAtTarget + Vector3.up * (-halfLineFallback + 0.08f);
        top = centerAtTarget + Vector3.up * halfLineFallback;
    }

    private IEnumerator ClimbRoutine(Vector3 targetPosition, Quaternion targetRotation)
    {
        isClimbing = true;
        storedKinematic = body.isKinematic;
        storedGravity = body.useGravity;
        hasStoredBodyState = true;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        player.SetCanMove(false);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.isKinematic = true;
        ResolveVisualAnimator()?.PlayClimb();

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, climbDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, eased),
                Quaternion.Slerp(startRotation, targetRotation, eased));
            yield return null;
        }

        transform.SetPositionAndRotation(targetPosition, targetRotation);
        body.position = targetPosition;
        body.rotation = targetRotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        RestoreMovement();
        climbRoutine = null;
    }

    private void RestoreMovement()
    {
        isClimbing = false;
        if (player != null) player.SetCanMove(true);
        if (body != null)
        {
            if (hasStoredBodyState)
            {
                body.isKinematic = storedKinematic;
                body.useGravity = storedGravity;
                hasStoredBodyState = false;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }
    }

    private bool IsSelfCollider(Collider other)
    {
        return other != null && (other == playerCollider || other.transform == transform || other.transform.IsChildOf(transform));
    }

    private PlayerVisualAnimator ResolveVisualAnimator()
    {
        if (visualAnimator == null) visualAnimator = GetComponentInChildren<PlayerVisualAnimator>(true);
        return visualAnimator;
    }
}
