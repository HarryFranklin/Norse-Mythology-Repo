using UnityEngine;
using System;

public enum ActivationMode
{
    Instant, // Activates immediately on keypress
    ClickToTarget // Enters targeting mode, then activates on mouse click
}

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

public abstract class Ability : ScriptableObject
{
    [Header("Base Ability Settings")]
    public string abilityName;
    public Sprite abilityIcon;
    
    [Header("Use Code Matrix")]
    public bool useCodeDefinedMatrix = false;
    
    [Header("Ability Stacking")]
    [SerializeField] private int abilityStacks = 1; // How many copies of this ability are owned
    [SerializeField] private bool canStackAbility = true; // Whether this ability can be stacked
    
    [Header("Level Data (Levels 1-5)")]
    [SerializeField] private AbilityLevelData[] levelData = new AbilityLevelData[5];
    [SerializeField] private int currentLevel = 1;
    
    [Header("Current Stacks")]
    [SerializeField] private int currentStacks = 1;
    private float nextStackRegenTime = 0f;
    
    [Header("Activation Mode")]
    public ActivationMode activationMode = ActivationMode.Instant;
    
    [Header("Targeting Settings (for ClickToTarget abilities)")]
    public Sprite targetingCursor;
    public bool showTargetingLine = false;
    public Color targetingLineColor = Color.white;
    public float maxTargetingRange = 10f;
    
    [Header("Description")]
    [TextArea(2, 4)]
    public string description;

    // Properties to get current level values
    public float CurrentCooldown => GetCurrentLevelData().cooldown;
    public float CurrentDamage => GetCurrentLevelData().damage;
    public float CurrentDuration => GetCurrentLevelData().duration;
    public float CurrentRadius => GetCurrentLevelData().radius;
    public float CurrentSpeed => GetCurrentLevelData().speed;
    public float CurrentDistance => GetCurrentLevelData().distance;
    public float CurrentSpecialValue1 => GetCurrentLevelData().specialValue1;
    public float CurrentSpecialValue2 => GetCurrentLevelData().specialValue2;
    public float CurrentSpecialValue3 => GetCurrentLevelData().specialValue3;
    
    public int CurrentLevel => currentLevel;
    public int CurrentStacks => currentStacks;
    public int MaxStacksAtCurrentLevel => GetCurrentLevelData().maxStacksAtLevel;
    public float CurrentStackRegenTime => GetCurrentLevelData().stackRegenTime;
    public int MaxLevel => 5;
    
    // Ability stacking properties
    public int AbilityStacks => abilityStacks;
    public bool CanStackAbility => canStackAbility;

    private void OnEnable()
    {
        // Initialise level data array if needed
        if (levelData == null || levelData.Length != 5)
        {
            levelData = new AbilityLevelData[5];
            for (int i = 0; i < 5; i++)
            {
                if (levelData[i] == null)
                    levelData[i] = new AbilityLevelData();
            }
        }
        
        // Initialise with code-defined matrix if specified
        if (useCodeDefinedMatrix)
        {
            InitialiseFromCodeMatrix();
        }
        
        // Initialise stack regeneration timer
        InitialiseStackRegeneration();
    }

    protected virtual void InitialiseFromCodeMatrix()
    {
        // Override this in derived classes to define ability values via code
    }

    public AbilityLevelData GetCurrentLevelData()
    {
        int index = Mathf.Clamp(currentLevel - 1, 0, 4);
        return levelData[index];
    }

