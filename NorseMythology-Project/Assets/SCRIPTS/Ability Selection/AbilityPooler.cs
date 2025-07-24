using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityPooler : MonoBehaviour
{
    [Header("Ability Settings")]
    public string abilityFolderPath = "Abilities"; // Folder path in Resources

    private List<Ability> allAbilities = new List<Ability>();

    void Awake()
    {
        LoadAbilitiesFromFolder();
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

    public List<Ability> GetRandomAbilities(int count, int playerLevel, int gameLevel, List<Ability> currentAbilities)
    {
        List<Ability> selectedAbilities = new List<Ability>();

        for (int i = 0; i < count; i++)
        {
            bool added = false;

            bool tryUpgrade = currentAbilities.Count > 0 && Random.value < 0.2f;

            if (tryUpgrade)
            {
                var upgradeable = currentAbilities
                    .Where(a => a.CanLevelUp())
                    .Select(a =>
                        allAbilities.FirstOrDefault(up =>
                            up.abilityName == a.abilityName &&
                            up.CurrentLevel == a.CurrentLevel + 1)
                    )
                    .Where(upgraded => upgraded != null)
                    .ToList();

                if (upgradeable.Count > 0)
                {
                    Ability upgrade = upgradeable[Random.Range(0, upgradeable.Count)];
                    selectedAbilities.Add(upgrade);
                    added = true;
                    Debug.Log($"Added upgrade: {upgrade.abilityName} (Level {upgrade.CurrentLevel})");
                }
            }

            if (!added)
            {
                var ownedNames = currentAbilities.Select(ca => ca.abilityName).ToHashSet();
                var availableNew = allAbilities
                    .Where(a => a.CurrentLevel == 1 && !ownedNames.Contains(a.abilityName))
                    .ToList();

                if (availableNew.Count > 0)
                {
                    Ability newAbility = availableNew[Random.Range(0, availableNew.Count)];
                    selectedAbilities.Add(newAbility);
                    added = true;
                    Debug.Log($"Added new ability: {newAbility.abilityName} (Level {newAbility.CurrentLevel})");
                }
            }

            if (!added)
            {
                var fallback = allAbilities
                    .Where(a => !selectedAbilities.Any(sa => sa.abilityName == a.abilityName && sa.CurrentLevel == a.CurrentLevel))
                    .ToList();

                if (fallback.Count > 0)
                {
                    Ability fallbackAbility = fallback[Random.Range(0, fallback.Count)];
                    selectedAbilities.Add(fallbackAbility);
                    Debug.LogWarning($"Fallback added: {fallbackAbility.abilityName} (Level {fallbackAbility.CurrentLevel})");
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
