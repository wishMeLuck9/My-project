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

    private FinalGateOutcomeController arena;
    private Transform player;
    private NavMeshAgent agent;
    private EnemyJumpController jumper;
    private int health;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool battling;
    private bool defeated;

    public string GuardianName => string.IsNullOrWhiteSpace(guardianName) ? name : guardianName;
    public bool IsDefeated => defeated;

    private void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        health = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        jumper = GetComponent<EnemyJumpController>();
    }

    public void Configure(string newName, bool forceGuardian, string[] lines = null)
    {
        guardianName = newName;
        isForceGuardian = forceGuardian;
        customLines = lines;
    }

    public override void Interact()
    {
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
        defeated = false;
        health = maxHealth;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        SetColliderEnabled(true);
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(spawnPosition);
            agent.speed = chaseSpeed;
            agent.isStopped = false;
        }
    }

    private void Update()
    {
        if (!battling || defeated || player == null || arena == null) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.position += direction.normalized * chaseSpeed * Time.deltaTime;
            }
        }

        Vector3 distance = player.position - transform.position;
        distance.y = 0f;
        jumper?.TickAutoJump(player, true, true);

        if (distance.sqrMagnitude <= catchDistance * catchDistance)
        {
            arena.ResetArenaAfterPlayerHit();
        }
    }

    public void ReceiveAttack()
    {
        if (!battling || defeated) return;

        health -= 1;
        if (health <= 0)
        {
            defeated = true;
            SetColliderEnabled(false);
            arena?.NotifyGuardianDefeated();
            return;
        }

        DialogueController.Instance?.ShowDialogue(GuardianName, "Удар принят. Врата все еще закрыты.");
    }

    public string BuildEvaluationMessage()
    {
        if (customLines != null && customLines.Length > 0)
        {
            return customLines[Random.Range(0, customLines.Length)];
        }

        WorldState state = WorldState.Instance;
        if (state == null) return "Состояние не найдено. Проверка отложена.";
        if (state.enemyShadowsDefeated > 0) return "Ты принес убийство. Теперь проход придется отнять.";
        return isForceGuardian
            ? "Ты удержал руку. Сила отступит, если ты отдашь все добровольно."
            : "Два фрагмента, память и жизнь. Иначе врата не откроются.";
    }

    private void SetColliderEnabled(bool state)
    {
        foreach (Collider collider in GetComponents<Collider>())
        {
            collider.enabled = state;
        }
    }
}
