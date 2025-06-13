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
}

public abstract class Ability : ScriptableObject
{
    [Header("Base Ability Settings")]
    public string abilityName;
    public Sprite abilityIcon;
    
    [Header("Stacking")]
    public int maxStacks = 1;
    [SerializeField] private int currentStacks = 1;
    
    [Header("Level Data (Levels 1-5)")]
    [SerializeField] private AbilityLevelData[] levelData = new AbilityLevelData[5];
    [SerializeField] private int currentLevel = 1;
    
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
    public int MaxLevel => 5;

    private void OnEnable()
    {
        // Initialize level data array if needed
        if (levelData == null || levelData.Length != 5)
        {
            levelData = new AbilityLevelData[5];
            for (int i = 0; i < 5; i++)
            {
                if (levelData[i] == null)
                    levelData[i] = new AbilityLevelData();
            }
        }
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
            Debug.Log($"{abilityName} leveled up to {currentLevel}!");
        }
    }

    public bool CanAddStack()
    {
        return currentStacks < maxStacks;
    }

    public void AddStack()
    {
        if (CanAddStack())
        {
            currentStacks++;
            Debug.Log($"{abilityName} stack added! ({currentStacks}/{maxStacks})");
        }
    }

    public void RemoveStack()
    {
        if (currentStacks > 0)
        {
            currentStacks--;
            Debug.Log($"{abilityName} stack removed! ({currentStacks}/{maxStacks})");
        }
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
        return !player.isDead;
    }

    private void OnValidate()
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, MaxLevel);
        currentStacks = Mathf.Clamp(currentStacks, 0, maxStacks);
        
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
    }
}