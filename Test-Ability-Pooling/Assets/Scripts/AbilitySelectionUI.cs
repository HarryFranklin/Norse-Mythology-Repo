using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class AbilitySelectionUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button[] abilityButtons = new Button[3];
    public Button skipButton;
    public Button continueButton;
    public TextMeshProUGUI levelText;

    [Header("Selector UI")]
    public GameObject selectorPanel;

    [Header("Replacement UI")]
    public GameObject replacementPanel;
    public Button[] replaceButtons = new Button[4];
    public TextMeshProUGUI[] replaceButtonLabels = new TextMeshProUGUI[4];

    
    [Header("Ability Display")]
    public TextMeshProUGUI[] abilityNames = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] abilityDescriptions = new TextMeshProUGUI[3];
    public Image[] abilityIcons = new Image[3];
    
    private List<Ability> offeredAbilities;
    private int selectedAbilityIndex = -1;
    private AbilityPooler abilityPooler;
    private GameManager.PlayerData playerData;
    
    void Start()
    {
        abilityPooler = FindObjectOfType<AbilityPooler>();
        
        // Get player data from GameManager
        if (GameManager.Instance != null)
        {
            playerData = GameManager.Instance.GetPlayerData();
        }
        else
        {
            // Fallback if no GameManager
            playerData = new GameManager.PlayerData();
        }
        
        SetupUI();
        GenerateAbilityOptions();
    }
    
    void SetupUI()
    {
        // Setup button listeners
        for (int i = 0; i < abilityButtons.Length; i++)
        {
            int buttonIndex = i; // Capture for closure
            abilityButtons[i].onClick.AddListener(() => SelectAbility(buttonIndex));
        }
        
        skipButton.onClick.AddListener(SkipSelection);
        continueButton.onClick.AddListener(ContinueToNextLevel);
        continueButton.interactable = false;
        
        // Update level display
        if (levelText != null)
            levelText.text = $"Level {GameManager.Instance.gameLevel} -> {GameManager.Instance.gameLevel + 1}";
    }
    
    void GenerateAbilityOptions()
    {
        offeredAbilities = abilityPooler.GetRandomAbilities(3, playerData.playerLevel, GameManager.Instance.gameLevel, playerData.abilities);
        
        for (int i = 0; i < offeredAbilities.Count && i < abilityButtons.Length; i++)
        {
            UpdateAbilityDisplay(i, offeredAbilities[i]);
        }
    }
    
    void UpdateAbilityDisplay(int index, Ability ability)
    {
        if (abilityNames[index] != null)
            abilityNames[index].text = $"{ability.abilityName} {(ability.abilityLevel > 1 ? $"(Level {ability.abilityLevel})" : "")}";
        
        if (abilityDescriptions[index] != null)
            abilityDescriptions[index].text = ability.description;
        
        if (abilityIcons[index] != null)
            abilityIcons[index].color = ability.abilityColor;

        // Check if the ability is a duplicate and not an upgrade
        bool isDuplicate = playerData.abilities.Exists(a =>
            a.abilityName == ability.abilityName &&
            a.abilityLevel >= ability.abilityLevel);

        abilityButtons[index].interactable = !isDuplicate;
                
        // Colour button based on rarity
        ColorBlock colors = abilityButtons[index].colors;
        colors.normalColor = GetRarityColor(ability.rarity);
        abilityButtons[index].colors = colors;
    }
    
    Color GetRarityColor(AbilityRarity rarity)
    {
        switch (rarity)
        {
            case AbilityRarity.Common: return Color.white;
            case AbilityRarity.Uncommon: return Color.green;
            case AbilityRarity.Rare: return Color.blue;
            case AbilityRarity.Epic: return Color.magenta;
            case AbilityRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }
    
    public void SelectAbility(int index)
    {
        selectedAbilityIndex = index;
        Ability selected = offeredAbilities[index];

        // Check for upgrade
        for (int i = 0; i < playerData.abilities.Count; i++)
        {
            if (playerData.abilities[i].abilityName == selected.abilityName)
            {
                if (selected.abilityLevel > playerData.abilities[i].abilityLevel)
                {
                    playerData.abilities[i] = selected;
                    FinaliseSelection();
                    return;
                }
                else
                {
                    // Same level or lower — do nothing
                    Debug.Log("Same or lower level ability already owned.");
                    return;
                }
            }
        }

        // It's a new ability — need to replace
        if (playerData.abilities.Count < 4)
        {
            playerData.abilities.Add(selected);
            FinaliseSelection();
        }
        else
        {
            ShowReplacementOptions(selected);
        }
    }

    void ShowReplacementOptions(Ability newAbility)
    {
        selectorPanel.SetActive(false);
        replacementPanel.SetActive(true);

        for (int i = 0; i < replaceButtons.Length; i++)
        {
            int replaceIndex = i;
            replaceButtonLabels[i].text = playerData.abilities[i].abilityName;
            replaceButtons[i].onClick.RemoveAllListeners();
            replaceButtons[i].onClick.AddListener(() =>
            {
                playerData.abilities[replaceIndex] = newAbility;
                replacementPanel.SetActive(false);
                FinaliseSelection();
            });
        }
    }
    
    public void SkipSelection()
    {
        selectedAbilityIndex = -1;
        selectorPanel.SetActive(false); // Hide selector panel on skip
        FinaliseSelection();            // Call finalise here directly
    }
    
    void FinaliseSelection()
    {
        selectorPanel.SetActive(false); // Hide the main selector panel
        replacementPanel.SetActive(false); // Just in case it's still open
        continueButton.interactable = false;

        playerData.playerLevel++;
        GameManager.Instance.UpdatePlayerData(playerData);
        GameManager.Instance.StartNextLevel();
    }
    
    public void ContinueToNextLevel()
    {
        if (selectedAbilityIndex == -1)
        {
            // Player skipped
            playerData.playerLevel++;
            GameManager.Instance.UpdatePlayerData(playerData);
            GameManager.Instance.StartNextLevel();
        }
    }
}