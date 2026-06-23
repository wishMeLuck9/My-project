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
    [SerializeField] private SquarePortalController exitPortal;
    [SerializeField] private float runSpeed = 3.2f;
    [SerializeField] private float helperTravelTimeout = 3f;
    [Header("Combat Tutorial Pacing")]
    [SerializeField] private bool waitForTrainingBeforeAggro = true;
    [SerializeField] private CorruptionTrainingTarget[] trainingTargets;
    [SerializeField] private float initialAggroFallbackDelay = 24f;

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
        if (state != null &&
            state.nightFragmentRoute != WorldState.NightFragmentRoute.None &&
            !state.hasInnerNightFragment)
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
        SetPeacefulFleeing(true);
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
        DialogueController.Instance?.ShowDialogue("SHADOW", localizer.Get("raw.night.observe"));

        helper.gameObject.SetActive(true);
        helper.RestoreForEvent();
        helper.SetFleeingFromPlayer(false);
        afraid.transform.rotation = Quaternion.Euler(0f, afraid.transform.eulerAngles.y, 72f);
        afraid.transform.localScale = new Vector3(afraidOriginalScale.x, afraidOriginalScale.y * 0.65f, afraidOriginalScale.z);

        Vector3 target = afraid.transform.position + Vector3.back * 1.2f;
        bool hasPath = PrepareHelperPath(target);
        float startedAt = Time.time;
        while ((helper.transform.position - target).sqrMagnitude > 0.09f)
        {
            if (WorldState.Instance == null || WorldState.Instance.nightViolenceAttempted)
            {
                RestoreMercyActors();
                yield break;
            }

            if (!hasPath || Time.time - startedAt >= helperTravelTimeout)
            {
                helper.transform.position = target;
                break;
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.75f);
        if (WorldState.Instance == null || WorldState.Instance.nightViolenceAttempted)
        {
            RestoreMercyActors();
            yield break;
        }

        afraid.transform.rotation = afraidOriginalRotation;
        afraid.transform.localScale = afraidOriginalScale;
        CompleteRoute(WorldState.NightFragmentRoute.Mercy, GetHelperSpeakerName(), "raw.night.mercy");
    }

    private void StartInitialEnemyAggro()
    {
        if (initialAggroStarted) return;
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
                if (WorldState.Instance != null && WorldState.Instance.nightViolenceAttempted) break;
                yield return null;
            }
        }

        yield return ReleaseAggroAfterDialogue();
    }

    private void HandleTrainingTargetCompleted(CorruptionTrainingTarget target)
    {
        if (!waitForTrainingBeforeAggro || initialAggroStarted) return;
        if (AreTrainingTargetsComplete())
        {
            StartCoroutine(ReleaseAggroAfterDialogue());
        }
    }

    private IEnumerator ReleaseAggroAfterDialogue()
    {
        if (releaseSequenceStarted) yield break;
        releaseSequenceStarted = true;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        DialogueController dialogue = DialogueController.Instance;
        if (dialogue != null)
        {
            bool completed = false;
            dialogue.ShowDialogue(
                localizer.Get("speaker.system", "SYSTEM"),
                localizer.Get("raw.night.training.release"),
                () => completed = true);

            while (!completed && dialogue.IsDialogueOpen)
            {
                yield return null;
            }
        }
        else
        {
            RuntimeHudController.Instance?.ShowSystemMessage(localizer.Get("raw.night.training.release"), 5f);
            yield return new WaitForSecondsRealtime(1.2f);
        }

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

        innerNightFragment.Configure(LightFragmentPickup.FragmentKind.InnerNight);
        if (mercyDropPoint != null)
        {
            innerNightFragment.transform.SetPositionAndRotation(mercyDropPoint.position, mercyDropPoint.rotation);
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
