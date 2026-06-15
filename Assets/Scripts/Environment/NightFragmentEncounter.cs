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

    private bool mercyStarted;
    private bool routeCompleted;
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

    private void Start()
    {
        if (innerNightFragment != null)
        {
            innerNightFragment.gameObject.SetActive(false);
        }

        if (helper != null) helper.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (routeCompleted || WorldState.Instance == null) return;

        if (WorldState.Instance.nightViolenceAttempted)
        {
            if (mercyStarted) RestoreMercyActors();

            if (AreAllShadowsDefeated())
            {
                CompleteRoute(WorldState.NightFragmentRoute.Violence);
                DialogueController.Instance?.ShowDialogue("SYSTEM", "Площадь опустела. Фрагмент выпал из последней тени.");
            }
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

        DialogueController.Instance?.ShowDialogue("SHADOW", "Не подходи. Просто смотри.");

        helper.gameObject.SetActive(true);
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
        CompleteRoute(WorldState.NightFragmentRoute.Mercy);
        DialogueController.Instance?.ShowDialogue(GetHelperSpeakerName(), "Она встала. Забери то, что осталось на земле, и не делай из этого охоту.");
    }

    private void CompleteRoute(WorldState.NightFragmentRoute route)
    {
        routeCompleted = true;
        WorldState.Instance.GrantNightFragmentRoute(route);
        exitPortal?.Unlock();

        if (innerNightFragment != null)
        {
            if (mercyDropPoint != null) innerNightFragment.transform.position = mercyDropPoint.position;
            innerNightFragment.gameObject.SetActive(true);
        }
    }

    private bool AreAllShadowsDefeated()
    {
        ResolveReferences();
        if (allShadows == null || allShadows.Length == 0) return false;

        foreach (PrototypeShadowActor shadow in allShadows)
        {
            if (shadow == null || !shadow.IsDefeated) return false;
        }

        return true;
    }

    private void RestoreMercyActors()
    {
        if (afraid != null)
        {
            afraid.transform.rotation = afraidOriginalRotation;
            afraid.transform.localScale = afraidOriginalScale;
        }
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
        if (allShadows == null || allShadows.Length == 0)
        {
            allShadows = FindObjectsByType<PrototypeShadowActor>(FindObjectsSortMode.None)
                .Where(shadow => shadow != null && shadow.Role == PrototypeShadowActor.ShadowRole.Enemy)
                .ToArray();
        }
    }

    private string GetHelperSpeakerName()
    {
        return helper != null && helper.name.Contains("Ally") ? "SHADOW_ALLY" : "SHADOW_PLEADING";
    }
}
