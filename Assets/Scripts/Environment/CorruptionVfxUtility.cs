using UnityEngine;

public static class CorruptionVfxUtility
{
    public static readonly Color Cyan = new Color(0.15f, 0.95f, 1f, 1f);
    public static readonly Color Violet = new Color(0.34f, 0.05f, 0.74f, 1f);
    private static Material coreMaterial;
    private static Material trailMaterial;
    private static Material markMaterial;
    private static Material particleMaterial;

    public static Material CreateCoreMaterial()
    {
        if (coreMaterial == null) coreMaterial = CreateMaterial("Corruption_Core_Runtime", Primary, true);
        SetMaterialColor(coreMaterial, Primary);
        return coreMaterial;
    }

    public static Material CreateTrailMaterial()
    {
        if (trailMaterial == null) trailMaterial = CreateMaterial("Corruption_Trail_Runtime", Primary, true);
        SetMaterialColor(trailMaterial, Primary);
        return trailMaterial;
    }

    public static Material CreateMarkMaterial()
    {
        Color markColor = IsColorblindFriendly ? new Color(0.16f, 0.11f, 0.02f, 0.72f) : new Color(0.1f, 0.02f, 0.22f, 0.72f);
        if (markMaterial == null) markMaterial = CreateMaterial("Corruption_Mark_Runtime", markColor, true);
        SetMaterialColor(markMaterial, markColor);
        return markMaterial;
    }

    public static Material CreateParticleMaterial()
    {
        if (particleMaterial == null) particleMaterial = CreateMaterial("Corruption_Particle_Runtime", Primary, true);
        SetMaterialColor(particleMaterial, Primary);
        return particleMaterial;
    }

    public static Gradient CreateCyanVioletGradient(float startAlpha, float endAlpha)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Primary, 0f),
                new GradientColorKey(Secondary, 0.72f),
                new GradientColorKey(Color.black, 1f)
            },
            new[]
            {
                new GradientAlphaKey(startAlpha, 0f),
                new GradientAlphaKey(startAlpha * 0.6f, 0.55f),
                new GradientAlphaKey(endAlpha, 1f)
            });
        return gradient;
    }

    public static Color Primary => IsColorblindFriendly ? new Color(1f, 0.76f, 0.08f, 1f) : Cyan;
    public static Color Secondary => IsColorblindFriendly ? new Color(0.08f, 0.45f, 1f, 1f) : Violet;
    public static bool ReduceMotion => SettingsManager.Instance != null && SettingsManager.Instance.ReduceMotion;

    private static bool IsColorblindFriendly => SettingsManager.Instance != null && SettingsManager.Instance.ColorblindFriendly;

    private static Material CreateMaterial(string name, Color color, bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            name = name
        };
        SetMaterialColor(material, color);

        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        return material;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color);
    }
}
