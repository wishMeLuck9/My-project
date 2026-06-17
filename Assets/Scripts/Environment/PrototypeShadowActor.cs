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
    [SerializeField] private float contactCooldown = 1.25f;

    private Transform player;
    private EnemyJumpController jumper;
    private NavMeshAgent agent;
    private CombatantHealth healthComponent;
    private Vector3 originalScale;
    private float fearUntil = -1f;
    private float nextContactDamageTime;
    private float nextNavRecoveryTime;
    private bool defeated;
    private bool hunting;
    private bool fleeing;

    public bool IsDefeated => defeated;
    public bool WasAttacked { get; private set; }
    public ShadowRole Role => role;
    public event Action<PrototypeShadowActor> Defeated;

    private void Awake()
    {
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
            agent.SetDestination(player.position);
        }

        Vector3 distance = player.position - transform.position;
        distance.y = 0f;
        if (distance.sqrMagnitude > contactDistance * contactDistance || Time.time < nextContactDamageTime) return;

        nextContactDamageTime = Time.time + contactCooldown;
        PlayerHealthController healthController = player.GetComponent<PlayerHealthController>();
        if (healthController != null && healthController.ApplyDamage(1, gameObject))
        {
            RuntimeHudController.Instance?.ShowSystemMessage(
                LocalizationManager.EnsureInstance().Format("hud.damage", healthController.CurrentHealth, healthController.MaxHealth),
                1.2f);
        }
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
