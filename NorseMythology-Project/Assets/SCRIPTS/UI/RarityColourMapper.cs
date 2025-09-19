using UnityEngine;

public static class RarityColourMapper
{
    // Pre-defined colours for each rarity level.
    private static readonly Color commonColour = new Color(0.12f, 0.75f, 0.31f); // Green
    private static readonly Color uncommonColour = new Color(0.6f, 0.6f, 0.6f);     // Gray
    private static readonly Color rareColour = new Color(0.22f, 0.58f, 0.99f);   // Blue
    private static readonly Color epicColour = new Color(0.64f, 0.22f, 0.99f);   // Purple
    private static readonly Color legendaryColour = new Color(1.0f, 0.8f, 0.22f);   // Golden

    public static Color GetColour(AbilityRarity rarity)
    {
        switch (rarity)
        {
            case AbilityRarity.Common:
                return commonColour;
            case AbilityRarity.Uncommon:
                return uncommonColour;
            case AbilityRarity.Rare:
                return rareColour;
            case AbilityRarity.Epic:
                return epicColour;
            case AbilityRarity.Legendary:
                return legendaryColour;
            default:
                return Color.white; // Fallback colour
        }
    }
}
