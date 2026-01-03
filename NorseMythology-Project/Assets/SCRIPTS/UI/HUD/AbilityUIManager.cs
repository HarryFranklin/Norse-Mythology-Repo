using UnityEngine;

public class AbilityUIManager : MonoBehaviour
{
    [Header("References")]
    public AbilityManager abilityManager;
    public AbilitySlotUI[] abilitySlots = new AbilitySlotUI[4];

    void Start()
    {
        if (abilityManager == null)
        {
            Debug.LogError("AbilityUIManager: AbilityManager reference is missing!");
            return;
        }
        
        RefreshAllSlots();
    }

    private void OnEnable()
    {
        if (abilityManager != null)
        {
            // Subscribe to events so UI text/icons update instantly 
            // when Targeting Starts, Ends, or Ability is Used
            abilityManager.OnAbilityTargetingStarted += UpdateSlot;
            abilityManager.OnAbilityTargetingEnded += UpdateSlot;
            abilityManager.OnAbilityUsed += UpdateSlot;
        }
    }

    private void OnDisable()
    {
        if (abilityManager != null)
        {
            abilityManager.OnAbilityTargetingStarted -= UpdateSlot;
            abilityManager.OnAbilityTargetingEnded -= UpdateSlot;
            abilityManager.OnAbilityUsed -= UpdateSlot;
        }
    }

    public void UpdateSlot(int index)
    {
        if (index >= 0 && index < abilitySlots.Length && abilitySlots[index] != null)
        {
            abilitySlots[index].Initialise(abilityManager, index);
        }
    }
    
    // Method to manually refresh all slots
    public void RefreshAllSlots()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            UpdateSlot(i);
        }
    }
}