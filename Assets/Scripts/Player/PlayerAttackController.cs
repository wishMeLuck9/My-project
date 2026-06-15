using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerAttackController : MonoBehaviour
{
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackRadius = 1.0f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float cooldown = 0.35f;
    [SerializeField] private float spellRange = 18f;
    [SerializeField] private float spellSpeed = 16f;
    [SerializeField] private float spellRadius = 0.18f;
    [SerializeField] private Vector3 fallbackCastOffset = new Vector3(0.35f, 1.15f, 0.45f);

    private readonly Collider[] hitBuffer = new Collider[16];
    private float nextAttackTime;
    private bool canAttack = true;
    private PlayerInputReader inputReader;
    private PlayerVisualAnimator visualAnimator;
    [SerializeField] private bool attackEnabledByScene = true;

    public bool IsSceneAttackEnabled => IsCorruptionPowerAvailable();

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
        if (!canAttack || !IsCorruptionPowerAvailable()) return;
        if (Time.time < nextAttackTime) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        nextAttackTime = Time.time + cooldown;
        PerformAttack(attackEnabledByScene);
    }

    private void PerformAttack(bool resolveGameplayHit)
    {
        ResolveVisualAnimator()?.PlayAttack();
        CastCorruptionSpell();
        if (!resolveGameplayHit) return;

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

    private void CastCorruptionSpell()
    {
        int mask = hitMask.value == 0 ? Physics.DefaultRaycastLayers : hitMask.value;
        Vector3 origin = ResolveCastOrigin();
        Vector3 direction = transform.forward.sqrMagnitude > 0.001f ? transform.forward.normalized : Vector3.forward;
        Vector3 destination = origin + direction * spellRange;
        Vector3 normal = -direction;
        Transform target = null;

        if (Physics.SphereCast(origin, spellRadius, direction, out RaycastHit hit, spellRange, mask, QueryTriggerInteraction.Ignore))
        {
            destination = hit.point;
            normal = hit.normal;
            target = hit.collider.transform;
        }

        CorruptionSpellProjectile.Spawn(origin, destination, normal, target, spellSpeed, spellRadius);
    }

    private Vector3 ResolveCastOrigin()
    {
        PlayerVisualAnimator visual = ResolveVisualAnimator();
        Animator animator = visual != null ? visual.GetComponent<Animator>() : GetComponentInChildren<Animator>(true);
        Transform hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (hand != null) return hand.position + transform.forward * 0.15f;

        return transform.TransformPoint(fallbackCastOffset);
    }

    private PlayerVisualAnimator ResolveVisualAnimator()
    {
        if (visualAnimator == null) visualAnimator = GetComponentInChildren<PlayerVisualAnimator>(true);
        return visualAnimator;
    }

    private bool IsCorruptionPowerAvailable()
    {
        if (!attackEnabledByScene) return false;

        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == SceneIds.Night || sceneName == SceneIds.Final;
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
