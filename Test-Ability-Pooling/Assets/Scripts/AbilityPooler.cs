using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityPooler : MonoBehaviour
{
    [Header("Ability Settings")]
    public string abilityFolderPath = "Abilities"; // Folder path in Resources

    private List<Ability> allAbilities = new List<Ability>();
    private Dictionary<AbilityRarity, List<Ability>> abilitiesByRarity;

    void Awake()
    {
        LoadAbilitiesFromFolder();
        InitialiseAbilityPool();
    }

    void LoadAbilitiesFromFolder()
    {
        // Load all Ability ScriptableObjects from the specified folder
        Ability[] loadedAbilities = Resources.LoadAll<Ability>(abilityFolderPath);
        allAbilities = new List<Ability>(loadedAbilities);

        Debug.Log($"Loaded {allAbilities.Count} abilities from {abilityFolderPath} folder");

        if (allAbilities.Count == 0)
        {
            Debug.LogWarning($"No abilities found in Resources/{abilityFolderPath}/ folder. Make sure abilities are ScriptableObjects in that location.");
        }
    }

    void InitialiseAbilityPool()
    {
        // Group abilities by rarity
        abilitiesByRarity = new Dictionary<AbilityRarity, List<Ability>>();
        foreach (var rarityValue in System.Enum.GetValues(typeof(AbilityRarity)))
        {
            abilitiesByRarity[(AbilityRarity)rarityValue] = new List<Ability>();
        }

        // Categorise abilities by rarity
        foreach (var ability in allAbilities)
        {
            abilitiesByRarity[ability.rarity].Add(ability);
        }
    }

    public List<Ability> GetRandomAbilities(int count, int playerLevel, int gameLevel, List<Ability> currentAbilities)
    {
        Debug.Log($"GetRandomAbilities called: requesting {count} abilities for player level {playerLevel}, game level {gameLevel}");
        Debug.Log($"Current player abilities: {currentAbilities.Count}");
        
        List<Ability> selectedAbilities = new List<Ability>();

        // Calculate luck modifier based on level difference
        float luckModifier = CalculateLuckModifier(playerLevel, gameLevel);

        for (int i = 0; i < count; i++)
        {
            bool abilityAdded = false;
            
            // Decide if this should be an upgrade or new ability
            bool shouldUpgrade = currentAbilities.Count > 0 && Random.Range(0f, 1f) < 0.2f; // Make rarer

            if (shouldUpgrade)
            {
                // Try to upgrade existing ability
                var upgradeableAbilities = currentAbilities.Where(a => a.CanUpgrade()).ToList();
                if (upgradeableAbilities.Count > 0)
                {
                    var randomAbility = upgradeableAbilities[Random.Range(0, upgradeableAbilities.Count)];
                    selectedAbilities.Add(randomAbility.CreateUpgraded());
                    abilityAdded = true;
                    Debug.Log($"Added upgrade: {randomAbility.abilityName}");
                }
            }

            if (!abilityAdded)
            {
                // Get new ability based on rarity and luck
                AbilityRarity selectedRarity = GetRandomRarity(luckModifier);
                var availableAbilities = abilitiesByRarity[selectedRarity].Where(a => !currentAbilities.Any(ca => ca.abilityName == a.abilityName)).ToList();

                if (availableAbilities.Count > 0)
                {
                    var originalAbility = availableAbilities[Random.Range(0, availableAbilities.Count)];
                    // Create a copy of the ability
                    var newAbility = ScriptableObject.CreateInstance<Ability>();
                    newAbility.abilityName = originalAbility.abilityName;
                    newAbility.description = originalAbility.description;
                    newAbility.prefab = originalAbility.prefab;
                    newAbility.abilityColor = originalAbility.abilityColor;
                    newAbility.abilityLevel = originalAbility.abilityLevel;
                    newAbility.maxLevel = originalAbility.maxLevel;
                    newAbility.rarity = originalAbility.rarity;

                    selectedAbilities.Add(newAbility);
                    abilityAdded = true;
                    Debug.Log($"Added new ability: {newAbility.abilityName} ({selectedRarity})");
                }
                else
                {
                    Debug.LogWarning($"No available abilities for rarity {selectedRarity}");
                }
            }

            // I'll change this later
            // SAFETY NET: If we couldn't add an ability, try other rarities
            if (!abilityAdded)
            {
                Debug.LogWarning($"Failed to add ability {i + 1}, trying fallback options");

                // Try all rarities to find any available ability
                foreach (AbilityRarity rarity in System.Enum.GetValues(typeof(AbilityRarity)))
                {
                    if (abilitiesByRarity.ContainsKey(rarity))
                    {
                        var fallbackAbilities = abilitiesByRarity[rarity].Where(a => !currentAbilities.Any(ca => ca.abilityName == a.abilityName)).ToList();
                        if (fallbackAbilities.Count > 0)
                        {
                            var originalAbility = fallbackAbilities[Random.Range(0, fallbackAbilities.Count)];
                            var newAbility = ScriptableObject.CreateInstance<Ability>();
                            newAbility.abilityName = originalAbility.abilityName;
                            newAbility.description = originalAbility.description;
                            newAbility.prefab = originalAbility.prefab;
                            newAbility.abilityColor = originalAbility.abilityColor;
                            newAbility.abilityLevel = originalAbility.abilityLevel;
                            newAbility.maxLevel = originalAbility.maxLevel;
                            newAbility.rarity = originalAbility.rarity;

                            selectedAbilities.Add(newAbility);
                            abilityAdded = true;
                            Debug.Log($"Added fallback ability: {newAbility.abilityName} ({rarity})");
                            break;
                        }
                    }
                }
            }

            // FINAL SAFETY NET: AAAAAAAAAAAAAAAAAAAA
            if (!abilityAdded)
            {
                Debug.LogWarning("Not enough abilities");
            }
        }

        Debug.Log($"Returning {selectedAbilities.Count} abilities (requested {count})");
        return selectedAbilities;
    }

    // The idea is that, if the player is above or below the game's level in their own player level, their loot will get better/worse to balance the game
    float CalculateLuckModifier(int playerLevel, int gameLevel)
    {
        if (playerLevel > gameLevel)
        {
            float levelDifference = playerLevel - gameLevel;
            return Mathf.Max(0.1f, 1f - (levelDifference * 0.1f)); // Reduce luck by 10% per level difference
        }
        return 1f;
    }

    AbilityRarity GetRandomRarity(float luckModifier)
    {
        float roll = Random.Range(0f, 1f) * luckModifier;

        if (roll < 0.05f) return AbilityRarity.Legendary;
        if (roll < 0.15f) return AbilityRarity.Epic;
        if (roll < 0.35f) return AbilityRarity.Rare;
        if (roll < 0.60f) return AbilityRarity.Uncommon;
        return AbilityRarity.Common;
    }
}