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
        description = "Slam a massive hammer into the ground.";
        activationMode = ActivationMode.Instant;
    }

    public override void InitialiseFromCodeMatrix()
    {
        SetLevelData(1, cooldown: 10f, damage: 4f, duration: 1.0f, radius: 3.5f, speed: 11f, distance: 3f, specialValue1: 1.25f, specialValue2: 2f, specialValue3: 0.5f, maxStacks: 1, stackRegenTime: 10f);
        SetLevelData(2, cooldown: 9f, damage: 6f, duration: 1.2f, radius: 4.0f, speed: 13f, distance: 4f, specialValue1: 1.75f, specialValue2: 4f, specialValue3: 0.5f, maxStacks: 1, stackRegenTime: 9f);
        SetLevelData(3, cooldown: 8f, damage: 9f, duration: 1.5f, radius: 4.5f, speed: 15f, distance: 5f, specialValue1: 2.25f, specialValue2: 6f, specialValue3: 0.7f, maxStacks: 2, stackRegenTime: 8f);
        SetLevelData(4, cooldown: 7f, damage: 12f, duration: 1.8f, radius: 5.0f, speed: 18f, distance: 5.5f, specialValue1: 2.75f, specialValue2: 9f, specialValue3: 0.8f, maxStacks: 2, stackRegenTime: 7f);
        SetLevelData(5, cooldown: 6f, damage: 15f, duration: 2.0f, radius: 5.5f, speed: 20f, distance: 6f, specialValue1: 3.25f, specialValue2: 12f, specialValue3: 1.0f, maxStacks: 3, stackRegenTime: 6f);
        
        Debug.Log($"HammerSlamAbility Initialised. Level 1: {GetStatsForLevel(1).damage} damage");
    }

    public override bool CanActivate(Player player)
    {
        return player != null && !player.isDead && CurrentStacks > 0;
    }

    public override void Activate(Player player, PlayerMovement playerMovement = null)
    {
        if (player == null) return;

        RemoveStack();

        Transform spawnTransform = player.hammerSpawnPoint;
        
        // Fallback: Use the player's position if I forgot to drag it in.
        Vector3 slamPosition = (spawnTransform != null) ? spawnTransform.position : player.transform.position;

        if (spawnTransform == null)
        {
            slamPosition = player.transform.position;
            slamPosition.y -= 0.5f;
        }
        else
        {
            slamPosition = spawnTransform.position;
        }

        Quaternion slamRotation = (spawnTransform != null) ? spawnTransform.rotation : Quaternion.identity;

        // Play sound effects
        if (slamSound != null)
            AudioSource.PlayClipAtPoint(slamSound, slamPosition);
        
        // Spawn visual effects
        SpawnImpactEffects(slamPosition, slamRotation);
        
        // Apply knockback damage 
        ApplyHammerSlamDamage(slamPosition);
        
        // Play delayed shockwave sound
        if (shockwaveSound != null)
        {
            player.StartCoroutine(PlayDelayedShockwaveSound(0.2f));
        }
        
        Debug.Log($"Hammer Slam activated at {slamPosition}");
    }

    private void ApplyHammerSlamDamage(Vector3 center)
    {
        LayerMask enemyLayerMask = 1 << 11; // Enemy layer
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, StackedRadius, enemyLayerMask);
        
        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Entity enemy = collider.GetComponent<Entity>();
                if (enemy == null || enemy.isDead) continue;

                float distance = Vector2.Distance(center, collider.transform.position);
                float normalizedDistance = distance / StackedRadius;
                
                // Damage
                float damageMultiplier = damageFalloff.Evaluate(1f - normalizedDistance);
                float finalDamage = Mathf.Lerp(StackedSpecialValue2, StackedDamage, damageMultiplier);
                
                float variation = finalDamage * StackedSpecialValue3;
                finalDamage += Random.Range(-variation, variation);
                
                // Stun
                float stunMultiplier = stunFalloff.Evaluate(1f - normalizedDistance);
                float stunDuration = Mathf.Lerp(0.8f, StackedDuration, stunMultiplier);
                
                bool isLethal = finalDamage >= enemy.currentHealth;

                if (isLethal)
                {
                    enemy.RegisterLethalDamage(finalDamage);
                    enemy.Stun(stunDuration);
                }
                else
                {
                    enemy.TakeDamage(finalDamage, stunDuration);
                }
                
                // Knockback
                if (!enemy.isDead)
                {
                    float knockbackMultiplier = knockbackFalloff.Evaluate(1f - normalizedDistance);
                    float knockbackDistance = Mathf.Lerp(StackedSpecialValue1, StackedDistance, knockbackMultiplier);
                    Vector2 knockbackDirection = (collider.transform.position - center).normalized;
                    KnockbackSystem.ApplySimpleKnockback(enemy, knockbackDirection, knockbackDistance, StackedSpeed);
                }
            }
        }
    }

    private System.Collections.IEnumerator PlayDelayedShockwaveSound(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (shockwaveSound != null)
        {
            Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(shockwaveSound, soundPos);
        }
    }

    private void SpawnImpactEffects(Vector3 position, Quaternion rotation)
    {
        // 1. Hammer Visual
        if (hammerPrefab != null)
        {
            Instantiate(hammerPrefab, position, rotation);
        }

        // 2. Shockwave
        if (shockwavePrefab != null)
        {
            GameObject shockwave = Instantiate(shockwavePrefab, position, Quaternion.identity);
            shockwave.transform.localScale = Vector3.one * (StackedRadius / 4f); 
        }

        // 3. Dust Cloud
        if (dustCloudPrefab != null)
        {
            Instantiate(dustCloudPrefab, position, Quaternion.identity);
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