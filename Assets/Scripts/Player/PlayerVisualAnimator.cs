using UnityEngine;

public class PlayerVisualAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController3D player;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (player == null) player = GetComponentInParent<PlayerController3D>();
    }

    private void Update()
    {
        if (animator == null || player == null) return;

        float normalizedSpeed = Mathf.Clamp01(player.MovementAmount);
        animator.speed = normalizedSpeed > 0.02f ? Mathf.Lerp(0.65f, 1.15f, normalizedSpeed) : 0f;
    }
}
