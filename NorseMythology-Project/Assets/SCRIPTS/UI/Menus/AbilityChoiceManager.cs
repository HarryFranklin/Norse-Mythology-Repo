using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityChoiceManager : MonoBehaviour
{
    [Header("Abilities Data")]
    [SerializeField] private Ability[] abilities = new Ability[3];

    [Header("UI: Panels")]
    [Tooltip("Element 0 = Left, 1 = Middle, 2 = Right")]
    [SerializeField] private GameObject[] choicePanels = new GameObject[3];

    [Header("UI: Icons")]
    [SerializeField] private Image[] abilityIcons = new Image[3];

    [Header("UI: Titles")]
    [SerializeField] private TextMeshProUGUI[] abilityTitles = new TextMeshProUGUI[3];

    [Header("UI: Levels")]
    [SerializeField] private TextMeshProUGUI[] abilityLevels = new TextMeshProUGUI[3];
    
    [Header("UI: Descriptions")]
    [Tooltip("Optional: Assign if you want to display ability descriptions")]
    [SerializeField] private TextMeshProUGUI[] abilityDescriptions = new TextMeshProUGUI[3];

    void Start()
    {
        UpdateAllDisplays();
    }
    
    // --- Setters & Getters ---

    public void SetAbility(int slotIndex, Ability newAbility)
    {
        if (!IsValidSlot(slotIndex)) return;
        
        abilities[slotIndex] = newAbility;
        UpdateSlotDisplay(slotIndex);
    }
    
    public Ability GetAbility(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return null;
        return abilities[slotIndex];
    }
    
    public void SetAllAbilities(Ability ability1, Ability ability2, Ability ability3)
    {
        abilities[0] = ability1;
        abilities[1] = ability2;
        abilities[2] = ability3;
        UpdateAllDisplays();
    }
    
    public void SetAbilities(Ability[] newAbilities)
    {
        if (newAbilities == null) return;
        
        int count = Mathf.Min(newAbilities.Length, 3);
        for (int i = 0; i < count; i++)
        {
            abilities[i] = newAbilities[i];
        }
        // Clear remaining slots if input array is smaller than 3
        for (int i = count; i < 3; i++)
        {
            abilities[i] = null;
        }
        
        UpdateAllDisplays();
    }
    
    // --- Display Logic ---

    public void UpdateSlotDisplay(int i)
    {
        if (!IsValidSlot(i)) return;
        
        Ability ability = abilities[i];

        if (ability != null)
        {
            // Update Icon
            if (abilityIcons[i] != null) 
                abilityIcons[i].sprite = ability.abilityIcon;
            
            // Update Title
            if (abilityTitles[i] != null) 
                abilityTitles[i].text = ability.abilityName;
            
            // Update Level
            if (abilityLevels[i] != null) 
                abilityLevels[i].text = $"Level {ability.CurrentLevel}";

            // Update Description (New)
            if (abilityDescriptions[i] != null)
                abilityDescriptions[i].text = ability.description;
        }
        else
        {
            ClearSlotDisplay(i);
        }
    }
    
    private void ClearSlotDisplay(int i)
    {
        if (abilityIcons[i] != null) abilityIcons[i].sprite = null;
        if (abilityTitles[i] != null) abilityTitles[i].text = "";
        if (abilityLevels[i] != null) abilityLevels[i].text = "";
        if (abilityDescriptions[i] != null) abilityDescriptions[i].text = "";
    }
    
    public void UpdateAllDisplays()
    {
        for (int i = 0; i < 3; i++)
        {
            UpdateSlotDisplay(i);
        }
    }
    
    // --- Management ---

    public void ClearSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return;
        
        abilities[slotIndex] = null;
        ClearSlotDisplay(slotIndex);
    }
    
    public void ClearAllSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            ClearSlot(i);
        }
    }
    
    public void RefreshAllDisplays()
    {
        UpdateAllDisplays();
    }
    
    private bool IsValidSlot(int index)
    {
        if (index < 0 || index >= 3)
        {
            Debug.LogError($"Invalid slot index: {index}. Must be 0, 1, or 2.");
            return false;
        }
        return true;
    }

    // Ensure arrays are sized correctly in Editor
    void OnValidate()
    {
        ResizeArray(ref choicePanels, 3);
        ResizeArray(ref abilityIcons, 3);
        ResizeArray(ref abilityTitles, 3);
        ResizeArray(ref abilityLevels, 3);
        ResizeArray(ref abilityDescriptions, 3);
        ResizeArray(ref abilities, 3);
    }

    private void ResizeArray<T>(ref T[] array, int size)
    {
        if (array == null || array.Length != size)
        {
            System.Array.Resize(ref array, size);
        }
    }
}