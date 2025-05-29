using UnityEngine;

[CreateAssetMenu(fileName = "HammerThrowAbility", menuName = "Abilities/Attack/Hammer Throw")]
public class HammerThrowAbility : AttackAbility
{
    [Header("Hammer Throw Settings")]
    public GameObject hammerPrefab;
    public float hammerSpeed = 12f;
    public float hammerRange = 8f;
    public bool returnsToPlayer = true;
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 720f;
    
    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        // This is called for instant activation mode
        // Get direction based on player movement or facing direction
        Vector2 throwDirection = GetMovementDirection(playerMovement);
        
        if (throwDirection == Vector2.zero)
        {
            Debug.Log("No movement direction for hammer throw!");
            return;
        }
        
        ThrowHammer(player, playerMovement, throwDirection);
    }
    
    public override void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        // This is called for click-to-target activation mode
        ThrowHammer(player, playerMovement, targetDirection);
    }
    
    private void ThrowHammer(Player player, PlayerMovement playerMovement, Vector2 throwDirection)
    {
        // Update player facing direction
        if (throwDirection.x < 0)
        {
            playerMovement.facingDirection = Vector2.left;
        }
        else if (throwDirection.x > 0)
        {
            playerMovement.facingDirection = Vector2.right;
        }
        
        // Create the hammer projectile
        GameObject hammer = Instantiate(hammerPrefab, player.transform.position, Quaternion.identity);
        
        // Flip the hammer sprite if facing left
        if (throwDirection.x < 0)
        {
            SpriteRenderer hammerSprite = hammer.GetComponent<SpriteRenderer>();
            if (hammerSprite != null)
            {
                hammerSprite.flipX = true;
            }
        }
        
        // Set up the projectile component
        Projectile hammerProjectile = hammer.GetComponent<Projectile>();
        if (hammerProjectile == null)
            hammerProjectile = hammer.AddComponent<Projectile>();
        
        // Add rotation component for spinning
        SpriteRotation rotatorScript = hammer.GetComponent<SpriteRotation>();
        if (rotatorScript == null)
            rotatorScript = hammer.AddComponent<SpriteRotation>();
        
        rotatorScript.rotationSpeed = rotationSpeed;
        
        // Calculate damage
        float totalDamage = 10; // Using your override value
        
        hammerProjectile.Initialise(throwDirection, hammerSpeed, hammerRange, totalDamage, returnsToPlayer, player.transform);
        
        Debug.Log($"Hammer thrown in direction: {throwDirection}");
    }
    
    private Vector2 GetMovementDirection(PlayerMovement playerMovement)
    {
        Vector2 lastMovement = playerMovement.lastMovementDirection;
        
        if (lastMovement != Vector2.zero)
        {
            return lastMovement.normalized;
        }
        
        return playerMovement.facingDirection;
    }
    
    public override void EnterTargetingMode(Player player)
    {
        // Optional: Add any special behavior when entering targeting mode
        // For example, you could play a sound effect or show a UI indicator
    }
    
    public override void ExitTargetingMode(Player player)
    {
        // Optional: Add any cleanup when exiting targeting mode
    }
}