using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Text;
using System.Linq; // Needed for OrderBy

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
        // Dynamically load all CharacterClass assets from the folder
        availableClasses = Resources.LoadAll<CharacterClass>("Classes").ToList();
        
        // Find the default class from the loaded assets
        defaultClass = availableClasses.FirstOrDefault(c => c.className == defaultClassName);

        // Ensure the default class appears first in the grid
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
        DisplayClassInfo(selectedClass);

        // This loop is the key: it tells EVERY grid item to update its appearance
        foreach (var item in gridItems)
        {
            item.UpdateHighlight(item.CharacterClass == selectedClass);
        }
    }
    
    private string GenerateStatsComparisonText(CharacterClass classToCompare)
    {
        if (defaultClass == null || classToCompare == null) return "Stats not available.";

        PlayerStats baseStats = defaultClass.startingStats;
        PlayerStats compareStats = classToCompare.startingStats;
        bool isDefault = (classToCompare == defaultClass);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>Base Stats</b>");
        sb.AppendLine(CompareStat("Max Health", baseStats.maxHealth, compareStats.maxHealth, isDefault, "F0"));
        sb.AppendLine(CompareStat("Health Regen", baseStats.healthRegen, compareStats.healthRegen, isDefault, "F1"));
        sb.AppendLine(CompareStat("Move Speed", baseStats.moveSpeed, compareStats.moveSpeed, isDefault, "F1"));
        sb.AppendLine(CompareStat("Attack Damage", baseStats.attackDamage, compareStats.attackDamage, isDefault, "F1"));
        sb.AppendLine(CompareStat("Attack Speed", baseStats.attackSpeed, compareStats.attackSpeed, isDefault, "F1"));

        return sb.ToString();
    }

    private string CompareStat(string label, float baseValue, float compareValue, bool isDefault, string format)
    {
        string formattedValue = compareValue.ToString(format);
        if (isDefault)
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