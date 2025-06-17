using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilitySlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image abilityIcon;
    public Image cooldownOverlay;
    public TextMeshProUGUI stackCount; // Add this reference

    private AbilityManager abilityManager;
    private int abilityIndex;

    public void Initialise(AbilityManager manager, int index)
    {
        abilityManager = manager;
        abilityIndex = index;

        Ability ability = manager.equippedAbilities[index];
        if (ability != null)
        {
            abilityIcon.sprite = ability.abilityIcon;
            abilityIcon.enabled = true;
        }
        else
        {
            abilityIcon.enabled = false;
        }
        
        // Update stack count immediately
        UpdateStackCount();
    }

    void Awake()
    {
        if (stackCount == null)
        {
            Debug.Log("Stack count text is null, finding through code.");
            stackCount = transform.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        if (abilityManager == null) return;

        Ability ability = abilityManager.equippedAbilities[abilityIndex];
        if (ability == null) 
        {
            // Hide stack count if no ability equipped
            if (stackCount != null)
                stackCount.gameObject.SetActive(false);
            return;
        }

        // Update cooldown overlay
        float remaining = abilityManager.GetAbilityCooldownRemaining(abilityIndex);
        float max = ability.CurrentCooldown * (1f - (abilityManager.player.currentStats.abilityCooldownReduction / 100f));
        if (max <= 0f) max = 0.01f; // prevent divide by zero
        
        cooldownOverlay.fillAmount = Mathf.Clamp01(remaining / max);
        
        // Update stack count every frame
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
        
        // Show the stack count
        stackCount.gameObject.SetActive(true);
        
        // Display current stacks / max stacks
        stackCount.text = $"{ability.CurrentStacks}/{ability.MaxStacksAtCurrentLevel}";
        
        // Optional: Change color based on availability
        if (ability.CurrentStacks <= 0)
        {
            stackCount.color = Color.red; // No charges available
        }
        else if (ability.CurrentStacks < ability.MaxStacksAtCurrentLevel)
        {
            stackCount.color = Color.yellow; // Partially charged
        }
        else
        {
            stackCount.color = Color.white; // Fully charged
        }
    }
}