using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SpriteRenderer spriteRenderer;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        // Check if player is effectively moving (including dashing)
        bool isMoving = playerMovement.IsEffectivelyMoving();
        
        if (isMoving)
        {
            animator.SetBool("isMoving", true);
            
            // Handle sprite flipping based on current facing direction
            if (playerMovement.lastHorizontalVector > 0)
            {
                spriteRenderer.flipX = false;
            }
            else if (playerMovement.lastHorizontalVector < 0)
            {
                spriteRenderer.flipX = true;
            }
        }
        else
        {
            animator.SetBool("isMoving", false);
        }
    }
}