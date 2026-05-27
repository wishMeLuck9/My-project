using System.Collections;
using UnityEngine;

public class NightFragmentEncounter : MonoBehaviour
{
    [SerializeField] private PrototypeShadowActor pleading;
    [SerializeField] private PrototypeShadowActor afraid;
    [SerializeField] private PrototypeShadowActor[] allShadows;
    [SerializeField] private LightFragmentPickup innerNightFragment;
    [SerializeField] private Transform mercyDropPoint;
    [SerializeField] private float runSpeed = 3.2f;

    private bool mercyStarted;
    private bool routeCompleted;
    private Vector3 afraidOriginalScale;
    private Quaternion afraidOriginalRotation;

    private void Awake()
    {
        ResolveReferences();
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
        if (pleading == null || afraid == null)
        {
            mercyStarted = false;
            yield break;
        }

        DialogueController.Instance?.ShowDialogue("SHADOW", "Не подходи. Просто смотри.");

        afraid.transform.rotation = Quaternion.Euler(0f, afraid.transform.eulerAngles.y, 72f);
        afraid.transform.localScale = new Vector3(afraidOriginalScale.x, afraidOriginalScale.y * 0.65f, afraidOriginalScale.z);

        Vector3 target = afraid.transform.position + Vector3.back * 1.2f;
        while ((pleading.transform.position - target).sqrMagnitude > 0.04f)
        {
            if (WorldState.Instance == null || WorldState.Instance.nightViolenceAttempted)
            {
                RestoreMercyActors();
                yield break;
            }

            pleading.transform.position = Vector3.MoveTowards(pleading.transform.position, target, runSpeed * Time.deltaTime);
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
        DialogueController.Instance?.ShowDialogue("SHADOW_PLEADING", "Она встала. Забери то, что осталось на земле, и не делай из этого охоту.");
    }

    private void CompleteRoute(WorldState.NightFragmentRoute route)
    {
        routeCompleted = true;
        WorldState.Instance.GrantNightFragmentRoute(route);
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

    private void ResolveReferences()
    {
        if (pleading == null) pleading = FindActor("SHADOW_Pleading_01");
        if (afraid == null) afraid = FindActor("SHADOW_Afraid_01");
        if (innerNightFragment == null) innerNightFragment = FindFirstObjectByType<LightFragmentPickup>(FindObjectsInactive.Include);
        if (allShadows == null || allShadows.Length == 0) allShadows = FindObjectsByType<PrototypeShadowActor>(FindObjectsSortMode.None);
    }

    private static PrototypeShadowActor FindActor(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        return obj != null ? obj.GetComponent<PrototypeShadowActor>() : null;
    }
}
