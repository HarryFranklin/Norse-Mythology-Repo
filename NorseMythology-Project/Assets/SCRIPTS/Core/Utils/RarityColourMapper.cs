using UnityEngine;

public static class RarityColourMapper
{
    // Defines the specific colors for each rarity tier.
    private static readonly Color commonColor = new Color(0.12f, 0.75f, 0.31f); // Green
    private static readonly Color uncommonColor = new Color(0.6f, 0.6f, 0.6f);     // Gray
    private static readonly Color rareColor = new Color(0.22f, 0.58f, 0.99f);   // Blue
    private static readonly Color epicColor = new Color(0.64f, 0.22f, 0.99f);   // Purple
    private static readonly Color legendaryColor = new Color(1.0f, 0.8f, 0.22f);   // Golden

    public static Color GetColour(AbilityRarity rarity)
    {
        switch (rarity)
        {
            case AbilityRarity.Common:
                return commonColor;
            case AbilityRarity.Uncommon:
                return uncommonColor;
            case AbilityRarity.Rare:
                return rareColor;
            case AbilityRarity.Epic:
                return epicColor;
            case AbilityRarity.Legendary:
                return legendaryColor;
            default:
                return Color.white; // A fallback colour in case of an error.
        }
    }

    public static AbilityRarity GetRarityFromLevel(int level)
    {
        switch (level)
        {
            case 1:  return AbilityRarity.Uncommon;
            case 2:  return AbilityRarity.Common;
            case 3:  return AbilityRarity.Rare;
            case 4:  return AbilityRarity.Epic;
            case 5:  return AbilityRarity.Legendary;
            default: return AbilityRarity.Uncommon; // Fallback for levels outside the 1-5 range.
        }
    }
}

