using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Text;

public class AbilitySelectorManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button[] abilityButtons = new Button[3];
    public Button skipButton;
    public TextMeshProUGUI levelText;

    [Header("Selector UI")]
    public GameObject selectorPanel;

    [Header("Replacement UI")]
    public GameObject replacementPanel;
    public Button[] replaceButtons = new Button[4];
    public TextMeshProUGUI[] replaceButtonLabels = new TextMeshProUGUI[4];
    public Image[] replaceButtonIcons = new Image[4];
    public TextMeshProUGUI[] replaceButtonLevels = new TextMeshProUGUI[4];

    [Header("Ability Display")]
    public TextMeshProUGUI[] abilityNames = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] abilityDescriptions = new TextMeshProUGUI[3];
    public Image[] abilityIcons = new Image[3];
    
    private List<Ability> offeredAbilities;
    private GameManager.PlayerData playerData;
    
    private LevelUpManager levelUpManager;
    
    void Start()
    {
        levelUpManager = FindFirstObjectByType<LevelUpManager>();
        
        if (GameManager.Instance != null)
        {
            playerData = GameManager.Instance.currentPlayerData;
        }
        else
        {
            playerData = new GameManager.PlayerData();
        }
        
        SetupUI();
        ShowAbilitySelector(); 
    }
    
    void SetupUI()
    {
        for (int i = 0; i < abilityButtons.Length; i++)
        {
            int buttonIndex = i; 
            abilityButtons[i].onClick.AddListener(() => SelectAbility(buttonIndex));
        }
        
        skipButton.onClick.AddListener(SkipSelection);
        
        if (levelText != null && GameManager.Instance != null)
            levelText.text = $"Level {GameManager.Instance.gameLevel} -> {GameManager.Instance.gameLevel + 1}";
    }
    
    public void ShowAbilitySelector()
    {
        if (GameManager.Instance != null)
        {
            playerData = GameManager.Instance.currentPlayerData;
        }
        
        GenerateAbilityOptions();
        
        if (selectorPanel != null)
            selectorPanel.SetActive(true);
        
        if (replacementPanel != null)
            replacementPanel.SetActive(false);
    }
    
    void GenerateAbilityOptions()
    {
        if (AbilityPooler.Instance == null) return;
        offeredAbilities = AbilityPooler.Instance.GetAbilityChoices(playerData.abilities);
        
        for (int i = 0; i < offeredAbilities.Count; i++)
        {
            UpdateAbilityDisplay(i, offeredAbilities[i]);
        }
    }

    void UpdateAbilityDisplay(int index, Ability ability)
    {
        var existingState = playerData.abilities.FirstOrDefault(state => state.ability != null && state.ability.abilityName == ability.abilityName);

        if (existingState != null)
        {
            int nextLevel = existingState.level + 1;
            abilityNames[index].text = $"{ability.abilityName} (Lvl {existingState.level} -> {nextLevel})";
            abilityDescriptions[index].text = GetStatUpgradeDescription(
                ability.GetStatsForLevel(existingState.level),
                ability.GetStatsForLevel(nextLevel)
            );
        }
        else
        {
            abilityNames[index].text = $"{ability.abilityName} (New!)";
            abilityDescriptions[index].text = ability.description;
        }

        ColorBlock colors = abilityButtons[index].colors;
        colors.normalColor = GetRarityColor(ability.rarity);
        colors.highlightedColor = GetRarityColor(ability.rarity) * 1.2f; 
        abilityButtons[index].colors = colors;
        
        if (abilityIcons[index] != null) abilityIcons[index].sprite = ability.abilityIcon;
        abilityButtons[index].interactable = true;
    }

    private string GetStatUpgradeDescription(AbilityLevelData oldStats, AbilityLevelData newStats)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>Upgrades:</b>"); 
        if (newStats.cooldown < oldStats.cooldown)
            sb.AppendLine($"Cooldown: {oldStats.cooldown:F1}s -> <color=green>{newStats.cooldown:F1}s</color>");
        if (newStats.damage > oldStats.damage)
            sb.AppendLine($"Damage: {oldStats.damage:F1} -> <color=green>{newStats.damage:F1}</color>");
        if (newStats.duration > oldStats.duration)
            sb.AppendLine($"Duration: {oldStats.duration:F1}s -> <color=green>{newStats.duration:F1}s</color>");
        if (newStats.radius > oldStats.radius)
            sb.AppendLine($"Radius: {oldStats.radius:F1}m -> <color=green>{newStats.radius:F1}m</color>");
        if (newStats.distance > oldStats.distance)
            sb.AppendLine($"Distance: {oldStats.distance:F1}m -> <color=green>{newStats.distance:F1}m</color>");
        if (newStats.maxStacksAtLevel > oldStats.maxStacksAtLevel)
            sb.AppendLine($"Max Charges: {oldStats.maxStacksAtLevel} -> <color=green>{newStats.maxStacksAtLevel}</color>");
        if (newStats.stackRegenTime < oldStats.stackRegenTime && newStats.maxStacksAtLevel > 1)
            sb.AppendLine($"Charge Regen: {oldStats.stackRegenTime:F1}s -> <color=green>{newStats.stackRegenTime:F1}s</color>");
        if (sb.Length <= "<b>Upgrades:</b>\n".Length)
            sb.AppendLine("General improvements to effectiveness.");
        return sb.ToString();
    }

    Color GetRarityColor(AbilityRarity rarity)
    {
        switch (rarity)
        {
            case AbilityRarity.Common: return Color.white;
            case AbilityRarity.Uncommon: return new Color(0.1f, 1f, 0.1f);
            case AbilityRarity.Rare: return new Color(0.2f, 0.6f, 1f); 
            case AbilityRarity.Epic: return new Color(0.7f, 0.2f, 1f); 
            case AbilityRarity.Legendary: return new Color(1f, 0.8f, 0f); 
            default: return Color.white;
        }
    }
    
    public void SelectAbility(int index)
    {
        Ability selected = offeredAbilities[index];
        var existingState = playerData.abilities.FirstOrDefault(state => state.ability != null && state.ability.abilityName == selected.abilityName);

        if (existingState != null)
        {
            existingState.level++;
            FinaliseAndReturn();
        }
        else
        {
            if (playerData.abilities.Count < 4)
            {
                 playerData.abilities.Add(new GameManager.PlayerAbilityState(selected, 1));
                 // CHANGED: Finalise the selection and return to the LevelUpManager
                 FinaliseAndReturn();
            }
            else
            {
                ShowReplacementOptions(selected);
            }
        }
    }

    void ShowReplacementOptions(Ability newAbility)
    {
        selectorPanel.SetActive(false);
        replacementPanel.SetActive(true);

        for (int i = 0; i < replaceButtons.Length; i++)
        {
            int replaceIndex = i;
            var existingAbilityState = playerData.abilities[i];
            
            // --- START of CHANGES ---
            
            // Update the ability name label
            if (replaceButtonLabels[i] != null)
                replaceButtonLabels[i].text = existingAbilityState.ability.abilityName;

            // Update the ability icon
            if (replaceButtonIcons[i] != null)
            {
                replaceButtonIcons[i].sprite = existingAbilityState.ability.abilityIcon;
                replaceButtonIcons[i].enabled = true;
            }
                
            // Update the ability level/stack text
            if (replaceButtonLevels[i] != null)
            {
                var ability = existingAbilityState.ability;
                // This fetches the max stacks for the current level of the ability
                int maxStacks = ability.GetStatsForLevel(existingAbilityState.level).maxStacksAtLevel;
                replaceButtonLevels[i].text = $"Lvl {existingAbilityState.level} ({maxStacks} max)";
            }
            
            replaceButtons[i].onClick.RemoveAllListeners();
            replaceButtons[i].onClick.AddListener(() =>
            {
                playerData.abilities[replaceIndex] = new GameManager.PlayerAbilityState(newAbility, 1);
                replacementPanel.SetActive(false);
                FinaliseAndReturn();
            });
        }
    }
    
    public void SkipSelection()
    {
        FinaliseAndReturn();
    }
    
    void FinaliseAndReturn()
    {
        // Save the player's ability choices back to the GameManager
        GameManager.Instance.currentPlayerData = playerData;

        // Notify the LevelUpManager that selection is complete
        if (levelUpManager != null)
        {
            levelUpManager.OnAbilitySelectionCompleted();
        }

        // Hide this entire UI panel
        gameObject.SetActive(false);
    }
}