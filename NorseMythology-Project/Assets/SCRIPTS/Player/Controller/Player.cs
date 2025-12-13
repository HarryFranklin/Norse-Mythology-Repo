using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

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
    public PlayerStats baseStats; // This is the fallback/default stats
    public GameManager gameManager;
    public Rigidbody2D rigidBody;
    public AbilityManager abilityManager;
    public HealthXPUIManager healthXPUIManager;

    [Header("Level Up System")]
    public bool isLevelUpPending = false;
    private float pendingExperience = 0f;

    [Header("Level Configuration")]
    public List<PlayerLevelData> levelData = new List<PlayerLevelData>();
    protected Dictionary<int, PlayerLevelData> codeLevelData;

    // Player-specific stats
    public float healthRegen;
    public float healthRegenDelay;
    public float attackSpeed;
    public float meleeRange;
    public float projectileSpeed;
    public float projectileRange;
    public float experienceToNextLevel;
    public float abilityCooldownReduction;

    private Coroutine regenCoroutine;

    private PlayerStats _currentStats;
    [Header("Player Runtime Stats")]
    public PlayerStats currentStats
    {
        get { return _currentStats; }
        set { _currentStats = value; }
    }

    void Awake()
    {
        // Initialise the dictionary for level-up data
        codeLevelData = new Dictionary<int, PlayerLevelData>();
        
        gameManager = GameManager.Instance;

        // Prioritise loading stats from the GameManager (which holds the class selection)
        if (gameManager != null && gameManager.GetCurrentPlayerStats() != null)
        {
            currentStats = gameManager.GetCurrentPlayerStats();
            Debug.Log($"Player stats successfully loaded from GameManager for class: {currentStats.name}");
        }
        else if (baseStats != null)
        {
            // Fallback if the scene is run directly, using the default stats assigned in the Inspector
            currentStats = baseStats.CreateRuntimeCopy();
            Debug.LogWarning("GameManager stats not found. Initializing player with local baseStats.");
        }
        
        InitialisePlayer();
        
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        StartHealthRegeneration();
    }

    void Start()
    {
        if (abilityManager == null)
        {
            abilityManager = GetComponent<AbilityManager>();
        }
    }

    private void ApplyStatsFromScriptableObject()
    {
        if (currentStats == null)
        {
            Debug.LogError("currentStats is null! Cannot apply initial stats.");
            return;
        }

        maxHealth = currentStats.maxHealth;
        moveSpeed = currentStats.moveSpeed;
        damage = currentStats.attackDamage;
        healthRegen = currentStats.healthRegen;
        healthRegenDelay = currentStats.healthRegenDelay;
        attackSpeed = currentStats.attackSpeed;
        meleeRange = currentStats.meleeRange;
        projectileSpeed = currentStats.projectileSpeed;
        projectileRange = currentStats.projectileRange;
        experienceToNextLevel = currentStats.experienceToNextLevel;
        abilityCooldownReduction = currentStats.abilityCooldownReduction;
        currentLevel = currentStats.level;
    }

    private void InitialisePlayer()
    {
        ApplyStatsFromScriptableObject();

        if (!useInspectorLevels)
        {
            InitialiseFromCodeMatrix();
        }
    }
        
    void Update()
    {
        if (gameManager != null && !gameManager.IsGameActive())
        {
            return;
        }
    }

    public List<GameManager.PlayerAbilityState> GetAbilities()
    {
        if (abilityManager == null || gameManager == null || gameManager.GetAbilityPooler() == null)
        {
            return new List<GameManager.PlayerAbilityState>();
        }

        var abilityStates = new List<GameManager.PlayerAbilityState>();
        foreach (var equippedAbility in abilityManager.equippedAbilities)
        {
            if (equippedAbility != null)
            {
                var originalAsset = gameManager.GetAbilityPooler().GetAbilityByName(equippedAbility.abilityName);
                if (originalAsset != null)
                {
                    abilityStates.Add(new GameManager.PlayerAbilityState(originalAsset, equippedAbility.CurrentLevel));
                }
            }
        }
        return abilityStates;
    }

    public void SetAbilities(List<GameManager.PlayerAbilityState> abilityStates)
    {
        if (abilityManager == null) return;

        for (int i = 0; i < abilityManager.equippedAbilities.Length; i++)
        {
            if (i < abilityStates.Count && abilityStates[i].ability != null)
            {
                Ability runtimeInstance = Instantiate(abilityStates[i].ability);
                runtimeInstance.CurrentLevel = abilityStates[i].level;
                abilityManager.equippedAbilities[i] = runtimeInstance;
            }
            else
            {
                abilityManager.equippedAbilities[i] = null;
            }
        }
    }

    public void StartHealthRegeneration()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        if (gameObject.activeInHierarchy && !isDead)
        {
            regenCoroutine = StartCoroutine(HealthRegeneration());
        }
    }

    protected override void OnDeath()
    {
        Debug.Log("Player died!");
        gameManager?.OnPlayerDied();
    }

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
        
        StartHealthRegeneration();
        
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
            PlayerLevelData fallback = null;
            foreach (var data in levelData)
            {
                if (data.level == level) return data;
                if (data.level < level && (fallback == null || data.level > fallback.level))
                    fallback = data;
            }
            return fallback;
        }
        else
        {
            if (codeLevelData.ContainsKey(level)) return codeLevelData[level];
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
    }
    
    private IEnumerator HealthRegeneration()
    {
        // Settings
        float minHealAmount = 1f;   // We want to heal at least this much per tick
        float minInterval = 0.5f;   // Don't tick faster than this (prevents UI spam)

        while (!isDead)
        {
            // 1. Determine the wait time dynamically based on current regen speed
            float currentRate = healthRegen;
            float waitTime;

            if (currentRate > 0f)
            {
                // Calculate how long we need to wait to get 1.0 HP
                float timeToAccumulateMinHeal = minHealAmount / currentRate;
                
                // Wait at least the calculated time, but never faster than minInterval
                waitTime = Mathf.Max(minInterval, timeToAccumulateMinHeal);
            }
            else
            {
                // If no regen, just check again in a second
                waitTime = 1f;
            }

            yield return new WaitForSeconds(waitTime);

            // 2. Apply Regeneration
            if (currentHealth < maxHealth && Time.time >= lastDamageTime + healthRegenDelay && currentRate > 0f)
            {
                float potentialRegen = currentRate * waitTime;
                
                // Calculate actual amount healed (don't exceed max health)
                float previousHealth = currentHealth;
                currentHealth = Mathf.Min(currentHealth + potentialRegen, maxHealth);
                float actualHealAmount = currentHealth - previousHealth;

                // Only show popup if we actually healed something
                if (actualHealAmount > 0.01f)
                {
                    PopupManager.Instance?.ShowRegen(actualHealAmount, transform.position);
                    healthXPUIManager?.OnHealthChanged();
                }
            }
        }
    }

    protected override void OnHealed(float amount)
    {
        healthXPUIManager?.OnHealthChanged();
    }

    protected override void OnDamageTaken(float damageAmount)
    {
        healthXPUIManager?.OnHealthChanged();
    }
    
    public void GainExperience(float xp)
    {
        if (isDead || currentStats == null) return;
        PopupManager.Instance?.ShowXP(xp, transform.position);
        pendingExperience += xp;
        CheckForPendingLevelUp();
        healthXPUIManager?.OnXPChanged();
    }
    
    private void CheckForPendingLevelUp()
    {
        if (currentStats != null && currentStats.experience + pendingExperience >= experienceToNextLevel)
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
        ApplyLevelStats(currentLevel);
        currentHealth = maxHealth;
        PopupManager.Instance?.ShowPopup($"LEVEL {currentLevel}!", transform.position + Vector3.up * 1f, Color.magenta, 36, 2f, 100f);
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
        healthXPUIManager?.OnHealthChanged();
        healthXPUIManager?.OnXPChanged();
        
        return levelUpsGained;
    }

    public void ProcessPendingExperience()
    {
        ProcessPendingExperienceAndReturnLevelUps();
    }

    public int GetPlayerLevel() => currentStats != null ? currentStats.level : 1;
    public void SetPlayerLevel(int level)
    {
        if (currentStats != null) currentStats.level = level;
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
    
    public float GetTotalExperience() => currentStats != null ? currentStats.experience + pendingExperience : 0f;
    public float GetCurrentExperience() => currentStats != null ? currentStats.experience : 0f;
    public float GetPendingExperience() => pendingExperience;
    public float GetExperienceToNextLevel() => experienceToNextLevel;
    public float GetHealthRegen() => healthRegen;
    public float GetAttackSpeed() => attackSpeed;
    public float GetMeleeRange() => meleeRange;
    public float GetProjectileSpeed() => projectileSpeed;
    public float GetProjectileRange() => projectileRange;
    public float GetAbilityCooldownReduction() => abilityCooldownReduction;
}