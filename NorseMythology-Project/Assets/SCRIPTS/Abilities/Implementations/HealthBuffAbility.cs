using UnityEngine;

[CreateAssetMenu(fileName = "New Health Buff Ability", menuName = "Abilities/Health Buff")]
public class HealthBuffAbility : Ability
{
    [Header("Health Buff Visual Effects")]
    public GameObject buffEffectPrefab; // Optional visual effect for activation
    public Color flashColor = Color.green;
    
    private void Awake()
    {
        // Set up the ability with code-defined values
        useCodeDefinedMatrix = true;
        activationMode = ActivationMode.Instant;
    }

    public override void InitialiseFromCodeMatrix()
    {
        // Initialise ability name and description
        abilityName = "Vitality Boost";
        description = "Instantly grants temporary health that decays back to original max health when lost.";
        
        // Level 1: 25 health, 20s cooldown, 1 stack max
        SetLevelData(1, 
            cooldown: 20f, 
            damage: 0f,  // Not used for this ability
            duration: 0f, // Not used - instant effect
            radius: 0f,  // Not used for this ability
            speed: 0f,   // Not used for this ability
            distance: 0f, // Not used for this ability
            specialValue1: 25f, // Health increase amount
            specialValue2: 0f,  // Reserved for future use
            specialValue3: 0f,  // Reserved for future use
            maxStacks: 1, 
            stackRegenTime: 22f);
        
        // Level 2: 35 health, 18s cooldown, 2 stacks max
        SetLevelData(2, 
            cooldown: 18f, 
            specialValue1: 35f, 
            maxStacks: 2, 
            stackRegenTime: 20f);
        
        // Level 3: 45 health, 16s cooldown, 2 stacks max
        SetLevelData(3, 
            cooldown: 16f, 
            specialValue1: 45f, 
            maxStacks: 2, 
            stackRegenTime: 18f);
        
        // Level 4: 55 health, 14s cooldown, 3 stacks max
        SetLevelData(4, 
            cooldown: 14f, 
            specialValue1: 55f, 
            maxStacks: 3, 
            stackRegenTime: 16f);
        
        // Level 5: 70 health, 12s cooldown, 3 stacks max
        SetLevelData(5, 
            cooldown: 12f, 
            specialValue1: 70f, 
            maxStacks: 3, 
            stackRegenTime: 14f);
    }

    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        if (!CanActivate(player))
            return;

        // Apply instant health boost
        ApplyInstantHealthBoost(player);
        
        // Remove a stack since we used the ability
        RemoveStack();
        
        Debug.Log($"{abilityName} activated! Instant health boost of {StackedSpecialValue1}!");
    }

    private void ApplyInstantHealthBoost(Player player)
    {
        float healthIncrease = StackedSpecialValue1;
        
        // Store original max health for tracking
        float originalMaxHealth = player.maxHealth;
        
        // Increase both max health and current health instantly
        player.maxHealth += healthIncrease;
        player.currentHealth += healthIncrease;
        
        // Update current stats if they exist
        if (player.currentStats != null)
        {
            player.currentStats.maxHealth = player.maxHealth;
        }
        
        // Apply visual flash effect
        if (buffEffectPrefab != null)
        {
            GameObject flashEffect = Instantiate(buffEffectPrefab, player.transform.position, Quaternion.identity);
            Destroy(flashEffect, 1f); // Clean up after 1 second
        }
        
        // Brief color flash
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            player.StartCoroutine(FlashEffect(playerSprite));
        }
        
        // Set up health tracking to revert max health when this bonus health is lost
        player.StartCoroutine(TrackTemporaryHealth(player, originalMaxHealth, healthIncrease));
        
        // Update UI
        if (player.healthXPUIManager != null)
        {
            player.healthXPUIManager.OnHealthChanged();
        }
        
        Debug.Log($"Instant health boost applied: +{healthIncrease} health (Max: {player.maxHealth}, Current: {player.currentHealth})");
    }

    private System.Collections.IEnumerator FlashEffect(SpriteRenderer sprite)
    {
        Color originalColor = sprite.color;
        sprite.color = flashColor;
        yield return new WaitForSeconds(0.2f);
        sprite.color = originalColor;
    }

    private System.Collections.IEnumerator TrackTemporaryHealth(Player player, float originalMaxHealth, float bonusHealth)
    {
        float thresholdHealth = originalMaxHealth;
        
        // Keep tracking until the player's health drops to or below their original max health
        while (player != null && !player.isDead && player.currentHealth > thresholdHealth)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
        
        // Once health drops to original max or below, revert the max health
        if (player != null && !player.isDead)
        {
            player.maxHealth = originalMaxHealth;
            
            // Update current stats if they exist
            if (player.currentStats != null)
            {
                player.currentStats.maxHealth = originalMaxHealth;
            }
            
            // Ensure current health doesn't exceed the reverted max health
            if (player.currentHealth > player.maxHealth)
            {
                player.currentHealth = player.maxHealth;
            }
            
            // Update UI
            if (player.healthXPUIManager != null)
            {
                player.healthXPUIManager.OnHealthChanged();
            }
            
            Debug.Log($"Temporary health expired. Max health reverted to {originalMaxHealth}");
        }
    }

    public override bool CanActivate(Player player)
    {
        // Use the base CanActivate check first
        if (!base.CanActivate(player))
            return false;
        
        // Allow activation even at full health since this provides bonus health
        return true;
    }
}