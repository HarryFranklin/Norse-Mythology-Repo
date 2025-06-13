using UnityEngine;

[CreateAssetMenu(fileName = "HammerThrowAbility", menuName = "Abilities/HammerThrow")]
public class HammerThrowAbility : Ability
{
    [Header("Hammer Throw Settings")]
    [SerializeField] private GameObject hammerPrefab;
    [SerializeField] private bool returnsToPlayer = true;
    [SerializeField] private float rotationSpeed = 720f;

    private void Awake()
    {
        abilityName = "Hammer Throw";
        description = "Throw a spinning hammer that deals damage and returns to you.";
        activationMode = ActivationMode.ClickToTarget;
        showTargetingLine = true;
        targetingLineColor = Color.red;
        maxStacks = 1;
    }
    
    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        // For instant activation mode - use movement direction
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
        // For click-to-target activation mode
        ThrowHammer(player, playerMovement, targetDirection);
    }
    
    private void ThrowHammer(Player player, PlayerMovement playerMovement, Vector2 throwDirection)
    {
        // Update player facing direction
        if (throwDirection.x < 0)
            playerMovement.facingDirection = Vector2.left;
        else if (throwDirection.x > 0)
            playerMovement.facingDirection = Vector2.right;
        
        // Create the hammer projectile
        GameObject hammer = Instantiate(hammerPrefab, player.transform.position, Quaternion.identity);
        
        // Flip the hammer sprite if facing left
        if (throwDirection.x < 0)
        {
            SpriteRenderer hammerSprite = hammer.GetComponent<SpriteRenderer>();
            if (hammerSprite != null)
                hammerSprite.flipX = true;
        }
        
        // Set up the projectile component
        Projectile hammerProjectile = hammer.GetComponent<Projectile>();
        if (hammerProjectile == null)
            hammerProjectile = hammer.AddComponent<Projectile>();
        
        // Add rotation component for spinning
        SpriteRotation rotatorScript = hammer.GetComponent<SpriteRotation>();
        if (rotatorScript == null)
            rotatorScript = hammer.AddComponent<SpriteRotation>();
        
        // Use specialValue1 for rotation speed multiplier if set, otherwise use default
        float finalRotationSpeed = CurrentSpecialValue1 > 0 ? rotationSpeed * CurrentSpecialValue1 : rotationSpeed;
        rotatorScript.rotationSpeed = finalRotationSpeed;
        
        // Initialize projectile with current level values
        hammerProjectile.Initialise(
            throwDirection, 
            CurrentSpeed,           // Speed
            CurrentDistance,        // Range  
            CurrentDamage,          // Damage
            returnsToPlayer, 
            player.transform
        );
        
        Debug.Log($"Hammer Throw Level {CurrentLevel}: {CurrentDamage} damage, {CurrentSpeed} speed, {CurrentDistance} range");
    }
    
    private Vector2 GetMovementDirection(PlayerMovement playerMovement)
    {
        Vector2 lastMovement = playerMovement.lastMovementDirection;
        
        if (lastMovement != Vector2.zero)
            return lastMovement.normalized;
        
        return playerMovement.facingDirection;
    }
    
    public override void EnterTargetingMode(Player player)
    {
        Debug.Log($"Hammer Throw targeting: Level {CurrentLevel} - {CurrentDistance}u range");
        maxTargetingRange = CurrentDistance;
    }
    
    public override void ExitTargetingMode(Player player)
    {
        Debug.Log("Exit hammer throw targeting mode");
    }
}