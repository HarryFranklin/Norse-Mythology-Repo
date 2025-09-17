using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityPooler : MonoBehaviour
{
    public static AbilityPooler Instance { get; private set; }

    [Header("Ability Settings")]
    public string abilityFolderPath = "Abilities"; 

    private List<Ability> allAbilities = new List<Ability>();

    void Awake()
    {
        // Singleton pattern to ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make it persist between scenes
            LoadAbilities();
        }
        else
        {
            Destroy(gameObject); // Destroy any duplicates
        }
    }

    void LoadAbilities()
    {
        allAbilities = Resources.LoadAll<Ability>(abilityFolderPath).ToList();
        Debug.Log($"[AbilityPooler] Loaded {allAbilities.Count} abilities from Resources.");
    }

    public List<Ability> GetAbilityChoices(List<GameManager.PlayerAbilityState> currentAbilities, int count = 3)
    {
        var choices = new List<Ability>();
        var ownedAbilityNames = currentAbilities.Select(state => state.ability.abilityName).ToHashSet();

        // Find upgradeable abilities
        var upgradeable = currentAbilities
            .Where(state => state.ability != null && state.level < state.ability.MaxLevel)
            .Select(state => state.ability)
            .ToList();
        
        // Find new abilities
        var newAbilities = allAbilities
            .Where(a => !ownedAbilityNames.Contains(a.abilityName))
            .ToList();

        // Prioritize upgrades, then new abilities
        var potentialChoices = upgradeable.Concat(newAbilities).Distinct().ToList();
        
        while (choices.Count < count && potentialChoices.Count > 0)
        {
            int randIndex = Random.Range(0, potentialChoices.Count);
            choices.Add(potentialChoices[randIndex]);
            potentialChoices.RemoveAt(randIndex);
        }
        
        return choices;
    }
    
    public Ability GetAbilityByName(string name)
    {
        return allAbilities.FirstOrDefault(a => a.abilityName == name);
    }
}