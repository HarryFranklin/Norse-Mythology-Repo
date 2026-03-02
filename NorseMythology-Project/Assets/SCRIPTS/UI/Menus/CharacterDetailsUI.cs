using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;

public class CharacterDetailsUI : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private GameObject contentParent; 
    [SerializeField] private TextMeshProUGUI classNameText;
    [SerializeField] private TextMeshProUGUI classDescriptionText;
    [SerializeField] private Image classIconImage;
    [SerializeField] private TextMeshProUGUI previewLabel;

    [Header("The Single Stat Box")]
    [SerializeField] private TextMeshProUGUI allStatsText; 

    public void ClearUI()
    {
        if (contentParent != null) contentParent.SetActive(false);
    }

    public void UpdateUI(CharacterClass target, CharacterClass baseline = null)
    {
        if (target == null)
        {
            ClearUI();
            return;
        }

        if (contentParent != null) contentParent.SetActive(true);

        // Update the Preview Label text based on selection state
        if (previewLabel != null)
        {
            previewLabel.text = (baseline == null) ? "SELECTED" : "PREVIEW";
        }

        classNameText.text = target.className;
        classDescriptionText.text = target.description;
        if (classIconImage != null) 
        {
            classIconImage.sprite = target.classSprite;
            classIconImage.preserveAspect = true; // This prevents stretching
        }

        StringBuilder sb = new StringBuilder();
        PlayerStats tStats = target.startingStats;
        PlayerStats bStats = (baseline != null) ? baseline.startingStats : null;

        sb.AppendLine("<b>BASE STATS & LEVELS</b>");

        sb.AppendLine(FormatLine("Max Health", tStats.maxHealth, target.healthPerLevel, bStats?.maxHealth ?? -1, baseline?.healthPerLevel ?? -999));
        sb.AppendLine(FormatLine("Damage", tStats.attackDamage, target.damagePerLevel, bStats?.attackDamage ?? -1, baseline?.damagePerLevel ?? -999));
        sb.AppendLine(FormatLine("Move Speed", tStats.moveSpeed, target.moveSpeedPerLevel, bStats?.moveSpeed ?? -1, baseline?.moveSpeedPerLevel ?? -999));
        sb.AppendLine(FormatLine("Atk Speed", tStats.attackSpeed, target.attackSpeedPerLevel, bStats?.attackSpeed ?? -1, baseline?.attackSpeedPerLevel ?? -999));
        sb.AppendLine(FormatLine("Health Regen", tStats.healthRegen, target.regenPerLevel, bStats?.healthRegen ?? -1, baseline?.regenPerLevel ?? -999));

        float targetRange = (tStats.attackType == AttackType.Melee) ? tStats.meleeRange : tStats.projectileRange;
        float baseRange = -1;
        if (bStats != null)
        {
            baseRange = (bStats.attackType == AttackType.Melee) ? bStats.meleeRange : bStats.projectileRange;
        }
        sb.AppendLine(FormatLine("Range", targetRange, target.rangePerLevel, baseRange, baseline?.rangePerLevel ?? -999));

        allStatsText.text = sb.ToString();
    }

    private string FormatLine(string label, float tVal, float tGrowth, float bVal, float bGrowth)
    {
        string valPart = $"{tVal:0.#}";
        if (bVal >= 0 && !Mathf.Approximately(tVal, bVal))
        {
            string color = tVal > bVal ? "green" : "red";
            float diff = tVal - bVal;
            string diffSign = diff > 0 ? "+" : "";
            valPart = $"<color={color}>{tVal:0.#} ({diffSign}{diff:0.#})</color>";
        }   

        string growthPart = $" <color=#A0A0A0>({tGrowth:0.#}/lvl)</color>";
        if (bGrowth > -900 && !Mathf.Approximately(tGrowth, bGrowth))
        {
            string gColor = tGrowth > bGrowth ? "green" : "red";
            growthPart = $" <color={gColor}>({tGrowth:0.#}/lvl)</color>";
        }

        return $"{label}: {valPart}{growthPart}";
    }
}