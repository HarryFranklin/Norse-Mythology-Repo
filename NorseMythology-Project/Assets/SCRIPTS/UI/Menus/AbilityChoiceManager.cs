using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class AbilityChoice
{
    [Header("Panel Reference")]
    public GameObject panel;
    
    [Header("UI Components")]
    public Image abilityIconImage;
    public TextMeshProUGUI abilityNameText;
    public TextMeshProUGUI abilityLevelText;
    
    [Header("Derived Values (Debug)")]
    [SerializeField] private string abilityName;
    [SerializeField] private int abilityLevel;
    [SerializeField] private Sprite abilityIconSprite;
    
    public string AbilityName => abilityName;
    public int AbilityLevel => abilityLevel;
    public Sprite AbilityIconSprite => abilityIconSprite;
    
    public void UpdateDisplay(Ability ability)
    {
        if (ability == null)
        {
            ClearDisplay();
            return;
        }
        
        abilityName = ability.abilityName;
        abilityLevel = ability.CurrentLevel;
        abilityIconSprite = ability.abilityIcon;
        
        if (abilityIconImage != null)
            abilityIconImage.sprite = ability.abilityIcon;
            
        if (abilityNameText != null)
            abilityNameText.text = ability.abilityName;
            
        if (abilityLevelText != null)
            abilityLevelText.text = $"Level {ability.CurrentLevel}";
    }
    
    public void ClearDisplay()
    {
        abilityName = "";
        abilityLevel = 0;
        abilityIconSprite = null;
        
        if (abilityIconImage != null)
            abilityIconImage.sprite = null;
            
        if (abilityNameText != null)
            abilityNameText.text = "";
            
        if (abilityLevelText != null)
            abilityLevelText.text = "";
    }
}

public class AbilityChoiceManager : MonoBehaviour
{
    [Header("Abilities")]
    [SerializeField] private Ability[] abilities = new Ability[3];
    
    [Header("Ability Choice Slots")]
    [SerializeField] private AbilityChoice[] abilityChoices = new AbilityChoice[3];
    
    void Start()
    {
        UpdateAllDisplays();
    }
    
    public void SetAbility(int slotIndex, Ability newAbility)
    {
        if (slotIndex < 0 || slotIndex >= 3)
        {
            Debug.LogError($"Invalid slot index: {slotIndex}. Must be 0-2.");
            return;
        }
        
        abilities[slotIndex] = newAbility;
        UpdateSlotDisplay(slotIndex);
    }
    
    public Ability GetAbility(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3)
        {
            Debug.LogError($"Invalid slot index: {slotIndex}. Must be 0-2.");
            return null;
        }
        
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
        if (newAbilities == null)
        {
            Debug.LogError("Abilities array is null.");
            return;
        }
        
        int count = Mathf.Min(newAbilities.Length, 3);
        for (int i = 0; i < count; i++)
        {
            abilities[i] = newAbilities[i];
        }
        
        for (int i = count; i < 3; i++)
        {
            abilities[i] = null;
        }
        
        UpdateAllDisplays();
    }
    
    public void UpdateSlotDisplay(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3)
        {
            Debug.LogError($"Invalid slot index: {slotIndex}. Must be 0-2.");
            return;
        }
        
        if (abilityChoices[slotIndex] == null)
        {
            Debug.LogError($"Ability choice {slotIndex} is not assigned.");
            return;
        }
        
        abilityChoices[slotIndex].UpdateDisplay(abilities[slotIndex]);
    }
    
    public void UpdateAllDisplays()
    {
        for (int i = 0; i < 3; i++)
        {
            UpdateSlotDisplay(i);
        }
    }
    
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3)
        {
            Debug.LogError($"Invalid slot index: {slotIndex}. Must be 0-2.");
            return;
        }
        
        abilities[slotIndex] = null;
        if (abilityChoices[slotIndex] != null)
        {
            abilityChoices[slotIndex].ClearDisplay();
        }
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
    
    void OnValidate()
    {
        if (abilityChoices == null || abilityChoices.Length != 3)
        {
            Debug.LogWarning("AbilityChoiceManager: abilityChoices array should have exactly 3 elements.");
        }
        
        if (abilities == null || abilities.Length != 3)
        {
            Debug.LogWarning("AbilityChoiceManager: abilities array should have exactly 3 elements.");
        }
    }
}