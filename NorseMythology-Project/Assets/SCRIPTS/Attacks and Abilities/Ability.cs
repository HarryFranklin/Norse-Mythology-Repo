using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// These enums define the types and rarities for your abilities.
public enum ActivationMode { Instant, ClickToTarget }
public enum AbilityRarity { Common, Uncommon, Rare, Epic, Legendary }

/// <summary>
/// Holds all the gameplay stats for a single level of an ability.
/// This allows you to define the entire level progression in one place.
/// </summary>
[Serializable]
public class AbilityLevelData
{
    [Header("Level Stats")]
    public float cooldown = 5f;
    public float damage = 0f;
    public float duration = 0f;
    public float radius = 0f;
    public float speed = 0f;
    public float distance = 0f;

    [Header("Special Values")]
    public float specialValue1 = 0f; // For custom ability-specific values
    public float specialValue2 = 0f;
    public float specialValue3 = 0f;

    [Header("Stacking System")]
    public int maxStacksAtLevel = 1;
    public float stackRegenTime = 1f; // Time to regenerate one stack
}

/// <summary>
/// The base class for all Ability ScriptableObjects. An instance of this class
/// represents the DEFINITION of an ability (e.g., "Dash"), while a runtime
/// clone of it represents the player's actual equipped ability.
/// </summary>
[CreateAssetMenu(fileName = "New Ability", menuName = "Abilities/Ability Definition")]
public abstract class Ability : ScriptableObject
{
    [Header("Identity")]
    public string abilityName;
    public Sprite abilityIcon;
    public AbilityRarity rarity = AbilityRarity.Common;

    [Header("Description")]
    [TextArea(2, 4)]
    public string description;

    [Header("Behavior")]
    public ActivationMode activationMode = ActivationMode.Instant;
    public bool useCodeDefinedMatrix = true;

    [Header("Targeting")]
    public bool showTargetingLine = false;
    public Color targetingLineColor = Color.white;
    public float maxTargetingRange = 10f;
    public Sprite targetingCursor;

    [Header("Level Progression Data")]
    // This array holds the stats for each level, from 1 to 5.
    [SerializeField] private AbilityLevelData[] levelData = new AbilityLevelData[5];

    // --- RUNTIME DATA ---
    // These values are only set when the game is running.
    // They are NOT saved to the ScriptableObject asset file.
    [NonSerialized] private int currentLevel = 1;
    [NonSerialized] private int currentStacks = 1;
    [NonSerialized] private float nextStackRegenTime = 0f;
    [NonSerialized] private int abilityStacks = 1; // Stacking for owning multiple copies

    // --- PUBLIC PROPERTIES ---
    public int CurrentLevel { get => currentLevel; set => currentLevel = value; }
    public int CurrentStacks { get => currentStacks; }
    public int AbilityStacks { get => abilityStacks; set => abilityStacks = value; }
    public int MaxLevel => levelData.Length;
    public float CurrentCooldown => GetCurrentLevelData().cooldown;
    public float CurrentStackRegenTime => GetCurrentLevelData().stackRegenTime;
    public int MaxStacksAtCurrentLevel => GetCurrentLevelData().maxStacksAtLevel;

