using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class PrototypeShadowActor : MonoBehaviour
{
    public enum ShadowRole
    {
        Neutral,
        Enemy,
        Ally,
        Afraid,
        GuardianProxy
    }

    [SerializeField] private ShadowRole role = ShadowRole.Neutral;
    [SerializeField] private int health = 2;
    [SerializeField] private float fearMoveSpeed = 2.5f;
    [SerializeField] private float fearDuration = 2f;
    [SerializeField] private float huntMoveSpeed = 3.25f;
    [SerializeField] private float guardianMoveSpeed = 4.05f;
    [SerializeField] private float contactDistance = 1.05f;
    [SerializeField] private float maxContactHeightDifference = 0.75f;
    [SerializeField] private float contactCooldown = 1.25f;
    [SerializeField] private LayerMask contactLineOfSightMask = ~0;
    [SerializeField] private float stareAtPlayerDistance = 5.5f;
    [SerializeField] private float reactionDistance = 8.5f;
    [SerializeField] private float reactionCooldownMin = 3f;
    [SerializeField] private float reactionCooldownMax = 5.6f;
    [Header("Group Tactics")]
    [SerializeField] private float flankRadius = 1.15f;
    [SerializeField] private float destinationRefreshInterval = 0.24f;
    [SerializeField] private float personalSpaceRadius = 0.9f;
    [SerializeField] private float personalSpaceWeight = 0.55f;
    [SerializeField] private float stuckVelocityThreshold = 0.14f;
    [SerializeField] private float stuckRepathAfter = 0.8f;
    [SerializeField] private float stuckSideStepDistance = 1.35f;

    private Transform player;
    private EnemyJumpController jumper;
    private NavMeshAgent agent;
    private CombatantHealth healthComponent;
    private Vector3 originalScale;
    private float fearUntil = -1f;
    private float nextContactDamageTime;
    private float nextNavRecoveryTime;
    private float nextReactionTime;
    private float nextDestinationUpdateTime;
    private float stuckStartedAt = -1f;
    private float formationAngle;
    private Vector3 currentDestination;
    private bool hasCurrentDestination;
    private bool defeated;
    private bool hunting;
    private bool fleeing;

    private static float nextSharedReactionTime;
    private static readonly string[] HuntReactionKeys =
    {
        "raw.shadow.hunt.reaction.seen",
        "raw.shadow.hunt.reaction.close"
    };

    public bool IsDefeated => defeated;
    public bool IsHunting => hunting && !defeated;
    public bool WasAttacked { get; private set; }
    public ShadowRole Role => role;
    public event Action<PrototypeShadowActor> Defeated;

    private void Awake()
    {
        formationAngle = Mathf.Repeat(Mathf.Abs(GetInstanceID()) * 137.508f, 360f);
        originalScale = transform.localScale;
        healthComponent = GetComponent<CombatantHealth>() ?? gameObject.AddComponent<CombatantHealth>();
        healthComponent.Configure(health);
    }

    public void Configure(ShadowRole newRole, int newHealth)
    {
        role = newRole;
        health = Mathf.Max(1, newHealth);
        if (healthComponent != null) healthComponent.Configure(health);
        ApplyRoleColor();
    }

    public void SetHunting(bool state)
    {
        hunting = state;
        fleeing = false;
        ResolvePlayer();
        if (agent != null)
        {
            agent.speed = role == ShadowRole.GuardianProxy ? guardianMoveSpeed : huntMoveSpeed;
            agent.isStopped = !state;
            agent.avoidancePriority = 25 + Mathf.Abs(GetInstanceID()) % 45;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        if (hunting)
        {
            nextReactionTime = Time.time + UnityEngine.Random.Range(1.4f, 2.8f);
            nextDestinationUpdateTime = 0f;
            stuckStartedAt = -1f;
            hasCurrentDestination = false;
        }
    }

    public void SetFleeingFromPlayer(bool state)
    {
        fleeing = state;
        if (state) hunting = false;
        ResolvePlayer();
    }

    public void PromoteToGuardianProxy()
    {
        if (defeated || role == ShadowRole.GuardianProxy) return;

        role = ShadowRole.GuardianProxy;
        health = Mathf.Max(health, 3);
        healthComponent.Configure(health);
        SetHunting(true);
        ApplyRoleColor();
    }

    private void Start()
    {
        ResolvePlayer();
        jumper = GetComponent<EnemyJumpController>();
        agent = GetComponent<NavMeshAgent>();
        ApplyRoleColor();
    }

    private void Update()
    {
        if (defeated || player == null) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        if (hunting)
        {
            TickHunt();
            return;
        }

        jumper?.TickAutoJump(player, role == ShadowRole.Enemy || role == ShadowRole.GuardianProxy, false);

        if (fleeing || Time.time <= fearUntil)
        {
            TickFlee();
        }
    }

    public void ReceiveAttack(Transform attacker)
    {
        if (defeated) return;

        WasAttacked = true;
        hunting = true;
        fleeing = false;
        if (!healthComponent.ApplyDamage(1, attacker != null ? attacker.gameObject : null)) return;
        health = healthComponent.CurrentHealth;
        fearUntil = Time.time + fearDuration;

        bool nowDefeated = healthComponent.IsDead;
        WorldState.Instance?.RecordShadowAttack(nowDefeated);

        if (nowDefeated)
        {
            defeated = true;
            transform.localScale = originalScale * 0.45f;
            SetColliderEnabled(false);
            Defeated?.Invoke(this);
        }

        ApplyRoleColor();
        ShowReaction(nowDefeated);
    }

    public void RestoreForEvent()
    {
        defeated = false;
        WasAttacked = false;
        hunting = false;
        fleeing = false;
        transform.localScale = originalScale;
        healthComponent?.ResetHealth();
        ApplyRoleColor();
        SetColliderEnabled(true);
    }

    private void TickHunt()
    {
        jumper?.TickAutoJump(player, true, true);
        if (EnsureAgentOnNavMesh())
        {
            agent.speed = role == ShadowRole.GuardianProxy ? guardianMoveSpeed : huntMoveSpeed;
            agent.isStopped = false;
            UpdateHuntDestination();
        }

        TickHuntPresence();

        if (!IsPlayerInContactRange() || Time.time < nextContactDamageTime) return;

        nextContactDamageTime = Time.time + contactCooldown;
        PlayerHealthController healthController = player.GetComponent<PlayerHealthController>();
        if (healthController != null && healthController.ApplyDamage(1, gameObject))
        {
            RuntimeHudController.Instance?.ShowSystemMessage(
                LocalizationManager.EnsureInstance().Format("hud.damage", healthController.CurrentHealth, healthController.MaxHealth),
                1.2f);
        }
    }

    private void UpdateHuntDestination()
    {
        if (agent == null || player == null) return;
        if (Time.time < nextDestinationUpdateTime && hasCurrentDestination)
        {
            agent.SetDestination(currentDestination);
            return;
        }

        nextDestinationUpdateTime = Time.time + destinationRefreshInterval;
        currentDestination = ResolveHuntDestination(player.position);
        hasCurrentDestination = true;
        agent.SetDestination(currentDestination);
    }

    private Vector3 ResolveHuntDestination(Vector3 target)
    {
        Vector3 toPlayer = target - transform.position;
        toPlayer.y = 0f;
        Vector3 desired = target;

        if (flankRadius > 0.01f && toPlayer.sqrMagnitude > contactDistance * contactDistance)
        {
            float angle = formationAngle + Time.time * 8f;
            desired += Quaternion.Euler(0f, angle, 0f) * Vector3.forward * flankRadius;
        }

        desired += CalculateLocalSeparation();

        if (IsAgentStuckTryingToMove())
        {
            Vector3 forward = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            float sideSign = GetInstanceID() % 2 == 0 ? 1f : -1f;
            desired = transform.position + forward * 0.65f + side * stuckSideStepDistance * sideSign;
        }

        return NavMesh.SamplePosition(desired, out NavMeshHit hit, 2f, NavMesh.AllAreas)
            ? hit.position
            : target;
    }

    private Vector3 CalculateLocalSeparation()
    {
        if (personalSpaceRadius <= 0.01f) return Vector3.zero;

        Vector3 separation = Vector3.zero;
        PrototypeShadowActor[] shadows = FindObjectsByType<PrototypeShadowActor>(FindObjectsSortMode.None);
        for (int i = 0; i < shadows.Length; i++)
        {
            PrototypeShadowActor other = shadows[i];
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

    private void TickHuntPresence()
    {
        if (player == null) return;

        Vector3 planar = player.position - transform.position;
        float heightDifference = planar.y;
        planar.y = 0f;
        if (planar.sqrMagnitude <= 0.01f) return;

        bool playerIsAbove = heightDifference > maxContactHeightDifference;
        if (playerIsAbove || planar.sqrMagnitude <= stareAtPlayerDistance * stareAtPlayerDistance)
        {
            Quaternion targetRotation = Quaternion.LookRotation(planar.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 320f * Time.deltaTime);
        }

        if (Time.time < nextReactionTime || Time.time < nextSharedReactionTime) return;
        if (planar.sqrMagnitude > reactionDistance * reactionDistance) return;
        if (!HasClearPathToPlayer()) return;

        string key = playerIsAbove
            ? "raw.shadow.hunt.reaction.above"
            : HuntReactionKeys[UnityEngine.Random.Range(0, HuntReactionKeys.Length)];
        RuntimeHudController.Instance?.ShowSystemMessage(LocalizationManager.EnsureInstance().Get(key), 1.8f);

        float cooldown = UnityEngine.Random.Range(reactionCooldownMin, reactionCooldownMax);
        nextReactionTime = Time.time + cooldown;
        nextSharedReactionTime = Time.time + cooldown * 0.75f;
    }

    private bool IsPlayerInContactRange()
    {
        if (player == null) return false;

        Vector3 separation = player.position - transform.position;
        if (Mathf.Abs(separation.y) > maxContactHeightDifference) return false;

        separation.y = 0f;
        if (separation.sqrMagnitude > contactDistance * contactDistance) return false;

        return HasClearPathToPlayer();
    }

    private bool HasClearPathToPlayer()
    {
        int mask = contactLineOfSightMask.value == 0 ? Physics.DefaultRaycastLayers : contactLineOfSightMask.value;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 destination = player.position + Vector3.up * 0.8f;
        return PhysicsLineOfSight.HasClearPath(transform, player, origin, destination, mask);
    }

    private void TickFlee()
    {
        Vector3 away = transform.position - player.position;
        away.y = 0f;
        if (away.sqrMagnitude <= 0.01f) return;
        if (!EnsureAgentOnNavMesh()) return;
        if (!NavMesh.SamplePosition(transform.position + away.normalized * 2.6f, out NavMeshHit destination, 2.6f, NavMesh.AllAreas)) return;

        agent.speed = fearMoveSpeed;
        agent.isStopped = false;
        agent.SetDestination(destination.position);
    }

    private void ApplyRoleColor()
    {
        Renderer renderer = GetComponentsInChildren<Renderer>(true)
            .FirstOrDefault(candidate => candidate.enabled);
        if (renderer == null) return;

        Color color = role switch
        {
            ShadowRole.Enemy => new Color(0.28f, 0.02f, 0.04f, 1f),
            ShadowRole.Ally => new Color(0.08f, 0.22f, 0.32f, 1f),
            ShadowRole.Afraid => new Color(0.15f, 0.15f, 0.2f, 1f),
            ShadowRole.GuardianProxy => new Color(0.25f, 0.08f, 0.35f, 1f),
            _ => new Color(0.08f, 0.08f, 0.1f, 1f)
        };

        if (defeated) color = new Color(0.02f, 0.02f, 0.025f, 1f);
        renderer.material.color = color;
    }

    private void ShowReaction(bool nowDefeated)
    {
        if (DialogueController.Instance == null || DialogueController.Instance.IsDialogueOpen) return;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        DialogueController.Instance.ShowDialogue(
            "SHADOW",
            localizer.Get(nowDefeated ? "raw.shadow.hit.first" : "raw.shadow.hit"));
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;
        if (Time.time < nextNavRecoveryTime) return false;

        nextNavRecoveryTime = Time.time + 0.5f;
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) return false;

        agent.Warp(hit.position);
        agent.isStopped = false;
        return agent.isOnNavMesh;
    }

    private void ResolvePlayer()
    {
        if (player != null) return;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    private void SetColliderEnabled(bool state)
    {
        foreach (Collider collider in GetComponents<Collider>())
        {
            collider.enabled = state;
        }
    }
}