    public AbilityLevelData GetLevelData(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, 4);
        return levelData[index];
    }

    public bool CanLevelUp()
    {
        return currentLevel < MaxLevel;
    }

    public void LevelUp()
    {
        if (CanLevelUp())
        {
            currentLevel++;
            
            // Update stacks to match new level if current stacks exceed new max
            int newMaxStacks = MaxStacksAtCurrentLevel;
            if (currentStacks > newMaxStacks)
            {
                currentStacks = newMaxStacks;
            }
            
            Debug.Log($"{abilityName} leveled up to {currentLevel}! Max stacks: {newMaxStacks}");
            
            // Reset stack regeneration timing
            InitialiseStackRegeneration();
        }
    }

    public bool CanAddAbilityStack()
    {
        return canStackAbility && abilityStacks < 10; // Limit to 10 stacks maximum
    }

    public void AddAbilityStack()
    {
        if (CanAddAbilityStack())
        {
            abilityStacks++;
            Debug.Log($"{abilityName} ability stack added! Total stacks: {abilityStacks}");
        }
        else
        {
            Debug.Log($"Cannot add ability stack to {abilityName}. Current: {abilityStacks}, Can stack: {canStackAbility}");
        }
    }

    public void RemoveAbilityStack()
    {
        if (abilityStacks > 1)
        {
            abilityStacks--;
            Debug.Log($"{abilityName} ability stack removed! Total stacks: {abilityStacks}");
        }
        else
        {
            Debug.Log($"Cannot remove ability stack from {abilityName}. Must have at least 1 stack.");
        }
    }

    // Get effective value based on ability stacks (for multiplicative stacking)
    public float GetStackedValue(float baseValue, bool isMultiplicative = false)
    {
        if (abilityStacks <= 1) return baseValue;
        
        if (isMultiplicative)
        {
            // Each additional stack adds 50% of base value
            return baseValue * (1f + (abilityStacks - 1) * 0.5f);
        }
        else
        {
            // Additive stacking
            return baseValue * abilityStacks;
        }
    }

    // Get effective stacked values for common properties
    public float StackedDamage => GetStackedValue(CurrentDamage, true);
    public float StackedDuration => GetStackedValue(CurrentDuration, false);
    public float StackedRadius => GetStackedValue(CurrentRadius, false);
    public float StackedDistance => GetStackedValue(CurrentDistance, false);
    public float StackedSpeed => GetStackedValue(CurrentSpeed, false);
    public float StackedSpecialValue1 => GetStackedValue(CurrentSpecialValue1, false);
    public float StackedSpecialValue2 => GetStackedValue(CurrentSpecialValue2, false);
    public float StackedSpecialValue3 => GetStackedValue(CurrentSpecialValue3, false);

    // Cooldown reduction with stacks (diminishing returns)
    public float StackedCooldown
    {
        get
        {
            if (abilityStacks <= 1) return CurrentCooldown;
            
            // Each additional stack reduces cooldown by 10% with diminishing returns
            float reduction = 1f - (0.1f * (abilityStacks - 1) * (1f / abilityStacks));
            return CurrentCooldown * Mathf.Max(0.1f, reduction); // Minimum 10% of original cooldown
        }
    }

    public bool CanAddStack()
    {
        return currentStacks < MaxStacksAtCurrentLevel;
    }

    public void AddStack()
    {
        if (CanAddStack())
        {
            currentStacks++;
            Debug.Log($"{abilityName} stack added! ({currentStacks}/{MaxStacksAtCurrentLevel})");
        }
    }

    public void RemoveStack()
    {
        if (currentStacks > 0)
        {
            currentStacks--;
            Debug.Log($"{abilityName} stack removed! ({currentStacks}/{MaxStacksAtCurrentLevel})");
            
            // Reset regeneration timer if we're now below max stacks
            if (currentStacks < MaxStacksAtCurrentLevel)
            {
                InitialiseStackRegeneration();
            }
        }
    }

    // Call this method regularly (e.g., from Update in a MonoBehaviour manager)
    public void UpdateStackRegeneration()
    {
        if (currentStacks < MaxStacksAtCurrentLevel && Time.time >= nextStackRegenTime)
        {
            AddStack();
            
            // Set next regeneration time if we still need more stacks
            if (currentStacks < MaxStacksAtCurrentLevel)
            {
                nextStackRegenTime = Time.time + CurrentStackRegenTime;
            }
        }
    }

    private void InitialiseStackRegeneration()
    {
        // Only set regeneration timer if we need to regenerate stacks
        if (currentStacks < MaxStacksAtCurrentLevel)
        {
            nextStackRegenTime = Time.time + CurrentStackRegenTime;
        }
    }

    // Helper method to set level data programmatically
    protected void SetLevelData(int level, float cooldown = 0, float damage = 0, float duration = 0, 
        float radius = 0, float speed = 0, float distance = 0, float specialValue1 = 0, 
        float specialValue2 = 0, float specialValue3 = 0, int maxStacks = 1, float stackRegenTime = 1f)
    {
        if (level < 1 || level > 5) return;
        
        int index = level - 1;
        if (levelData[index] == null)
            levelData[index] = new AbilityLevelData();
            
        var data = levelData[index];
        if (cooldown > 0) data.cooldown = cooldown;
        if (damage > 0) data.damage = damage;
        if (duration > 0) data.duration = duration;
        if (radius > 0) data.radius = radius;
        if (speed > 0) data.speed = speed;
        if (distance > 0) data.distance = distance;
        if (specialValue1 != 0) data.specialValue1 = specialValue1; // Allow 0 values
        if (specialValue2 != 0) data.specialValue2 = specialValue2;
        if (specialValue3 != 0) data.specialValue3 = specialValue3;
        
        data.maxStacksAtLevel = maxStacks;
        data.stackRegenTime = stackRegenTime;
    }

    // Abstract methods for activation
    public abstract void Activate(Player player, PlayerMovement playerMovement);
    
    public virtual void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        Activate(player, playerMovement);
    }

    public virtual void EnterTargetingMode(Player player) { }
    public virtual void ExitTargetingMode(Player player) { }
    
    public virtual bool CanActivate(Player player)
    {
        // AbilityManager handles UpdateStackRegeneration()
        bool canActivate = player != null && !player.isDead && CurrentStacks > 0;
        
        if (!canActivate)
        {
            Debug.Log($"{abilityName} cannot activate - Player null: {player == null}, Dead: {player?.isDead}, Stacks: {CurrentStacks}/{MaxStacksAtCurrentLevel}");
        }
        
        return canActivate;
    }

    private void OnValidate()
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, MaxLevel);
        
        // Ensure level data array is properly sized
        if (levelData == null || levelData.Length != 5)
        {
            AbilityLevelData[] newLevelData = new AbilityLevelData[5];
            for (int i = 0; i < 5; i++)
            {
                if (levelData != null && i < levelData.Length && levelData[i] != null)
                    newLevelData[i] = levelData[i];
                else
                    newLevelData[i] = new AbilityLevelData();
            }
            levelData = newLevelData;
        }
        
        // Clamp current stacks to current level's max and ability stacks
        if (levelData != null && levelData.Length > 0)
        {
            int maxStacksForLevel = GetCurrentLevelData().maxStacksAtLevel;
            currentStacks = Mathf.Clamp(currentStacks, 0, maxStacksForLevel);
            abilityStacks = Mathf.Clamp(abilityStacks, 1, 10);
        }
    }
}