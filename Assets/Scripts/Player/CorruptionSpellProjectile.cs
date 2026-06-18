using UnityEngine;

public sealed class CorruptionSpellProjectile : MonoBehaviour
{
    private const float MinimumTravelTime = 0.08f;

    private Vector3 start;
    private Vector3 destination;
    private Vector3 impactNormal;
    private Transform impactTarget;
    private float duration;
    private float elapsed;
    private float radius;
    private int hitMask;
    private bool resolveGameplayHit;
    private Transform attackSource;
    private bool impactResolved;
    private Vector3 previousPosition;

    private TrailRenderer trail;
    private Light glow;
    private readonly RaycastHit[] travelHits = new RaycastHit[24];

    public static CorruptionSpellProjectile Spawn(
        Vector3 start,
        Vector3 destination,
        Vector3 impactNormal,
        Transform impactTarget,
        float speed,
        float radius,
        bool resolveGameplayHit = false,
        Transform attackSource = null,
        int hitMask = Physics.DefaultRaycastLayers)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "Corruption_Spell_Projectile";
        Destroy(projectileObject.GetComponent<Collider>());

        CorruptionSpellProjectile projectile = projectileObject.AddComponent<CorruptionSpellProjectile>();
        projectile.Initialize(start, destination, impactNormal, impactTarget, speed, radius, resolveGameplayHit, attackSource, hitMask);
        return projectile;
    }

    private void Initialize(
        Vector3 start,
        Vector3 destination,
        Vector3 impactNormal,
        Transform impactTarget,
        float speed,
        float radius,
        bool resolveGameplayHit,
        Transform attackSource,
        int hitMask)
    {
        this.start = start;
        this.destination = destination;
        this.impactNormal = impactNormal.sqrMagnitude > 0.001f ? impactNormal.normalized : Vector3.up;
        this.impactTarget = impactTarget;
        this.radius = Mathf.Max(0.04f, radius);
        this.resolveGameplayHit = resolveGameplayHit;
        this.attackSource = attackSource;
        this.hitMask = hitMask == 0 ? Physics.DefaultRaycastLayers : hitMask;

        transform.position = start;
        previousPosition = start;
        transform.localScale = Vector3.one * this.radius * 2f;
        duration = Mathf.Max(MinimumTravelTime, Vector3.Distance(start, destination) / Mathf.Max(1f, speed));

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = CorruptionVfxUtility.CreateCoreMaterial();

        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = CorruptionVfxUtility.ReduceMotion ? 0.08f : 0.22f;
        trail.minVertexDistance = 0.04f;
        trail.widthMultiplier = this.radius * 1.35f;
        trail.material = CorruptionVfxUtility.CreateTrailMaterial();
        trail.colorGradient = CorruptionVfxUtility.CreateCyanVioletGradient(0.9f, 0f);

        glow = gameObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = CorruptionVfxUtility.Primary;
        glow.range = 2.2f;
        glow.intensity = CorruptionVfxUtility.ReduceMotion ? 0.8f : 1.4f;
        glow.shadows = LightShadows.None;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float arc = CorruptionVfxUtility.ReduceMotion ? 0f : Mathf.Sin(t * Mathf.PI) * 0.35f;
        Vector3 nextPosition = Vector3.Lerp(start, destination, t) + Vector3.up * arc;
        if (TryResolveTravelImpact(previousPosition, nextPosition))
        {
            CorruptionImpactEffect.Spawn(destination, impactNormal, impactTarget);
            Destroy(gameObject);
            return;
        }

        transform.position = nextPosition;
        previousPosition = nextPosition;
        if (!CorruptionVfxUtility.ReduceMotion)
        {
            transform.Rotate(180f * Time.deltaTime, 260f * Time.deltaTime, 120f * Time.deltaTime, Space.Self);
        }

        if (glow != null) glow.intensity = Mathf.Lerp(CorruptionVfxUtility.ReduceMotion ? 0.8f : 1.6f, 0.7f, t);

        if (t >= 1f)
        {
            ResolveGameplayImpact();
            CorruptionImpactEffect.Spawn(destination, impactNormal, impactTarget);
            Destroy(gameObject);
        }
    }

    private bool TryResolveTravelImpact(Vector3 from, Vector3 to)
    {
        if (!resolveGameplayHit || impactResolved) return false;

        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.001f) return false;

        int hitCount = Physics.SphereCastNonAlloc(
            from,
            radius,
            delta / distance,
            travelHits,
            distance,
            hitMask,
            QueryTriggerInteraction.Collide);

        RaycastHit bestHit = default;
        float nearestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = travelHits[i];
            Collider collider = candidate.collider;
            if (collider == null || IsPartOfSource(collider.transform)) continue;
            if (collider.isTrigger && !IsCombatTarget(collider.transform)) continue;
            if (candidate.distance >= nearestDistance) continue;

            nearestDistance = candidate.distance;
            bestHit = candidate;
            found = true;
        }

        if (!found) return false;

        destination = bestHit.point;
        impactNormal = bestHit.normal.sqrMagnitude > 0.001f ? bestHit.normal.normalized : -delta.normalized;
        impactTarget = bestHit.collider.transform;
        transform.position = destination;
        previousPosition = destination;

        ResolveGameplayImpact();
        return true;
    }

    private void ResolveGameplayImpact()
    {
        if (!resolveGameplayHit || impactResolved || impactTarget == null) return;

        impactResolved = true;
        PrototypeShadowActor shadow = impactTarget.GetComponentInParent<PrototypeShadowActor>();
        if (shadow != null)
        {
            shadow.ReceiveAttack(attackSource);
            return;
        }

        GuardianController guardian = impactTarget.GetComponentInParent<GuardianController>();
        guardian?.ReceiveAttack();
    }

    private static bool IsCombatTarget(Transform target)
    {
        if (target == null) return false;
        return target.GetComponentInParent<PrototypeShadowActor>() != null ||
               target.GetComponentInParent<GuardianController>() != null;
    }

    private bool IsPartOfSource(Transform candidate)
    {
        return candidate != null
            && attackSource != null
            && (candidate == attackSource || candidate.IsChildOf(attackSource) || attackSource.IsChildOf(candidate));
    }
}
