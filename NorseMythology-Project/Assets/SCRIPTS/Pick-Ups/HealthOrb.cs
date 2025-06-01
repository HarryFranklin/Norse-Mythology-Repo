using UnityEngine;

public class HealthOrb : Pickup
{
    protected override void Start()
    {
        // Set the pickup type
        pickupType = PickupType.Health;

        // Health orbs don't chase the player (static pickups)
        canChasePlayer = false;

        // Different pickup settings for health
        pickupDistance = 0.7f; // Slightly larger pickup range
        
        // Call base Start to setup collider
        base.Start();
    }
    
    protected override void ApplyPickupEffect(Player player)
    {
        // Heal the player
        player.Heal(value);
    }
    
    protected override void OnPickupCollected()
    {
        Debug.Log($"Player healed for {value} health");
    }
    
    // Override to check if player actually needs health
    protected override void CollectPickup()
    {
        Player playerScript = player.GetComponent<Player>();
        if (playerScript != null)
        {
            // Only collect if player isn't at full health
            if (playerScript.currentHealth < playerScript.maxHealth)
            {
                base.CollectPickup();
            }
            // If player is at full health, the orb just sits there
        }
    }
}