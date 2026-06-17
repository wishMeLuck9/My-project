using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ExteriorGateCutsceneController : MonoBehaviour
{
    [SerializeField] private LocationTransition transition;
    [SerializeField] private SquarePortalController portal;
    [SerializeField] private ExteriorHuntController hunt;
    [SerializeField] private PlayerController3D player;
    [SerializeField] private PlayerInputReader playerInput;
    [SerializeField] private PlayerAttackController playerAttack;
    [SerializeField] private Light playerGateLight;
    [SerializeField] private Light gateGlow;
    [SerializeField] private float revealDistance = 9f;
    [SerializeField] private float shakeDistance = 5.5f;
    [SerializeField] private float cutsceneDistance = 2.8f;
    [SerializeField] private float maxPlayerLightIntensity = 3.2f;
    [SerializeField] private float maxGateLightIntensity = 3.8f;
    [SerializeField] private float shakeAmplitude = 0.045f;
    [SerializeField] private float shadowRingRadius = 3.8f;

    private Vector3 portalBaseLocalPosition;
    private bool hasPortalBasePosition;
    private bool cutsceneStarted;
    private bool cutsceneCompleted;
    private bool attackRequested;
    private Coroutine cutsceneRoutine;

    public bool IsCutsceneActive => cutsceneStarted && !cutsceneCompleted;

    private void Start()
    {
        ResolveReferences();
        CapturePortalBasePosition();
        SetVisualIntensity(0f);
    }

    private void OnDestroy()
    {
        if (playerInput != null) playerInput.AttackPressed -= HandleAttackPressed;
    }

    private void Update()
    {
        if (cutsceneCompleted || WorldState.Instance == null || !WorldState.Instance.hasExteriorFragment) return;

        ResolveReferences();
        if (player == null) return;

        float distance = Vector3.Distance(Planar(player.transform.position), Planar(transform.position));
        float proximity = Mathf.Clamp01(1f - Mathf.InverseLerp(cutsceneDistance, revealDistance, distance));
        bool shouldReveal = distance <= revealDistance || IsCutsceneActive;

        if (portal != null)
        {
            portal.SetPortalVisible(shouldReveal);
            if (!portal.IsUnlocked) portal.Lock();
        }

        SetVisualIntensity(shouldReveal ? proximity : 0f);
        TickGateShake(distance, proximity);

        if (!cutsceneStarted && distance <= cutsceneDistance)
        {
            StartGateCutscene();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartFromTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartFromTrigger(other);
    }

    public void Configure(LocationTransition newTransition, SquarePortalController newPortal, ExteriorHuntController newHunt = null)
    {
        transition = newTransition != null ? newTransition : transition;
        portal = newPortal != null ? newPortal : portal;
        hunt = newHunt != null ? newHunt : hunt;
        ResolveReferences();
        CapturePortalBasePosition();
        SetVisualIntensity(0f);
    }

    public void StartFromInteraction()
    {
        if (cutsceneCompleted) return;
        StartGateCutscene();
    }

    private void StartGateCutscene()
    {
        if (cutsceneStarted || cutsceneCompleted) return;
        if (WorldState.Instance == null || !WorldState.Instance.hasExteriorFragment) return;

        cutsceneStarted = true;
        cutsceneRoutine = StartCoroutine(GateCutsceneRoutine());
    }

    private IEnumerator GateCutsceneRoutine()
    {
        ResolveReferences();
        hunt?.PauseForGateSequence(true);
        SetPlayerLocked(true);

        if (portal != null)
        {
            portal.SetPortalVisible(true);
            portal.Lock();
        }

        SetVisualIntensity(1f);

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        yield return ShowPagesAndKeepLocked(
            "SYSTEM",
            localizer.Get("raw.exterior.gate.cutscene.found"),
            localizer.Get("raw.exterior.gate.cutscene.vision"));

        attackRequested = false;
        if (playerInput != null) playerInput.AttackPressed += HandleAttackPressed;

        while (!attackRequested)
        {
            RuntimeHudController.Instance?.ShowSystemMessage(localizer.Get("raw.exterior.gate.cutscene.prompt"), 1.2f);
            SetPlayerLocked(true);
            yield return null;
        }

        if (playerInput != null) playerInput.AttackPressed -= HandleAttackPressed;
        PlayCutsceneStrike();
        yield return new WaitForSeconds(0.45f);

        ArrangePursuersInFear();
        yield return ShowPagesAndKeepLocked(
            "SYSTEM",
            localizer.Get("raw.exterior.gate.cutscene.shadows"),
            localizer.Get("raw.exterior.gate.cutscene.impossible"));

        portal?.SetPortalVisible(true);
        portal?.Unlock();
        SetVisualIntensity(1f);
        yield return new WaitForSeconds(0.35f);

        cutsceneCompleted = true;
        transition?.TransitionFromExteriorGateCutscene();
    }

    private IEnumerator ShowPagesAndKeepLocked(string speaker, params string[] pages)
    {
        bool completed = false;
        if (DialogueController.Instance != null)
        {
            DialogueController.Instance.ShowDialoguePages(speaker, pages, () => completed = true);
            while (!completed)
            {
                yield return null;
            }
        }

        SetPlayerLocked(true);
    }

    private void HandleAttackPressed()
    {
        attackRequested = true;
    }

    private void PlayCutsceneStrike()
    {
        ResolveReferences();
        player?.GetComponentInChildren<PlayerVisualAnimator>(true)?.PlayAttack();

        Vector3 start = player != null
            ? player.transform.position + Vector3.up * 1.15f + player.transform.forward * 0.45f
            : transform.position + Vector3.up;
        Vector3 end = portal != null ? portal.transform.position + Vector3.up * 1.5f : transform.position + Vector3.up * 1.5f;
        Vector3 direction = end - start;

        CorruptionSpellProjectile.Spawn(
            start,
            end,
            direction.sqrMagnitude > 0.001f ? -direction.normalized : Vector3.up,
            portal != null ? portal.transform : transform,
            18f,
            0.22f);
    }

    private void ArrangePursuersInFear()
    {
        if (player == null) return;

        ExteriorPursuer[] pursuers = FindObjectsByType<ExteriorPursuer>(FindObjectsSortMode.None);
        if (pursuers == null || pursuers.Length == 0) return;

        for (int i = 0; i < pursuers.Length; i++)
        {
            ExteriorPursuer pursuer = pursuers[i];
            if (pursuer == null) continue;

            float angle = (360f / pursuers.Length) * i;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * shadowRingRadius;
            Vector3 target = player.transform.position + offset;
            target.y = pursuer.transform.position.y;

            NavMeshAgent agent = pursuer.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.Warp(target);
            }
            else
            {
                pursuer.transform.position = target;
            }

            Vector3 look = player.transform.position - pursuer.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f) pursuer.transform.rotation = Quaternion.LookRotation(look.normalized);
            TintShadow(pursuer);
        }
    }

    private void TintShadow(ExteriorPursuer pursuer)
    {
        foreach (Renderer renderer in pursuer.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null) renderer.material.color = new Color(0.06f, 0.11f, 0.16f, 1f);
        }
    }

    private void TickGateShake(float distance, float proximity)
    {
        if (portal == null || !hasPortalBasePosition) return;

        if (distance > shakeDistance || cutsceneCompleted)
        {
            portal.transform.localPosition = portalBaseLocalPosition;
            return;
        }

        float strength = Mathf.Clamp01(proximity) * shakeAmplitude;
        Vector3 jitter = new Vector3(
            Random.Range(-strength, strength),
            Random.Range(-strength * 0.35f, strength * 0.35f),
            Random.Range(-strength, strength));
        portal.transform.localPosition = portalBaseLocalPosition + jitter;
    }

    private void SetVisualIntensity(float t)
    {
        EnsurePlayerLight();
        EnsureGateGlow();

        if (playerGateLight != null)
        {
            playerGateLight.enabled = t > 0.01f;
            playerGateLight.intensity = Mathf.Lerp(0f, maxPlayerLightIntensity, t);
            playerGateLight.range = Mathf.Lerp(1.2f, 6f, t);
        }

        if (gateGlow != null)
        {
            gateGlow.enabled = t > 0.01f;
            gateGlow.intensity = Mathf.Lerp(0f, maxGateLightIntensity, t);
            gateGlow.range = Mathf.Lerp(2f, 9f, t);
        }
    }

    private void TryStartFromTrigger(Collider other)
    {
        if (cutsceneStarted || cutsceneCompleted || other == null || !other.CompareTag("Player")) return;
        if (WorldState.Instance == null || !WorldState.Instance.hasExteriorFragment) return;
        StartGateCutscene();
    }

    private void SetPlayerLocked(bool locked)
    {
        if (player != null) player.SetCanMove(!locked);
        if (playerAttack != null) playerAttack.SetCanAttack(!locked);
    }

    private void ResolveReferences()
    {
        if (transition == null) transition = GetComponent<LocationTransition>();
        if (portal == null && transition != null) portal = transition.Portal;
        if (hunt == null) hunt = FindFirstObjectByType<ExteriorHuntController>();
        if (player == null) player = FindFirstObjectByType<PlayerController3D>();
        if (playerInput == null && player != null) playerInput = player.GetComponent<PlayerInputReader>();
        if (playerAttack == null && player != null) playerAttack = player.GetComponent<PlayerAttackController>();
        EnsurePlayerLight();
        EnsureGateGlow();
    }

    private void EnsurePlayerLight()
    {
        if (playerGateLight != null || player == null) return;

        Transform existing = player.transform.Find("ExteriorGatePlayerLight");
        GameObject lightObject = existing != null ? existing.gameObject : new GameObject("ExteriorGatePlayerLight");
        lightObject.transform.SetParent(player.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.15f, 0.18f);
        playerGateLight = lightObject.GetComponent<Light>() ?? lightObject.AddComponent<Light>();
        playerGateLight.type = LightType.Point;
        playerGateLight.color = new Color(1f, 0.78f, 0.34f, 1f);
        playerGateLight.shadows = LightShadows.None;
    }

    private void EnsureGateGlow()
    {
        if (gateGlow != null || portal == null) return;

        Transform existing = portal.transform.Find("ExteriorGateCutsceneGlow");
        GameObject lightObject = existing != null ? existing.gameObject : new GameObject("ExteriorGateCutsceneGlow");
        lightObject.transform.SetParent(portal.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 2.2f, -0.4f);
        gateGlow = lightObject.GetComponent<Light>() ?? lightObject.AddComponent<Light>();
        gateGlow.type = LightType.Point;
        gateGlow.color = new Color(1f, 0.42f, 0.22f, 1f);
        gateGlow.shadows = LightShadows.None;
    }

    private void CapturePortalBasePosition()
    {
        if (portal == null || hasPortalBasePosition) return;

        portalBaseLocalPosition = portal.transform.localPosition;
        hasPortalBasePosition = true;
    }

    private static Vector3 Planar(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
