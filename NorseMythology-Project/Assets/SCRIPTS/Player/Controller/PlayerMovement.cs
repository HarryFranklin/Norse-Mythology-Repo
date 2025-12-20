using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    private Rigidbody2D rigidBody;
    private Player player;
    public bool isMovementLocked = false;
    public bool isDashing = false;
    
    [HideInInspector] public Vector2 moveDir;
    [HideInInspector] public float lastHorizontalVector = 1f; // Default facing right
    [HideInInspector] public float lastVerticalVector = 0f;
    [HideInInspector] public Vector2 facingDirection = Vector2.right; // Current facing direction
    [HideInInspector] public Vector2 dashDirection = Vector2.zero;

    public Vector2 lastMovementDirection = Vector2.right; // Default facing right

    void Start()
    {
        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody2D>();
        }
        
        if (player == null)
        {
            player = GetComponent<Player>();
        }
        
        // Use player's move speed from stats if available
        if (player != null && player.currentStats != null)
        {
            moveSpeed = player.currentStats.moveSpeed;
        }
    }

    void Update()
    {
        InputManagement();
        UpdateFacingDirection();
    }

    private void FixedUpdate()
    {
        Move();
    }

    void InputManagement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveDir = new Vector2(moveX, moveY).normalized;

        // Update last movement direction when actually moving
        if (moveDir != Vector2.zero)
        {
            lastMovementDirection = moveDir;
        }

        // Update individual axis tracking (only when not dashing)
        if (!isDashing)
        {
            if (moveDir.x != 0)
            {
                lastHorizontalVector = moveDir.x;
            }
            if (moveDir.y != 0)
            {
                lastVerticalVector = moveDir.y;
            }
        }
    }
    
    void UpdateFacingDirection()
    {
        // Only update facing direction from input when movement is not locked
        // This prevents WASD input from overriding dash direction
        if (!isMovementLocked)
        {
            // Update facing direction based on movement
            if (moveDir != Vector2.zero)
            {
                facingDirection = moveDir;
            }
            else
            {
                // When not moving, use last movement direction
                facingDirection = new Vector2(lastHorizontalVector, lastVerticalVector).normalized;
            }
        }
    }

    // Allow external systems (like abilities) to set facing direction
    public void SetFacingDirection(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            facingDirection = direction.normalized;
            
            // Update the horizontal vector for sprite flipping
            if (direction.x != 0)
            {
                lastHorizontalVector = direction.x;
            }
            if (direction.y != 0)
            {
                lastVerticalVector = direction.y;
            }
            
            // Update last movement direction as well
            lastMovementDirection = direction.normalized;
        }
    }

    // Set dash state and direction
    public void SetDashState(bool dashing, Vector2 direction = default)
    {
        isDashing = dashing;
        if (dashing && direction != Vector2.zero)
        {
            dashDirection = direction.normalized;
            SetFacingDirection(direction);
        }
        else
        {
            dashDirection = Vector2.zero;
        }
    }

    // Check if player is effectively moving (including dashing)
    public bool IsEffectivelyMoving()
    {
        return (moveDir != Vector2.zero) || isDashing;
    }

    private void Move()
    {
        if (isMovementLocked) return;

        float currentSpeed = player.currentStats.moveSpeed;

        // If Time is slowed (e.g. 0.05), divide speed by 0.05 to multiply it by 20.
        // This keeps "Real Time" speed constant.
        if (Time.timeScale < 1f && Time.timeScale > 0.001f)
        {
            currentSpeed /= Time.timeScale;
        }

        rigidBody.linearVelocity = new Vector2(moveDir.x * currentSpeed, moveDir.y * currentSpeed);
    }
}