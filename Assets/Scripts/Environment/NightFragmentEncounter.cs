using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class NightFragmentEncounter : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("pleading")] private PrototypeShadowActor helper;
    [SerializeField] private PrototypeShadowActor afraid;
    [SerializeField] private PrototypeShadowActor[] allShadows;
    [SerializeField] private LightFragmentPickup innerNightFragment;
    [SerializeField] private Transform mercyDropPoint;
    [SerializeField] private Transform violenceDropPoint;
    [SerializeField] private SquarePortalController exitPortal;
    [SerializeField] private float runSpeed = 3.2f;
    [SerializeField] private float helperTravelTimeout = 3f;
    [Header("Combat Tutorial Pacing")]
    [SerializeField] private bool waitForTrainingBeforeAggro = true;
    [SerializeField] private CorruptionTrainingTarget[] trainingTargets;
    [SerializeField] private float initialAggroFallbackDelay = 24f;
    [SerializeField] private float releaseHintSeconds = 2f;
    [Header("Mercy Witness Staging")]
    [SerializeField] private float mercyHelperStopDistance = 1.2f;
    [SerializeField] private float mercyHelperApproachDistance = 2.1f;
    [SerializeField] private float mercyWitnessLockSeconds = 1.25f;
    [SerializeField] private float mercyPreRevealDelay = 1.25f;

    private bool mercyStarted;
    private bool routeCompleted;
    private bool violenceChainStarted;
    private bool initialAggroStarted;
    private bool releaseSequenceStarted;
    private int lastDefeatedCount;
    private Vector3 afraidOriginalScale;
    private Quaternion afraidOriginalRotation;
    private NavMeshAgent helperAgent;

    private void Awake()
    {
        ResolveReferences();
        if (helper != null) helperAgent = helper.GetComponent<NavMeshAgent>();
        if (afraid != null)
        {
            afraidOriginalScale = afraid.transform.localScale;
            afraidOriginalRotation = afraid.transform.rotation;
        }
    }

    private void OnEnable()
    {
        CorruptionTrainingTarget.TrainingTargetCompleted += HandleTrainingTargetCompleted;
    }

    private void OnDisable()
    {
        CorruptionTrainingTarget.TrainingTargetCompleted -= HandleTrainingTargetCompleted;
    }

    private void Start()
    {
        ResolveReferences();
        ResolveTrainingTargets();

        WorldState state = WorldState.Instance;
        if (state != null && state.hasInnerNightFragment)
        {
            routeCompleted = true;
            if (innerNightFragment != null) innerNightFragment.gameObject.SetActive(false);
        }
        else if (state != null &&
                 state.nightFragmentRoute != WorldState.NightFragmentRoute.None)
        {
            routeCompleted = true;
            RevealNightFragment(state.nightFragmentRoute);
        }
        else if (innerNightFragment != null)
        {
            innerNightFragment.gameObject.SetActive(false);
        }

        if (helper != null) helper.gameObject.SetActive(false);
        if (!routeCompleted) StartCoroutine(StartInitialEnemyAggroWhenReady());
    }

    private void Update()
    {
        if (routeCompleted || WorldState.Instance == null) return;

        if (!WorldState.Instance.nightViolenceAttempted) return;

        if (mercyStarted) RestoreMercyActors();
        if (!violenceChainStarted) StartViolenceRoute();

        int defeatedCount = CountDefeatedShadows();
        if (defeatedCount != lastDefeatedCount)
        {
            lastDefeatedCount = defeatedCount;
            WorldState.Instance.SetNightGuardianChainDefeatedCount(defeatedCount);
            PromoteNextLivingShadow();
        }

        if (AreAllShadowsDefeated())
        {
            CompleteRoute(WorldState.NightFragmentRoute.Violence, "SYSTEM", "raw.night.drop");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (routeCompleted || mercyStarted || !other.CompareTag("Player") || WorldState.Instance == null) return;
        if (WorldState.Instance.nightViolenceAttempted) return;

        mercyStarted = true;
        StartCoroutine(PlayMercyWitnessEvent());
    }

    private IEnumerator PlayMercyWitnessEvent()
    {
        if (helper == null || afraid == null)
        {
            mercyStarted = false;
            yield break;
        }

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        SetPlayerControl(false);
        SetPeacefulFleeing(true);
        RuntimeHudController.EnsureInstance().ShowSystemMessage(
            "\u0421\u0418\u0421\u0422\u0415\u041C\u0410:\n\u0421\u0432\u0438\u0434\u0435\u0442\u0435\u043B\u044C \u043E\u0431\u043D\u0430\u0440\u0443\u0436\u0435\u043D.\n\u041D\u0435 \u0432\u043C\u0435\u0448\u0438\u0432\u0430\u0439\u0442\u0435\u0441\u044C.",
            2.6f);
        DialogueController.Instance?.ShowDialogue("SHADOW", localizer.Get("raw.night.observe"));

        Vector3 target = ResolveMercyHelperTarget();
        Vector3 start = ResolveMercyHelperStart(target);
        helper.transform.position = start;
        FacePosition(helper.transform, afraid.transform.position);
        helper.gameObject.SetActive(true);
        helper.RestoreForEvent();
        helper.transform.position = start;
        FacePosition(helper.transform, afraid.transform.position);
        helper.SetHunting(false);
        helper.SetFleeingFromPlayer(false);

        yield return new WaitForSeconds(Mathf.Max(0.1f, mercyWitnessLockSeconds));
        if (WorldState.Instance == null || WorldState.Instance.nightViolenceAttempted)
        {
            RestoreMercyActors();
            SetPlayerControl(true);
            yield break;
        }

        afraid.transform.rotation = Quaternion.Euler(0f, afraid.transform.eulerAngles.y, 72f);
        afraid.transform.localScale = new Vector3(afraidOriginalScale.x, afraidOriginalScale.y * 0.65f, afraidOriginalScale.z);

        bool hasPath = PrepareHelperPath(target);
        float startedAt = Time.time;
        while ((helper.transform.position - target).sqrMagnitude > 0.09f)
        {
            if (WorldState.Instance == null || WorldState.Instance.nightViolenceAttempted)
            {
                RestoreMercyActors();
                SetPlayerControl(true);
                yield break;
            }

            if (!hasPath || Time.time - startedAt >= helperTravelTimeout)
            {
                helper.transform.position = target;
                break;
            }

            yield return null;
        }

        FacePosition(helper.transform, afraid.transform.position);
        yield return new WaitForSeconds(Mathf.Max(0.15f, mercyPreRevealDelay));
        if (WorldState.Instance == null || WorldState.Instance.nightViolenceAttempted)
        {
            RestoreMercyActors();
            SetPlayerControl(true);
            yield break;
        }

        afraid.transform.rotation = afraidOriginalRotation;
        afraid.transform.localScale = afraidOriginalScale;
        CompleteRoute(WorldState.NightFragmentRoute.Mercy, GetHelperSpeakerName(), "raw.night.mercy");
        SetPlayerControl(true);
    }

    private void StartInitialEnemyAggro()
    {
        if (initialAggroStarted || routeCompleted) return;
        initialAggroStarted = true;

        ResolveReferences();
        foreach (PrototypeShadowActor shadow in allShadows)
        {
            if (shadow == null || shadow.IsDefeated) continue;
            if (shadow.Role == PrototypeShadowActor.ShadowRole.Enemy)
            {
                shadow.SetHunting(true);
            }
        }
    }

    private IEnumerator StartInitialEnemyAggroWhenReady()
    {
        while (!NightPhaseController.IntroComplete)
        {
            yield return null;
        }

        float startedAt = Time.time;
        if (waitForTrainingBeforeAggro)
        {
            RuntimeHudController.Instance?.ShowSystemMessage(
                LocalizationManager.EnsureInstance().Get("raw.night.training.prompt"),
                6f);

            while (!AreTrainingTargetsComplete() && Time.time - startedAt < initialAggroFallbackDelay)
            {
                if (routeCompleted) yield break;
                if (WorldState.Instance != null && WorldState.Instance.nightViolenceAttempted) break;
                yield return null;
            }
        }

        if (routeCompleted) yield break;
        yield return ReleaseAggroAfterHint();
    }

    private void HandleTrainingTargetCompleted(CorruptionTrainingTarget target)
    {
        if (!waitForTrainingBeforeAggro || initialAggroStarted || routeCompleted) return;
        if (AreTrainingTargetsComplete())
        {
            StartCoroutine(ReleaseAggroAfterHint());
        }
    }

    private IEnumerator ReleaseAggroAfterHint()
    {
        if (routeCompleted) yield break;
        if (releaseSequenceStarted) yield break;
        releaseSequenceStarted = true;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        float hintSeconds = Mathf.Max(0.1f, releaseHintSeconds);
        RuntimeHudController.Instance?.ShowSystemMessage(localizer.Get("raw.night.training.release"), hintSeconds);
        yield return new WaitForSecondsRealtime(hintSeconds);

        if (routeCompleted) yield break;
        StartInitialEnemyAggro();
    }

    private void StartViolenceRoute()
    {
        violenceChainStarted = true;
        WorldState.Instance?.BeginNightGuardianChain();
        ResolveReferences();

        if (helper != null && !helper.gameObject.activeSelf)
        {
            helper.gameObject.SetActive(true);
            helper.RestoreForEvent();
        }

        foreach (PrototypeShadowActor shadow in allShadows)
        {
            if (shadow == null || shadow.IsDefeated) continue;
            shadow.SetHunting(true);
        }

        PromoteNextLivingShadow();
    }

    private void PromoteNextLivingShadow()
    {
        ResolveReferences();
        PrototypeShadowActor next = allShadows
            .FirstOrDefault(shadow => shadow != null &&
                                      !shadow.IsDefeated &&
                                      shadow.Role != PrototypeShadowActor.ShadowRole.GuardianProxy);
        next?.PromoteToGuardianProxy();
    }

    private void SetPeacefulFleeing(bool state)
    {
        ResolveReferences();
        foreach (PrototypeShadowActor shadow in allShadows)
        {
            if (shadow == null || shadow.IsDefeated) continue;
            if (shadow == helper || shadow == afraid) continue;
            shadow.SetFleeingFromPlayer(state);
        }
    }

    private void CompleteRoute(WorldState.NightFragmentRoute route, string speaker, string messageKey)
    {
        routeCompleted = true;
        WorldState state = WorldState.Instance;
        state?.GrantNightFragmentRoute(route);
        exitPortal?.Lock();
        RevealNightFragment(route);

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        DialogueController.Instance?.ShowDialogue(
            localizer.TranslateRaw(speaker),
            localizer.Get(messageKey));
    }

    private void RevealNightFragment(WorldState.NightFragmentRoute route)
    {
        if (innerNightFragment == null) return;

        WorldState state = WorldState.Instance;
        if (state != null && state.hasInnerNightFragment)
        {
            innerNightFragment.gameObject.SetActive(false);
            return;
        }

        innerNightFragment.Configure(LightFragmentPickup.FragmentKind.InnerNight);
        Transform dropPoint = route == WorldState.NightFragmentRoute.Mercy
            ? mercyDropPoint
            : route == WorldState.NightFragmentRoute.Violence
                ? violenceDropPoint
                : null;

        if (dropPoint != null)
        {
            innerNightFragment.transform.SetPositionAndRotation(dropPoint.position, dropPoint.rotation);
        }

        innerNightFragment.gameObject.SetActive(true);
    }

    private bool AreAllShadowsDefeated()
    {
        ResolveReferences();
        return allShadows != null &&
               allShadows.Length > 0 &&
               allShadows.All(shadow => shadow != null && shadow.IsDefeated);
    }

    private int CountDefeatedShadows()
    {
        ResolveReferences();
        return allShadows?.Count(shadow => shadow != null && shadow.IsDefeated) ?? 0;
    }

    private void RestoreMercyActors()
    {
        if (afraid != null)
        {
            afraid.transform.rotation = afraidOriginalRotation;
            afraid.transform.localScale = afraidOriginalScale;
        }

        SetPeacefulFleeing(false);
    }

    private Vector3 ResolveMercyHelperTarget()
    {
        Vector3 awayFromPlayer = Vector3.back;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && afraid != null)
        {
            awayFromPlayer = afraid.transform.position - player.transform.position;
            awayFromPlayer.y = 0f;
        }

        if (awayFromPlayer.sqrMagnitude <= 0.001f) awayFromPlayer = Vector3.back;

        Vector3 target = afraid.transform.position + awayFromPlayer.normalized * Mathf.Max(0.5f, mercyHelperStopDistance);
        return SampleNavMeshPosition(target, 2f, out Vector3 sampled) ? sampled : target;
    }

    private Vector3 ResolveMercyHelperStart(Vector3 target)
    {
        Vector3 awayFromAfraid = target - afraid.transform.position;
        awayFromAfraid.y = 0f;
        if (awayFromAfraid.sqrMagnitude <= 0.001f) awayFromAfraid = Vector3.back;

        Vector3 start = target + awayFromAfraid.normalized * Mathf.Max(0.5f, mercyHelperApproachDistance);
        return SampleNavMeshPosition(start, 3f, out Vector3 sampled) ? sampled : start;
    }

    private static bool SampleNavMeshPosition(Vector3 position, float maxDistance, out Vector3 sampled)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            sampled = hit.position;
            return true;
        }

        sampled = position;
        return false;
    }

    private static void FacePosition(Transform actor, Vector3 target)
    {
        if (actor == null) return;

        Vector3 direction = target - actor.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return;

        actor.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static void SetPlayerControl(bool state)
    {
        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null) player.SetCanMove(state);

        PlayerAttackController attack = FindFirstObjectByType<PlayerAttackController>();
        if (attack != null) attack.SetCanAttack(state);
    }

    private bool PrepareHelperPath(Vector3 target)
    {
        if (helperAgent == null || !helperAgent.enabled) return false;
        if (!helperAgent.isOnNavMesh &&
            NavMesh.SamplePosition(helper.transform.position, out NavMeshHit start, 2f, NavMesh.AllAreas))
        {
            helperAgent.Warp(start.position);
        }

        if (!helperAgent.isOnNavMesh ||
            !NavMesh.SamplePosition(target, out NavMeshHit destination, 2f, NavMesh.AllAreas))
        {
            return false;
        }

        helperAgent.speed = runSpeed;
        helperAgent.isStopped = false;
        return helperAgent.SetDestination(destination.position);
    }

    private void ResolveReferences()
    {
        if (innerNightFragment == null) innerNightFragment = FindFirstObjectByType<LightFragmentPickup>(FindObjectsInactive.Include);
        if (helper == null || helper.name.Contains("Ally"))
        {
            PrototypeShadowActor previousHelper = helper;
            PrototypeShadowActor preferredHelper = FindShadowActorByName("SHADOW_Pleading_01");
            if (preferredHelper != null && preferredHelper != helper)
            {
                if (previousHelper != null) previousHelper.gameObject.SetActive(false);
                helper = preferredHelper;
            }
        }

        if (helper != null && (helperAgent == null || helperAgent.gameObject != helper.gameObject))
        {
            helperAgent = helper.GetComponent<NavMeshAgent>();
        }

        PrototypeShadowActor[] discovered = FindObjectsByType<PrototypeShadowActor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(shadow => shadow != null && shadow.Role == PrototypeShadowActor.ShadowRole.Enemy)
            .ToArray();

        allShadows = (allShadows ?? new PrototypeShadowActor[0])
            .Where(shadow => shadow != null)
            .Concat(new[] { helper, afraid }.Where(shadow => shadow != null))
            .Concat(discovered)
            .Distinct()
            .ToArray();
    }

    private static PrototypeShadowActor FindShadowActorByName(string objectName)
    {
        return FindObjectsByType<PrototypeShadowActor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(shadow => shadow != null && shadow.name == objectName);
    }

    private void ResolveTrainingTargets()
    {
        if (trainingTargets != null && trainingTargets.Any(target => target != null)) return;
        trainingTargets = FindObjectsByType<CorruptionTrainingTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private bool AreTrainingTargetsComplete()
    {
        ResolveTrainingTargets();
        if (trainingTargets == null || trainingTargets.Length == 0) return true;

        for (int i = 0; i < trainingTargets.Length; i++)
        {
            CorruptionTrainingTarget target = trainingTargets[i];
            if (target != null && target.gameObject.activeInHierarchy && !target.IsDefeated) return false;
        }

        return true;
    }

    private string GetHelperSpeakerName()
    {
        return helper != null && helper.name.Contains("Ally") ? "SHADOW_ALLY" : "SHADOW_PLEADING";
    }
}
