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
        LoadAbilities();
    }

    void LoadAbilities()
    {
        allAbilities = Resources.LoadAll<Ability>(abilityFolderPath).ToList();
        Debug.Log($"Loaded {allAbilities.Count} abilities from Resources.");
    }

    public List<Ability> GetAbilityChoices(List<GameManager.PlayerAbilityState> currentAbilities, int count = 3)
    {
        var choices = new List<Ability>();
        var ownedAbilityNames = currentAbilities.Select(state => state.ability.abilityName).ToHashSet();

        // 1. Find upgradeable abilities
        var upgradeable = currentAbilities
            .Where(state => state.ability != null && state.level < state.ability.MaxLevel)
            .Select(state => state.ability)
            .ToList();
        
        // 2. Find new abilities
        var newAbilities = allAbilities
            .Where(a => !ownedAbilityNames.Contains(a.abilityName))
            .ToList();

        // 3. Prioritize upgrades, then new abilities
        var potentialChoices = upgradeable.Concat(newAbilities).Distinct().ToList();

        // 4. Shuffle and take the required number of choices
        while (choices.Count < count && potentialChoices.Count > 0)
        {
            int randIndex = Random.Range(0, potentialChoices.Count);
            choices.Add(potentialChoices[randIndex]);
            potentialChoices.RemoveAt(randIndex);
        }
        
        // 5. Fallback: If not enough choices, add random owned abilities that can be upgraded
        if (choices.Count < count)
        {
            var fallbackPool = allAbilities
                .Where(a => !choices.Any(c => c.abilityName == a.abilityName))
                .ToList();
            
            while (choices.Count < count && fallbackPool.Count > 0)
            {
                int randIndex = Random.Range(0, fallbackPool.Count);
                choices.Add(fallbackPool[randIndex]);
                fallbackPool.RemoveAt(randIndex);
            }
        }

        return choices;
    }
    
    public Ability GetAbilityByName(string name)
    {
        return allAbilities.FirstOrDefault(a => a.abilityName == name);
    }
}