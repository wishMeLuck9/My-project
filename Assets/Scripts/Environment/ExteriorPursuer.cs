using UnityEngine;
using UnityEngine.AI;

public class ExteriorPursuer : MonoBehaviour
{
    [SerializeField] private float chaseSpeed = 4.3f;
    [SerializeField] private float catchDistance = 1.15f;
    [SerializeField] private float maxCatchHeightDifference = 0.75f;
    [SerializeField] private LayerMask catchLineOfSightMask = ~0;
    [SerializeField] private float stareAtPlayerDistance = 5.5f;
    [SerializeField] private float reactionDistance = 9f;
    [SerializeField] private float reactionCooldownMin = 2.8f;
    [SerializeField] private float reactionCooldownMax = 5.2f;
    [Header("Group Tactics")]
    [SerializeField] private float flankRadius = 1.35f;
    [SerializeField] private float destinationRefreshInterval = 0.22f;
    [SerializeField] private float personalSpaceRadius = 0.95f;
    [SerializeField] private float personalSpaceWeight = 0.65f;
    [SerializeField] private float stuckVelocityThreshold = 0.14f;
    [SerializeField] private float stuckRepathAfter = 0.75f;
    [SerializeField] private float stuckSideStepDistance = 1.45f;
    [Header("Large Pursuer Shove")]
    [SerializeField] private bool largePursuerCanShove = true;
    [SerializeField] private float largePursuerScaleThreshold = 1.15f;
    [SerializeField] private float largePursuerVisualHeightThreshold = 2.35f;
    [SerializeField] private float shoveDistance = 2.15f;
    [SerializeField] private float shoveHeightAdvantage = 0.72f;
    [SerializeField] private float shoveImpulse = 4.1f;
    [SerializeField] private float shoveUpwardImpulse = 0.35f;
    [SerializeField] private float shoveControlLockDuration = 0.22f;
    [SerializeField] private float shoveCooldown = 1.6f;

    private Transform player;
    private PlayerController3D playerController;
    private NavMeshAgent agent;
    private ShadowNPC dialogue;
    private EnemyJumpController jumper;
    private ExteriorHuntController hunt;
    private bool isHunting;
    private bool pausedForGateSequence;
    private float nextRecoveryTime;
    private float nextReactionTime;
    private float nextDestinationUpdateTime;
    private float stuckStartedAt = -1f;
    private float nextShoveTime;
    private float formationAngle;
    private Vector3 currentDestination;
    private bool hasCurrentDestination;
    private bool largePursuerResolved;
    private bool isLargePursuer;

    private static float nextSharedReactionTime;
    private static float nextSharedShoveMessageTime;
    private static readonly string[] HuntReactionKeys =
    {
        "raw.hunt.reaction.seen",
        "raw.hunt.reaction.close"
    };

