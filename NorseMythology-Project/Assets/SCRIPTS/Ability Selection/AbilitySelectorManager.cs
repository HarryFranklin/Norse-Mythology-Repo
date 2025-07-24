using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class AbilitySelectorManager : MonoBehaviour
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
    
    [Header("Continue UI")]
    public GameObject continuePanel;

    
    [Header("Ability Display")]
    public TextMeshProUGUI[] abilityNames = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] abilityDescriptions = new TextMeshProUGUI[3];
    public Image[] abilityIcons = new Image[3];
    
    private List<Ability> offeredAbilities;
    private int selectedAbilityIndex = -1;
    [SerializeField] private AbilityPooler abilityPooler;
    private GameManager.PlayerData playerData;
    
    void Start()
    {
        if (abilityPooler == null)
        {
            abilityPooler = FindFirstObjectByType<AbilityPooler>();
        }
        
        // Get player data from GameManager
        if (GameManager.Instance != null)
        {
            playerData = GameManager.Instance.currentPlayerData;
        }
        else
        {
            // Fallback if no GameManager
            playerData = new GameManager.PlayerData();
        }
        
        SetupUI();
        
        // Hide all panels initially - they will be shown when called by LevelUpManager
        if (selectorPanel != null)
            selectorPanel.SetActive(false);
        if (replacementPanel != null)
            replacementPanel.SetActive(false);
        if (continuePanel != null)
            continuePanel.SetActive(false);
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
        if (levelText != null && GameManager.Instance != null)
            levelText.text = $"Level {GameManager.Instance.gameLevel} -> {GameManager.Instance.gameLevel + 1}";
    }
    
    // Public method to show the ability selector
    public void ShowAbilitySelector()
    {
        Debug.Log("Showing ability selector");
        
        // Make sure we have fresh player data
        if (GameManager.Instance != null)
        {
            playerData = GameManager.Instance.currentPlayerData;
        }
        
        // Generate new ability options
        GenerateAbilityOptions();
        
        // Show the selector panel
        if (selectorPanel != null)
            selectorPanel.SetActive(true);
        
        // Hide other panels
        if (replacementPanel != null)
            replacementPanel.SetActive(false);
        if (continuePanel != null)
            continuePanel.SetActive(false);
        
        // Reset continue button state
        continueButton.interactable = false;
        selectedAbilityIndex = -1;
        
        // Update level display
        if (levelText != null && GameManager.Instance != null)
            levelText.text = $"Level {GameManager.Instance.gameLevel} -> {GameManager.Instance.gameLevel + 1}";
    }
    
    void GenerateAbilityOptions()
    {
        if (abilityPooler == null)
        {
            Debug.LogError("AbilityPooler not found!");
            return;
        }
        
        offeredAbilities = abilityPooler.GetRandomAbilities(3, playerData.playerLevel, GameManager.Instance.gameLevel, playerData.abilities);
        
        for (int i = 0; i < offeredAbilities.Count && i < abilityButtons.Length; i++)
        {
            UpdateAbilityDisplay(i, offeredAbilities[i]);
        }
    }
    
    void UpdateAbilityDisplay(int index, Ability ability)
    {
        if (abilityNames[index] != null)
            abilityNames[index].text = $"{ability.abilityName} {(ability.CurrentLevel > 1 ? $"(Level {ability.CurrentLevel})" : "")}";
        
        if (abilityDescriptions[index] != null)
            abilityDescriptions[index].text = ability.description;
        
        if (abilityIcons[index] != null && ability.abilityIcon != null)
            abilityIcons[index].sprite = ability.abilityIcon;

        // Check if the ability is a duplicate and not an upgrade
        bool isDuplicate = playerData.abilities.Exists(a =>
            a.abilityName == ability.abilityName &&
            a.CurrentLevel >= ability.CurrentLevel);

        abilityButtons[index].interactable = !isDuplicate;
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
                if (selected.CurrentLevel > playerData.abilities[i].CurrentLevel)
                {
                    playerData.abilities[i] = selected;
                    // Hide selector panel and show continue panel for upgrades
                    if (selectorPanel != null)
                        selectorPanel.SetActive(false);
                    ShowContinuePanel();
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
            // Hide selector panel and show continue panel for new abilities
            if (selectorPanel != null)
                selectorPanel.SetActive(false);
            ShowContinuePanel();
        }
        else
        {
            // Hide selector panel and show replacement options
            ShowReplacementOptions(selected);
        }
    }

    void ShowReplacementOptions(Ability newAbility)
    {
        // Hide selector panel first
        if (selectorPanel != null)
            selectorPanel.SetActive(false);
            
        // Then show replacement panel
        if (replacementPanel != null)
            replacementPanel.SetActive(true);

        for (int i = 0; i < replaceButtons.Length; i++)
        {
            int replaceIndex = i;
            replaceButtonLabels[i].text = playerData.abilities[i].abilityName;
            replaceButtons[i].onClick.RemoveAllListeners();
            replaceButtons[i].onClick.AddListener(() =>
            {
                playerData.abilities[replaceIndex] = newAbility;
                // Hide replacement panel and show continue panel
                if (replacementPanel != null)
                    replacementPanel.SetActive(false);
                ShowContinuePanel();
            });
        }
    }
    
    public void SkipSelection()
    {
        selectedAbilityIndex = -1;
        // Hide selector panel and show continue panel when skipping
        if (selectorPanel != null)
            selectorPanel.SetActive(false);
        ShowContinuePanel();
    }
    
    void ShowContinuePanel()
    {
        // Show the continue panel so player can proceed to next level
        if (continuePanel != null)
            continuePanel.SetActive(true);
            
        // Finalise the selection
        FinaliseSelection();
    }
    
    void FinaliseSelection()
    {
        // Ensure selector and replacement panels are hidden
        if (selectorPanel != null)
            selectorPanel.SetActive(false);
        if (replacementPanel != null)
            replacementPanel.SetActive(false);
            
        // Enable continue button (continue panel should be visible at this point)
        continueButton.interactable = true;

        playerData.playerLevel++;
        
        // Update the GameManager's player data directly
        GameManager.Instance.currentPlayerData = playerData;
        
        Debug.Log($"Ability selection finalised. Player level: {playerData.playerLevel}, Abilities: {playerData.abilities.Count}");
    }
    
    public void ContinueToNextLevel()
    {
        Debug.Log("Ability selector continue button clicked - proceeding to next wave");
        
        // Hide the continue panel
        if (continuePanel != null)
            continuePanel.SetActive(false);
        
        // Use GameManager's existing method to continue to next wave
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ContinueToNextWave();
        }
        else
        {
            Debug.LogError("GameManager Instance is null!");
        }
    }
}