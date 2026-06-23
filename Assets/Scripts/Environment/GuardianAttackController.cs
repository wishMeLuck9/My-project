using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(GuardianController))]
public class GuardianAttackController : MonoBehaviour
{
    private const float MaxEffectiveRangedRange = 6.25f;
    private const float RangedHitGrace = 0.2f;

    [SerializeField] private int meleeDamage = 1;
    [SerializeField] private float meleeRange = 2.05f;
    [SerializeField] private float meleeRadius = 1.15f;
    [SerializeField] private float maxMeleeHeightDifference = 0.75f;
    [SerializeField] private float meleeCooldown = 1.45f;
    [SerializeField] private float meleeTelegraphSeconds = 0.42f;
    [SerializeField] private int rangedDamage = 1;
    [SerializeField] private float rangedRange = 8.5f;
    [SerializeField] private float rangedCooldown = 4.5f;
    [SerializeField] private float rangedTelegraphSeconds = 0.62f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    private FinalGateOutcomeController arena;
    private PlayerHealthController playerHealth;
    private Transform target;
    private NavMeshAgent agent;
    private Renderer[] renderers;
    private Color[] originalColors;
    private MaterialPropertyBlock propertyBlock;
    private bool battleActive;
    private bool attacking;
    private bool phaseTwo;
    private bool isForceGuardian;
    private float nextMeleeTime;
    private float nextRangedTime;
    private float baseAgentSpeed;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool IsAttacking => attacking;
    public float PreferredCombatRange => isForceGuardian ? meleeRange * 0.85f : EffectiveRangedRange * 0.65f;

    private float EffectiveRangedRange => Mathf.Min(Mathf.Max(0.1f, rangedRange), MaxEffectiveRangedRange);

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        baseAgentSpeed = agent != null ? agent.speed : 0f;
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        propertyBlock = new MaterialPropertyBlock();
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = ResolveRendererColor(renderers[i]);
        }
    }

    public void BeginBattle(FinalGateOutcomeController controller, Transform newTarget, bool forceGuardian)
    {
        arena = controller;
        target = newTarget;
        playerHealth = target != null ? target.GetComponent<PlayerHealthController>() : null;
        isForceGuardian = forceGuardian;
        battleActive = true;
        attacking = false;
        SetPhaseTwo(false);
        nextMeleeTime = Time.time + 0.8f;
        nextRangedTime = Time.time + 1.5f;
        RestoreTint();
    }

    public void ResetBattle()
    {
        StopAllCoroutines();
        attacking = false;
        battleActive = false;
        SetPhaseTwo(false);
        RestoreTint();
    }

    public void SetPhaseTwo(bool state)
    {
        phaseTwo = state;
        if (agent != null) agent.speed = baseAgentSpeed * (state ? 1.12f : 1f);
    }

    public void TickBattle()
    {
        if (!battleActive || attacking || target == null || playerHealth == null || playerHealth.IsDead) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        Vector3 delta = target.position - transform.position;
        delta.y = 0f;
        float distance = delta.magnitude;

        if (distance <= meleeRange && IsTargetWithinMeleeHeight() && HasLineOfSight() && Time.time >= nextMeleeTime)
        {
            StartCoroutine(MeleeRoutine());
            return;
        }

        bool canUseRanged = !isForceGuardian || phaseTwo;
        if (canUseRanged && distance <= EffectiveRangedRange && Time.time >= nextRangedTime && HasLineOfSight())
        {
            StartCoroutine(RangedRoutine());
        }
    }

    private IEnumerator MeleeRoutine()
    {
        attacking = true;
        nextMeleeTime = Time.time + meleeCooldown * (phaseTwo ? 0.78f : 1f);
        SetTint(new Color(1f, 0.35f, 0.2f, 1f));
        yield return new WaitForSeconds(meleeTelegraphSeconds);

        if (playerHealth != null && IsTargetInMeleeArc() && playerHealth.ApplyDamage(meleeDamage, gameObject))
        {
            arena?.NotifyPlayerDamaged(playerHealth);
        }

        RestoreTint();
        attacking = false;
    }

    private IEnumerator RangedRoutine()
    {
        attacking = true;
        nextRangedTime = Time.time + rangedCooldown * (phaseTwo ? 0.72f : 1f);
        SetTint(new Color(0.35f, 0.75f, 1f, 1f));
        yield return new WaitForSeconds(rangedTelegraphSeconds);

        if (playerHealth != null && IsTargetInRangedRange() && HasLineOfSight() && playerHealth.ApplyDamage(rangedDamage, gameObject))
        {
            arena?.NotifyPlayerDamaged(playerHealth);
        }

        RestoreTint();
        attacking = false;
    }

    private bool IsTargetInMeleeArc()
    {
        if (target == null) return false;
        if (!IsTargetWithinMeleeHeight()) return false;
        if (!HasLineOfSight()) return false;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > meleeRadius * meleeRadius) return false;
        if (toTarget.sqrMagnitude <= 0.01f) return true;
        return Vector3.Dot(transform.forward, toTarget.normalized) > -0.15f;
    }

    private bool IsTargetWithinMeleeHeight()
    {
        if (target == null) return false;
        return Mathf.Abs(target.position.y - transform.position.y) <= maxMeleeHeightDifference;
    }

    private bool IsTargetInRangedRange()
    {
        if (target == null) return false;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float allowedRange = EffectiveRangedRange + RangedHitGrace;
        return toTarget.sqrMagnitude <= allowedRange * allowedRange;
    }

    private bool HasLineOfSight()
    {
        if (target == null) return false;

        int mask = lineOfSightMask.value == 0 ? Physics.DefaultRaycastLayers : lineOfSightMask.value;
        Vector3 origin = transform.position + Vector3.up * 1.15f;
        Vector3 destination = target.position + Vector3.up * 0.9f;
        return PhysicsLineOfSight.HasClearPath(transform, target, origin, destination, mask);
    }

    private void SetTint(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            ApplyRendererColor(renderers[i], color);
        }
    }

    private void RestoreTint()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            ApplyRendererColor(renderers[i], i < originalColors.Length ? originalColors[i] : Color.white);
        }
    }

    private static Color ResolveRendererColor(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null) return Color.white;
        Material material = renderer.sharedMaterial;
        if (material.HasProperty(BaseColorId)) return material.GetColor(BaseColorId);
        if (material.HasProperty(ColorId)) return material.GetColor(ColorId);
        return Color.white;
    }

    private void ApplyRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;

        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);
        renderer.SetPropertyBlock(propertyBlock);
    }
}
