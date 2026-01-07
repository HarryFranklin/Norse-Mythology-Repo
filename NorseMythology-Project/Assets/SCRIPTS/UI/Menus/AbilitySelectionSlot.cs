using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilitySelectionSlot : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject tooltipPanel;

    [Header("Visuals")]
    [SerializeField] private Image rarityPanel;
    [SerializeField] private Image abilityIcon;

    [Header("Ability Information")]
    [SerializeField] private TextMeshProUGUI abilityTitleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    // Public Accessors
    public Button SelectButton => selectButton;
    public GameObject TooltipPanel => tooltipPanel;

    public void SetupVisuals(Ability ability, Color rarityColour)
    {
        if (ability == null) return;

        // 1. Set the Icon
        if (abilityIcon != null)
        {
            abilityIcon.sprite = ability.abilityIcon;
        }

        // 2. Set the Rarity Colour
        if (rarityPanel != null)
        {
            rarityPanel.color = rarityColour;
        }
    }

    public void SetText(string title, string description)
    {
        if (abilityTitleText != null) abilityTitleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
    }
}