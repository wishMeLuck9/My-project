using UnityEngine;

public class PlayerVisualAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController3D player;

    private bool wasMoving;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (player == null) player = GetComponentInParent<PlayerController3D>();
    }

    private void Update()
    {
        if (animator == null || player == null) return;

        float normalizedSpeed = Mathf.Clamp01(player.MovementAmount);
        bool isMoving = normalizedSpeed > 0.02f;
        if (isMoving)
        {
            if (!wasMoving) animator.Play("Walk", 0, 0f);
            animator.speed = Mathf.Lerp(0.65f, 1.15f, normalizedSpeed);
        }
        else
        {
            if (wasMoving) animator.Play("Walk", 0, 0f);
            animator.speed = 0f;
        }

        wasMoving = isMoving;
    }
}
