using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HammerSlamAbility", menuName = "Abilities/Defend/HammerSlam")]
public class HammerSlamAbility : DefendAbility
{
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
    
    public override bool CanActivate(PlayerController playerController)
    {
        return playerController != null && !playerController.isDead;
    }
    
    public override void Activate(PlayerController playerController, PlayerMovement playerMovement = null)
    {
        if (playerController == null) return;
        
        // Start the hammer slam coroutine
        playerController.StartCoroutine(PerformHammerSlam(playerController));
    }
    
    private IEnumerator PerformHammerSlam(PlayerController playerController)
    {
        Transform playerTransform = playerController.transform;
        Vector3 slamPosition = playerTransform.position;
        
        // Create visual hammer effect
        GameObject hammer = null;
        if (hammerPrefab != null)
        {
            hammer = Object.Instantiate(hammerPrefab, slamPosition + Vector3.up * 2f, Quaternion.identity);
        }
        
        // Animate hammer slam
        float elapsedTime = 0f;
        Vector3 startPos = slamPosition + Vector3.up * 2f;
        Vector3 endPos = slamPosition;
        
        while (elapsedTime < slamAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / slamAnimationDuration;
            
            // Ease-in animation for more impact
            float easedT = t * t;
            
            if (hammer != null)
            {
                hammer.transform.position = Vector3.Lerp(startPos, endPos, easedT);
            }
            
            yield return null;
        }
        
        // Execute the slam effect
        ExecuteSlamEffect(playerController, slamPosition);
        
        // Create visual effects
        CreateVisualEffects(slamPosition);
        
        // Clean up hammer
        if (hammer != null)
        {
            Object.Destroy(hammer, 1f);
        }
    }
    
    private void ExecuteSlamEffect(PlayerController playerController, Vector3 slamPosition)
    {
        // Find all enemies in the outer radius
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(slamPosition, outerKnockbackRadius, LayerMask.GetMask("Enemy"));
        List<Enemy> enemiesToProcess = new List<Enemy>();
        
        foreach (Collider2D collider in enemiesInRange)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemiesToProcess.Add(enemy);
            }
        }
        
        // Process each enemy
        foreach (Enemy enemy in enemiesToProcess)
        {
            float distanceFromSlam = Vector2.Distance(slamPosition, enemy.transform.position);
            
            if (distanceFromSlam <= innerDamageRadius)
            {
                // Inner radius: Deal direct damage and stun
                DealDirectDamage(enemy, innerDamage);
                ApplyStun(enemy, stunDuration);
            }
            else if (distanceFromSlam <= outerKnockbackRadius)
            {
                // Outer radius: Knockback and stun
                ApplyKnockback(enemy, slamPosition);
                ApplyStun(enemy, stunDuration);
            }
        }
    }
    
    private void DealDirectDamage(Enemy enemy, float damage)
    {
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"Hammer slam dealt {damage} direct damage to {enemy.name}");
        }
    }
    
    private void ApplyKnockback(Enemy enemy, Vector3 slamPosition)
    {
        if (enemy == null) return;
        
        Vector2 knockbackDirection = (enemy.transform.position - slamPosition).normalized;
        
        // Start knockback coroutine
        enemy.StartCoroutine(KnockbackEnemy(enemy, knockbackDirection));
    }
    
    private IEnumerator KnockbackEnemy(Enemy enemy, Vector2 knockbackDirection)
    {
        float knockbackDuration = 0.5f;
        float elapsedTime = 0f;
        Vector3 startPosition = enemy.transform.position;
        Vector3 targetPosition = startPosition + (Vector3)(knockbackDirection * knockbackForce);
        
        while (elapsedTime < knockbackDuration && enemy != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / knockbackDuration;
            
            // Ease-out for more natural knockback
            float easedT = 1f - (1f - t) * (1f - t);
            
            enemy.transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
            yield return null;
        }
        
        // Deal landing damage
        if (enemy != null)
        {
            DealDirectDamage(enemy, landingDamage);
            Debug.Log($"Enemy {enemy.name} took {landingDamage} landing damage");
        }
    }
    
    private void ApplyStun(Enemy enemy, float duration)
    {
        if (enemy == null) return;
        
        // Empty for now
    }
    
    private void CreateVisualEffects(Vector3 position)
    {
        // Create shockwave effect
        if (shockwavePrefab != null)
        {
            GameObject shockwave = Object.Instantiate(shockwavePrefab, position, Quaternion.identity);
            Object.Destroy(shockwave, 2f);
        }
        
        // Create dust cloud effect
        if (dustCloudPrefab != null)
        {
            GameObject dustCloud = Object.Instantiate(dustCloudPrefab, position, Quaternion.identity);
            Object.Destroy(dustCloud, 3f);
        }
    }
}