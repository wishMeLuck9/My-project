using UnityEngine;
using UnityEngine.AI;

public class GuardianController : Interactable
{
    [SerializeField] private string guardianName;
    [SerializeField] private bool isForceGuardian;
    [SerializeField] private string[] customLines;
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float chaseSpeed = 3.8f;
    [SerializeField] private float catchDistance = 1.45f;
    [SerializeField] private float maxCatchHeightDifference = 0.75f;
    [SerializeField] private LayerMask catchLineOfSightMask = ~0;

    private FinalGateOutcomeController arena;
    private Transform player;
    private NavMeshAgent agent;
    private EnemyJumpController jumper;
    private CombatantHealth healthComponent;
    private GuardianAttackController attacks;
    private int health;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool battling;
    private bool defeated;
    private float nextRecoveryTime;

    private static readonly string[] BattleInteractionKeys =
    {
        "raw.guardian.battle.interact.judgement",
        "raw.guardian.battle.interact.silence"
    };

    public string GuardianName => string.IsNullOrWhiteSpace(guardianName) ? name : guardianName;
    public bool IsDefeated => defeated;
    public int MaxHealth => Health.MaxHealth;
    public int CurrentHealth => defeated ? 0 : Health.CurrentHealth;
    public CombatantHealth Health => healthComponent != null ? healthComponent : healthComponent = GetComponent<CombatantHealth>() ?? gameObject.AddComponent<CombatantHealth>();

    private void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        healthComponent = GetComponent<CombatantHealth>() ?? gameObject.AddComponent<CombatantHealth>();
        healthComponent.Configure(maxHealth);
        health = healthComponent.CurrentHealth;
        agent = GetComponent<NavMeshAgent>();
        jumper = GetComponent<EnemyJumpController>();
        attacks = GetComponent<GuardianAttackController>();
    }

    public void Configure(string newName, bool forceGuardian, string[] lines = null)
    {
        guardianName = newName;
        isForceGuardian = forceGuardian;
        customLines = lines;
        Health.Configure(maxHealth, false);
    }

    public void ConfigureBattleStats(int newMaxHealth, float newChaseSpeed, float newCatchDistance)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        chaseSpeed = Mathf.Max(0.5f, newChaseSpeed);
        catchDistance = Mathf.Max(0.5f, newCatchDistance);
        Health.Configure(maxHealth);
    }

    public override void Interact()
    {
        if (battling && !defeated)
        {
            RuntimeHudController.Instance?.ShowSystemMessage(
                LocalizationManager.EnsureInstance().Get(BattleInteractionKeys[Random.Range(0, BattleInteractionKeys.Length)]),
                1.8f);
            return;
        }

        if (!battling && DialogueController.Instance != null)
        {
            DialogueController.Instance.ShowDialogue(GuardianName, BuildEvaluationMessage());
        }
    }

    public void BeginBattle(FinalGateOutcomeController controller, Transform target)
    {
        arena = controller;
        player = target;
        battling = true;
        ResetBattleState();
    }

    public void ResetBattleState()
    {
        bool shouldResumeBattle = battling && arena != null && player != null;

        defeated = false;
        Health.Configure(maxHealth);
        health = Health.CurrentHealth;
        ResolveAttackController()?.ResetBattle();
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        SetColliderEnabled(true);
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(spawnPosition);
            agent.speed = chaseSpeed;
            agent.isStopped = false;
        }

        if (shouldResumeBattle)
        {
            ResolveAttackController()?.BeginBattle(arena, player, isForceGuardian);
        }
    }

    private void Update()
    {
        if (!battling || defeated || player == null || arena == null) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        if (EnsureAgentOnNavMesh())
        {
            float preferredRange = attacks != null ? attacks.PreferredCombatRange : catchDistance;
            Vector3 planar = player.position - transform.position;
            planar.y = 0f;
            agent.isStopped = planar.sqrMagnitude <= preferredRange * preferredRange && attacks != null;
            if (!agent.isStopped) agent.SetDestination(player.position);
        }

        jumper?.TickAutoJump(player, true, true);
        attacks?.TickBattle();

        if (attacks == null && IsPlayerCatchable())
        {
            arena.ResetArenaAfterPlayerHit();
        }
    }

    private bool IsPlayerCatchable()
    {
        if (player == null) return false;

        Vector3 separation = player.position - transform.position;
        if (Mathf.Abs(separation.y) > maxCatchHeightDifference) return false;

        separation.y = 0f;
        if (separation.sqrMagnitude > catchDistance * catchDistance) return false;

        int mask = catchLineOfSightMask.value == 0 ? Physics.DefaultRaycastLayers : catchLineOfSightMask.value;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 destination = player.position + Vector3.up * 0.8f;
        return PhysicsLineOfSight.HasClearPath(transform, player, origin, destination, mask);
    }

    public void ReceiveAttack()
    {
        if (!battling || defeated) return;

        if (!Health.ApplyDamage(1, null)) return;
        health = Health.CurrentHealth;
        if (Health.IsDead)
        {
            defeated = true;
            SetColliderEnabled(false);
            ResolveAttackController()?.ResetBattle();
            arena?.NotifyGuardianDefeated();
            return;
        }

        DialogueController.Instance?.ShowDialogue(
            GuardianName,
            LocalizationManager.EnsureInstance().Get("raw.guardian.hit"));
    }

    public string BuildEvaluationMessage()
    {
        if (customLines != null && customLines.Length > 0)
        {
            return customLines[Random.Range(0, customLines.Length)];
        }

        WorldState state = WorldState.Instance;
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        if (state == null) return localizer.Get("raw.guardian.missing");
        if (state.enemyShadowsDefeated > 0 || state.nightViolenceAttempted) return localizer.Get("raw.guardian.violent");
        return isForceGuardian
            ? localizer.Get("raw.guardian.mercy")
            : localizer.Get("raw.guardian.price");
    }

    private void SetColliderEnabled(bool state)
    {
        foreach (Collider collider in GetComponents<Collider>())
        {
            collider.enabled = state;
        }
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

    private GuardianAttackController ResolveAttackController()
    {
        if (attacks == null) attacks = GetComponent<GuardianAttackController>();
        return attacks;
    }
}
