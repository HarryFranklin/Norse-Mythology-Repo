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
    public GameObject selectorPanel; // The main panel with 3 ability choices

    [Header("Replacement UI")]
    public GameObject replacementPanel; // The panel with 4 replacement slots
    public Button[] replaceButtons = new Button[4];
    public TextMeshProUGUI[] replaceButtonLabels = new TextMeshProUGUI[4];
    public Image[] replaceButtonIcons = new Image[4]; // For the icons
    public TextMeshProUGUI[] replaceButtonLevels = new TextMeshProUGUI[4]; // For the level text

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
            // Create dummy data if running scene directly
            playerData = new GameManager.PlayerData();
            Debug.LogWarning("GameManager not found, using empty player data.");
        }
        
        SetupUI();
    }
    
    void SetupUI()
    {
        for (int i = 0; i < abilityButtons.Length; i++)
        {
            int buttonIndex = i; 
            abilityButtons[i].onClick.AddListener(() => SelectAbility(buttonIndex));
        }
        
        if (skipButton != null) skipButton.onClick.AddListener(SkipSelection);
        
        if (levelText != null && GameManager.Instance != null)
            levelText.text = $"Level {GameManager.Instance.gameLevel} -> {GameManager.Instance.gameLevel + 1}";

        GenerateAbilityOptions();
    }

    public void SelectAbility(int index)
    {
        SetAbilityButtonsInteractable(false); // Prevent multiple clicks

        Ability selectedAbility = offeredAbilities[index];
        var existingState = playerData.abilities.FirstOrDefault(state => state.ability != null && state.ability.abilityName == selectedAbility.abilityName);

        if (existingState != null)
        {
            existingState.level++;
            FinaliseAndReturn();
        }
        else
        {
            if (playerData.abilities.Count < 4)
            {
                 playerData.abilities.Add(new GameManager.PlayerAbilityState(selectedAbility, 1));
                 FinaliseAndReturn();
            }
            else
            {
                // This is the path that was likely failing
                ShowReplacementOptions(selectedAbility);
            }
        }
    }

    void ShowReplacementOptions(Ability newAbility)
    {
        if (selectorPanel != null) selectorPanel.SetActive(false);
        if (replacementPanel == null)
        {
            Debug.LogError("Replacement Panel is not assigned in the Inspector! Halting.");
            return; // Stop execution if the panel is missing
        }
        replacementPanel.SetActive(true);

        // Loop through the 4 equipped abilities and display them
        for (int i = 0; i < replaceButtons.Length; i++)
        {
            int replaceIndex = i; // Important for the listener
            GameManager.PlayerAbilityState equippedAbilityState = playerData.abilities[i];
            Ability equippedAbility = equippedAbilityState.ability;

            if (replaceButtonLabels[i] != null)
                replaceButtonLabels[i].text = equippedAbility.abilityName;

            if (replaceButtonIcons[i] != null)
                replaceButtonIcons[i].sprite = equippedAbility.abilityIcon;
            
            if (replaceButtonLevels[i] != null)
            {
                int maxStacks = equippedAbility.GetStatsForLevel(equippedAbilityState.level).maxStacksAtLevel;
                replaceButtonLevels[i].text = $"Lvl {equippedAbilityState.level} ({maxStacks} max)";
            }

            replaceButtons[i].onClick.RemoveAllListeners();
            replaceButtons[i].onClick.AddListener(() =>
            {
                playerData.abilities[replaceIndex] = new GameManager.PlayerAbilityState(newAbility, 1);
                if (replacementPanel != null) replacementPanel.SetActive(false);
                FinaliseAndReturn();
            });
        }
    }

    void FinaliseAndReturn()
    {
        GameManager.Instance.currentPlayerData = playerData;

        if (levelUpManager != null)
        {
            levelUpManager.OnAbilitySelectionCompleted();
        }

        // Deactivate the parent GameObject that holds all the ability selection UI
        gameObject.SetActive(false);
    }
    
    // --- Helper methods and the rest of the script ---
    private void SetAbilityButtonsInteractable(bool isInteractable)
    {
        foreach (var button in abilityButtons)
        {
            if (button != null) button.interactable = isInteractable;
        }
        if (skipButton != null) skipButton.interactable = isInteractable;
    }

    void GenerateAbilityOptions()
    {
        if (AbilityPooler.Instance == null) return;
        offeredAbilities = AbilityPooler.Instance.GetAbilityChoices(playerData.abilities);
        
        for (int i = 0; i < offeredAbilities.Count; i++)
        {
            if (i < abilityButtons.Length)
            {
                 UpdateAbilityDisplay(i, offeredAbilities[i]);
            }
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

        if (abilityButtons[index] != null)
        {
            ColorBlock colors = abilityButtons[index].colors;
            colors.normalColor = GetRarityColor(ability.rarity);
            colors.highlightedColor = GetRarityColor(ability.rarity) * 1.2f; 
            abilityButtons[index].colors = colors;
        }
        
        if (abilityIcons[index] != null) abilityIcons[index].sprite = ability.abilityIcon;
    }

    public void SkipSelection()
    {
        SetAbilityButtonsInteractable(false);
        FinaliseAndReturn();
    }
    
    // ... (GetStatUpgradeDescription and GetRarityColor methods remain the same) ...
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
}