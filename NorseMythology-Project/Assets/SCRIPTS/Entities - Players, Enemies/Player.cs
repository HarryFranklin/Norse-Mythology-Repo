using UnityEngine;
using System.Collections;

public class Player : Entity
{
    [Header("Player References")]
    public PlayerStats baseStats; // ScriptableObject reference
    public GameManager gameManager;
    public Rigidbody2D rigidBody; // Reference to the player's Rigidbody2D for movement - Using in DashAbility
    
    [Header("Player Runtime Stats")]
    public PlayerStats currentStats; // Runtime copy
    
    [Header("Level Up System")]
    public bool isLevelUpPending = false;
    private float pendingExperience = 0f; // XP gained but not yet processed for level-ups
    
    [Header("UI References")]
    public HealthXPUIManager healthXPUIManager;
    
    [Header("Health Regeneration")]
    private float healthRegenDelay = 5f; // Default delay before regen starts

    private Coroutine regenCoroutine;


    protected override void Start()
    {
        base.Start(); // Call Entity's Start method
        InitialisePlayer();

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody2D>();
        }
    }

    protected override void InitialiseEntity()
    {
        InitialisePlayer();
    }

    private void InitialisePlayer()
    {
        if (currentStats == null)
        {
            if (baseStats != null)
            {
                currentStats = ScriptableObject.CreateInstance<PlayerStats>();
                CopyStatsFromBase();
            }
            else
            {
                Debug.LogError("Player: Both currentStats and baseStats are null! Please assign baseStats in the inspector.");
                return;
            }
        }

        maxHealth = currentStats.maxHealth;
        currentHealth = maxHealth;
        moveSpeed = currentStats.moveSpeed;
        damage = currentStats.attackDamage;
        healthRegenDelay = currentStats.healthRegenDelay;

        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        regenCoroutine = StartCoroutine(HealthRegeneration());

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
        float tickInterval = 0.5f;
        WaitForSeconds wait = new WaitForSeconds(tickInterval);

        while (!isDead)
        {
            yield return wait;

            if (currentStats == null || currentHealth >= maxHealth) continue;

            if (Time.time >= lastDamageTime + healthRegenDelay)
            {
                currentHealth = Mathf.Min(currentHealth + 1, maxHealth);
                PopupManager.Instance?.ShowRegen(1, transform.position);
                healthXPUIManager?.OnHealthChanged();
            }
        }
    }
        
    public override void Heal(float amount)
    {
        if (isDead || currentStats == null) return;
        
        base.Heal(amount);
    }

    protected override void OnHealed(float amount)
    {
        // Update UI when health changes
        if (healthXPUIManager != null)
            healthXPUIManager.OnHealthChanged();
    }

    protected override void OnDamageTaken(float damageAmount)
    {
        // Update UI when health changes
        if (healthXPUIManager != null)
            healthXPUIManager.OnHealthChanged();
    }
    
    public void GainExperience(float xp)
    {
        if (isDead || currentStats == null) return;
        
        // Show XP popup
        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowXP(xp, transform.position);
        }
        
        pendingExperience += xp;
        CheckForPendingLevelUp();
        
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
        
        // Update Entity's maxHealth and damage
        maxHealth = currentStats.maxHealth;
        damage = currentStats.attackDamage;
        
        // Heal to full on level up
        currentHealth = maxHealth;

        // Pop-up
        PopupManager.Instance?.ShowPopup(
            $"LEVEL {currentStats.level}!",
            transform.position + Vector3.up * 1f,
            Color.magenta,
            fontSize: 36,
            duration: 2f,
            moveDistance: 100f
        );
        
        Debug.Log($"Level Up! Now level {currentStats.level}");
    }
    
    public int ProcessPendingExperienceAndReturnLevelUps()
    {
        // Returns the number of level-ups that occurred
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

    protected override void OnDeath()
    {
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