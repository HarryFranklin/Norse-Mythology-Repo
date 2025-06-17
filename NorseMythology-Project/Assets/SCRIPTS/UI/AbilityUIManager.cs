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
        
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            if (abilitySlots[i] != null)
            {
                abilitySlots[i].Initialise(abilityManager, i);
            }
            else
            {
                Debug.LogWarning($"AbilityUIManager: AbilitySlot {i} is null!");
            }
        }
    }

    public void UpdateSlot(int index)
    {
        if (index >= 0 && index < abilitySlots.Length && abilitySlots[index] != null)
        {
            abilitySlots[index].Initialise(abilityManager, index);
        }
    }
    
    // Optional: Method to manually refresh all slots
    public void RefreshAllSlots()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            UpdateSlot(i);
        }
    }
}