using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityReplacementSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Image rarityPanel;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI stackCountText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button replaceButton;

    // --- Public Properties (Accessors) ---
    public GameObject Panel => mainPanel;
    public Image RarityPanel => rarityPanel;
    public Image Icon => icon;
    public TextMeshProUGUI StackCount => stackCountText;
    public TextMeshProUGUI Title => titleText;
    public Button ReplaceButton => replaceButton;

    public void Setup(Ability ability, Color rarityColour)
    {
        if (ability == null) return;

        // 1. Fix: Ensure the Title uses the actual Ability Name
        if (titleText != null)
        {
            titleText.text = ability.abilityName;
        }

        // 2. Set the Icon
        if (icon != null)
        {
            icon.sprite = ability.abilityIcon;
        }

        // 3. Set the Rarity/Level Colour
        if (rarityPanel != null)
        {
            rarityPanel.color = rarityColour;
        }

        // 4. Set the Stack Count Text
        // Displays as "1/1" or "3/3" using the max stacks for the current level.
        if (stackCountText != null)
        {
            int maxStacks = ability.MaxStacksAtCurrentLevel;
            // Assuming in a selector screen we show full capacity, 
            // or you can pass in a specific 'current' value if needed.
            stackCountText.text = $"{maxStacks}/{maxStacks}";
        }
    }
}