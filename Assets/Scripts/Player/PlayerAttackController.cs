using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackController : MonoBehaviour
{
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackRadius = 1.0f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float cooldown = 0.35f;

    private readonly Collider[] hitBuffer = new Collider[16];
    private float nextAttackTime;
    private bool canAttack = true;
    [SerializeField] private bool attackEnabledByScene = true;

    public void SetCanAttack(bool state)
    {
        canAttack = state;
    }

    public void SetSceneAttackEnabled(bool state)
    {
        attackEnabledByScene = state;
    }

    private void Update()
    {
        if (!attackEnabledByScene || !canAttack || Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (Time.time < nextAttackTime) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        nextAttackTime = Time.time + cooldown;
        PerformAttack();
    }

    private void PerformAttack()
    {
        int mask = hitMask.value == 0 ? Physics.DefaultRaycastLayers : hitMask.value;
        Vector3 center = transform.position + Vector3.up * 0.8f + transform.forward * attackRange;
        int hitCount = Physics.OverlapSphereNonAlloc(center, attackRadius, hitBuffer, mask, QueryTriggerInteraction.Collide);
        bool hitSomething = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null || hit.transform == transform) continue;

            PrototypeShadowActor shadow = hit.GetComponentInParent<PrototypeShadowActor>();
            if (shadow != null)
            {
                shadow.ReceiveAttack(transform);
                hitSomething = true;
                continue;
            }

            GuardianController guardian = hit.GetComponentInParent<GuardianController>();
            if (guardian != null)
            {
                guardian.ReceiveAttack();
                hitSomething = true;
            }
        }

        if (!hitSomething && WorldState.Instance != null)
        {
            WorldState.Instance.pursuitLevel += 1;
        }
    }
}
