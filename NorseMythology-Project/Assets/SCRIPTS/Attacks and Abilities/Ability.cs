using UnityEngine;

public enum AbilityType
{
    Attack,
    Defend
}

public enum ActivationMode
{
    Instant, // Activates immediately on keypress
    ClickToTarget // Enters targeting mode, then activates on mouse click
}

public abstract class Ability : ScriptableObject
{
    [Header("Base Ability Settings")]
    public string abilityName;
    public AbilityType abilityType;
    public float cooldown = 5f;
    public Sprite abilityIcon;

    [Header("Activation Mode")]
    public ActivationMode activationMode = ActivationMode.Instant;

    [Header("Targeting Settings (for ClickToTarget abilities)")]
    public Sprite targetingCursor;  // Optional custom cursor sprite
    public bool showTargetingLine = false;
    public Color targetingLineColor = Color.white;
    public float maxTargetingRange = 10f;  // Maximum range for targeting

    [Header("Description")]
    [TextArea(2, 4)]
    public string description;

    // For instant abilities - activates immediately
    public abstract void Activate(Player player, PlayerMovement playerMovement);

    // For click-to-target abilities - activates on mouse click after targeting
    public virtual void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        // Default implementation just calls regular Activate
        Activate(player, playerMovement);
    }

    // Called when entering targeting mode (for UI feedback, cursor changes, etc.)
    public virtual void EnterTargetingMode(Player player)
    {
        // Override in subclasses if needed
    }
    
    // Called when exiting targeting mode
    public virtual void ExitTargetingMode(Player player)
    {
        // Override in subclasses if needed
    }
    
    public virtual bool CanActivate(Player player)
    {
        return !player.isDead;
    }
}
