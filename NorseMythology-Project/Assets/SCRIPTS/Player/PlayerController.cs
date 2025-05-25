using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerStats baseStats; // ScriptableObject reference
    
    [Header("Runtime Stats")]
    public PlayerStats currentStats; // Runtime copy
    public float currentHealth;
    public bool isDead = false;
    
    [Header("Level Up System")]
    public bool isLevelUpPending = false;
    private float pendingExperience = 0f; // XP gained but not yet processed for level-ups
    
    [Header("UI References")]
    public HealthXPUIManager healthXPUIManager;
    
    [Header("Health Regeneration")]
    private float lastDamageTime = 0f; // Time when player last took damage
    
    private void Start()
    {
        InitialisePlayer();
    }

    private void InitialisePlayer()
    {
        // Create runtime copy of base stats
        // currentStats = baseStats.CreateRuntimeCopy(); // Now in GameManager
        currentHealth = currentStats.maxHealth;
        StartCoroutine(HealthRegeneration());
        
        if (healthXPUIManager != null)
        {
            healthXPUIManager.OnHealthChanged();
            healthXPUIManager.OnXPChanged();
        }
    }
    
    private IEnumerator HealthRegeneration()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds for more responsive regeneration
            
            // Only regenerate if enough time has passed since last damage and health is not full
            if (currentHealth < currentStats.maxHealth && 
                Time.time >= lastDamageTime + currentStats.healthRegenDelay)
            {
                currentHealth = Mathf.Min(currentStats.maxHealth, currentHealth + (currentStats.healthRegen * 0.1f));
                
                // Update UI when health regenerates
                if (healthXPUIManager != null)
                    healthXPUIManager.OnHealthChanged();
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        lastDamageTime = Time.time; // Reset the damage timer
        
        if (currentHealth <= 0)
        {
            Die();
        }
        
        // Update UI when health changes
        if (healthXPUIManager != null)
            healthXPUIManager.OnHealthChanged();
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentStats.maxHealth, currentHealth + amount);
        
        // Update UI when health changes
        if (healthXPUIManager != null)
            healthXPUIManager.OnHealthChanged();
    }
    
    public void GainExperience(float xp)
    {
        if (isDead) return;
        
        // Add XP to pending experience instead of directly to current stats
        pendingExperience += xp;
        
        // Check if we'll have enough XP for a level up when processed
        CheckForPendingLevelUp();
        
        // Update UI when XP changes
        if (healthXPUIManager != null)
            healthXPUIManager.OnXPChanged();
    }
    
    private void CheckForPendingLevelUp()
    {
        float totalXP = currentStats.experience + pendingExperience;
        if (totalXP >= currentStats.experienceToNextLevel)
        {
            isLevelUpPending = true;
        }
    }
    
    // Call this method at the end of a level/stage to process all pending XP and level-ups
    public void ProcessPendingExperience()
    {
        if (pendingExperience <= 0) return;
        
        // Add all pending XP to current stats
        currentStats.experience += pendingExperience;
        pendingExperience = 0f;
        
        // Process all level-ups that are now available
        while (currentStats.experience >= currentStats.experienceToNextLevel)
        {
            LevelUp();
        }
        
        // Reset the pending flag
        isLevelUpPending = false;
        
        // Update UI after processing all level-ups
        if (healthXPUIManager != null)
        {
            healthXPUIManager.OnHealthChanged(); // Health may have changed due to level-ups
            healthXPUIManager.OnXPChanged();
        }
    }
    
    private void LevelUp()
    {
        currentStats.experience -= currentStats.experienceToNextLevel;
        currentStats.level++;
        
        // Increase stats on level up (customise as needed)
        currentStats.maxHealth += 10f;
        currentStats.attackDamage += 2f;
        currentStats.experienceToNextLevel = Mathf.Floor(currentStats.experienceToNextLevel * 1.2f);
        
        // Heal to full on level up
        currentHealth = currentStats.maxHealth;
        
        Debug.Log($"Level Up! Now level {currentStats.level}");
    }
    
    private void Die()
    {
        isDead = true;
        Debug.Log("Player died!");
        // Handle death logic (restart level, game over screen, etc.)
    }
    
    // Method to save current stats back to base stats (for persistence between levels)
    public void SaveStatsToBase()
    {
        baseStats.level = currentStats.level;
        baseStats.experience = currentStats.experience;
        baseStats.experienceToNextLevel = currentStats.experienceToNextLevel;
        baseStats.moveSpeed = currentStats.moveSpeed;
        baseStats.maxHealth = currentStats.maxHealth;
        baseStats.healthRegen = currentStats.healthRegen;
        baseStats.meleeRange = currentStats.meleeRange;
        baseStats.attackDamage = currentStats.attackDamage;
        baseStats.attackSpeed = currentStats.attackSpeed;
        baseStats.projectileSpeed = currentStats.projectileSpeed;
        baseStats.projectileRange = currentStats.projectileRange;
        baseStats.abilityCooldownReduction = currentStats.abilityCooldownReduction;
    }
    
    // Getter methods for UI display
    public float GetTotalExperience()
    {
        return currentStats.experience + pendingExperience;
    }
    
    public float GetCurrentExperience()
    {
        return currentStats.experience;
    }
    
    public float GetPendingExperience()
    {
        return pendingExperience;
    }
}