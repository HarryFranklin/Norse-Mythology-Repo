using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "HammerSlamAbility", menuName = "Abilities/HammerSlam")]
public class HammerSlamAbility : Ability
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject hammerPrefab;    
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private GameObject dustCloudPrefab;
    
    [Header("Audio")]
    [SerializeField] private AudioClip slamSound;
    [Range(0f, 1f)] 
    [SerializeField] private float slamVolume = 1f;

    [Tooltip("The secondary lightning/shockwave noise.")]
    [SerializeField] private AudioClip shockwaveSound;
    [Range(0f, 1f)] 
    [SerializeField] private float shockwaveVolume = 1f;

    [Tooltip("If true, randomises pitch slightly for a heavier, organic feel.")]
    [SerializeField] private bool useRandomPitch = true;
    
    [Header("Screen Shake")]
    [Tooltip("Base shake magnitude.")]
    [SerializeField] private float shakeMagnitude = 0.4f;
    [Tooltip("Fallback duration if no shockwave sound is assigned.")]
    [SerializeField] private float defaultShakeDuration = 0.3f;

    [Header("Movement")]
    [Tooltip("How long the player cannot move after activating the slam.")]
    [SerializeField] private float movementLockDuration = 0.5f;

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
    }

    public override bool CanActivate(Player player)
    {
        // Don't allow activation if we are already locked (e.g. dashing or stunned)
        if (player.GetComponent<PlayerMovement>().isMovementLocked) return false;

        return player != null && !player.isDead && CurrentStacks > 0;
    }

    public override void Activate(Player player, PlayerMovement playerMovement = null)
    {
        if (player == null) return;
        if (playerMovement == null) playerMovement = player.GetComponent<PlayerMovement>();

        RemoveStack();

        // Start the Coroutine on the Player MonoBehaviour
        player.StartCoroutine(ExecuteSlamRoutine(player, playerMovement));
    }

    private IEnumerator ExecuteSlamRoutine(Player player, PlayerMovement playerMovement)
    {
        // 1. Lock Movement
        bool wasLocked = false;
        if (playerMovement != null)
        {
            wasLocked = playerMovement.isMovementLocked;
            playerMovement.isMovementLocked = true;
            
            // Stop existing momentum immediately so they don't slide while slamming
            if (player.rigidBody != null)
            {
                player.rigidBody.linearVelocity = Vector2.zero;
            }
        }

        // 2. Perform the Slam (Audio, Visuals, Damage)
        PerformSlamLogic(player);

        // 3. Wait for animation/recovery
        yield return new WaitForSeconds(movementLockDuration);

        // 4. Unlock
        if (playerMovement != null)
        {
            playerMovement.isMovementLocked = wasLocked;
        }
    }

    private void PerformSlamLogic(Player player)
    {
        Transform spawnTransform = player.hammerSpawnPoint;
        Vector3 slamPosition = (spawnTransform != null) ? spawnTransform.position : player.transform.position;

        if (spawnTransform == null)
        {
            slamPosition = player.transform.position;
            slamPosition.y -= 0.5f;
        }

        Quaternion slamRotation = (spawnTransform != null) ? spawnTransform.rotation : Quaternion.identity;

        // --- AUDIO ---
        if (AudioManager.Instance != null)
        {
            if (slamSound != null) AudioManager.Instance.PlaySFX(slamSound, slamVolume, useRandomPitch);
            if (shockwaveSound != null) AudioManager.Instance.PlaySFX(shockwaveSound, shockwaveVolume, useRandomPitch);
        }

        // --- SCREEN SHAKE ---
        if (Camera.main != null)
        {
            CameraController cam = Camera.main.GetComponent<CameraController>();
            if (cam != null)
            {
                float finalShakeDuration = (shockwaveSound != null) ? shockwaveSound.length : defaultShakeDuration;
                cam.TriggerShake(finalShakeDuration, shakeMagnitude);
            }
        }

        // Spawn visual effects
        SpawnImpactEffects(slamPosition, slamRotation);
        
        // Apply knockback damage 
        ApplyHammerSlamDamage(slamPosition);
        
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

    private void SpawnImpactEffects(Vector3 position, Quaternion rotation)
    {
        if (hammerPrefab != null) Instantiate(hammerPrefab, position, rotation);
        
        if (shockwavePrefab != null)
        {
            GameObject shockwave = Instantiate(shockwavePrefab, position, Quaternion.identity);
            shockwave.transform.localScale = Vector3.one * (StackedRadius / 4f); 
        }

        if (dustCloudPrefab != null) Instantiate(dustCloudPrefab, position, Quaternion.identity);
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