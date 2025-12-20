using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Text;
using UnityEngine.EventSystems;

public class AbilitySelectorManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button[] abilityButtons = new Button[3];
    public Image[] abilityRarityPanels = new Image[3];
    public Button skipButton;
    public TextMeshProUGUI levelText;

    [Header("Selector UI")]
    public GameObject selectorPanel;

    [Header("Tooltips")]
    public AbilityTooltipPanel[] tooltipPanels; 

    [Header("Replacement UI")]
    public GameObject replacementPanel;
    public Button[] replaceButtons = new Button[4];
    public Image[] replaceRarityPanels = new Image[4];
    public TextMeshProUGUI[] replaceButtonLabels = new TextMeshProUGUI[4];
    public Image[] replaceButtonIcons = new Image[4];
    public TextMeshProUGUI[] replaceButtonLevels = new TextMeshProUGUI[4];

    [Header("Ability Display")]
    public TextMeshProUGUI[] abilityNames = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] abilityDescriptions = new TextMeshProUGUI[3];
    public Image[] abilityIcons = new Image[3];
    
    [Header("Debug Settings")]
    [Tooltip("Tick this to force debug mode even if playing from the main menu.")]
    public bool forceDebugMode = false;
    [Tooltip("Drag abilities here to test the menu in isolation.")]
    public List<Ability> debugAbilities; 

    private List<Ability> offeredAbilities;
    private GameManager.PlayerData playerData;
    private LevelUpManager levelUpManager;
    
    // Automatically treats it as "Debug Mode" if GameManager is missing OR if you forced it.
    private bool IsDebugMode => GameManager.Instance == null || forceDebugMode;

    void Start()
    {
        levelUpManager = FindFirstObjectByType<LevelUpManager>();
        
        if (!IsDebugMode)
        {
            playerData = GameManager.Instance.currentPlayerData;
        }
        else
        {
            // Debug Flow: Create empty/dummy data
            playerData = new GameManager.PlayerData();
            Debug.LogWarning("AbilitySelectorManager: DEBUG MODE ACTIVE (GameManager missing or forced). Using dummy player data.");
        }
        
        SetupUI();
    }
    
    void SetupUI()
    {
        for (int i = 0; i < abilityButtons.Length; i++)
        {
            int buttonIndex = i; 
            abilityButtons[i].onClick.AddListener(() => SelectAbility(buttonIndex));

            // Setup Hover Events on the ICONS
            if (abilityIcons[i] != null)
            {
                // Ensure raycast target is ON so the mouse is detected
                abilityIcons[i].raycastTarget = true; 
                AddHoverEvents(abilityIcons[i].gameObject, buttonIndex);
            }
        }
        
        if (skipButton != null) skipButton.onClick.AddListener(SkipSelection);
        
        if (levelText != null)
        {
             int currentLvl = (GameManager.Instance != null) ? GameManager.Instance.gameLevel : 1;
             levelText.text = $"Level {currentLvl} -> {currentLvl + 1}";
        }

        GenerateAbilityOptions();
    }

    private void AddHoverEvents(GameObject targetObject, int index)
    {
        EventTrigger trigger = targetObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = targetObject.AddComponent<EventTrigger>();
        
        trigger.triggers.Clear();

        // HOVER ENTER
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => {
            if (offeredAbilities != null && index < offeredAbilities.Count)
            {
                if (tooltipPanels != null && index < tooltipPanels.Length && tooltipPanels[index] != null)
                {
                    Ability ability = offeredAbilities[index];
                    int currentLvl = 0;
                    var existing = playerData.abilities.FirstOrDefault(a => a.ability.abilityName == ability.abilityName);
                    if (existing != null) currentLvl = existing.level;
                    
                    tooltipPanels[index].ShowTooltip(ability, currentLvl);
                }
            }
        });
        trigger.triggers.Add(pointerEnter);
        
        // HOVER EXIT
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => {
            if (tooltipPanels != null && index < tooltipPanels.Length && tooltipPanels[index] != null)
            {
                tooltipPanels[index].HideTooltip();
            }
        });
        trigger.triggers.Add(pointerExit);
    }

    public void SelectAbility(int index)
    {
        SetAbilityButtonsInteractable(false);
        HideAllTooltips(); 

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
                ShowReplacementOptions(selectedAbility);
            }
        }
    }

    void ShowReplacementOptions(Ability newAbility)
    {
        if (selectorPanel != null) selectorPanel.SetActive(false);
        if (replacementPanel != null)
        {
            replacementPanel.SetActive(true);
            for (int i = 0; i < replaceButtons.Length; i++)
            {
                int replaceIndex = i;
                // Safety check for debug mode where you might have < 4 abilities
                if (replaceIndex >= playerData.abilities.Count) 
                {
                    replaceButtons[i].gameObject.SetActive(false);
                    continue;
                }
                
                replaceButtons[i].gameObject.SetActive(true);
                GameManager.PlayerAbilityState equippedAbilityState = playerData.abilities[i];
                Ability equippedAbility = equippedAbilityState.ability;

                if (replaceButtonLabels[i] != null) replaceButtonLabels[i].text = equippedAbility.abilityName;
                if (replaceButtonIcons[i] != null) replaceButtonIcons[i].sprite = equippedAbility.abilityIcon;
                if (replaceButtonLevels[i] != null) replaceButtonLevels[i].text = $"Lvl {equippedAbilityState.level}";

                if(replaceRarityPanels[i] != null)
                {
                    AbilityRarity currentRarity = RarityColourMapper.GetRarityFromLevel(equippedAbilityState.level);
                    replaceRarityPanels[i].color = RarityColourMapper.GetColour(currentRarity);
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
    }

    void FinaliseAndReturn()
    {
        // Only save to GameManager if we are NOT in debug mode
        if (!IsDebugMode && GameManager.Instance != null)
        {
            GameManager.Instance.currentPlayerData = playerData;
        }
        else
        {
            Debug.Log($"[Debug Mode] Selection Finalised. Picked: {playerData.abilities.LastOrDefault()?.ability.abilityName}");
        }
        
        if (levelUpManager != null) levelUpManager.OnAbilitySelectionCompleted();
        gameObject.SetActive(false);
    }
    
    private void SetAbilityButtonsInteractable(bool isInteractable)
    {
        foreach (var button in abilityButtons)
            if (button != null) button.interactable = isInteractable;
        if (skipButton != null) skipButton.interactable = isInteractable;
    }

    private void HideAllTooltips()
    {
        if (tooltipPanels != null)
        {
            foreach (var panel in tooltipPanels)
            {
                if (panel != null) panel.HideTooltip();
            }
        }
    }

    void GenerateAbilityOptions()
    {
        // If NOT in debug mode (and Pooler exists), use the Pooler.
        if (!IsDebugMode && AbilityPooler.Instance != null)
        {
            offeredAbilities = AbilityPooler.Instance.GetAbilityChoices(playerData.abilities);
        }
        // Otherwise (Debug Mode OR Pooler missing), use the Inspector List.
        else
        {
            Debug.LogWarning("AbilitySelectorManager: Using DEBUG ABILITIES List.");
            offeredAbilities = new List<Ability>();
            
            if (debugAbilities != null && debugAbilities.Count > 0)
            {
                // Take up to 3 abilities from the inspector list
                for(int i = 0; i < Mathf.Min(3, debugAbilities.Count); i++)
                {
                    if (debugAbilities[i] != null)
                        offeredAbilities.Add(debugAbilities[i]);
                }
            }
            else
            {
                Debug.LogError("Debug Mode is active but 'Debug Abilities' list is empty in Inspector!");
            }
        }
        
        // Update UI
        if (offeredAbilities != null)
        {
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                if (i < offeredAbilities.Count)
                {
                    abilityButtons[i].gameObject.SetActive(true);
                    UpdateAbilityDisplay(i, offeredAbilities[i]);
                }
                else
                {
                    // Hide buttons if we don't have 3 abilities (e.g. only 1 in debug list)
                    abilityButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    void UpdateAbilityDisplay(int index, Ability ability)
    {
        var existingState = playerData.abilities.FirstOrDefault(state => state.ability != null && state.ability.abilityName == ability.abilityName);
        AbilityRarity displayRarity;

        if (existingState != null)
        {
            int nextLevel = existingState.level + 1;
            displayRarity = RarityColourMapper.GetRarityFromLevel(nextLevel);
            abilityNames[index].text = $"{ability.abilityName} (Lvl {existingState.level} -> {nextLevel})";
            abilityDescriptions[index].text = GetStatUpgradeDescription(
                ability.GetStatsForLevel(existingState.level),
                ability.GetStatsForLevel(nextLevel)
            );
        }
        else
        {
            displayRarity = RarityColourMapper.GetRarityFromLevel(1); 
            abilityNames[index].text = $"{ability.abilityName} (New!)";
            abilityDescriptions[index].text = ability.description;
        }
        
        if (abilityRarityPanels[index] != null) abilityRarityPanels[index].color = RarityColourMapper.GetColour(displayRarity);
        if (abilityIcons[index] != null) abilityIcons[index].sprite = ability.abilityIcon;
    }

    public void SkipSelection()
    {
        SetAbilityButtonsInteractable(false);
        HideAllTooltips();
        FinaliseAndReturn();
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
}