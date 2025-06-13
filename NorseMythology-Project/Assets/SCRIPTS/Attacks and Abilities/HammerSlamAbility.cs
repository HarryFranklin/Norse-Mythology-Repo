using UnityEngine;

[CreateAssetMenu(fileName = "HammerSlamAbility", menuName = "Abilities/HammerSlam")]
public class HammerSlamAbility : Ability
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject hammerPrefab;
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private GameObject dustCloudPrefab;
    
    [Header("Audio")]
    [SerializeField] private AudioClip slamSound;
    [SerializeField] private AudioClip shockwaveSound;
    
    [Header("Falloff Curves")]
    [SerializeField] private AnimationCurve damageFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f);
    [SerializeField] private AnimationCurve knockbackFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.4f);
    [SerializeField] private AnimationCurve stunFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

    private void Awake()
    {
        abilityName = "Hammer Slam";
        description = "Slam a massive hammer into the ground, dealing heavy damage to nearby enemies and knocking them back with devastating force.";
        activationMode = ActivationMode.Instant;
        maxStacks = 1; // Single use ability
    }

    public override bool CanActivate(Player player)
    {
        return player != null && !player.isDead;
    }

    public override void Activate(Player player, PlayerMovement playerMovement = null)
    {
        if (player == null) return;

        Vector3 slamPosition = player.transform.position;
        
        // Play sound effects
        if (slamSound != null)
            AudioSource.PlayClipAtPoint(slamSound, slamPosition);
        
        // Spawn visual effects
        SpawnImpactEffects(slamPosition);
        
        // Apply knockback damage using the new knockback system
        ApplyHammerSlamDamage(slamPosition);
        
        // Play delayed shockwave sound
        if (shockwaveSound != null && player != null)
        {
            player.StartCoroutine(PlayDelayedShockwaveSound(0.2f));
        }
    }

    private void ApplyHammerSlamDamage(Vector3 center)
    {
        LayerMask enemyLayerMask = 1 << 8; // Enemy layer
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, CurrentRadius, enemyLayerMask);
        
        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Entity enemy = collider.GetComponent<Entity>();
                if (enemy == null || enemy.isDead) continue;

                // Calculate distance-based damage and effects
                float distance = Vector2.Distance(center, collider.transform.position);
                float normalizedDistance = distance / CurrentRadius;
                
                // Apply damage with falloff
                float damageMultiplier = damageFalloff.Evaluate(1f - normalizedDistance);
                float finalDamage = Mathf.Lerp(CurrentSpecialValue2, CurrentDamage, damageMultiplier); // specialValue2 = minDamage
                
                // Add damage variation
                float variation = finalDamage * CurrentSpecialValue3; // specialValue3 = damageVariation
                finalDamage += Random.Range(-variation, variation);
                
                // Apply stun
                float stunMultiplier = stunFalloff.Evaluate(1f - normalizedDistance);
                float stunDuration = Mathf.Lerp(0.8f, CurrentDuration, stunMultiplier); // Using duration for max stun
                
                // Apply damage and stun using Entity's built-in methods
                enemy.TakeDamage(finalDamage, stunDuration);
                
                // Apply knockback using the new system
                if (!enemy.isDead)
                {
                    float knockbackMultiplier = knockbackFalloff.Evaluate(1f - normalizedDistance);
                    float knockbackDistance = Mathf.Lerp(CurrentSpecialValue1, CurrentDistance, knockbackMultiplier); // specialValue1 = minKnockback, distance = maxKnockback
                    
                    Vector2 knockbackDirection = (collider.transform.position - center).normalized;
                    
                    // Use the simple knockback method from the new system
                    KnockbackSystem.ApplySimpleKnockback(enemy, knockbackDirection, knockbackDistance, CurrentSpeed);
                }
                
                Debug.Log($"Hammer Slam hit {enemy.name}: {finalDamage} damage, stun: {stunDuration}s");
            }
        }
    }

    private System.Collections.IEnumerator PlayDelayedShockwaveSound(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (shockwaveSound != null)
        {
            AudioSource.PlayClipAtPoint(shockwaveSound, Vector3.zero);
        }
    }

    private void SpawnImpactEffects(Vector3 position)
    {
        if (hammerPrefab != null)
        {
            GameObject hammer = Instantiate(hammerPrefab, position, Quaternion.identity);
            Destroy(hammer, 2f);
        }

        if (shockwavePrefab != null)
        {
            GameObject shockwave = Instantiate(shockwavePrefab, position, Quaternion.identity);
            // Scale shockwave based on current radius
            shockwave.transform.localScale = Vector3.one * (CurrentRadius / 4f); // Assuming base radius of 4
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
            ApplyHammerSlamDamage(Vector3.zero);
        }
    }
}