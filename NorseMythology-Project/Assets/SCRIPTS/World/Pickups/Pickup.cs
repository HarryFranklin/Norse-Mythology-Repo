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
    
    [Header("Audio")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] 
    public float pickupVolume = 1f;
    
    [Tooltip("If true, pitch varies slightly. Recommended for XP orbs.")]
    public bool useRandomPitch = true;

    [Header("Visual Effects")]
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
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        circleCollider.isTrigger = true;
        float baseRadius = canChasePlayer ? attractionDistance : pickupDistance;
        circleCollider.radius = baseRadius + 0.2f; 
    }
    
    protected virtual void Update()
    {
        if (player == null || !canChasePlayer) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Handle movement logic
        if (distanceToPlayer <= attractionDistance)
        {
            if (!isMovingToPlayer) isMovingToPlayer = true;
            MoveTowardPlayer();
        }
        else
        {
            if (isMovingToPlayer) DeceleratePickup();
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
        velocity += direction * acceleration * Time.deltaTime;
        
        if (velocity.magnitude > moveSpeed)
            velocity = velocity.normalized * moveSpeed;
        
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
            ApplyPickupEffect(playerScript);
            PlayPickupEffects();
            OnPickupCollected();
        }
        
        Destroy(gameObject);
    }
    
    protected abstract void ApplyPickupEffect(Player player);
    
    protected virtual void PlayPickupEffects()
    {
        // --- AUDIO LOGIC ---
        if (pickupSound != null && AudioManager.Instance != null)
        {
            // Use the Manager so we get volume control and pitch variance
            AudioManager.Instance.PlaySFX(pickupSound, pickupVolume, useRandomPitch);
        }
        // Fallback if AudioManager is missing for some reason
        else if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
        }
        
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
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attractionDistance); 
        }
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupDistance); 
    }
}