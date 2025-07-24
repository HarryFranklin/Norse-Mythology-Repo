using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilitySlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image abilityIcon;
    public Image cooldownOverlay;
    public TextMeshProUGUI stackCount;

    [SerializeField] private AbilityManager abilityManager;
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
        }
        else
        {
            abilityIcon.enabled = false;
        }

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
        if (!isInitialised ||
            abilityManager == null ||
            abilityManager.player == null ||
            abilityManager.player.currentStats == null ||
            abilityManager.equippedAbilities == null ||
            abilityIndex < 0 ||
            abilityIndex >= abilityManager.equippedAbilities.Length)
            return;

        Ability ability = abilityManager.equippedAbilities[abilityIndex];
        if (ability == null)
        {
            cooldownOverlay.fillAmount = 0f;
            if (stackCount != null)
                stackCount.gameObject.SetActive(false);
            return;
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