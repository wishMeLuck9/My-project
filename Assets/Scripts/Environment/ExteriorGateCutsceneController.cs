using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Light playerGateDirectionLight;
    [SerializeField] private Light gateGlow;
    [Header("Guiding Fragment Light")]
    [SerializeField] private bool guidePlayerToGate = true;
    [SerializeField] private float guideDistance = 120f;
    [SerializeField] private float guideMinIntensity = 0.24f;
    [SerializeField] private float guideFacingBoost = 0.12f;
    [SerializeField] private float guideDistanceBoost = 0.72f;
    [SerializeField] private float guidePulseSpeed = 2.6f;
    [SerializeField] private float guideOrbDistance = 0.75f;
    [SerializeField] private float guideOrbHeight = 1.2f;

    [Header("Gate Reveal / Cutscene")]
    [SerializeField] private float revealDistance = 16f;
    [SerializeField] private float shakeDistance = 5.5f;
    [SerializeField] private float cutsceneDistance = 2.8f;
    [SerializeField] private float maxPlayerLightIntensity = 4.6f;
    [SerializeField] private float maxGateLightIntensity = 3.8f;
    [SerializeField] private float shakeAmplitude = 0.045f;
    [SerializeField] private float shadowRingRadius = 3.8f;
    [SerializeField] private int minimumCutscenePursuers = 6;

    private Vector3 portalBaseLocalPosition;
    private bool hasPortalBasePosition;
    private bool cutsceneStarted;
    private bool cutsceneCompleted;
    private bool attackRequested;
    private Coroutine cutsceneRoutine;
    private readonly List<GameObject> temporaryRingShadows = new List<GameObject>();

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
        ClearTemporaryRingShadows();
    }

    private void Update()
    {
        if (cutsceneCompleted) return;

        if (WorldState.Instance == null || !WorldState.Instance.hasExteriorFragment)
        {
            SetVisualIntensity(0f, 0f);
            if (portal != null && !portal.IsUnlocked) portal.SetPortalVisible(false);
            if (portal != null && hasPortalBasePosition) portal.transform.localPosition = portalBaseLocalPosition;
            return;
        }

        ResolveReferences();
        if (player == null) return;

        float distance = Vector3.Distance(Planar(player.transform.position), Planar(transform.position));
        float gateProximity = Mathf.Clamp01(1f - Mathf.InverseLerp(cutsceneDistance, revealDistance, distance));
        bool shouldRevealGate = distance <= revealDistance || IsCutsceneActive;

        if (portal != null)
        {
            portal.SetPortalVisible(shouldRevealGate);
            if (!portal.IsUnlocked) portal.Lock();
        }

        float playerGuide = CalculateGuidingIntensity(distance);
        float playerIntensity = IsCutsceneActive ? 1f : Mathf.Max(shouldRevealGate ? gateProximity : 0f, playerGuide);
        float gateIntensity = shouldRevealGate ? gateProximity : 0f;

        SetVisualIntensity(playerIntensity, gateIntensity);
        TickGateShake(distance, gateProximity);

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

        ClearTemporaryRingShadows();
        ExteriorPursuer[] pursuers = FindObjectsByType<ExteriorPursuer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int realCount = pursuers != null ? pursuers.Length : 0;
        int ringCount = Mathf.Max(minimumCutscenePursuers, realCount);

        for (int i = 0; i < ringCount; i++)
        {
            Vector3 target = ResolveRingPosition(i, ringCount);
            if (i < realCount && pursuers[i] != null)
            {
                PlacePursuerInRing(pursuers[i], target);
            }
            else
            {
                CreateTemporaryRingShadow(i, target);
            }
        }
    }

    private Vector3 ResolveRingPosition(int index, int count)
    {
        float angle = (360f / Mathf.Max(1, count)) * index;
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * shadowRingRadius;
        Vector3 target = player.transform.position + offset;
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2.8f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        Vector3 rayOrigin = target + Vector3.up * 8f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, 18f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return groundHit.point;
        }

        target.y = player.transform.position.y;
        return target;
    }

    private void PlacePursuerInRing(ExteriorPursuer pursuer, Vector3 target)
    {
        pursuer.gameObject.SetActive(true);
        pursuer.PauseForGateSequence(true);

        NavMeshAgent agent = pursuer.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(target, out NavMeshHit hit, 2.8f, NavMesh.AllAreas))
            {
                target = hit.position;
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.Warp(target);
            }
            else
            {
                pursuer.transform.position = target;
            }
        }
        else
        {
            pursuer.transform.position = target;
        }

        LookAtPlayer(pursuer.transform);
        TintShadow(pursuer);
    }

    private void CreateTemporaryRingShadow(int index, Vector3 target)
    {
        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        shadow.name = $"VIRUS9_GateRingShadow_{index:00}";
        shadow.transform.position = target + Vector3.up * 0.9f;
        shadow.transform.localScale = new Vector3(0.52f, 0.92f, 0.52f);
        Collider collider = shadow.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Renderer renderer = shadow.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.025f, 0.05f, 0.07f, 1f);
        }

        LookAtPlayer(shadow.transform);
        temporaryRingShadows.Add(shadow);
    }

    private void LookAtPlayer(Transform target)
    {
        if (target == null || player == null) return;

        Vector3 look = player.transform.position - target.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.001f) target.rotation = Quaternion.LookRotation(look.normalized);
    }

    private void ClearTemporaryRingShadows()
    {
        for (int i = temporaryRingShadows.Count - 1; i >= 0; i--)
        {
            GameObject shadow = temporaryRingShadows[i];
            if (shadow != null) Destroy(shadow);
        }

        temporaryRingShadows.Clear();
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

        bool reduceScreenShake = SettingsManager.Instance != null && SettingsManager.Instance.ReduceScreenShake;
        if (reduceScreenShake || distance > shakeDistance || cutsceneCompleted)
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

    private float CalculateGuidingIntensity(float distance)
    {
        if (!guidePlayerToGate || player == null) return 0f;
        if (IsCutsceneActive || distance > guideDistance) return 0f;

        Vector3 toGate = Planar(transform.position - player.transform.position);
        if (toGate.sqrMagnitude < 0.001f) return 0f;

        Vector3 playerForward = Planar(player.transform.forward);
        float facingGate = 0.5f;
        if (playerForward.sqrMagnitude > 0.001f)
        {
            facingGate = Mathf.Clamp01((Vector3.Dot(playerForward.normalized, toGate.normalized) + 1f) * 0.5f);
        }

        float closeness = Mathf.Clamp01(1f - Mathf.InverseLerp(cutsceneDistance, guideDistance, distance));
        float pulse = 0.7f + Mathf.Sin(Time.time * guidePulseSpeed) * 0.3f;

        return Mathf.Clamp01(guideMinIntensity + facingGate * guideFacingBoost + closeness * guideDistanceBoost) * pulse;
    }

    private void SetVisualIntensity(float t)
    {
        SetVisualIntensity(t, t);
    }

    private void SetVisualIntensity(float playerT, float gateT)
    {
        playerT = Mathf.Clamp01(playerT);
        gateT = Mathf.Clamp01(gateT);

        EnsurePlayerLight();
        EnsureDirectionLight();
        EnsureGateGlow();
        ConfigurePlayerLightMode();
        PositionPlayerGuideLight();
        PositionDirectionLight(playerT);

        Light playerLight = ResolveUsableLight(playerGateLight);
        if (playerLight != null)
        {
            playerLight.enabled = playerT > 0.01f;
            playerLight.intensity = Mathf.Lerp(0f, maxPlayerLightIntensity, playerT);
            playerLight.range = Mathf.Lerp(2.4f, 7.5f, playerT);
        }

        Light directionLight = ResolveUsableLight(playerGateDirectionLight);
        if (directionLight != null)
        {
            directionLight.enabled = playerT > 0.01f;
            directionLight.intensity = Mathf.Lerp(0f, 1.9f, playerT);
            directionLight.range = Mathf.Lerp(1.1f, 2.6f, playerT);
        }

        Light portalGlow = ResolveUsableLight(gateGlow);
        if (portalGlow != null)
        {
            portalGlow.enabled = gateT > 0.01f;
            portalGlow.intensity = Mathf.Lerp(0f, maxGateLightIntensity, gateT);
            portalGlow.range = Mathf.Lerp(2f, 9f, gateT);
        }
    }

    private void PositionPlayerGuideLight()
    {
        playerGateLight = ResolveUsableLight(playerGateLight);
        if (playerGateLight == null || player == null) return;

        if (playerGateLight.transform.parent != player.transform)
        {
            playerGateLight.transform.SetParent(player.transform, false);
        }

        playerGateLight.transform.localPosition = new Vector3(0f, 1.15f, 0.18f);
        playerGateLight.transform.localRotation = Quaternion.identity;
    }

    private void ConfigurePlayerLightMode()
    {
        playerGateLight = ResolveUsableLight(playerGateLight);
        if (playerGateLight == null) return;

        playerGateLight.type = LightType.Point;
    }

    private void PositionDirectionLight(float intensity)
    {
        playerGateDirectionLight = ResolveUsableLight(playerGateDirectionLight);
        if (playerGateDirectionLight == null || player == null)
        {
            return;
        }

        Vector3 toGate = transform.position - player.transform.position;
        toGate.y = 0f;
        if (toGate.sqrMagnitude <= 0.001f || intensity <= 0.01f)
        {
            playerGateDirectionLight.enabled = false;
            return;
        }

        Vector3 worldPosition = player.transform.position + Vector3.up * guideOrbHeight + toGate.normalized * guideOrbDistance;
        if (playerGateDirectionLight.transform.parent != player.transform)
        {
            playerGateDirectionLight.transform.SetParent(player.transform, false);
        }

        playerGateDirectionLight.transform.position = worldPosition;
        playerGateDirectionLight.transform.rotation = Quaternion.LookRotation(toGate.normalized, Vector3.up);
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
        EnsureDirectionLight();
        EnsureGateGlow();
    }

    private void EnsurePlayerLight()
    {
        playerGateLight = ResolveUsableLight(playerGateLight);
        if (playerGateLight != null || player == null) return;

        playerGateLight = EnsureChildLight(
            player.transform,
            "ExteriorGatePlayerLight",
            new Vector3(0f, 1.15f, 0.18f),
            new Color(1f, 0.78f, 0.34f, 1f));
        playerGateLight.type = LightType.Point;
        playerGateLight.shadows = LightShadows.None;
    }

    private void EnsureDirectionLight()
    {
        playerGateDirectionLight = ResolveUsableLight(playerGateDirectionLight);
        if (playerGateDirectionLight != null || player == null) return;

        playerGateDirectionLight = EnsureChildLight(
            player.transform,
            "ExteriorGateGuideDirectionLight",
            new Vector3(0f, guideOrbHeight, guideOrbDistance),
            new Color(1f, 0.58f, 0.22f, 1f));
        playerGateDirectionLight.type = LightType.Point;
        playerGateDirectionLight.shadows = LightShadows.None;
    }

    private void EnsureGateGlow()
    {
        gateGlow = ResolveUsableLight(gateGlow);
        if (gateGlow != null || portal == null) return;

        gateGlow = EnsureChildLight(
            portal.transform,
            "ExteriorGateCutsceneGlow",
            new Vector3(0f, 2.2f, -0.4f),
            new Color(1f, 0.42f, 0.22f, 1f));
        gateGlow.type = LightType.Point;
        gateGlow.shadows = LightShadows.None;
    }

    private static Light EnsureChildLight(Transform parent, string childName, Vector3 localPosition, Color color)
    {
        Transform existing = parent.Find(childName);
        GameObject lightObject = existing != null ? existing.gameObject : new GameObject(childName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;
        Light light = ResolveUsableLight(lightObject.GetComponent<Light>());
        if (light == null)
        {
            light = lightObject.AddComponent<Light>();
        }

        light.color = color;
        light.enabled = false;
        return light;
    }

    private static Light ResolveUsableLight(Light light)
    {
        if (light == null) return null;

        try
        {
            _ = light.enabled;
            _ = light.transform;
            return light;
        }
        catch (MissingComponentException)
        {
            return null;
        }
        catch (MissingReferenceException)
        {
            return null;
        }
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