    public bool IsHunting => isHunting;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        dialogue = GetComponent<ShadowNPC>();
        jumper = GetComponent<EnemyJumpController>();
        formationAngle = Mathf.Repeat(Mathf.Abs(GetInstanceID()) * 137.508f, 360f);
    }

    public void SetHunting(bool state, ExteriorHuntController controller)
    {
        hunt = controller;
        isHunting = state && !pausedForGateSequence;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
        playerController = player != null ? player.GetComponent<PlayerController3D>() : null;

        if (dialogue != null) dialogue.enabled = !state && !pausedForGateSequence;
        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.isStopped = !isHunting;
            agent.avoidancePriority = 25 + Mathf.Abs(GetInstanceID()) % 45;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        if (isHunting)
        {
            nextReactionTime = Time.time + Random.Range(1.1f, 2.4f);
            nextDestinationUpdateTime = 0f;
            stuckStartedAt = -1f;
            hasCurrentDestination = false;
        }
    }

    public void PauseForGateSequence(bool paused)
    {
        pausedForGateSequence = paused;
        if (paused)
        {
            isHunting = false;
            if (dialogue != null) dialogue.enabled = false;
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }
        else if (dialogue != null)
        {
            dialogue.enabled = !isHunting;
        }
    }

    private void Update()
    {
        if (!isHunting || player == null || hunt == null) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        Vector3 target = player.position;
        if (EnsureAgentOnNavMesh())
        {
            UpdateHuntDestination(target);
        }

        jumper?.TickAutoJump(player, true, true);
        TickHuntPresence(target);
        TryShovePlayerFromHighGround(target);

        if (IsPlayerCatchable(target))
        {
            hunt.CapturePlayer();
        }
    }

    public void ShowHuntingInteractionResponse()
    {
        if (!isHunting) return;

        RuntimeHudController.Instance?.ShowSystemMessage(
            LocalizationManager.EnsureInstance().Get(Random.value > 0.5f
                ? "raw.hunt.interact.close"
                : "raw.hunt.interact.run"),
            1.8f);
        nextReactionTime = Time.time + reactionCooldownMax;
    }

    private void TickHuntPresence(Vector3 target)
    {
        Vector3 planar = target - transform.position;
        float heightDifference = planar.y;
        planar.y = 0f;
        if (planar.sqrMagnitude <= 0.01f) return;

        bool playerIsAbove = heightDifference > maxCatchHeightDifference;
        if (playerIsAbove || planar.sqrMagnitude <= stareAtPlayerDistance * stareAtPlayerDistance)
        {
            Quaternion targetRotation = Quaternion.LookRotation(planar.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
        }

        if (Time.time < nextReactionTime || Time.time < nextSharedReactionTime) return;
        if (planar.sqrMagnitude > reactionDistance * reactionDistance) return;
        if (!HasClearPathToPlayer(target)) return;

        string key = playerIsAbove
            ? "raw.hunt.reaction.above"
            : HuntReactionKeys[Random.Range(0, HuntReactionKeys.Length)];
        RuntimeHudController.Instance?.ShowSystemMessage(LocalizationManager.EnsureInstance().Get(key), 1.8f);

        float cooldown = Random.Range(reactionCooldownMin, reactionCooldownMax);
        nextReactionTime = Time.time + cooldown;
        nextSharedReactionTime = Time.time + cooldown * 0.75f;
    }

    private void UpdateHuntDestination(Vector3 target)
    {
        if (agent == null) return;
        if (Time.time < nextDestinationUpdateTime && hasCurrentDestination)
        {
            agent.SetDestination(currentDestination);
            return;
        }

        nextDestinationUpdateTime = Time.time + destinationRefreshInterval;
        currentDestination = ResolveHuntDestination(target);
        hasCurrentDestination = true;
        agent.SetDestination(currentDestination);
    }

    private Vector3 ResolveHuntDestination(Vector3 target)
    {
        Vector3 toPlayer = target - transform.position;
        toPlayer.y = 0f;
        Vector3 desired = target;

        if (flankRadius > 0.01f && toPlayer.sqrMagnitude > catchDistance * catchDistance)
        {
            float angle = formationAngle + Time.time * 10f;
            desired += Quaternion.Euler(0f, angle, 0f) * Vector3.forward * flankRadius;
        }

        desired += CalculateLocalSeparation();

        if (IsAgentStuckTryingToMove())
        {
            Vector3 forward = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            float sideSign = GetInstanceID() % 2 == 0 ? 1f : -1f;
            desired = transform.position + forward * 0.7f + side * stuckSideStepDistance * sideSign;
        }

        return NavMesh.SamplePosition(desired, out NavMeshHit hit, 2.25f, NavMesh.AllAreas)
            ? hit.position
            : target;
    }

    private Vector3 CalculateLocalSeparation()
    {
        if (personalSpaceRadius <= 0.01f) return Vector3.zero;

        Vector3 separation = Vector3.zero;
        ExteriorPursuer[] pursuers = FindObjectsByType<ExteriorPursuer>(FindObjectsSortMode.None);
        for (int i = 0; i < pursuers.Length; i++)
        {
            ExteriorPursuer other = pursuers[i];
            if (other == null || other == this || !other.IsHunting) continue;

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance <= 0.01f || distance >= personalSpaceRadius) continue;

            separation += away.normalized * ((personalSpaceRadius - distance) / personalSpaceRadius);
        }

        return separation * personalSpaceWeight;
    }

    private bool IsAgentStuckTryingToMove()
    {
        if (agent == null || !agent.hasPath || agent.pathPending)
        {
            stuckStartedAt = -1f;
            return false;
        }

        if (agent.remainingDistance <= agent.stoppingDistance + 0.35f)
        {
            stuckStartedAt = -1f;
            return false;
        }

        bool wantsToMove = agent.desiredVelocity.sqrMagnitude > 0.2f * 0.2f;
        bool barelyMoving = agent.velocity.sqrMagnitude < stuckVelocityThreshold * stuckVelocityThreshold;
        if (!wantsToMove || !barelyMoving)
        {
            stuckStartedAt = -1f;
            return false;
        }

        if (stuckStartedAt < 0f) stuckStartedAt = Time.time;
        return Time.time - stuckStartedAt >= stuckRepathAfter;
    }

    private void TryShovePlayerFromHighGround(Vector3 target)
    {
        if (!largePursuerCanShove || Time.time < nextShoveTime || playerController == null) return;
        if (!IsLargePursuer()) return;

        Vector3 separation = target - transform.position;
        if (separation.y < shoveHeightAdvantage) return;

        Vector3 planar = separation;
        planar.y = 0f;
        if (planar.sqrMagnitude > shoveDistance * shoveDistance) return;
        if (!HasClearPathToPlayer(target)) return;

        Vector3 direction = planar.sqrMagnitude > 0.01f ? planar.normalized : player.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.01f) direction = transform.forward;
        direction.Normalize();

        playerController.ApplyExternalImpulse(
            direction * shoveImpulse + Vector3.up * shoveUpwardImpulse,
            shoveControlLockDuration);
        nextShoveTime = Time.time + shoveCooldown;

        if (Time.time >= nextSharedShoveMessageTime)
        {
            RuntimeHudController.Instance?.ShowSystemMessage(
                LocalizationManager.EnsureInstance().Get("raw.hunt.reaction.shove"),
                1.6f);
            nextSharedShoveMessageTime = Time.time + 2.8f;
        }
    }

    private bool IsLargePursuer()
    {
        if (largePursuerResolved) return isLargePursuer;

        largePursuerResolved = true;
        isLargePursuer = transform.lossyScale.y >= largePursuerScaleThreshold || ResolveVisualHeight() >= largePursuerVisualHeightThreshold;
        return isLargePursuer;
    }

    private float ResolveVisualHeight()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return 0f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null) bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.size.y;
    }

    private bool IsPlayerCatchable(Vector3 target)
    {
        Vector3 separation = target - transform.position;
        if (Mathf.Abs(separation.y) > maxCatchHeightDifference) return false;

        separation.y = 0f;
        if (separation.sqrMagnitude > catchDistance * catchDistance) return false;

        return HasClearPathToPlayer(target);
    }

    private bool HasClearPathToPlayer(Vector3 target)
    {
        int mask = catchLineOfSightMask.value == 0 ? Physics.DefaultRaycastLayers : catchLineOfSightMask.value;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 destination = target + Vector3.up * 0.8f;
        return PhysicsLineOfSight.HasClearPath(transform, player, origin, destination, mask);
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;
        if (Time.time < nextRecoveryTime) return false;

        nextRecoveryTime = Time.time + 0.5f;
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) return false;

        agent.Warp(hit.position);
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        return agent.isOnNavMesh;
    }
}
