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

    private TrailRenderer trail;
    private Light glow;

    public static CorruptionSpellProjectile Spawn(
        Vector3 start,
        Vector3 destination,
        Vector3 impactNormal,
        Transform impactTarget,
        float speed,
        float radius)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "Corruption_Spell_Projectile";
        Destroy(projectileObject.GetComponent<Collider>());

        CorruptionSpellProjectile projectile = projectileObject.AddComponent<CorruptionSpellProjectile>();
        projectile.Initialize(start, destination, impactNormal, impactTarget, speed, radius);
        return projectile;
    }

    private void Initialize(Vector3 start, Vector3 destination, Vector3 impactNormal, Transform impactTarget, float speed, float radius)
    {
        this.start = start;
        this.destination = destination;
        this.impactNormal = impactNormal.sqrMagnitude > 0.001f ? impactNormal.normalized : Vector3.up;
        this.impactTarget = impactTarget;
        this.radius = Mathf.Max(0.04f, radius);

        transform.position = start;
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
        transform.position = Vector3.Lerp(start, destination, t) + Vector3.up * arc;
        if (!CorruptionVfxUtility.ReduceMotion)
        {
            transform.Rotate(180f * Time.deltaTime, 260f * Time.deltaTime, 120f * Time.deltaTime, Space.Self);
        }

        if (glow != null) glow.intensity = Mathf.Lerp(CorruptionVfxUtility.ReduceMotion ? 0.8f : 1.6f, 0.7f, t);

        if (t >= 1f)
        {
            CorruptionImpactEffect.Spawn(destination, impactNormal, impactTarget);
            Destroy(gameObject);
        }
    }
}
