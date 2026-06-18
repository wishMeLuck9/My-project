using System.Collections.Generic;
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
    [SerializeField] private float spellHitRadius = 0.75f;
    [SerializeField] private float spellAimAssistRadius = 1.35f;
    [SerializeField] private float spellAimAssistAngle = 26f;
    [SerializeField] private int shadowAttackDamage = 2;
    [SerializeField] private Vector3 fallbackCastOffset = new Vector3(0.35f, 1.15f, 0.45f);

    private readonly Collider[] hitBuffer = new Collider[16];
    private readonly RaycastHit[] spellCastHits = new RaycastHit[24];
    private readonly HashSet<PrototypeShadowActor> resolvedShadowHits = new HashSet<PrototypeShadowActor>();
    private readonly HashSet<GuardianController> resolvedGuardianHits = new HashSet<GuardianController>();
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
        bool projectileWillResolveHit = CastCorruptionSpell(resolveGameplayHit);
        if (!resolveGameplayHit || projectileWillResolveHit) return;

        int mask = hitMask.value == 0 ? Physics.DefaultRaycastLayers : hitMask.value;
        Vector3 center = transform.position + Vector3.up * 0.8f + transform.forward * attackRange;
        int hitCount = Physics.OverlapSphereNonAlloc(center, attackRadius, hitBuffer, mask, QueryTriggerInteraction.Collide);
        bool hitSomething = false;
        resolvedShadowHits.Clear();
        resolvedGuardianHits.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null || hit.transform == transform) continue;

            PrototypeShadowActor shadow = hit.GetComponentInParent<PrototypeShadowActor>();
            if (shadow != null && !resolvedShadowHits.Contains(shadow) && HasLineOfSight(shadow.transform, hit))
            {
                resolvedShadowHits.Add(shadow);
                shadow.ReceiveAttack(transform, shadowAttackDamage);
                hitSomething = true;
                continue;
            }

            GuardianController guardian = hit.GetComponentInParent<GuardianController>();
            if (guardian != null && !resolvedGuardianHits.Contains(guardian) && HasLineOfSight(guardian.transform, hit))
            {
                resolvedGuardianHits.Add(guardian);
                guardian.ReceiveAttack();
                hitSomething = true;
            }
        }

        if (!hitSomething && WorldState.Instance != null)
        {
            WorldState.Instance.pursuitLevel += 1;
        }
    }

    private bool CastCorruptionSpell(bool resolveGameplayHit)
    {
        int mask = hitMask.value == 0 ? Physics.DefaultRaycastLayers : hitMask.value;
        Vector3 origin = ResolveCastOrigin();
        Vector3 direction = transform.forward.sqrMagnitude > 0.001f ? transform.forward.normalized : Vector3.forward;
        Vector3 destination = origin + direction * spellRange;
        Vector3 normal = -direction;
        Transform target = null;
        bool canResolveProjectileHit = false;
        float gameplayHitRadius = Mathf.Max(spellRadius, spellHitRadius);

        if (resolveGameplayHit && TryFindSpellCombatTarget(origin, direction, spellRange, mask, gameplayHitRadius, out Transform combatTarget, out Vector3 aimPoint, out Vector3 aimNormal))
        {
            destination = aimPoint;
            normal = aimNormal;
            target = combatTarget;
            canResolveProjectileHit = true;
        }
        else if (TryFindSpellImpact(origin, direction, spellRange, mask, out RaycastHit hit))
        {
            destination = hit.point;
            normal = hit.normal;
            target = hit.collider.transform;
            canResolveProjectileHit = resolveGameplayHit && IsCombatTarget(target);
        }

        CorruptionSpellProjectile.Spawn(
            origin,
            destination,
            normal,
            target,
            spellSpeed,
            spellRadius,
            resolveGameplayHit,
            transform,
            mask,
            gameplayHitRadius,
            shadowAttackDamage);
        return canResolveProjectileHit;
    }

    private bool TryFindSpellCombatTarget(
        Vector3 origin,
        Vector3 direction,
        float range,
        int mask,
        float gameplayHitRadius,
        out Transform target,
        out Vector3 aimPoint,
        out Vector3 impactNormal)
    {
        target = null;
        aimPoint = origin + direction * range;
        impactNormal = -direction;

        float assistRadius = Mathf.Max(spellAimAssistRadius, gameplayHitRadius);
        float minForwardDot = Mathf.Cos(Mathf.Clamp(spellAimAssistAngle, 1f, 89f) * Mathf.Deg2Rad);
        float bestScore = float.MaxValue;

        PrototypeShadowActor[] shadows = FindObjectsByType<PrototypeShadowActor>(FindObjectsSortMode.None);
        for (int i = 0; i < shadows.Length; i++)
        {
            PrototypeShadowActor shadow = shadows[i];
            if (shadow == null) continue;

            TryConsiderSpellCombatTarget(
                shadow.transform,
                origin,
                direction,
                range,
                mask,
                assistRadius,
                minForwardDot,
                ref target,
                ref aimPoint,
                ref impactNormal,
                ref bestScore);
        }

        GuardianController[] guardians = FindObjectsByType<GuardianController>(FindObjectsSortMode.None);
        for (int i = 0; i < guardians.Length; i++)
        {
            GuardianController guardian = guardians[i];
            if (guardian == null) continue;

            TryConsiderSpellCombatTarget(
                guardian.transform,
                origin,
                direction,
                range,
                mask,
                assistRadius,
                minForwardDot,
                ref target,
                ref aimPoint,
                ref impactNormal,
                ref bestScore);
        }

        return target != null;
    }

    private void TryConsiderSpellCombatTarget(
        Transform combatRoot,
        Vector3 origin,
        Vector3 direction,
        float range,
        int mask,
        float assistRadius,
        float minForwardDot,
        ref Transform target,
        ref Vector3 aimPoint,
        ref Vector3 impactNormal,
        ref float bestScore)
    {
        if (combatRoot == null || IsPartOfSource(combatRoot) || IsDefeatedCombatTarget(combatRoot)) return;
        if (!combatRoot.gameObject.activeInHierarchy) return;
        if (!TryResolveCombatAimPoint(combatRoot, null, out Vector3 candidateAimPoint)) return;

        Vector3 toTarget = candidateAimPoint - origin;
        float projectedDistance = Vector3.Dot(toTarget, direction);
        if (projectedDistance <= 0.1f || projectedDistance > range) return;

        Vector3 projectedPoint = origin + direction * projectedDistance;
        float lateralDistance = Vector3.Distance(candidateAimPoint, projectedPoint);
        float forwardDot = toTarget.sqrMagnitude > 0.001f ? Vector3.Dot(direction, toTarget.normalized) : 1f;
        if (lateralDistance > assistRadius && forwardDot < minForwardDot) return;
        if (!HasProjectileLineOfSight(origin, combatRoot, candidateAimPoint, mask)) return;

        float score = projectedDistance + lateralDistance * 1.75f;
        if (score >= bestScore) return;

        bestScore = score;
        target = combatRoot;
        aimPoint = candidateAimPoint;
        impactNormal = toTarget.sqrMagnitude > 0.001f ? -toTarget.normalized : -direction;
    }

    private bool TryResolveCombatAimPoint(Transform combatRoot, Collider firstCollider, out Vector3 aimPoint)
    {
        aimPoint = combatRoot != null ? combatRoot.position + Vector3.up * 0.8f : Vector3.zero;
        if (combatRoot == null) return false;

        Bounds bounds = default;
        bool hasBounds = false;
        Collider[] colliders = combatRoot.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (!hasBounds && firstCollider != null)
        {
            bounds = firstCollider.bounds;
            hasBounds = true;
        }

        if (hasBounds) aimPoint = bounds.center;
        return true;
    }

    private bool TryFindSpellImpact(Vector3 origin, Vector3 direction, float range, int mask, out RaycastHit bestHit)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            spellRadius,
            direction,
            spellCastHits,
            range,
            mask,
            QueryTriggerInteraction.Collide);

        bestHit = default;
        float nearestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = spellCastHits[i];
            Collider collider = candidate.collider;
            if (collider == null || IsPartOfSource(collider.transform)) continue;
            if (collider.isTrigger && !IsCombatTarget(collider.transform)) continue;
            if (candidate.distance >= nearestDistance) continue;

            nearestDistance = candidate.distance;
            bestHit = candidate;
            found = true;
        }

        return found;
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

    private bool HasProjectileLineOfSight(Vector3 origin, Transform target, Vector3 targetPoint, int mask)
    {
        return PhysicsLineOfSight.HasClearPath(transform, target, origin, targetPoint, mask);
    }

    private static bool IsCombatTarget(Transform target)
    {
        return ResolveCombatRoot(target) != null;
    }

    private static Transform ResolveCombatRoot(Transform target)
    {
        if (target == null) return null;

        PrototypeShadowActor shadow = target.GetComponentInParent<PrototypeShadowActor>();
        if (shadow != null) return shadow.transform;

        GuardianController guardian = target.GetComponentInParent<GuardianController>();
        return guardian != null ? guardian.transform : null;
    }

    private static bool IsDefeatedCombatTarget(Transform target)
    {
        if (target == null) return true;

        PrototypeShadowActor shadow = target.GetComponentInParent<PrototypeShadowActor>();
        if (shadow != null) return shadow.IsDefeated;

        GuardianController guardian = target.GetComponentInParent<GuardianController>();
        return guardian != null && guardian.IsDefeated;
    }

    private bool IsPartOfSource(Transform candidate)
    {
        return candidate != null && (candidate == transform || candidate.IsChildOf(transform) || transform.IsChildOf(candidate));
    }
}
