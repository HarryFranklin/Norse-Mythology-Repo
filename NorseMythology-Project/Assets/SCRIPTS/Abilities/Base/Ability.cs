using UnityEngine;
using System;

// Enums defining the types and rarities for abilities
public enum ActivationMode { Instant, ClickToTarget }
public enum AbilityRarity { Common, Uncommon, Rare, Epic, Legendary }

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
    public float specialValue1 = 0f;
    public float specialValue2 = 0f;
    public float specialValue3 = 0f;

    [Header("Stacking System")]
    public int maxStacksAtLevel = 1;
    public float stackRegenTime = 1f; 
}

[CreateAssetMenu(fileName = "New Ability", menuName = "Abilities/Ability Definition")]
public abstract class Ability : ScriptableObject
{
    [Header("Identity")]
    public string abilityName;
    public Sprite abilityIcon;

    [Header("Description")]
    [TextArea(2, 4)]
    public string description;

    [Header("Behaviour")]
    public ActivationMode activationMode = ActivationMode.Instant; 
    public bool useCodeDefinedMatrix = true; 

    [Header("Targeting")]
    public bool showTargetingLine = false; 
    public Color targetingLineColor = Color.white;
    public float maxTargetingRange = 10f;
    public Texture2D targetingCursor;

    [Header("Level Progression Data")]
    [SerializeField] private AbilityLevelData[] levelData = new AbilityLevelData[5];

    // --- Runtime data ---
    [NonSerialized] private int currentLevel = 1;
    [NonSerialized] private int currentStacks = 1;
    [NonSerialized] private int abilityStacks = 1;
    
    [NonSerialized] private float cooldownTimer = 0f; 

    // --- Public properties ---
    public int CurrentLevel { get => currentLevel; set => currentLevel = value; }
    public int CurrentStacks { get => currentStacks; }
    public int AbilityStacks { get => abilityStacks; set => abilityStacks = value; }
    public int MaxLevel => levelData.Length;
    
    // Returns the Max Cooldown (Stat)
    public float MaxCooldown => GetCurrentLevelData().cooldown; 
    public float CurrentStackRegenTime => GetCurrentLevelData().stackRegenTime;
    public int MaxStacksAtCurrentLevel => GetCurrentLevelData().maxStacksAtLevel;

    // --- Helper methods ---
    public AbilityLevelData GetCurrentLevelData()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, levelData.Length - 1);
        return levelData[index];
    }
    
    public AbilityLevelData GetStatsForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, levelData.Length - 1);
        return levelData[index];
    }

    public bool CanLevelUp()
    {
        return currentLevel < MaxLevel;
    }

    protected void SetLevelData(int level, float cooldown = -1, float damage = -1, float duration = -1, float radius = -1, float speed = -1, float distance = -1, float specialValue1 = -1, float specialValue2 = -1, float specialValue3 = -1, int maxStacks = -1, float stackRegenTime = -1)
    {
        if (level < 1 || level > levelData.Length) return;
        int index = level - 1;
        
        if (index > 0)
        {
            // Clone previous level as base
            var prev = levelData[index - 1];
            levelData[index] = new AbilityLevelData
            {
                cooldown = prev.cooldown, damage = prev.damage, duration = prev.duration,
                radius = prev.radius, speed = prev.speed, distance = prev.distance,
                specialValue1 = prev.specialValue1, specialValue2 = prev.specialValue2, specialValue3 = prev.specialValue3,
                maxStacksAtLevel = prev.maxStacksAtLevel, stackRegenTime = prev.stackRegenTime
            };
        }
        else
        {
            levelData[index] = new AbilityLevelData();
        }

        var d = levelData[index];
        if (cooldown >= 0) d.cooldown = cooldown;
        if (damage >= 0) d.damage = damage;
        if (duration >= 0) d.duration = duration;
        if (radius >= 0) d.radius = radius;
        if (speed >= 0) d.speed = speed;
        if (distance >= 0) d.distance = distance;
        if (specialValue1 >= 0) d.specialValue1 = specialValue1;
        if (specialValue2 >= 0) d.specialValue2 = specialValue2;
        if (specialValue3 >= 0) d.specialValue3 = specialValue3;
        if (maxStacks >= 0) d.maxStacksAtLevel = maxStacks;
        if (stackRegenTime >= 0) d.stackRegenTime = stackRegenTime;
    }

    // --- Stacked value properties ---
    public float StackedDamage => GetCurrentLevelData().damage * (1f + (abilityStacks - 1) * 0.5f);
    public float StackedDuration => GetCurrentLevelData().duration * abilityStacks;
    public float StackedRadius => GetCurrentLevelData().radius * abilityStacks;
    public float StackedDistance => GetCurrentLevelData().distance * abilityStacks;
    public float StackedSpeed => GetCurrentLevelData().speed * abilityStacks;
    public float StackedSpecialValue1 => GetCurrentLevelData().specialValue1 * abilityStacks;
    public float StackedSpecialValue2 => GetCurrentLevelData().specialValue2 * abilityStacks;
    public float StackedSpecialValue3 => GetCurrentLevelData().specialValue3 * abilityStacks;

    public float StackedMaxCooldown
    {
        get
        {
            if (abilityStacks <= 1) return GetCurrentLevelData().cooldown;
            float reduction = 1f - (0.1f * (abilityStacks - 1));
            return GetCurrentLevelData().cooldown * Mathf.Max(0.2f, reduction);
        }
    }

    // --- Virtual and abstract methods ---
    public virtual void InitialiseFromCodeMatrix() { }
    public abstract void Activate(Player player, PlayerMovement playerMovement);
    public virtual void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition) { Activate(player, playerMovement); }
    public virtual void EnterTargetingMode(Player player) { }
    public virtual void ExitTargetingMode(Player player) { }

    public virtual bool CanActivate(Player player)
    {
        return player != null && !player.isDead && currentStacks > 0;
    }

    public void UpdateCooldownLogic(float deltaTime)
    {
        int maxStacks = GetCurrentLevelData().maxStacksAtLevel;
        float regenTime = GetCurrentLevelData().stackRegenTime;

        // If we are full on stacks, do nothing
        if (currentStacks >= maxStacks) 
        {
            cooldownTimer = 0f;
            return;
        }

        // Advance timer
        cooldownTimer += deltaTime;

        // Check for completion
        if (cooldownTimer >= regenTime)
        {
            currentStacks++;
            cooldownTimer -= regenTime; // Keep overflow for next stack
            
            // If full, clamp timer
            if (currentStacks >= maxStacks)
            {
                cooldownTimer = 0f;
            }
        }
    }

    public void RemoveStack()
    {
        if (currentStacks > 0)
        {
            currentStacks--;
            // If we were at max stacks, we now need to start the timer
            // But timer naturally starts counting up from 0 in UpdateCooldownLogic
        }
    }
    
    public float GetStackCooldownRemaining()
    {
        if (currentStacks >= MaxStacksAtCurrentLevel)
        {
            return 0f;
        }
        // Remaining time is Total - Current
        return Mathf.Max(0f, GetCurrentLevelData().stackRegenTime - cooldownTimer);
    }

    public void AddAbilityStack()
    {
        abilityStacks++;
    }
}