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

    private AbilityManager abilityManager;
    private int abilityIndex;
    private bool isInitialised = false;

    public void Initialise(AbilityManager manager, int index)
    {
        abilityManager = manager;
        abilityIndex = index;
        isInitialised = true;

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
            return;
        }
        
        if (!abilityIcon.enabled)
        {
            Initialise(abilityManager, abilityIndex);
        }

        float remaining = abilityManager.GetAbilityCooldownRemaining(abilityIndex);
        float reduction = abilityManager.player.currentStats.abilityCooldownReduction;
        float max = ability.CurrentCooldown * (1f - (reduction / 100f));
        if (max <= 0f) max = 0.01f;

        cooldownOverlay.fillAmount = Mathf.Clamp01(remaining / max);
        UpdateStackCount();
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