using UnityEngine;

public sealed class CorruptionImpactEffect : MonoBehaviour
{
    private const float DefaultLifetime = 5.5f;

    private float lifetime = DefaultLifetime;
    private float elapsed;
    private Renderer markRenderer;
    private ParticleSystem burstParticles;
    private ParticleSystem emberParticles;
    private Light pulseLight;
    private LineRenderer[] tendrils;
    private MaterialPropertyBlock propertyBlock;

    public static CorruptionImpactEffect Spawn(Vector3 position, Vector3 normal, Transform target)
    {
        GameObject effectObject = new GameObject("Corruption_Impact_Effect");
        effectObject.transform.position = position + normal.normalized * 0.025f;
        effectObject.transform.rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up);

        CorruptionImpactEffect effect = effectObject.AddComponent<CorruptionImpactEffect>();
        effect.Build(position, normal, target);
        return effect;
    }

    private void Build(Vector3 position, Vector3 normal, Transform target)
    {
        Vector3 safeNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;

        GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mark.name = "Corruption_Surface_Mark";
        mark.transform.SetParent(transform, false);
        mark.transform.localPosition = Vector3.zero;
        mark.transform.localRotation = Quaternion.identity;
        mark.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
        Destroy(mark.GetComponent<Collider>());
        markRenderer = mark.GetComponent<Renderer>();
        if (markRenderer != null) markRenderer.sharedMaterial = CorruptionVfxUtility.CreateMarkMaterial();

        burstParticles = CreateBurstParticles("Corruption_Impact_Burst", position, safeNormal);
        emberParticles = CreateEmberParticles("Corruption_Embers", position, safeNormal);
        pulseLight = CreatePulseLight();
        tendrils = CreateTendrils(safeNormal);
        propertyBlock = new MaterialPropertyBlock();

        Renderer targetRenderer = target != null ? target.GetComponentInParent<Renderer>() : null;
        if (targetRenderer != null)
        {
            transform.SetParent(targetRenderer.transform, true);
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        float grow = Mathf.SmoothStep(0.25f, 1.35f, Mathf.Clamp01(t * 2.2f));
        float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((t - 0.55f) / 0.45f));
        float pulse = CorruptionVfxUtility.ReduceMotion ? 0.5f : 0.5f + Mathf.Sin(Time.time * 16f) * 0.5f;

        if (markRenderer != null)
        {
            transform.localScale = Vector3.one * grow;
            Color pulseColor = Color.Lerp(CorruptionVfxUtility.Secondary, CorruptionVfxUtility.Primary, pulse) * new Color(1f, 1f, 1f, alpha);
            propertyBlock.SetColor("_BaseColor", pulseColor);
            propertyBlock.SetColor("_Color", pulseColor);
            markRenderer.SetPropertyBlock(propertyBlock);
        }

        if (pulseLight != null)
        {
            pulseLight.intensity = alpha * Mathf.Lerp(0.7f, CorruptionVfxUtility.ReduceMotion ? 1.2f : 2.4f, pulse);
            pulseLight.range = Mathf.Lerp(1.4f, 3.2f, grow);
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private ParticleSystem CreateBurstParticles(string objectName, Vector3 position, Vector3 normal)
    {
        GameObject particleObject = new GameObject(objectName);
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.position = position + normal * 0.08f;
        particleObject.transform.rotation = Quaternion.LookRotation(normal);

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(CorruptionVfxUtility.Primary, CorruptionVfxUtility.Secondary);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(CorruptionVfxUtility.ReduceMotion ? 12 : 34)) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 42f;
        shape.radius = 0.16f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = CorruptionVfxUtility.CreateParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        particles.Play();
        return particles;
    }

    private ParticleSystem CreateEmberParticles(string objectName, Vector3 position, Vector3 normal)
    {
        GameObject particleObject = new GameObject(objectName);
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.position = position + normal * 0.04f;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = lifetime;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.055f);
        main.startColor = new ParticleSystem.MinMaxGradient(CorruptionVfxUtility.Primary, CorruptionVfxUtility.Secondary);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = CorruptionVfxUtility.ReduceMotion ? 3f : 10f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.55f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = CorruptionVfxUtility.CreateParticleMaterial();

        particles.Play();
        return particles;
    }

    private Light CreatePulseLight()
    {
        GameObject lightObject = new GameObject("Corruption_Pulse_Light");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = Vector3.zero;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = CorruptionVfxUtility.Primary;
        light.range = 2.4f;
        light.intensity = CorruptionVfxUtility.ReduceMotion ? 0.75f : 1.2f;
        light.shadows = LightShadows.None;
        return light;
    }

    private LineRenderer[] CreateTendrils(Vector3 normal)
    {
        int count = CorruptionVfxUtility.ReduceMotion ? 3 : 7;
        LineRenderer[] lines = new LineRenderer[count];
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(normal, Vector3.right);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 direction = Mathf.Cos(angle) * tangent + Mathf.Sin(angle) * bitangent;
            float length = Mathf.Lerp(0.35f, 0.9f, (i % 4) / 3f);

            GameObject lineObject = new GameObject("Corruption_Tendril");
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 3;
            line.useWorldSpace = false;
            line.widthMultiplier = 0.025f;
            line.material = CorruptionVfxUtility.CreateTrailMaterial();
            line.colorGradient = CorruptionVfxUtility.CreateCyanVioletGradient(0.8f, 0f);
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, direction * length * 0.55f + normal * 0.035f);
            line.SetPosition(2, direction * length);
            lines[i] = line;
        }

        return lines;
    }
}
