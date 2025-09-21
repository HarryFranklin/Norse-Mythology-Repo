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
    }

    protected override void InitialiseFromCodeMatrix()
    {
        // Define hammer slam ability values via code matrix
        // Level, cooldown, damage, duration(stun), radius, speed(knockback speed), distance(max knockback), specialValue1(min knockback), specialValue2(min damage), specialValue3(damage variation), maxStacks, stackRegenTime
        
        // Level 1: Basic slam
        SetLevelData(1, cooldown: 8f, damage: 5f, duration: 1.2f, radius: 3f, speed: 12f, distance: 4f, specialValue1: 1.5f, specialValue2: 20f, specialValue3: 0.15f, maxStacks: 1, stackRegenTime: 8f);
        
        // Level 2: Improved damage and area
        SetLevelData(2, cooldown: 7f, damage: 9f, duration: 1.4f, radius: 3.5f, speed: 14f, distance: 4.5f, specialValue1: 2f, specialValue2: 30f, specialValue3: 0.18f, maxStacks: 1, stackRegenTime: 7f);
        
        // Level 3: Better knockback and stun
        SetLevelData(3, cooldown: 6f, damage: 12f, duration: 1.6f, radius: 4f, speed: 16f, distance: 5f, specialValue1: 2.5f, specialValue2: 40f, specialValue3: 0.2f, maxStacks: 1, stackRegenTime: 6f);
        
        // Level 4: Major improvements
        SetLevelData(4, cooldown: 5f, damage: 14f, duration: 1.8f, radius: 4.5f, speed: 18f, distance: 5.5f, specialValue1: 3f, specialValue2: 50f, specialValue3: 0.22f, maxStacks: 1, stackRegenTime: 5f);
        
        // Level 5: Maximum power
        SetLevelData(5, cooldown: 4f, damage: 16f, duration: 2f, radius: 5f, speed: 20f, distance: 6f, specialValue1: 3.5f, specialValue2: 65f, specialValue3: 0.25f, maxStacks: 1, stackRegenTime: 4f);
        
        Debug.Log($"HammerSlamAbility Initialised from code matrix. Level 1: {StackedDamage} damage, {StackedRadius}u radius");
    }

    public override bool CanActivate(Player player)
    {
        bool canActivate = player != null && !player.isDead && CurrentStacks > 0;
        
        if (!canActivate)
        {
            Debug.Log($"Hammer Slam cannot activate - Player null: {player == null}, Dead: {player?.isDead}, Stacks: {CurrentStacks}/{MaxStacksAtCurrentLevel}");
        }
        
        return canActivate;
    }

    public override void Activate(Player player, PlayerMovement playerMovement = null)
    {
        if (player == null) return;

        // Use a stack
        RemoveStack();

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
        
        Debug.Log($"Hammer Slam activated! Level {CurrentLevel} (Stack {AbilityStacks}): {StackedDamage} damage, {StackedRadius}u radius, {CurrentStacks}/{MaxStacksAtCurrentLevel} charges remaining");
    }

    private void ApplyHammerSlamDamage(Vector3 center)
    {
        LayerMask enemyLayerMask = 1 << 8; // Enemy layer
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, StackedRadius, enemyLayerMask);
        
        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Entity enemy = collider.GetComponent<Entity>();
                if (enemy == null || enemy.isDead) continue;

                // Calculate distance-based damage and effects
                float distance = Vector2.Distance(center, collider.transform.position);
                float normalizedDistance = distance / StackedRadius;
                
                // Apply damage with falloff using stacked values
                float damageMultiplier = damageFalloff.Evaluate(1f - normalizedDistance);
                float finalDamage = Mathf.Lerp(StackedSpecialValue2, StackedDamage, damageMultiplier); // specialValue2 = minDamage
                
                // Add damage variation
                float variation = finalDamage * StackedSpecialValue3; // specialValue3 = damageVariation
                finalDamage += Random.Range(-variation, variation);
                
                // Apply stun
                float stunMultiplier = stunFalloff.Evaluate(1f - normalizedDistance);
                float stunDuration = Mathf.Lerp(0.8f, StackedDuration, stunMultiplier); // Using duration for max stun
                
                // Apply damage and stun using Entity's built-in methods
                enemy.TakeDamage(finalDamage, stunDuration);
                
                // Apply knockback using the new system
                if (!enemy.isDead)
                {
                    float knockbackMultiplier = knockbackFalloff.Evaluate(1f - normalizedDistance);
                    float knockbackDistance = Mathf.Lerp(StackedSpecialValue1, StackedDistance, knockbackMultiplier); // specialValue1 = minKnockback, distance = maxKnockback
                    
                    Vector2 knockbackDirection = (collider.transform.position - center).normalized;
                    
                    // Use the simple knockback method from the new system
                    KnockbackSystem.ApplySimpleKnockback(enemy, knockbackDirection, knockbackDistance, StackedSpeed);
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
            // Scale shockwave based on stacked radius
            shockwave.transform.localScale = Vector3.one * (StackedRadius / 4f); // Assuming base radius of 4
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