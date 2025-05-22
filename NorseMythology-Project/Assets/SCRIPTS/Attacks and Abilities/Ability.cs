using UnityEngine;

public enum AbilityType
{
    Attack,
    Defend
}

public abstract class Ability : ScriptableObject
{
    [Header("Base Ability Settings")]
    public string abilityName;
    public AbilityType abilityType;
    public float cooldown = 5f;
    public Sprite abilityIcon;
    
    [Header("Description")]
    [TextArea(2, 4)]
    public string description;
    
    public abstract void Activate(PlayerController player, PlayerMovement playerMovement);
    
    public virtual bool CanActivate(PlayerController player)
    {
        return !player.isDead;
    }
}