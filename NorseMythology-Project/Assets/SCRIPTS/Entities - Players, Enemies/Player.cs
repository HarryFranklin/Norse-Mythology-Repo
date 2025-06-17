using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class PlayerLevelData
{
    [Header("Level Info")]
    public int level = 1;

    [Header("Health")]
    public float maxHealth = 100f;
    public float healthRegen = 1f;
    public float healthRegenDelay = 2f;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackSpeed = 1f;
    public float meleeRange = 2f;
    public float projectileSpeed = 8f;
    public float projectileRange = 10f;

    [Header("Experience")]
    public float experienceToNextLevel = 100f;

    [Header("Abilities")]
    public float abilityCooldownReduction = 0f;
}

public class Player : Entity
{
    [Header("Player References")]
    public PlayerStats baseStats;
    public GameManager gameManager;
    public Rigidbody2D rigidBody;
    
    [Header("Player Runtime Stats")]
    public PlayerStats currentStats;
    
    [Header("Level Up System")]
    public bool isLevelUpPending = false;
    private float pendingExperience = 0f;
    
    [Header("UI References")]
    public HealthXPUIManager healthXPUIManager;
    
    [Header("Player Level Data")]
    public List<PlayerLevelData> levelData = new List<PlayerLevelData>();
    
    // Code-based level data
    protected Dictionary<int, PlayerLevelData> codeLevelData = new Dictionary<int, PlayerLevelData>();
    
    // Player-specific stats
    private float healthRegen = 1f;
    private float healthRegenDelay = 5f;
    private float attackSpeed = 1f;
    private float meleeRange = 2f;
    private float projectileSpeed = 8f;
    private float projectileRange = 10f;
    private float abilityCooldownReduction = 0f;
    private float experienceToNextLevel = 100f;
    
    private Coroutine regenCoroutine;

    protected override void InitialiseFromCodeMatrix()
    {
        // Define player progression via code
        SetPlayerLevelData(1, maxHealth: 100f, healthRegen: 1f, healthRegenDelay: 5f, moveSpeed: 5f, 
            attackDamage: 8f, attackSpeed: 1f, meleeRange: 1.25f, projectileSpeed: 8f, projectileRange: 10f, 
            experienceToNextLevel: 100f, abilityCooldownReduction: 0f);
        
        SetPlayerLevelData(2, maxHealth: 110f, healthRegen: 1.1f, healthRegenDelay: 4.5f, moveSpeed: 5.2f, 
            attackDamage: 10f, attackSpeed: 1.1f, meleeRange: 1.5f, projectileSpeed: 8.5f, projectileRange: 10.5f, 
            experienceToNextLevel: 125f, abilityCooldownReduction: 0.5f);
        
        SetPlayerLevelData(3, maxHealth: 120f, healthRegen: 1.2f, healthRegenDelay: 4f, moveSpeed: 5.4f, 
            attackDamage: 12f, attackSpeed: 1.2f, meleeRange: 1.75f, projectileSpeed: 9f, projectileRange: 11f, 
            experienceToNextLevel: 150f, abilityCooldownReduction: 0.75f);
        
        SetPlayerLevelData(4, maxHealth: 130f, healthRegen: 1.3f, healthRegenDelay: 3.5f, moveSpeed: 5.6f, 
            attackDamage: 14f, attackSpeed: 1.3f, meleeRange: 2f, projectileSpeed: 9.5f, projectileRange: 11.5f, 
            experienceToNextLevel: 175f, abilityCooldownReduction: 0.9f);
        
        SetPlayerLevelData(5, maxHealth: 150f, healthRegen: 1.4f, healthRegenDelay: 3f, moveSpeed: 6f, 
            attackDamage: 16f, attackSpeed: 1.5f, meleeRange: 2.25f, projectileSpeed: 10f, projectileRange: 12f, 
            experienceToNextLevel: 200f, abilityCooldownReduction: 1f);
        
        Debug.Log($"Player initialised from code matrix. Level 1: {maxHealth} health, {damage} damage");
    }
    
