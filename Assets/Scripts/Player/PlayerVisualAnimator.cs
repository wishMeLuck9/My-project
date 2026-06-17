using UnityEngine;

public class PlayerVisualAnimator : MonoBehaviour
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int RunningJumpHash = Animator.StringToHash("RunningJump");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController3D player;
    [SerializeField] private float movementDampTime = 0.1f;
    [SerializeField] private bool alignToPlayerCollider = true;
    [SerializeField] private float visualGroundOffset;

    private Collider playerCollider;
    private Renderer[] visualRenderers;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (player == null) player = GetComponentInParent<PlayerController3D>();
        if (player != null) playerCollider = player.GetComponent<Collider>();
        visualRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        if (animator == null || player == null) return;

        animator.SetFloat(MoveSpeedHash, Mathf.Clamp01(player.MovementAmount), movementDampTime, Time.deltaTime);
        animator.SetBool(GroundedHash, player.IsGrounded);
    }

    private void LateUpdate()
    {
        AlignVisualBottomToCollider();
    }

    public void PlayJump(bool running)
    {
        if (animator != null) animator.SetTrigger(running ? RunningJumpHash : JumpHash);
    }

    public void PlayAttack()
    {
        if (animator != null) animator.SetTrigger(AttackHash);
    }

    private void AlignVisualBottomToCollider()
    {
        if (!alignToPlayerCollider || playerCollider == null || visualRenderers == null || visualRenderers.Length == 0) return;
        if (!TryGetVisualBounds(out Bounds visualBounds)) return;

        float targetBottom = playerCollider.bounds.min.y + visualGroundOffset;
        float correction = targetBottom - visualBounds.min.y;
        if (Mathf.Abs(correction) <= 0.001f) return;

        transform.position += Vector3.up * correction;
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (Renderer visualRenderer in visualRenderers)
        {
            if (visualRenderer == null || !visualRenderer.enabled || !visualRenderer.gameObject.activeInHierarchy) continue;

            if (!hasBounds)
            {
                bounds = visualRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(visualRenderer.bounds);
            }
        }

        return hasBounds;
    }
}
