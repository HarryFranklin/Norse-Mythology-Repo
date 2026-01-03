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
        
        maxTargetingRange = GetStatsForLevel(1).distance;
    }

    public override void InitialiseFromCodeMatrix()
    {
        // Level 1: Basic throw
        SetLevelData(1, cooldown: 5f, damage: 4f, duration: 0f, radius: 0f, speed: 8f, distance: 6f, specialValue1: 1f, specialValue2: 0f, specialValue3: 0f, maxStacks: 1, stackRegenTime: 4f);
        SetLevelData(2, cooldown: 4.25f, damage: 7.5f, duration: 0f, radius: 0f, speed: 9f, distance: 7f, specialValue1: 1.2f, specialValue2: 0f, specialValue3: 0f, maxStacks: 1, stackRegenTime: 3.5f);
        SetLevelData(3, cooldown: 3.5f, damage: 10f, duration: 0f, radius: 0f, speed: 10f, distance: 8f, specialValue1: 1.4f, specialValue2: 0f, specialValue3: 0f, maxStacks: 2, stackRegenTime: 3f);
        SetLevelData(4, cooldown: 2.75f, damage: 12.5f, duration: 0f, radius: 0f, speed: 11f, distance: 9f, specialValue1: 1.6f, specialValue2: 0f, specialValue3: 0f, maxStacks: 2, stackRegenTime: 2.5f);
        SetLevelData(5, cooldown: 2f, damage: 14f, duration: 0f, radius: 0f, speed: 12f, distance: 10f, specialValue1: 1.8f, specialValue2: 0f, specialValue3: 0f, maxStacks: 3, stackRegenTime: 2f);
    }

    public override bool CanActivate(Player player)
    {
        return player != null && !player.isDead && CurrentStacks > 0;
    }
    
    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        if (player == null || playerMovement == null) return;
        RemoveStack();
        Vector2 throwDirection = GetMovementDirection(playerMovement);
        
        if (throwDirection == Vector2.zero) return;
        
        ThrowHammer(player, playerMovement, throwDirection);
    }
    
    public override void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        if (player == null || playerMovement == null) return;
        RemoveStack();
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
        
        // Use stacked specialValue1 for rotation speed multiplier
        float finalRotationSpeed = StackedSpecialValue1 > 0 ? rotationSpeed * StackedSpecialValue1 : rotationSpeed;
        rotatorScript.rotationSpeed = finalRotationSpeed;
        
        // Enable time freeze mitigation for the rotation
        rotatorScript.useAbilityTimeScale = true;
        
        // Initialise projectile with stacked level values
        // Pass 'true' at the end to enable time freeze mitigation for movement
        hammerProjectile.Initialise(
            throwDirection, 
            StackedSpeed,           
            StackedDistance,          
            StackedDamage,          
            returnsToPlayer, 
            player.transform,
            true // <--- useTimeScale = true
        );
    }
    
    private Vector2 GetMovementDirection(PlayerMovement playerMovement)
    {
        Vector2 lastMovement = playerMovement.lastMovementDirection;
        if (lastMovement != Vector2.zero) return lastMovement.normalized;
        return playerMovement.facingDirection;
    }
    
    public override void EnterTargetingMode(Player player)
    {
        maxTargetingRange = StackedDistance;
    }
    
    public override void ExitTargetingMode(Player player) {}
}