    protected void SetPlayerLevelData(int level, float maxHealth, float healthRegen, float healthRegenDelay, 
        float moveSpeed, float attackDamage, float attackSpeed, float meleeRange,
        float projectileSpeed, float projectileRange, float experienceToNextLevel,
        float abilityCooldownReduction)
    {
        PlayerLevelData data = new PlayerLevelData
        {
            level = level,
            maxHealth = maxHealth,
            healthRegen = healthRegen,
            healthRegenDelay = healthRegenDelay,
            moveSpeed = moveSpeed,
            attackDamage = attackDamage,
            attackSpeed = attackSpeed,
            meleeRange = meleeRange,
            projectileSpeed = projectileSpeed,
            projectileRange = projectileRange,
            experienceToNextLevel = experienceToNextLevel,
            abilityCooldownReduction = abilityCooldownReduction
        };
        
        codeLevelData[level] = data;
    }
    
    protected override void ApplyLevelStats(int level)
    {
        PlayerLevelData data = GetPlayerLevelData(level);
        if (data == null)
        {
            Debug.LogWarning($"No player level data found for level {level}");
            return;
        }
        
        // Store previous max health for health adjustment
        float previousMaxHealth = maxHealth;
        bool wasAtFullHealth = currentHealth >= maxHealth;
        
        // Apply stats to Entity base class and player-specific variables
        maxHealth = data.maxHealth;
        moveSpeed = data.moveSpeed;
        damage = data.attackDamage;
        healthRegen = data.healthRegen;
        healthRegenDelay = data.healthRegenDelay;
        attackSpeed = data.attackSpeed;
        meleeRange = data.meleeRange;
        projectileSpeed = data.projectileSpeed;
        projectileRange = data.projectileRange;
        experienceToNextLevel = data.experienceToNextLevel;
        abilityCooldownReduction = data.abilityCooldownReduction;
        
        // Adjust current health appropriately
        if (wasAtFullHealth)
        {
            currentHealth = maxHealth; // Maintain full health
        }
        else if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth; // Cap at new max
        }
        
        // Update PlayerStats if using them alongside the level system
        if (currentStats != null)
        {
            currentStats.maxHealth = data.maxHealth;
            currentStats.healthRegen = data.healthRegen;
            currentStats.healthRegenDelay = data.healthRegenDelay;
            currentStats.moveSpeed = data.moveSpeed;
            currentStats.attackDamage = data.attackDamage;
            currentStats.attackSpeed = data.attackSpeed;
            currentStats.meleeRange = data.meleeRange;
            currentStats.projectileSpeed = data.projectileSpeed;
            currentStats.projectileRange = data.projectileRange;
            currentStats.abilityCooldownReduction = data.abilityCooldownReduction;
            currentStats.experienceToNextLevel = data.experienceToNextLevel;
        }
        
        // Restart health regeneration with new stats
        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(HealthRegeneration());
        
