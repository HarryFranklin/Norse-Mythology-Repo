using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Text;
using UnityEngine.EventSystems;

public class AbilitySelectorManager : MonoBehaviour
{
    [Header("Selection UI")]
    [SerializeField] private GameObject selectorPanel;
    [SerializeField] private AbilitySelectionSlot[] selectionSlots;
    public Button skipButton; 

    [Header("Replacement UI")]
    [SerializeField] private GameObject replacementPanel;
    [SerializeField] private AbilityReplacementSlot[] replacementSlots;

    [Header("General UI")]
    public TextMeshProUGUI levelText;

    [Header("Debug Settings")]
    public bool forceDebugMode = false;
    public List<Ability> debugAbilities; 

    private List<Ability> offeredAbilities;
    private GameManager.PlayerData playerData;
    private LevelUpManager levelUpManager;
    
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
            playerData = new GameManager.PlayerData();
            Debug.LogWarning("AbilitySelectorManager: DEBUG MODE ACTIVE. Using dummy player data.");
        }
        
        SetupUI();
    }
    
    void SetupUI()
    {
        if (skipButton != null) skipButton.onClick.AddListener(SkipSelection);
        
        if (levelText != null)
        {
             int currentLvl = (GameManager.Instance != null) ? GameManager.Instance.gameLevel : 1;
             levelText.text = $"Level {currentLvl} -> {currentLvl + 1}";
        }

        GenerateAbilityOptions();
    }

    void GenerateAbilityOptions()
    {
        if (!IsDebugMode && AbilityPooler.Instance != null)
        {
            offeredAbilities = AbilityPooler.Instance.GetAbilityChoices(playerData.abilities);
        }
        else
        {
            offeredAbilities = new List<Ability>();
            if (debugAbilities != null && debugAbilities.Count > 0)
            {
                for(int i = 0; i < Mathf.Min(3, debugAbilities.Count); i++)
                {
                    if (debugAbilities[i] != null) offeredAbilities.Add(debugAbilities[i]);
                }
            }
        }
        
        ShowSelectionOptions(offeredAbilities);
    }

    private void ShowSelectionOptions(List<Ability> abilitiesToOffer)
    {
        for (int i = 0; i < selectionSlots.Length; i++)
        {
            AbilitySelectionSlot slot = selectionSlots[i];

            if (abilitiesToOffer != null && i < abilitiesToOffer.Count)
            {
                slot.gameObject.SetActive(true);
                Ability ability = abilitiesToOffer[i];
                int index = i; 

                // --- 1. Calculate Text and Rarity ---
                var existingState = playerData.abilities.FirstOrDefault(state => state.ability != null && state.ability.abilityName == ability.abilityName);
                
                string finalTitle;
                string finalDesc;
                AbilityRarity displayRarity;

                if (existingState != null)
                {
                    // It is an Upgrade
                    int nextLevel = existingState.level + 1;
                    displayRarity = RarityColourMapper.GetRarityFromLevel(nextLevel);
                    
                    finalTitle = $"{ability.abilityName} (Lvl {existingState.level} -> {nextLevel})";
                    finalDesc = GetStatUpgradeDescription(
                        ability.GetStatsForLevel(existingState.level),
                        ability.GetStatsForLevel(nextLevel)
                    );
                }
                else
                {
                    // It is New
                    displayRarity = RarityColourMapper.GetRarityFromLevel(1); 
                    finalTitle = $"{ability.abilityName} (New!)";
                    finalDesc = ability.description;
                }

                // --- 2. Apply Data to Slot ---
                Color uiColour = RarityColourMapper.GetColour(displayRarity);
                
                // Set Images and Colours
                slot.SetupVisuals(ability, uiColour);

                // Set Text (Using our calculated strings)
                slot.SetText(finalTitle, finalDesc);

                // --- 3. Button Logic ---
                slot.SelectButton.onClick.RemoveAllListeners();
                slot.SelectButton.onClick.AddListener(() => SelectAbility(index));

                // --- 4. Hover Logic ---
                // A. Listen to the Button (in case it blocks the root)
                AddHoverEvents(slot.SelectButton.gameObject, index, slot);

                // B. Listen to the Root Object (catches hover events for the whole panel)
                AddHoverEvents(slot.gameObject, index, slot);
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }
    }

    private void AddHoverEvents(GameObject targetObject, int index, AbilitySelectionSlot slot)
    {
        EventTrigger trigger = targetObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = targetObject.AddComponent<EventTrigger>();
        
        trigger.triggers.Clear();

        // Enter
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => {
            if (offeredAbilities != null && index < offeredAbilities.Count && slot.TooltipPanel != null)
            {
                Ability ability = offeredAbilities[index];
                int currentLvl = 0;
                var existing = playerData.abilities.FirstOrDefault(a => a.ability.abilityName == ability.abilityName);
                if (existing != null) currentLvl = existing.level;
                
                var tooltipScript = slot.TooltipPanel.GetComponent<AbilityTooltipPanel>();
                if (tooltipScript != null) tooltipScript.ShowTooltip(ability, currentLvl);
                else slot.TooltipPanel.SetActive(true);
            }
        });
        trigger.triggers.Add(pointerEnter);
        
        // Exit
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => {
            if (slot.TooltipPanel != null)
            {
                 var tooltipScript = slot.TooltipPanel.GetComponent<AbilityTooltipPanel>();
                 if (tooltipScript != null) tooltipScript.HideTooltip();
                 else slot.TooltipPanel.SetActive(false);
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

    private void ShowReplacementOptions(Ability newAbility)
    {
        if (selectorPanel != null) selectorPanel.SetActive(false);
        
        if (replacementPanel != null)
        {
            replacementPanel.SetActive(true);

            for (int i = 0; i < replacementSlots.Length; i++)
            {
                if (i >= playerData.abilities.Count)
                {
                    replacementSlots[i].Panel.SetActive(false);
                    continue;
                }

                replacementSlots[i].Panel.SetActive(true);

                GameManager.PlayerAbilityState equippedAbilityState = playerData.abilities[i];
                Ability equippedAbility = equippedAbilityState.ability;

                equippedAbility.CurrentLevel = equippedAbilityState.level;

                AbilityRarity currentRarity = RarityColourMapper.GetRarityFromLevel(equippedAbility.CurrentLevel);
                Color uiColour = RarityColourMapper.GetColour(currentRarity);

                replacementSlots[i].Setup(equippedAbility, uiColour);

                int replaceIndex = i; 
                replacementSlots[i].ReplaceButton.onClick.RemoveAllListeners();
                replacementSlots[i].ReplaceButton.onClick.AddListener(() =>
                {
                    PerformReplacement(replaceIndex, newAbility);
                });
            }
        }
    }

    private void PerformReplacement(int indexToReplace, Ability newAbility)
    {
        playerData.abilities[indexToReplace] = new GameManager.PlayerAbilityState(newAbility, 1);
        if (replacementPanel != null) replacementPanel.SetActive(false);
        FinaliseAndReturn();
    }

    void FinaliseAndReturn()
    {
        if (!IsDebugMode && GameManager.Instance != null)
        {
            GameManager.Instance.currentPlayerData = playerData;
        }
        
        if (levelUpManager != null) levelUpManager.OnAbilitySelectionCompleted();
        gameObject.SetActive(false);
    }
    
    private void SetAbilityButtonsInteractable(bool isInteractable)
    {
        foreach (var slot in selectionSlots)
        {
            if (slot != null && slot.SelectButton != null) 
                slot.SelectButton.interactable = isInteractable;
        }
        if (skipButton != null) skipButton.interactable = isInteractable;
    }

    private void HideAllTooltips()
    {
        foreach (var slot in selectionSlots)
        {
            if (slot != null && slot.TooltipPanel != null)
            {
                var tooltipScript = slot.TooltipPanel.GetComponent<AbilityTooltipPanel>();
                if (tooltipScript != null) tooltipScript.HideTooltip();
                else slot.TooltipPanel.SetActive(false);
            }
        }
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