using UnityEngine;

public enum PickupType
{
    Experience,
    Health
}

public abstract class Pickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public PickupType pickupType;
    public float value = 10f; // Amount to give (XP, health, etc.)
    
    [Header("Movement Settings")]
    public bool canChasePlayer = true;
    public float attractionDistance = 1f;
    public float moveSpeed = 5f;
    public float acceleration = 3f;
    public float deceleration = 4f;
    
    [Header("Pickup Distance")]
    public float pickupDistance = 0.5f;
    
    [Header("Audio/Visual Effects")]
    public AudioClip pickupSound;
    public GameObject pickupEffect; // Particle effect or animation
    
    protected Transform player;
    protected bool isMovingToPlayer = false;
    protected Vector2 velocity = Vector2.zero;
    protected float playerMoveSpeed = 5f;
    
    protected virtual void Start()
    {
        SetupCollider();
    }
    
    protected virtual void SetupCollider()
    {
        // Get or add a CircleCollider2D
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        // Set as trigger
        circleCollider.isTrigger = true;

        // Set radius based on attraction distance (or pickup distance if not chasing)
        float baseRadius = canChasePlayer ? attractionDistance : pickupDistance;
        circleCollider.radius = baseRadius + 0.2f; // Small buffer
        
        Debug.Log($"Pickup collider radius set to: {circleCollider.radius}");
    }
    
    protected virtual void Update()
    {
        if (player == null || !canChasePlayer) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Handle movement logic
        if (distanceToPlayer <= attractionDistance)
        {
            if (!isMovingToPlayer)
            {
                isMovingToPlayer = true;
            }
            MoveTowardPlayer();
        }
        else
        {
            if (isMovingToPlayer)
            {
                DeceleratePickup();
            }
        }
        
        // Check if close enough to be picked up
        if (distanceToPlayer <= pickupDistance)
        {
            CollectPickup();
        }
    }
    
    protected virtual void MoveTowardPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        
        // Accelerate toward the player
        velocity += direction * acceleration * Time.deltaTime;
        
        // Clamp velocity to max speed
        if (velocity.magnitude > moveSpeed)
        {
            velocity = velocity.normalized * moveSpeed;
        }
        
        // Move the pickup
        transform.position += (Vector3)velocity * Time.deltaTime;
    }
    
    protected virtual void DeceleratePickup()
    {
        float currentSpeed = velocity.magnitude;
        if (currentSpeed > 0.1f)
        {
            Vector2 decelerationForce = -velocity.normalized * deceleration * Time.deltaTime;
            velocity += decelerationForce;
            
            if (Vector2.Dot(velocity.normalized, decelerationForce.normalized) > 0)
            {
                velocity = Vector2.zero;
                isMovingToPlayer = false;
            }
        }
        else
        {
            velocity = Vector2.zero;
            isMovingToPlayer = false;
        }
        
        transform.position += (Vector3)velocity * Time.deltaTime;
    }
    
    protected virtual void CollectPickup()
    {
        Player playerScript = player.GetComponent<Player>();
        if (playerScript != null)
        {
            // Apply the pickup effect
            ApplyPickupEffect(playerScript);
            
            // Play sound and visual effects
            PlayPickupEffects();
            
            // Log pickup
            OnPickupCollected();
        }
        
        // Destroy the pickup
        Destroy(gameObject);
    }
    
    // Abstract method that derived classes must implement
    protected abstract void ApplyPickupEffect(Player player);
    
    // Virtual methods that can be overridden
    protected virtual void PlayPickupEffects()
    {
        // Play sound effect
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Spawn visual effect
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
    }
    
    protected virtual void OnPickupCollected()
    {
        Debug.Log($"Player collected {pickupType} worth {value}");
    }
    
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (player == null)
            {
                player = other.transform;
                
                // Adjust speed based on player speed if chasing is enabled
                if (canChasePlayer)
                {
                    Player playerScript = other.GetComponent<Player>();
                    if (playerScript != null)
                    {
                        playerMoveSpeed = playerScript.moveSpeed;
                        moveSpeed = playerMoveSpeed * 1.25f;
                    }
                }
            }
            
            // Check for immediate pickup
            float distanceToPlayer = Vector2.Distance(transform.position, other.transform.position);
            if (distanceToPlayer <= pickupDistance)
            {
                CollectPickup();
            }
        }
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        if (canChasePlayer)
        {
            // Draw attraction distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attractionDistance); // Yellow for attraction distance
        }
        
        // Draw pickup distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupDistance); // Green for pickup distance
    }
}