        // Update UI
        if (healthXPUIManager != null)
        {
            healthXPUIManager.OnHealthChanged();
            healthXPUIManager.OnXPChanged();
        }
    }
    
    protected PlayerLevelData GetPlayerLevelData(int level)
    {
        if (useInspectorLevels)
        {
            // Find level data in inspector list
            foreach (var data in levelData)
            {
                if (data.level == level)
                    return data;
            }
            
            // If exact level not found, use the highest available level
            PlayerLevelData fallback = null;
            foreach (var data in levelData)
            {
                if (data.level <= level && (fallback == null || data.level > fallback.level))
                    fallback = data;
            }
            return fallback;
        }
        else
        {
            // Use code-based data
            if (codeLevelData.ContainsKey(level))
                return codeLevelData[level];
                
            // If exact level not found, use the highest available level
            PlayerLevelData fallback = null;
            foreach (var kvp in codeLevelData)
            {
                if (kvp.Key <= level && (fallback == null || kvp.Key > fallback.level))
                    fallback = kvp.Value;
            }
            return fallback;
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
                Debug.LogWarning("Player: Both currentStats and baseStats are null!");
            }
        }

        // Apply level-based stats first
        if (!useInspectorLevels)
        {
            InitialiseFromCodeMatrix();
        }
        ApplyLevelStats(currentLevel);

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody2D>();
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

            if (currentHealth >= maxHealth) continue;

            if (Time.time >= lastDamageTime + healthRegenDelay)
            {
                float regenAmount = healthRegen * tickInterval;
                currentHealth = Mathf.Min(currentHealth + regenAmount, maxHealth);
                PopupManager.Instance?.ShowRegen(regenAmount, transform.position);
                healthXPUIManager?.OnHealthChanged();
            }
        }
    }

    protected override void OnHealed(float amount)
    {
        if (healthXPUIManager != null)
            healthXPUIManager.OnHealthChanged();
    }

    protected override void OnDamageTaken(float damageAmount)
    {
        if (healthXPUIManager != null)
            healthXPUIManager.OnHealthChanged();
    }
    
    public void GainExperience(float xp)
    {
        if (isDead || currentStats == null) return;
        
        PopupManager.Instance?.ShowXP(xp, transform.position);
        
        pendingExperience += xp;
        CheckForPendingLevelUp();
        
        if (healthXPUIManager != null)
            healthXPUIManager.OnXPChanged();
    }
    
    private void CheckForPendingLevelUp()
    {
        if (currentStats == null) return;
        
        float totalXP = currentStats.experience + pendingExperience;
        if (totalXP >= experienceToNextLevel)
        {
            isLevelUpPending = true;
        }
    }
    
    private void LevelUp()
    {
        if (currentStats == null) return;
        
        currentStats.experience -= experienceToNextLevel;
        currentStats.level++;
        currentLevel = currentStats.level;
        
        // Apply new level stats
        ApplyLevelStats(currentLevel);
        
        // Heal to full on level up
        currentHealth = maxHealth;

        PopupManager.Instance?.ShowPopup(
            $"LEVEL {currentLevel}!",
            transform.position + Vector3.up * 1f,
            Color.magenta,
            fontSize: 36,
            duration: 2f,
            moveDistance: 100f
        );
        
        Debug.Log($"Level Up! Now level {currentLevel}");
    }
    
    public int ProcessPendingExperienceAndReturnLevelUps()
    {
        if (pendingExperience <= 0 || currentStats == null) return 0;
        
        currentStats.experience += pendingExperience;
        pendingExperience = 0f;
        
        int levelUpsGained = 0;
        while (currentStats.experience >= experienceToNextLevel)
        {
            LevelUp();
            levelUpsGained++;
        }
        
        isLevelUpPending = false;
        
        if (healthXPUIManager != null)
        {
            healthXPUIManager.OnHealthChanged();
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
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
        }
    }
    
    public void SaveStatsToBase()
    {
        if (baseStats == null || currentStats == null) return;
        
        baseStats.level = currentStats.level;
        baseStats.experience = currentStats.experience;
        baseStats.experienceToNextLevel = experienceToNextLevel;
        baseStats.moveSpeed = moveSpeed;
        baseStats.maxHealth = maxHealth;
        baseStats.healthRegen = healthRegen;
        baseStats.meleeRange = meleeRange;
        baseStats.attackDamage = damage;
        baseStats.attackSpeed = attackSpeed;
        baseStats.projectileSpeed = projectileSpeed;
        baseStats.projectileRange = projectileRange;
        baseStats.abilityCooldownReduction = abilityCooldownReduction;
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
    
    public float GetExperienceToNextLevel()
    {
        return experienceToNextLevel;
    }
    
    // Additional getter methods for player-specific stats
    public float GetHealthRegen() => healthRegen;
    public float GetAttackSpeed() => attackSpeed;
    public float GetMeleeRange() => meleeRange;
    public float GetProjectileSpeed() => projectileSpeed;
    public float GetProjectileRange() => projectileRange;
    public float GetAbilityCooldownReduction() => abilityCooldownReduction;
}