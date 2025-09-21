using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Text;
using System.Linq;

public class ClassSelectorUI : MonoBehaviour
{
    [Header("UI - Info Panel")]
    [SerializeField] private TextMeshProUGUI classNameText;
    [SerializeField] private TextMeshProUGUI classDescriptionText;
    [SerializeField] private Image classSpriteImage;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("UI - Grid")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject classGridItemPrefab;

    [Header("Class Configuration")]
    [SerializeField] private string defaultClassName = "Einherjar";

    private CharacterClass defaultClass;
    private List<CharacterClass> availableClasses = new List<CharacterClass>();
    private List<ClassGridItem> gridItems = new List<ClassGridItem>();
    
    [HideInInspector]
    public CharacterClass selectedClass { get; private set; }
    
    // To store the class being hovered over
    private CharacterClass hoveredClass;

    void Start()
    {
        LoadClasses();
        PopulateGrid();

        if (defaultClass != null)
        {
            SelectClass(defaultClass);
        }
        else
        {
            Debug.LogError($"Default class '{defaultClassName}' not found in Resources/Classes!");
        }
    }

    private void LoadClasses()
    {
        availableClasses = Resources.LoadAll<CharacterClass>("Classes").ToList();
        defaultClass = availableClasses.FirstOrDefault(c => c.className == defaultClassName);

        if (defaultClass != null)
        {
            availableClasses = availableClasses.OrderBy(c => c.className != defaultClassName).ToList();
        }
    }

    private void PopulateGrid()
    {
        if (classGridItemPrefab == null || gridParent == null) return;

        foreach (var charClass in availableClasses)
        {
            GameObject itemGO = Instantiate(classGridItemPrefab, gridParent);
            ClassGridItem gridItem = itemGO.GetComponent<ClassGridItem>();
            if (gridItem != null)
            {
                gridItem.Initialise(charClass, this);
                gridItems.Add(gridItem);
            }
        }
    }

    public void DisplayClassInfo(CharacterClass classToDisplay)
    {
        if (classToDisplay == null) return;

        classNameText.text = classToDisplay.className;
        classDescriptionText.text = classToDisplay.description;
        classSpriteImage.sprite = classToDisplay.classSprite;
        statsText.text = GenerateStatsComparisonText(classToDisplay);
    }

    public void SelectClass(CharacterClass classToSelect)
    {
        if (classToSelect == null) return;

        selectedClass = classToSelect;
        
        // When a class is selected, update the display for the hovered class (if any)
        DisplayClassInfo(hoveredClass ?? selectedClass);

        foreach (var item in gridItems)
        {
            item.UpdateHighlight(item.CharacterClass == selectedClass);
        }
    }
    
    // Called from ClassGridItem when the mouse enters
    public void OnHoverStart(CharacterClass classToHover)
    {
        hoveredClass = classToHover;
        DisplayClassInfo(hoveredClass);
    }

    // Called from ClassGridItem when the mouse exits
    public void OnHoverEnd()
    {
        hoveredClass = null;
        DisplayClassInfo(selectedClass);
    }
    
    private string GenerateStatsComparisonText(CharacterClass classToCompare)
    {
        if (selectedClass == null || classToCompare == null) return "Stats not available.";

        PlayerStats baseStats = selectedClass.startingStats; 
        PlayerStats compareStats = classToCompare.startingStats;
        bool isComparingSelf = (classToCompare == selectedClass);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>Base Stats</b>");
        sb.AppendLine(CompareStat("Max Health", baseStats.maxHealth, compareStats.maxHealth, isComparingSelf, "F0"));
        sb.AppendLine(CompareStat("Health Regen", baseStats.healthRegen, compareStats.healthRegen, isComparingSelf, "F1"));
        sb.AppendLine(CompareStat("Move Speed", baseStats.moveSpeed, compareStats.moveSpeed, isComparingSelf, "F1"));
        sb.AppendLine(CompareStat("Attack Damage", baseStats.attackDamage, compareStats.attackDamage, isComparingSelf, "F1"));
        sb.AppendLine(CompareStat("Attack Speed", baseStats.attackSpeed, compareStats.attackSpeed, isComparingSelf, "F1"));

        return sb.ToString();
    }

    private string CompareStat(string label, float baseValue, float compareValue, bool isComparingSelf, string format)
    {
        string formattedValue = compareValue.ToString(format);
        if (isComparingSelf)
        {
            return $"{label}: {formattedValue}";
        }

        float diff = compareValue - baseValue;
        if (Mathf.Approximately(diff, 0))
        {
            return $"{label}: {formattedValue}";
        }
        else if (diff > 0)
        {
            return $"{label}: <color=green>{formattedValue} (+{diff.ToString(format)})</color>";
        }
        else
        {
            return $"{label}: <color=red>{formattedValue} ({diff.ToString(format)})</color>";
        }
    }
}