using UnityEngine;

[CreateAssetMenu(fileName = "AxeThrowAbility", menuName = "Abilities/Attack/Axe Throw")]
public class AxeThrowAbility : AttackAbility
{
    [Header("Axe Throw Settings")]
    public GameObject axePrefab;
    public float axeSpeed = 12f;
    public float axeRange = 8f;
    public bool returnsToPlayer = true;
    
    public override void Activate(PlayerController player, PlayerMovement playerMovement)
    {
        // Get direction based on player movement or last movement
        Vector2 throwDirection = GetThrowDirection(playerMovement);
        
        if (throwDirection == Vector2.zero)
        {
            Debug.Log("No movement direction for axe throw!");
            return;
        }
        
        // Create the axe projectile
        GameObject axe = Instantiate(axePrefab, player.transform.position, Quaternion.identity);
        
        // Set up the projectile component
        Projectile axeProjectile = axe.GetComponent<Projectile>();
        if (axeProjectile == null)
            axeProjectile = axe.AddComponent<Projectile>();
            
        // Calculate damage based on player stats and ability damage
        float totalDamage = damage + (player.currentStats.attackDamage * 0.5f);
        
        axeProjectile.Initialize(throwDirection, axeSpeed, axeRange, totalDamage, returnsToPlayer, player.transform);
        
        Debug.Log($"Axe thrown in direction: {throwDirection}");
    }
    
    private Vector2 GetThrowDirection(PlayerMovement playerMovement)
    {
        // First try current movement direction
        if (playerMovement.moveDir != Vector2.zero)
        {
            return playerMovement.moveDir.normalized;
        }
        
        // If not moving, use last movement direction
        Vector2 lastDirection = new Vector2(playerMovement.lastHorizontalVector, playerMovement.lastVerticalVector);
        if (lastDirection != Vector2.zero)
        {
            return lastDirection.normalized;
        }
        
        // Default to right if no movement recorded
        return Vector2.right;
    }
}