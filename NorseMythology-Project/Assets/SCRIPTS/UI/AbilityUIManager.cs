using UnityEngine;

public class AbilityUIManager : MonoBehaviour
{
    public AbilityManager abilityManager;
    public AbilitySlotUI[] abilitySlots = new AbilitySlotUI[4];

    void Start()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            abilitySlots[i].Initialise(abilityManager, i);
        }
    }

    public void UpdateSlot(int index)
    {
        if (index >= 0 && index < abilitySlots.Length)
        {
            abilitySlots[index].Initialise(abilityManager, index);
        }
    }
}
