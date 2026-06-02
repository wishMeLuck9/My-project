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

    private Transform player;
    private EnemyJumpController jumper;
    private NavMeshAgent agent;
    private float fearUntil = -1f;
    private bool defeated;

    public bool IsDefeated => defeated;
    public bool WasAttacked { get; private set; }
    public ShadowRole Role => role;

    public void Configure(ShadowRole newRole, int newHealth)
    {
        role = newRole;
        health = Mathf.Max(1, newHealth);
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        jumper = GetComponent<EnemyJumpController>();
        agent = GetComponent<NavMeshAgent>();
        ApplyRoleColor();
    }

    private void Update()
    {
        if (defeated || player == null) return;

        jumper?.TickAutoJump(player, role == ShadowRole.Enemy, false);

        if (Time.time > fearUntil) return;

        Vector3 away = transform.position - player.position;
        away.y = 0f;
        if (away.sqrMagnitude <= 0.01f) return;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (!NavMesh.SamplePosition(transform.position + away.normalized * 2f, out NavMeshHit destination, 2f, NavMesh.AllAreas)) return;

        agent.speed = fearMoveSpeed;
        agent.isStopped = false;
        agent.SetDestination(destination.position);
    }

    public void ReceiveAttack(Transform attacker)
    {
        if (defeated) return;

        WasAttacked = true;
        health -= 1;
        fearUntil = Time.time + fearDuration;

        bool nowDefeated = health <= 0;
        WorldState.Instance?.RecordShadowAttack(nowDefeated);

        if (nowDefeated)
        {
            defeated = true;
            transform.localScale *= 0.45f;

            Collider collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        ApplyRoleColor();
        ShowReaction(nowDefeated);
    }

    public void RestoreForEvent()
    {
        defeated = false;
        WasAttacked = false;
        ApplyRoleColor();
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;
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

        if (nowDefeated)
        {
            DialogueController.Instance.ShowDialogue("SHADOW", "Ночь записала это как право. Утро назовет это долгом.");
            return;
        }

        DialogueController.Instance.ShowDialogue("SHADOW", "Мы знали, что ночь выберет тебя.");
    }
}
