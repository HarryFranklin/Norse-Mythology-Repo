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
        
        // Set up targeting range based on level 1 distance
        maxTargetingRange = GetStatsForLevel(1).distance;
    }

    protected override void InitialiseFromCodeMatrix()
    {
        // Define hammer throw ability values via code matrix
        // Level, cooldown, damage, duration, radius, speed(projectile speed), distance(range), specialValue1(rotation multiplier), specialValue2, specialValue3, maxStacks, stackRegenTime
        
        // Level 1: Basic throw
        SetLevelData(1, cooldown: 5f, damage: 4f, duration: 0f, radius: 0f, speed: 8f, distance: 6f, specialValue1: 1f, specialValue2: 0f, specialValue3: 0f, maxStacks: 1, stackRegenTime: 4f);
        
        // Level 2: Improved damage and speed
        SetLevelData(2, cooldown: 4.25f, damage: 7.5f, duration: 0f, radius: 0f, speed: 9f, distance: 7f, specialValue1: 1.2f, specialValue2: 0f, specialValue3: 0f, maxStacks: 1, stackRegenTime: 3.5f);
        
        // Level 3: Better range and damage
        SetLevelData(3, cooldown: 3.5f, damage: 10f, duration: 0f, radius: 0f, speed: 10f, distance: 8f, specialValue1: 1.4f, specialValue2: 0f, specialValue3: 0f, maxStacks: 2, stackRegenTime: 3f);
        
        // Level 4: Major improvements with 2 charges
        SetLevelData(4, cooldown: 2.75f, damage: 12.5f, duration: 0f, radius: 0f, speed: 11f, distance: 9f, specialValue1: 1.6f, specialValue2: 0f, specialValue3: 0f, maxStacks: 2, stackRegenTime: 2.5f);
        
        // Level 5: Maximum power with fast recharge
        SetLevelData(5, cooldown: 2f, damage: 14f, duration: 0f, radius: 0f, speed: 12f, distance: 10f, specialValue1: 1.8f, specialValue2: 0f, specialValue3: 0f, maxStacks: 3, stackRegenTime: 2f);
        
        Debug.Log($"HammerThrowAbility initialised from code matrix. Level 1: {StackedDamage} damage, {StackedDistance}u range");
    }

    public override bool CanActivate(Player player)
    {
        bool canActivate = player != null && !player.isDead && CurrentStacks > 0;
        
        if (!canActivate)
        {
            Debug.Log($"Hammer Throw cannot activate - Player null: {player == null}, Dead: {player?.isDead}, Stacks: {CurrentStacks}/{MaxStacksAtCurrentLevel}");
        }
        
        return canActivate;
    }
    
    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        if (player == null || playerMovement == null) return;
        
        // Use a stack
        RemoveStack();
        
        // For instant activation mode - use movement direction
        Vector2 throwDirection = GetMovementDirection(playerMovement);
        
        if (throwDirection == Vector2.zero)
        {
            Debug.Log("No movement direction for hammer throw!");
            return;
        }
        
        ThrowHammer(player, playerMovement, throwDirection);
        
        Debug.Log($"Hammer Throw activated instantly! Level {CurrentLevel} (Stack {AbilityStacks}): {StackedDamage} damage, {StackedDistance}u range, {CurrentStacks}/{MaxStacksAtCurrentLevel} charges remaining");
    }
    
    public override void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        if (player == null || playerMovement == null) return;
        
        // Use a stack
        RemoveStack();
        
        // For click-to-target activation mode
        ThrowHammer(player, playerMovement, targetDirection);
        
        Debug.Log($"Hammer Throw activated at target! Level {CurrentLevel} (Stack {AbilityStacks}): {StackedDamage} damage, {StackedDistance}u range, {CurrentStacks}/{MaxStacksAtCurrentLevel} charges remaining");
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
        
        // Initialize projectile with stacked level values
        hammerProjectile.Initialise(
            throwDirection, 
            StackedSpeed,           // Speed
            StackedDistance,        // Range  
            StackedDamage,          // Damage
            returnsToPlayer, 
            player.transform
        );
        
        Debug.Log($"Hammer Throw Level {CurrentLevel}: {StackedDamage} damage, {StackedSpeed} speed, {StackedDistance} range");
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
        Debug.Log($"Hammer Throw targeting: Level {CurrentLevel} (Stack {AbilityStacks}) - {StackedDistance}u range, {CurrentStacks}/{MaxStacksAtCurrentLevel} charges");
        maxTargetingRange = StackedDistance;
    }
    
    public override void ExitTargetingMode(Player player)
    {
        Debug.Log("Exit hammer throw targeting mode");
    }
}