using System.Collections;
using UnityEngine;

public class ReturnGateController : Interactable
{
    [SerializeField] private string targetScene = SceneIds.Exterior;
    [SerializeField] private bool requiresExteriorFragment;
    [SerializeField] private bool requiresInnerNightFragment;
    [SerializeField] private bool triggerOnPlayerEnter;
    [SerializeField] private string lockedKey = "raw.return_gate.locked";
    [SerializeField] private string shotKey = "raw.return_gate.shot";
    [SerializeField] private string triggerKey = "raw.return_gate.enter";
    [SerializeField] private Light gateLight;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private float pulseSpeed = 1.6f;
    [SerializeField] private float minLightIntensity = 0.8f;
    [SerializeField] private float maxLightIntensity = 2.1f;
    [SerializeField] private float shotTransitionDelay = 1.05f;
    [SerializeField] private float triggerTransitionDelay = 0.55f;

    private bool transitionQueued;
    private float lockedMessageCooldownUntil;
    private Coroutine transitionRoutine;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public void Configure(string newTargetScene, bool requireExterior, bool requireInnerNight)
    {
        targetScene = newTargetScene;
        requiresExteriorFragment = requireExterior;
        requiresInnerNightFragment = requireInnerNight;
    }

    public bool CanUseGate => CanUseGateInternal();

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

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        if (!CanUseGateInternal())
        {
            RuntimeHudController.Instance?.ShowSystemMessage(localizer.Get(lockedKey), 4f);
            return;
        }

        TryStartInteractionReturn(null);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerReturn(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTriggerReturn(other);
    }

    public bool TryStartShotReturn(Transform source)
    {
        if (transitionQueued || GameFlowController.Instance == null) return false;
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        if (!CanUseGateInternal())
        {
            ShowLockedMessage(localizer);
            return false;
        }

        transitionQueued = true;
        Time.timeScale = 1f;
        transitionRoutine = StartCoroutine(ShotReturnRoutine(source));
        return true;
    }

    public bool TryStartTriggerReturn(Transform source)
    {
        if (!triggerOnPlayerEnter) return false;
        return TryStartInteractionReturn(source);
    }

    private bool TryStartInteractionReturn(Transform source)
    {
        if (transitionQueued || GameFlowController.Instance == null) return false;
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        if (!CanUseGateInternal())
        {
            ShowLockedMessage(localizer);
            return false;
        }

        transitionQueued = true;
        Time.timeScale = 1f;
        transitionRoutine = StartCoroutine(TriggerReturnRoutine(source));
        return true;
    }

    private IEnumerator ShotReturnRoutine(Transform source)
    {
        PlayerController3D player = source != null ? source.GetComponentInParent<PlayerController3D>() : FindFirstObjectByType<PlayerController3D>();
        PlayerAttackController attack = player != null ? player.GetComponent<PlayerAttackController>() : null;
        if (player != null) player.SetCanMove(false);
        if (attack != null) attack.SetCanAttack(false);

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        RuntimeHudController.Instance?.ShowSystemMessage(localizer.Get(shotKey), shotTransitionDelay + 0.6f);

        float elapsed = 0f;
        while (elapsed < shotTransitionDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        GameFlowController.Instance.TransitionToLocation(targetScene);
    }

    private IEnumerator TriggerReturnRoutine(Transform source)
    {
        PlayerController3D player = source != null ? source.GetComponentInParent<PlayerController3D>() : FindFirstObjectByType<PlayerController3D>();
        PlayerAttackController attack = player != null ? player.GetComponent<PlayerAttackController>() : null;
        if (player != null) player.SetCanMove(false);
        if (attack != null) attack.SetCanAttack(false);

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        RuntimeHudController.Instance?.ShowSystemMessage(localizer.Get(triggerKey), triggerTransitionDelay + 0.8f);

        float elapsed = 0f;
        while (elapsed < triggerTransitionDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        GameFlowController.Instance.TransitionToLocation(targetScene);
    }

    private void TryTriggerReturn(Collider other)
    {
        if (!triggerOnPlayerEnter || transitionQueued || other == null) return;

        PlayerController3D player = other.GetComponentInParent<PlayerController3D>();
        if (player == null) return;

        TryStartTriggerReturn(player.transform);
    }

    private void ShowLockedMessage(LocalizationManager localizer)
    {
        if (Time.unscaledTime < lockedMessageCooldownUntil) return;

        RuntimeHudController.Instance?.ShowSystemMessage(localizer.Get(lockedKey), 4f);
        lockedMessageCooldownUntil = Time.unscaledTime + 1.5f;
    }

    private bool CanUseGateInternal()
    {
        WorldState state = WorldState.Instance;
        if (state == null) return false;
        if (requiresExteriorFragment && !state.hasExteriorFragment) return false;
        if (requiresInnerNightFragment && !state.hasInnerNightFragment) return false;
        return !string.IsNullOrWhiteSpace(targetScene);
    }
}

public class CorruptionGateHitReceiver : MonoBehaviour
{
    [SerializeField] private ReturnGateController returnGate;

    public bool CanReceiveHit => returnGate != null && returnGate.CanUseGate;

    public void Configure(ReturnGateController gate)
    {
        returnGate = gate;
    }

    private void Awake()
    {
        if (returnGate == null)
        {
            returnGate = GetComponentInParent<ReturnGateController>();
        }
    }

    public bool ReceiveCorruptionHit(Transform source)
    {
        if (returnGate == null) return false;
        return returnGate.TryStartShotReturn(source);
    }
}
