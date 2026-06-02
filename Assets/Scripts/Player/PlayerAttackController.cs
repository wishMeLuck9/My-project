using UnityEngine;

public class PlayerAttackController : MonoBehaviour
{
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackRadius = 1.0f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float cooldown = 0.35f;

    private readonly Collider[] hitBuffer = new Collider[16];
    private float nextAttackTime;
    private bool canAttack = true;
    private PlayerInputReader inputReader;
    private PlayerVisualAnimator visualAnimator;
    [SerializeField] private bool attackEnabledByScene = true;

    public bool IsSceneAttackEnabled => attackEnabledByScene;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        visualAnimator = GetComponentInChildren<PlayerVisualAnimator>(true);
    }

    private void OnEnable()
    {
        if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
        if (inputReader != null) inputReader.AttackPressed += TryAttack;
    }

    private void OnDisable()
    {
        if (inputReader != null) inputReader.AttackPressed -= TryAttack;
    }

    public void SetCanAttack(bool state)
    {
        canAttack = state;
    }

    public void SetSceneAttackEnabled(bool state)
    {
        attackEnabledByScene = state;
    }

    private void TryAttack()
    {
        if (!attackEnabledByScene || !canAttack) return;
        if (Time.time < nextAttackTime) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        nextAttackTime = Time.time + cooldown;
        PerformAttack();
    }

    private void PerformAttack()
    {
        ResolveVisualAnimator()?.PlayAttack();

        int mask = hitMask.value == 0 ? Physics.DefaultRaycastLayers : hitMask.value;
        Vector3 center = transform.position + Vector3.up * 0.8f + transform.forward * attackRange;
        int hitCount = Physics.OverlapSphereNonAlloc(center, attackRadius, hitBuffer, mask, QueryTriggerInteraction.Collide);
        bool hitSomething = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null || hit.transform == transform) continue;

            PrototypeShadowActor shadow = hit.GetComponentInParent<PrototypeShadowActor>();
            if (shadow != null && HasLineOfSight(shadow.transform, hit))
            {
                shadow.ReceiveAttack(transform);
                hitSomething = true;
                continue;
            }

            GuardianController guardian = hit.GetComponentInParent<GuardianController>();
            if (guardian != null && HasLineOfSight(guardian.transform, hit))
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

    private PlayerVisualAnimator ResolveVisualAnimator()
    {
        if (visualAnimator == null) visualAnimator = GetComponentInChildren<PlayerVisualAnimator>(true);
        return visualAnimator;
    }

    private bool HasLineOfSight(Transform target, Collider hit)
    {
        int mask = hitMask.value == 0 ? Physics.DefaultRaycastLayers : hitMask.value;
        return PhysicsLineOfSight.HasClearPath(
            transform,
            target,
            transform.position + Vector3.up * 0.8f,
            hit.bounds.center,
            mask);
    }
}
