using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerStats baseStats; // ScriptableObject reference
    public GameManager gameManager;
    
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
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void InitialisePlayer()
    {
        // Check if currentStats is null and handle appropriately
        if (currentStats == null)
        {
            if (baseStats != null)
            {
                // Create runtime copy from base stats if available
                currentStats = ScriptableObject.CreateInstance<PlayerStats>();
                // Copy all values from baseStats to currentStats
                CopyStatsFromBase();
            }
            else
            {
                Debug.LogError("PlayerController: Both currentStats and baseStats are null! Please assign baseStats in the inspector.");
                return;
            }
        }
        
        currentHealth = currentStats.maxHealth;
        StartCoroutine(HealthRegeneration());
        
        // Update UI if available (it might not be ready yet due to initialization order)
        if (healthXPUIManager != null)
        {
            healthXPUIManager.OnHealthChanged();
            healthXPUIManager.OnXPChanged();
        }
    }
    
    private void CopyStatsFromBase()
    {
        if (baseStats == null || currentStats == null) return;
        
        currentStats.level = baseStats.level;
        currentStats.experience = baseStats.experience;
        currentStats.experienceToNextLevel = baseStats.experienceToNextLevel;
        currentStats.moveSpeed = baseStats.moveSpeed;
        currentStats.maxHealth = baseStats.maxHealth;
        currentStats.healthRegen = baseStats.healthRegen;
        currentStats.healthRegenDelay = baseStats.healthRegenDelay;
        currentStats.meleeRange = baseStats.meleeRange;
        currentStats.attackDamage = baseStats.attackDamage;
        currentStats.attackSpeed = baseStats.attackSpeed;
        currentStats.projectileSpeed = baseStats.projectileSpeed;
        currentStats.projectileRange = baseStats.projectileRange;
        currentStats.abilityCooldownReduction = baseStats.abilityCooldownReduction;
    }
    
    private IEnumerator HealthRegeneration()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds for more responsive regeneration
            
            // Add null check for currentStats
            if (currentStats == null) continue;
            
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
        if (isDead || currentStats == null) return;
        
        currentHealth = Mathf.Min(currentStats.maxHealth, currentHealth + amount);
        
        // Update UI when health changes
        if (healthXPUIManager != null)
            healthXPUIManager.OnHealthChanged();
    }
    
    public void GainExperience(float xp)
    {
        if (isDead || currentStats == null) return;
        
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
        if (currentStats == null) return;
        
        float totalXP = currentStats.experience + pendingExperience;
        if (totalXP >= currentStats.experienceToNextLevel)
        {
            isLevelUpPending = true;
        }
    }

    
    private void LevelUp()
    {
        if (currentStats == null) return;
        
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
    
    public int ProcessPendingExperienceAndReturnLevelUps()
    {
        // Returns the number of level-ups that occurreds
        if (pendingExperience <= 0 || currentStats == null) return 0;
        
        // Add all pending XP to current stats
        currentStats.experience += pendingExperience;
        pendingExperience = 0f;
        
        // Count and process all level-ups that are now available
        int levelUpsGained = 0;
        while (currentStats.experience >= currentStats.experienceToNextLevel)
        {
            LevelUp();
            levelUpsGained++;
        }
        
        // Reset the pending flag
        isLevelUpPending = false;
        
        // Update UI after processing all level-ups
        if (healthXPUIManager != null)
        {
            healthXPUIManager.OnHealthChanged(); // Health may have changed due to level-ups
            healthXPUIManager.OnXPChanged();
        }
        
        Debug.Log($"Processed pending XP and gained {levelUpsGained} levels");
        return levelUpsGained;
    }

    public void ProcessPendingExperience()
    {
        ProcessPendingExperienceAndReturnLevelUps();
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player died!");

        if (gameManager != null)
        {
            gameManager.OnPlayerDied();
        }
        else
        {
            // Fallback if GameManager instance is not available
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
        }
    }
    
    // Method to save current stats back to base stats (for persistence between levels)
    public void SaveStatsToBase()
    {
        if (baseStats == null || currentStats == null) return;
        
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
        if (currentStats == null) return 0f;
        return currentStats.experience + pendingExperience;
    }
    
    public float GetCurrentExperience()
    {
        if (currentStats == null) return 0f;
        return currentStats.experience;
    }
    
    public float GetPendingExperience()
    {
        return pendingExperience;
    }
}