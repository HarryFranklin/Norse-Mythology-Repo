using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    private Rigidbody2D rigidBody;
    private PlayerController playerController;
    public bool isMovementLocked = false;
    
    [HideInInspector] public Vector2 moveDir;
    [HideInInspector] public float lastHorizontalVector = 1f; // Default facing right
    [HideInInspector] public float lastVerticalVector = 0f;
    [HideInInspector] public Vector2 facingDirection = Vector2.right; // Current facing direction

    public Vector2 lastMovementDirection = Vector2.right; // Default facing right

    void Start()
    {
        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody2D>();
        }
        
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
        
        // Use player's move speed from stats if available
        if (playerController != null && playerController.currentStats != null)
        {
            moveSpeed = playerController.currentStats.moveSpeed;
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

        // Update individual axis tracking
        if (moveDir.x != 0)
        {
            lastHorizontalVector = moveDir.x;
        }
        if (moveDir.y != 0)
        {
            lastVerticalVector = moveDir.y;
        }
    }
    
    void UpdateFacingDirection()
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

    private void Move()
    {
        if (isMovementLocked)
        {
            rigidBody.linearVelocity = Vector2.zero;
            return;
        }

        if (playerController != null && playerController.currentStats != null)
        {
            moveSpeed = playerController.currentStats.moveSpeed;
        }

        rigidBody.linearVelocity = new Vector2(moveDir.x * moveSpeed, moveDir.y * moveSpeed);
    }
}