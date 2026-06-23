using System.Collections.Generic;
using UnityEngine;

public class ReturnGateController : Interactable
{
    [SerializeField] private string targetScene = SceneIds.Exterior;
    [SerializeField] private bool requiresExteriorFragment;
    [SerializeField] private bool requiresInnerNightFragment;
    [SerializeField] private string promptKey = "raw.return_gate.prompt";
    [SerializeField] private string lockedKey = "raw.return_gate.locked";
    [SerializeField] private string enterKey = "raw.return_gate.enter";
    [SerializeField] private string leaveKey = "raw.return_gate.leave";
    [SerializeField] private Light gateLight;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private float pulseSpeed = 1.6f;
    [SerializeField] private float minLightIntensity = 0.8f;
    [SerializeField] private float maxLightIntensity = 2.1f;

    private bool transitionQueued;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public void Configure(string newTargetScene, bool requireExterior, bool requireInnerNight)
    {
        targetScene = newTargetScene;
        requiresExteriorFragment = requireExterior;
        requiresInnerNightFragment = requireInnerNight;
    }

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        if (gateLight == null)
        {
            gateLight = GetComponentInChildren<Light>(true);
        }

        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        float pulse = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.5f;
        Color color = Color.Lerp(new Color(0.2f, 0.45f, 0.32f, 1f), new Color(0.92f, 0.72f, 0.28f, 1f), pulse);

        if (gateLight != null)
        {
            gateLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, pulse);
            gateLight.color = color;
        }

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

    public override void Interact()
    {
        if (transitionQueued) return;
        if (DialogueController.Instance == null || GameFlowController.Instance == null) return;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        if (!CanUseGate())
        {
            DialogueController.Instance.ShowDialogue(localizer.Get("speaker.gate", "GATE"), localizer.Get(lockedKey));
            return;
        }

        DialogueController.Instance.ShowChoices(
            localizer.Get("speaker.gate", "GATE"),
            localizer.Get(promptKey),
            new List<DialogueChoice>
            {
                new DialogueChoice(localizer.Get(enterKey), BeginTransition),
                new DialogueChoice(localizer.Get(leaveKey), null)
            });
    }

    private bool CanUseGate()
    {
        WorldState state = WorldState.Instance;
        if (state == null) return false;
        if (requiresExteriorFragment && !state.hasExteriorFragment) return false;
        if (requiresInnerNightFragment && !state.hasInnerNightFragment) return false;
        return !string.IsNullOrWhiteSpace(targetScene);
    }

    private void BeginTransition()
    {
        if (transitionQueued) return;

        transitionQueued = true;
        Time.timeScale = 1f;
        GameFlowController.Instance.TransitionToLocation(targetScene);
    }
}
