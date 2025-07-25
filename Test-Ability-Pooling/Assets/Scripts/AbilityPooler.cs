using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityPooler : MonoBehaviour
{
    [Header("Ability Settings")]
    public string abilityFolderPath = "Abilities"; // Folder path in Resources

    private List<Ability> allAbilities = new List<Ability>();
    private Dictionary<string, List<Ability>> abilitiesByName = new Dictionary<string, List<Ability>>();

    void Awake()
    {
        LoadAbilitiesFromFolder();
        InitialiseAbilityPool();
    }

    void LoadAbilitiesFromFolder()
    {
        Ability[] loadedAbilities = Resources.LoadAll<Ability>(abilityFolderPath);
        allAbilities = new List<Ability>(loadedAbilities);

        Debug.Log($"Loaded {allAbilities.Count} abilities from {abilityFolderPath} folder");

        if (allAbilities.Count == 0)
        {
            Debug.LogWarning($"No abilities found in Resources/{abilityFolderPath}/ folder.");
        }
    }

    void InitialiseAbilityPool()
    {
        // Group abilities by name and sort by level
        abilitiesByName = allAbilities
            .GroupBy(a => a.abilityName)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.abilityLevel).ToList()
            );
    }

    public List<Ability> GetRandomAbilities(int count, int playerLevel, int gameLevel, List<Ability> currentAbilities)
    {
        List<Ability> selectedAbilities = new List<Ability>();

        // Make a copy of all ability names
        List<string> allNames = abilitiesByName.Keys.ToList();

        // 1. Try to add upgrades first (20% chance per slot if any are upgradable)
        for (int i = 0; i < count; i++)
        {
            bool added = false;

            bool tryUpgrade = currentAbilities.Count > 0 && Random.value < 0.2f;

            if (tryUpgrade)
            {
                var upgradeable = currentAbilities
                    .Where(a => a.CanUpgrade() && abilitiesByName.ContainsKey(a.abilityName))
                    .Select(a =>
                        abilitiesByName[a.abilityName]
                            .FirstOrDefault(up => up.abilityLevel == a.abilityLevel + 1)
                    )
                    .Where(upgraded => upgraded != null)
                    .ToList();

                if (upgradeable.Count > 0)
                {
                    Ability upgrade = upgradeable[Random.Range(0, upgradeable.Count)];
                    selectedAbilities.Add(upgrade);
                    added = true;
                    Debug.Log($"Added upgrade: {upgrade.abilityName} (Level {upgrade.abilityLevel})");
                }
            }

            if (!added)
            {
                // Try to add a new ability not already owned
                var availableNew = abilitiesByName
                    .Where(kvp => !currentAbilities.Any(ca => ca.abilityName == kvp.Key))
                    .Select(kvp => kvp.Value.FirstOrDefault(a => a.abilityLevel == 1))
                    .Where(a => a != null)
                    .ToList();

                if (availableNew.Count > 0)
                {
                    Ability newAbility = availableNew[Random.Range(0, availableNew.Count)];
                    selectedAbilities.Add(newAbility);
                    added = true;
                    Debug.Log($"Added new ability: {newAbility.abilityName} (Level {newAbility.abilityLevel})");
                }
            }

            // SAFETY FALLBACK: Offer random non-duplicate if nothing else works
            if (!added)
            {
                var fallback = allAbilities
                    .Where(a => !selectedAbilities.Any(sa => sa.abilityName == a.abilityName && sa.abilityLevel == a.abilityLevel))
                    .ToList();

                if (fallback.Count > 0)
                {
                    Ability fallbackAbility = fallback[Random.Range(0, fallback.Count)];
                    selectedAbilities.Add(fallbackAbility);
                    Debug.LogWarning($"Fallback added: {fallbackAbility.abilityName} (Level {fallbackAbility.abilityLevel})");
                }
                else
                {
                    Debug.LogWarning("No abilities left to offer, returned fewer than requested.");
                    break;
                }
            }
        }

        return selectedAbilities;
    }
}
