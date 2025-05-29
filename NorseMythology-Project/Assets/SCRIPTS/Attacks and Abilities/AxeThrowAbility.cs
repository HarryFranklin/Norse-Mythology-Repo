using UnityEngine;

[CreateAssetMenu(fileName = "AxeThrowAbility", menuName = "Abilities/Attack/Axe Throw")]
public class AxeThrowAbility : AttackAbility
{
    [Header("Axe Throw Settings")]
    public GameObject axePrefab;
    public float axeSpeed = 12f;
    public float axeRange = 8f;
    public bool returnsToPlayer = true;
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 720f; // degrees per second
    
    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        // Get direction based on player movement or last movement
        Vector2 throwDirection = GetThrowDirection(playerMovement);

        if (throwDirection.x < 0)
        {
            // If facing left, flip the sprite
            playerMovement.facingDirection = Vector2.left;
        }
        else if (throwDirection.x > 0)
        {
            // If facing right, ensure facing direction is right
            playerMovement.facingDirection = Vector2.right;
        }
        
        if (throwDirection == Vector2.zero)
        {
            Debug.Log("No movement direction for axe throw!");
            return;
        }
        
        // Create the axe projectile
        GameObject axe = Instantiate(axePrefab, player.transform.position, Quaternion.identity);
        
        // Flip the axe sprite if facing left
        if (throwDirection.x < 0)
        {
            SpriteRenderer axeSprite = axe.GetComponent<SpriteRenderer>();
            if (axeSprite != null)
            {
                axeSprite.flipX = true;
            }
        }
        
        // Set up the projectile component
        Projectile axeProjectile = axe.GetComponent<Projectile>();
        if (axeProjectile == null)
            axeProjectile = axe.AddComponent<Projectile>();
        
        // Add rotation component for spinning
        AxeRotation axeRotation = axe.GetComponent<AxeRotation>();
        if (axeRotation == null)
            axeRotation = axe.AddComponent<AxeRotation>();
        
        axeRotation.rotationSpeed = rotationSpeed;
            
        // Calculate damage based on player stats and ability damage
        //float totalDamage = damage + (player.currentStats.attackDamage * 0.5f); -- Override for now because want to use base damage
        float totalDamage = 10;
        
        axeProjectile.Initialise(throwDirection, axeSpeed, axeRange, totalDamage, returnsToPlayer, player.transform);
        
        Debug.Log($"Axe thrown in direction: {throwDirection}");
    }
    
    private Vector2 GetThrowDirection(PlayerMovement playerMovement)
    {
        // Use the last movement direction instead of facing direction
        // This will give us the full diagonal movement direction
        Vector2 lastMovement = playerMovement.lastMovementDirection;
        
        // If we have a last movement direction, use it
        if (lastMovement != Vector2.zero)
        {
            return lastMovement.normalized;
        }
        
        // Fallback to facing direction if no movement has occurred
        return playerMovement.facingDirection;
    }
}