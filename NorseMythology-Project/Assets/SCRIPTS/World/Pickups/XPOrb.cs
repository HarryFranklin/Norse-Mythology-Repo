using UnityEngine;

public class XPOrb : Pickup
{
    protected override void Start()
    {
        // Set the pickup type
        pickupType = PickupType.Experience;
        
        // XP orbs should chase the player by default
        canChasePlayer = true;
        
        // Call base Start to setup collider
        base.Start();
    }
    
    protected override void ApplyPickupEffect(Player player)
    {
        // Give XP to the player
        player.GainExperience(value);
    }
    
    protected override void OnPickupCollected()
    {
        Debug.Log($"Player gained {value} XP from XP orb");
    }
}