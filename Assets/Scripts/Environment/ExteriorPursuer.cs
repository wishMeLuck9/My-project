using UnityEngine;
using UnityEngine.AI;

public class ExteriorPursuer : MonoBehaviour
{
    [SerializeField] private float chaseSpeed = 4.3f;
    [SerializeField] private float catchDistance = 1.15f;

    private Transform player;
    private NavMeshAgent agent;
    private ShadowNPC dialogue;
    private EnemyJumpController jumper;
    private ExteriorHuntController hunt;
    private bool isHunting;
    private float nextRecoveryTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        dialogue = GetComponent<ShadowNPC>();
        jumper = GetComponent<EnemyJumpController>();
    }

    public void SetHunting(bool state, ExteriorHuntController controller)
    {
        hunt = controller;
        isHunting = state;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;

        if (dialogue != null) dialogue.enabled = !state;
        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.isStopped = !state;
        }
    }

    private void Update()
    {
        if (!isHunting || player == null || hunt == null) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        Vector3 target = player.position;
        if (EnsureAgentOnNavMesh())
        {
            agent.SetDestination(target);
        }

        Vector3 separation = target - transform.position;
        separation.y = 0f;
        jumper?.TickAutoJump(player, true, true);

        if (separation.sqrMagnitude <= catchDistance * catchDistance)
        {
            hunt.CapturePlayer();
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
}
