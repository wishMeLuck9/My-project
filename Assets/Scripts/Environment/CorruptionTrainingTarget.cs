using System;
using UnityEngine;

public class CorruptionTrainingTarget : MonoBehaviour
{
    [SerializeField] private int hitsToComplete = 1;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Light feedbackLight;
    [SerializeField] private string completionMessageKey = "raw.night.training.hit";

    private int hitCount;
    private float feedbackUntil;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public static event Action<CorruptionTrainingTarget> TrainingTargetCompleted;
    public bool IsDefeated => hitCount >= Mathf.Max(1, hitsToComplete);

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        if (feedbackLight == null)
        {
            feedbackLight = GetComponentInChildren<Light>(true);
        }

        propertyBlock = new MaterialPropertyBlock();
        ApplyColor(new Color(0.12f, 0.28f, 0.22f, 1f));
    }

    private void Update()
    {
        if (feedbackLight != null)
        {
            feedbackLight.enabled = !IsDefeated;
            feedbackLight.intensity = Time.time < feedbackUntil ? 3f : 1.2f;
        }
    }

    public void ReceiveTrainingHit()
    {
        if (IsDefeated) return;

        hitCount += 1;
        feedbackUntil = Time.time + 0.35f;
        ApplyColor(IsDefeated ? new Color(0.06f, 0.08f, 0.07f, 1f) : new Color(0.78f, 0.92f, 0.6f, 1f));

        if (IsDefeated)
        {
            RuntimeHudController.Instance?.ShowSystemMessage(
                LocalizationManager.EnsureInstance().Get(completionMessageKey),
                4.5f);
            TrainingTargetCompleted?.Invoke(this);
            gameObject.SetActive(false);
        }
    }

    private void ApplyColor(Color color)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
