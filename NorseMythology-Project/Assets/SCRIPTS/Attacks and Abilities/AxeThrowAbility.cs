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
        
        // Add rotation component for spinning
        AxeRotation axeRotation = axe.GetComponent<AxeRotation>();
        if (axeRotation == null)
            axeRotation = axe.AddComponent<AxeRotation>();
        
        axeRotation.rotationSpeed = rotationSpeed;
            
        // Calculate damage based on player stats and ability damage
        float totalDamage = damage + (player.currentStats.attackDamage * 0.5f);
        
        axeProjectile.Initialise(throwDirection, axeSpeed, axeRange, totalDamage, returnsToPlayer, player.transform);
        
        Debug.Log($"Axe thrown in direction: {throwDirection}");
    }
    
    private Vector2 GetThrowDirection(PlayerMovement playerMovement)
    {
        // Use the facing direction from PlayerMovement
        return playerMovement.facingDirection;
    }
}