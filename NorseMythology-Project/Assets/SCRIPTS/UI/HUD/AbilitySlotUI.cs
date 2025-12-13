using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilitySlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image abilityIcon;
    public Image cooldownOverlay;
    public TextMeshProUGUI stackCount;
    public Image rarityPanel;
    public TextMeshProUGUI keyReminder;

    [Header("Text Settings")]
    public float nameDisplayDuration = 1.0f; // Time to show name after activation

    private AbilityManager abilityManager;
    private int abilityIndex;
    private bool isInitialised = false;
    
    // State for text display
    private string defaultKeyText;
    private bool isTargeting = false;
    private float nameDisplayTimer = 0f;

    public void Initialise(AbilityManager manager, int index)
    {
        // Unsubscribe from old events if re-initialising
        if (isInitialised && abilityManager != null)
        {
            UnsubscribeEvents();
        }

        abilityManager = manager;
        abilityIndex = index;
        isInitialised = true;
        
        // Subscribe to events
        if (abilityManager != null)
        {
            abilityManager.OnAbilityUsed += HandleAbilityUsed;
            abilityManager.OnAbilityTargetingStarted += HandleTargetingStarted;
            abilityManager.OnAbilityTargetingEnded += HandleTargetingEnded;
        }

        // Set default text based on index (1-4)
        defaultKeyText = $"[{index + 1}]";
        UpdateTextDisplay();

        Ability ability = manager.equippedAbilities[index];
        if (ability != null)
        {
            abilityIcon.sprite = ability.abilityIcon;
            abilityIcon.enabled = true;

            if (rarityPanel != null)
            {
                // Get rarity based on the ability's current level
                AbilityRarity currentRarity = RarityColourMapper.GetRarityFromLevel(ability.CurrentLevel);
                rarityPanel.color = RarityColourMapper.GetColour(currentRarity);
                rarityPanel.enabled = true;
            }
        }
        else
        {
            abilityIcon.enabled = false;
            if (rarityPanel != null)
            {
                rarityPanel.enabled = false;
            }
        }

        UpdateStackCount();
    }
    
    private void UnsubscribeEvents()
    {
        if (abilityManager != null)
        {
            abilityManager.OnAbilityUsed -= HandleAbilityUsed;
            abilityManager.OnAbilityTargetingStarted -= HandleTargetingStarted;
            abilityManager.OnAbilityTargetingEnded -= HandleTargetingEnded;
        }
    }
    
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // --- Event Handlers ---

    private void HandleAbilityUsed(int index)
    {
        if (index == abilityIndex)
        {
            // Ability activated, show name for a duration
            nameDisplayTimer = nameDisplayDuration;
            UpdateTextDisplay();
        }
    }

    private void HandleTargetingStarted(int index)
    {
        if (index == abilityIndex)
        {
            isTargeting = true;
            UpdateTextDisplay();
        }
    }

    private void HandleTargetingEnded(int index)
    {
        if (index == abilityIndex)
        {
            isTargeting = false;
            UpdateTextDisplay();
        }
    }

    // --- Update Logic ---

    void Awake()
    {
        if (stackCount == null)
        {
            stackCount = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        if (!isInitialised || abilityManager == null || abilityManager.player == null || abilityManager.player.currentStats == null)
            return;
            
        // Handle name display timer
        if (nameDisplayTimer > 0)
        {
            nameDisplayTimer -= Time.deltaTime;
            if (nameDisplayTimer <= 0)
            {
                UpdateTextDisplay();
            }
        }

        Ability ability = abilityManager.equippedAbilities[abilityIndex];
        if (ability == null)
        {
            cooldownOverlay.fillAmount = 0f;
            if (stackCount != null)
                stackCount.gameObject.SetActive(false);
            
            if (abilityIcon.enabled)
            {
                abilityIcon.enabled = false;
                if (rarityPanel != null) rarityPanel.enabled = false;
            }
            // Ensure text is cleared if slot is empty
            if (keyReminder != null) keyReminder.text = defaultKeyText;
            return;
        }
        
        if (!abilityIcon.enabled)
        {
            Initialise(abilityManager, abilityIndex);
        }

        float remaining = abilityManager.GetAbilityCooldownRemaining(abilityIndex);
        float max = ability.CurrentStackRegenTime;
        
        // Apply cooldown reduction from player stats
        if (abilityManager.player != null && abilityManager.player.currentStats != null)
        {
            float reduction = abilityManager.player.currentStats.abilityCooldownReduction;
            max *= (1f - (reduction / 100f));
        }
        
        if (max <= 0f) max = 0.01f;

        cooldownOverlay.fillAmount = Mathf.Clamp01(remaining / max);
        UpdateStackCount();
    }
    
    private void UpdateTextDisplay()
    {
        if (keyReminder == null) return;
        
        Ability ability = (abilityManager != null && abilityIndex < abilityManager.equippedAbilities.Length) 
            ? abilityManager.equippedAbilities[abilityIndex] : null;

        if (ability == null)
        {
            keyReminder.text = defaultKeyText;
            return;
        }

        // Priority 1: Just used (feedback timer)
        if (nameDisplayTimer > 0)
        {
            keyReminder.text = ability.abilityName;
        }
        // Priority 2: Currently targeting
        else if (isTargeting)
        {
            keyReminder.text = ability.abilityName;
        }
        // Priority 3: Default state
        else
        {
            keyReminder.text = defaultKeyText;
        }
    }
    
    private void UpdateStackCount()
    {
        if (stackCount == null || abilityManager == null) return;
        
        Ability ability = abilityManager.equippedAbilities[abilityIndex];
        if (ability == null)
        {
            stackCount.gameObject.SetActive(false);
            return;
        }
        
        stackCount.gameObject.SetActive(true);
        stackCount.text = $"{ability.CurrentStacks}/{ability.MaxStacksAtCurrentLevel}";
        
        if (ability.CurrentStacks <= 0)
        {
            stackCount.color = Color.red;
        }
        else if (ability.CurrentStacks < ability.MaxStacksAtCurrentLevel)
        {
            stackCount.color = Color.yellow;
        }
        else
        {
            stackCount.color = Color.white;
        }
    }
}