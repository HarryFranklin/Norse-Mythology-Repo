using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "HammerSlamAbility", menuName = "Abilities/Defend/HammerSlam")]
public class HammerSlamAbility : DefendAbility
{
    [Header("Hammer Slam Settings")]
    [SerializeField] private float innerDamageRadius = 2f;
    [SerializeField] private float outerKnockbackRadius = 4f;
    [SerializeField] private float innerDamage = 12f;
    [SerializeField] private float outerDamage = 5f;
    [SerializeField] private float innerKnockbackDistance = 2f;
    [SerializeField] private float outerKnockbackDistance = 1.5f;
    [SerializeField] private float innerStunDuration = 2f;
    [SerializeField] private float outerStunDuration = 1f;
    [SerializeField] private float knockbackDuration = 0.3f;
    [SerializeField] private float slamAnimationDuration = 0.5f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject hammerPrefab;
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private GameObject dustCloudPrefab;

    [Header("Screen Settings")]
    [SerializeField] private Vector2 screenBounds = new Vector2(10f, 6f);

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

        player.StartCoroutine(PerformHammerSlam(player));
    }

    private IEnumerator PerformHammerSlam(Player player)
    {
        yield return new WaitForSeconds(1f);

        Vector3 slamPosition = player.transform.position;

        SpawnVisualEffects(slamPosition);

        var damageZones = Knockback.CreateStandardZones(
            innerDamageRadius, outerKnockbackRadius,
            innerDamage, outerDamage,
            innerKnockbackDistance, outerKnockbackDistance,
            innerStunDuration, outerStunDuration
        );

        Knockback.ApplyRadialKnockback(slamPosition, damageZones, "Enemy", knockbackDuration, screenBounds);
    }

    private void SpawnVisualEffects(Vector3 position)
    {
        if (hammerPrefab != null)
        {
            GameObject hammer = Instantiate(hammerPrefab, position, Quaternion.identity);
            Destroy(hammer, 2f);
        }

        if (shockwavePrefab != null)
        {
            GameObject shockwave = Instantiate(shockwavePrefab, position, Quaternion.identity);
            Destroy(shockwave, 1f);
        }

        if (dustCloudPrefab != null)
        {
            GameObject dust = Instantiate(dustCloudPrefab, position, Quaternion.identity);
            Destroy(dust, 3f);
        }
    }

    [ContextMenu("Test Hammer Slam")]
    private void TestHammerSlam()
    {
        if (Application.isPlaying)
        {
            var damageZones = Knockback.CreateStandardZones(
                innerDamageRadius, outerKnockbackRadius,
                innerDamage, outerDamage,
                innerKnockbackDistance, outerKnockbackDistance,
                innerStunDuration, outerStunDuration
            );

            Knockback.ApplyRadialKnockback(Vector3.zero, damageZones, "Enemy", knockbackDuration, screenBounds);
        }
    }
}