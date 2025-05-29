using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HammerSlamAbility", menuName = "Abilities/Defend/HammerSlam")]
public class HammerSlamAbility : DefendAbility
{
    // Right I need to think what this ability does
    // On pressing "3", the player does a small jump in the direction they are travelling, or last travelled in.
        // This locks the player movement until you hit the ground.
    // When you hit the ground, you slam the hammer down, dealing great damage to enemies in a small radius around you.
        // Enemies in a small radius are damaged and stunned, and only die after hitting the ground themselves.
        // Enemies within a larger radius are knocked back, dealt less damage and stunned for a short duration.

    [Header("Hammer Slam Settings")]
    [SerializeField] private float innerDamageRadius = 2f;
    [SerializeField] private float outerKnockbackRadius = 4f;
    [SerializeField] private float innerDamage = 15f;
    [SerializeField] private float landingDamage = 8f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float slamAnimationDuration = 0.5f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject hammerPrefab;
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private GameObject dustCloudPrefab;

    private void Awake()
    {
        abilityName = "Hammer Slam";
        description = "Slam a massive hammer into the ground, dealing damage to nearby enemies and knocking back distant ones with a stunning shockwave.";
        cooldown = 8f;
        duration = slamAnimationDuration;
        effectStrength = innerDamage;
    }

    public override bool CanActivate(Player player)
    {
        return player != null && !player.isDead;
    }

    public override void Activate(Player player, PlayerMovement playerMovement = null)
    {
        if (player == null) return;

        // Start the hammer slam coroutine
        player.StartCoroutine(PerformHammerSlam(player));
    }

    private IEnumerator PerformHammerSlam(Player player)
    {
        yield return new WaitForSeconds(0.1f); // Small delay before slam
    }
}