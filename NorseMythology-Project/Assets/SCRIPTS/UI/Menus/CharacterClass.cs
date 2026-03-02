using UnityEngine;

[CreateAssetMenu(fileName = "New Character Class", menuName = "Game/Character Class")]
public class CharacterClass : ScriptableObject
{
    [Header("UI Info")]
    public string className;
    public Sprite classSprite;
    [TextArea] public string description;

    [Header("Base Stats (Level 1)")]
    public PlayerStats startingStats;

    [Header("XP Progression")]
    public float baseXpRequirement = 100f;
    
    [Tooltip("The base amount XP requirements increase by each level.")]
    public float xpIncreasePerLevel = 25f;
    
    [Tooltip("An additional increment added to the increase per level (e.g. 5).")]
    public float xpIncreaseIncrementPerLevel = 5f;

    [Header("Per Level Growth (Linear)")]
    public float healthPerLevel = 10f;
    public float damagePerLevel = 1f;
    public float regenPerLevel = 0f;
    public float attackSpeedPerLevel = 0f;
    public float moveSpeedPerLevel = 0f;
    
    [Tooltip("Usually negative (e.g. -0.1) to make regen faster.")]
    public float regenDelayPerLevel = 0f; 
    
    [Tooltip("Usually negative (e.g. -0.5) to make abilities faster.")]
    public float cooldownReductionPerLevel = 0f; 

    [Tooltip("Adds to Melee OR Projectile Range based on class type.")]
    public float rangePerLevel = 0f;

    [Header("Stat Caps (Clamping)")]
    public float maxMoveSpeed = 10f;
    public float maxAttackSpeed = 5f; 
    public float minRegenDelay = 0.5f; 
    public float maxCooldownReduction = 50f; 
    public float maxRange = 10f; 

    public PlayerStats GetStatsForLevel(int level)
    {
        if (startingStats == null) return null;

        PlayerStats stats = startingStats.CreateRuntimeCopy();
        stats.level = level;

        // 1. Calculate XP Requirement (Your New Formula)
        stats.experienceToNextLevel = baseXpRequirement 
                                    + ((level - 1) * xpIncreasePerLevel) 
                                    + ((level - 1) * xpIncreaseIncrementPerLevel);

        // 2. Calculate Level Multiplier
        int levelsGained = level - 1; 

        // 3. Apply Growth Formulas
        stats.maxHealth += healthPerLevel * levelsGained;
        stats.attackDamage += damagePerLevel * levelsGained;
        stats.healthRegen += regenPerLevel * levelsGained;
        
        // 4. Apply Growth with Caps
        stats.attackSpeed += attackSpeedPerLevel * levelsGained;
        stats.attackSpeed = Mathf.Min(stats.attackSpeed, maxAttackSpeed);

        stats.moveSpeed += moveSpeedPerLevel * levelsGained;
        stats.moveSpeed = Mathf.Min(stats.moveSpeed, maxMoveSpeed);

        stats.healthRegenDelay += regenDelayPerLevel * levelsGained;
        stats.healthRegenDelay = Mathf.Max(stats.healthRegenDelay, minRegenDelay);

        stats.abilityCooldownReduction += cooldownReductionPerLevel * levelsGained;
        stats.abilityCooldownReduction = Mathf.Min(stats.abilityCooldownReduction, maxCooldownReduction);

        // Smart Range Logic
        float totalRangeBonus = rangePerLevel * levelsGained;
        
        if (stats.attackType == AttackType.Melee)
        {
            stats.meleeRange += totalRangeBonus;
            stats.meleeRange = Mathf.Min(stats.meleeRange, maxRange);
        }
        else
        {
            stats.projectileRange += totalRangeBonus;
            stats.projectileRange = Mathf.Min(stats.projectileRange, maxRange);
        }

        return stats;
    }
}