    // --- HELPER METHODS ---
    /// <summary>
    /// Gets the stat block for the ability's CURRENT runtime level.
    /// </summary>
    public AbilityLevelData GetCurrentLevelData()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelData.Length - 1);
        return levelData[index];
    }
    
    /// <summary>
    /// Gets the stat block for ANY specified level. Used by the UI.
    /// </summary>
    public AbilityLevelData GetStatsForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, levelData.Length - 1);
        return levelData[index];
    }

    public bool CanLevelUp()
    {
        return currentLevel < MaxLevel;
    }

    /// <summary>
    /// A helper method to easily define level data from code.
    /// </summary>
    protected void SetLevelData(int level, float cooldown = -1, float damage = -1, float duration = -1, float radius = -1, float speed = -1, float distance = -1, float specialValue1 = -1, float specialValue2 = -1, float specialValue3 = -1, int maxStacks = -1, float stackRegenTime = -1)
    {
        if (level < 1 || level > levelData.Length) return;
        int index = level - 1;
        
        // If it's not the first level, copy from the previous level to fill in unchanged values.
        if (index > 0)
        {
            levelData[index] = new AbilityLevelData
            {
                cooldown = levelData[index - 1].cooldown,
                damage = levelData[index - 1].damage,
                duration = levelData[index - 1].duration,
                radius = levelData[index - 1].radius,
                speed = levelData[index - 1].speed,
                distance = levelData[index - 1].distance,
                specialValue1 = levelData[index - 1].specialValue1,
                specialValue2 = levelData[index - 1].specialValue2,
                specialValue3 = levelData[index - 1].specialValue3,
                maxStacksAtLevel = levelData[index - 1].maxStacksAtLevel,
                stackRegenTime = levelData[index - 1].stackRegenTime
            };
        }
        else
        {
            levelData[index] = new AbilityLevelData();
        }

        // Apply new values
        if (cooldown >= 0) levelData[index].cooldown = cooldown;
        if (damage >= 0) levelData[index].damage = damage;
        if (duration >= 0) levelData[index].duration = duration;
        if (radius >= 0) levelData[index].radius = radius;
        if (speed >= 0) levelData[index].speed = speed;
        if (distance >= 0) levelData[index].distance = distance;
        if (specialValue1 >= 0) levelData[index].specialValue1 = specialValue1;
        if (specialValue2 >= 0) levelData[index].specialValue2 = specialValue2;
        if (specialValue3 >= 0) levelData[index].specialValue3 = specialValue3;
        if (maxStacks >= 0) levelData[index].maxStacksAtLevel = maxStacks;
        if (stackRegenTime >= 0) levelData[index].stackRegenTime = stackRegenTime;
    }

    // --- STACKED VALUE PROPERTIES ---
    // These properties should be used by your ability logic (e.g., in DashAbility.cs) to get the final, calculated stat values.
    public float StackedDamage => GetCurrentLevelData().damage * (1f + (abilityStacks - 1) * 0.5f);
    public float StackedDuration => GetCurrentLevelData().duration * abilityStacks;
    public float StackedRadius => GetCurrentLevelData().radius * abilityStacks;
    public float StackedDistance => GetCurrentLevelData().distance * abilityStacks;
    public float StackedSpeed => GetCurrentLevelData().speed * abilityStacks;
    public float StackedSpecialValue1 => GetCurrentLevelData().specialValue1 * abilityStacks;
    public float StackedSpecialValue2 => GetCurrentLevelData().specialValue2 * abilityStacks;
    public float StackedSpecialValue3 => GetCurrentLevelData().specialValue3 * abilityStacks;

    public float StackedCooldown
    {
        get
        {
            if (abilityStacks <= 1) return GetCurrentLevelData().cooldown;
            float reduction = 1f - (0.1f * (abilityStacks - 1));
            return GetCurrentLevelData().cooldown * Mathf.Max(0.2f, reduction);
        }
    }

    // --- VIRTUAL & ABSTRACT METHODS ---
    protected virtual void InitialiseFromCodeMatrix() { }
    public abstract void Activate(Player player, PlayerMovement playerMovement);
    public virtual void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition) { Activate(player, playerMovement); }
    public virtual void EnterTargetingMode(Player player) { }
    public virtual void ExitTargetingMode(Player player) { }

    public virtual bool CanActivate(Player player)
    {
        return player != null && !player.isDead && currentStacks > 0;
    }

    // --- STACK REGENERATION LOGIC ---
    public void UpdateStackRegeneration()
    {
        var currentLevelStats = GetCurrentLevelData();
        if (currentStacks < currentLevelStats.maxStacksAtLevel && Time.time >= nextStackRegenTime)
        {
            currentStacks++;
            if (currentStacks < currentLevelStats.maxStacksAtLevel)
            {
                nextStackRegenTime = Time.time + currentLevelStats.stackRegenTime;
            }
        }
    }

    public void RemoveStack()
    {
        if (currentStacks > 0)
        {
            if (currentStacks == GetCurrentLevelData().maxStacksAtLevel)
            {
                nextStackRegenTime = Time.time + GetCurrentLevelData().stackRegenTime;
            }
            currentStacks--;
        }
    }

    public void AddAbilityStack()
    {
        abilityStacks++;
    }
}