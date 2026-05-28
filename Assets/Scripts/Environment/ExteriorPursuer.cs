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
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(target);
        }
        else
        {
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.position += direction.normalized * chaseSpeed * Time.deltaTime;
            }
        }

        Vector3 separation = target - transform.position;
        separation.y = 0f;
        jumper?.TickAutoJump(player, true, true);

        if (separation.sqrMagnitude <= catchDistance * catchDistance)
        {
            hunt.CapturePlayer();
        }
    }
}
