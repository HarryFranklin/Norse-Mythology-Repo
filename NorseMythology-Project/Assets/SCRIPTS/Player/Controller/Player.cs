using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Player : Entity
{
    [Header("Configuration")]
    [Tooltip("The Class asset that dictates stats and leveling progression.")]
    [SerializeField] private CharacterClass characterClass;

    [Header("Runtime State")]
    [SerializeField] private PlayerStats _currentStats;
    public PlayerStats currentStats
    {
        get { return _currentStats; }
        set { _currentStats = value; }
    }

    [Header("References")]
    public GameManager gameManager;
    public Rigidbody2D rigidBody;
    public AbilityManager abilityManager;
    public HealthXPUIManager healthXPUIManager;
    public Transform weaponHolder;
    public Transform hammerSpawnPoint;

    // --- Leveling State ---
    public bool isLevelUpPending = false;
    private float pendingExperience = 0f;
    private Coroutine regenCoroutine;

    void Awake()
    {
        gameManager = GameManager.Instance;
        
        // 1. Determine Source of Stats
        if (gameManager != null && gameManager.GetCurrentPlayerStats() != null)
        {
            // Case A: Stats passed from Character Selection / Save Data
            currentStats = gameManager.GetCurrentPlayerStats();
            Debug.Log($"Player stats loaded from GameManager: {currentStats.name}");
        }
        else if (characterClass != null)
        {
            // Case B: Testing directly in scene (use base stats from Class Definition)
            currentStats = characterClass.GetStatsForLevel(1);
            Debug.Log($"Player stats initialized from CharacterClass: {characterClass.name}");
        }
        else if (_currentStats != null)
        {
            // Case C: Fallback to whatever is assigned in Inspector (Legacy support)
            // Ideally, you shouldn't rely on this, but it prevents crashes
            Debug.LogWarning("Using Inspector-assigned currentStats. Ensure this is intended.");
        }
        else
        {
            Debug.LogError("CRITICAL: Player has no Stats! Assign a 'Character Class' or 'Current Stats'.");
        }

        // 2. Initialise Entity Base values
        if (currentStats != null)
        {
            currentHealth = currentStats.maxHealth;
        }

        InitialiseEntity();
    }

    private void OnEnable()
    {
        StartHealthRegeneration();
    }

    void Start()
    {
        if (abilityManager == null) abilityManager = GetComponent<AbilityManager>();
        
        // Ensure UI is up to date on start
        healthXPUIManager?.OnHealthChanged();
        healthXPUIManager?.OnXPChanged();
    }

    void Update()
    {
        if (gameManager != null && !gameManager.IsGameActive())
        {
            return;
        }
    }

    // ========================================================================
    //                         LEVELING & STATS
    // ========================================================================

    protected override void ApplyLevelStats(int level)
    {
        if (characterClass == null) 
        {
            Debug.LogWarning("Cannot apply level stats: CharacterClass is missing.");
            return;
        }

        // 1. Generate new stats for this specific level
        PlayerStats newStats = characterClass.GetStatsForLevel(level);

        // 2. Carry over runtime values (XP, Name, etc.)
        // We preserve the percentage of health, rather than the raw value
        float healthPercent = (currentStats.maxHealth > 0) ? currentHealth / currentStats.maxHealth : 1f;
        
        newStats.experience = currentStats.experience;
        newStats.attackType = currentStats.attackType; // Preserve weapon type choice if dynamic
        newStats.meleeWeaponPrefab = currentStats.meleeWeaponPrefab;
        newStats.projectilePrefab = currentStats.projectilePrefab;

        // 3. Apply
        currentStats = newStats;
        currentHealth = currentStats.maxHealth * healthPercent;

        // 4. Update UI & Systems
        StartHealthRegeneration();
        healthXPUIManager?.OnHealthChanged();
        healthXPUIManager?.OnXPChanged();
    }

    protected override void InitialiseFromCodeMatrix()
    {
        // DEPRECATED: Logic moved to CharacterDefinition ScriptableObject.
        // Kept empty to satisfy base class requirement if necessary.
    }

    protected override void InitialiseEntity()
    {
        // Optional initialization logic
    }

    // ========================================================================
    //                         HEALTH & REGEN
    // ========================================================================

    public void StartHealthRegeneration()
    {
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        if (gameObject.activeInHierarchy && !isDead)
        {
            regenCoroutine = StartCoroutine(HealthRegenerationRoutine());
        }
    }

    private IEnumerator HealthRegenerationRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1f); // Check every second for efficiency

        while (!isDead)
        {
            yield return wait;

            // Use currentStats as the Source of Truth
            if (currentStats != null && currentHealth < currentStats.maxHealth && 
                Time.time >= lastDamageTime + currentStats.healthRegenDelay && 
                currentStats.healthRegen > 0f)
            {
                float healAmount = currentStats.healthRegen; // Amount per second
                float previousHealth = currentHealth;
                
                currentHealth = Mathf.Min(currentHealth + healAmount, currentStats.maxHealth);
                
                float actualHeal = currentHealth - previousHealth;

                if (actualHeal > 0.01f)
                {
                    PopupManager.Instance?.ShowRegen(actualHeal, transform.position);
                    healthXPUIManager?.OnHealthChanged();
                }
            }
        }
    }

    protected override void OnHealed(float amount) => healthXPUIManager?.OnHealthChanged();

    protected override void OnDamageTaken(float damageAmount) => healthXPUIManager?.OnHealthChanged();

    protected override void OnDeath()
    {
        Debug.Log("Player died!");
        gameManager?.OnPlayerDied();
    }

    // ========================================================================
    //                         EXPERIENCE
    // ========================================================================

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
        if (currentStats != null && currentStats.experience + pendingExperience >= currentStats.experienceToNextLevel)
        {
            isLevelUpPending = true;
        }
    }

    public int ProcessPendingExperienceAndReturnLevelUps()
    {
        if (pendingExperience <= 0 || currentStats == null) return 0;
        
        currentStats.experience += pendingExperience;
        pendingExperience = 0f;
        
        int levelUpsGained = 0;
        
        // Loop in case we gained enough XP for multiple levels at once
        while (currentStats.experience >= currentStats.experienceToNextLevel)
        {
            LevelUp();
            levelUpsGained++;
        }
        
        isLevelUpPending = false;
        healthXPUIManager?.OnHealthChanged();
        healthXPUIManager?.OnXPChanged();
        
        return levelUpsGained;
    }

    private void LevelUp()
    {
        if (currentStats == null) return;

        // Carry over excess XP
        currentStats.experience -= currentStats.experienceToNextLevel;
        currentStats.level++;
        
        // Apply the new stats from the Definition
        ApplyLevelStats(currentStats.level);
        
        // Heal to full on level up (Optional design choice, feels good in roguelikes)
        currentHealth = currentStats.maxHealth;

        PopupManager.Instance?.ShowPopup($"LEVEL {currentStats.level}!", transform.position + Vector3.up * 1f, Color.magenta, 36, 2f, 100f);
    }

    // ========================================================================
    //                         ABILITIES
    // ========================================================================

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

    // ========================================================================
    //                         GETTERS / SETTERS
    // ========================================================================
    
    // These now Proxy directly to currentStats to ensure Single Source of Truth

    public int GetPlayerLevel() => currentStats != null ? currentStats.level : 1;
    
    public void SetPlayerLevel(int level)
    {
        if (currentStats != null) currentStats.level = level;
    }

    public void SaveStatsToBase()
    {
        if (currentStats == null) return;
        // Logic to save run data back to persistence if needed
        // For MVP, usually handled by GameManager saving the PlayerData struct
    }

    // Stats Accessors
    public float GetTotalExperience() => currentStats != null ? currentStats.experience + pendingExperience : 0f;
    public float GetCurrentExperience() => currentStats != null ? currentStats.experience : 0f;
    public float GetPendingExperience() => pendingExperience;
    public float GetExperienceToNextLevel() => currentStats != null ? currentStats.experienceToNextLevel : 100f;
    
    public float GetMoveSpeed() => currentStats != null ? currentStats.moveSpeed : 5f;
    public float GetHealthRegen() => currentStats != null ? currentStats.healthRegen : 0f;
    public float GetAttackSpeed() => currentStats != null ? currentStats.attackSpeed : 1f;
    public float GetMeleeRange() => currentStats != null ? currentStats.meleeRange : 1.5f;
    public float GetProjectileSpeed() => currentStats != null ? currentStats.projectileSpeed : 10f;
    public float GetProjectileRange() => currentStats != null ? currentStats.projectileRange : 10f;
    public float GetAbilityCooldownReduction() => currentStats != null ? currentStats.abilityCooldownReduction : 0f;
}