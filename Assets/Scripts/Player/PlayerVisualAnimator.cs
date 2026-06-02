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

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (player == null) player = GetComponentInParent<PlayerController3D>();
    }

    private void Update()
    {
        if (animator == null || player == null) return;

        animator.SetFloat(MoveSpeedHash, Mathf.Clamp01(player.MovementAmount), movementDampTime, Time.deltaTime);
        animator.SetBool(GroundedHash, player.IsGrounded);
    }

    public void PlayJump(bool running)
    {
        if (animator != null) animator.SetTrigger(running ? RunningJumpHash : JumpHash);
    }

    public void PlayAttack()
    {
        if (animator != null) animator.SetTrigger(AttackHash);
    }
}
