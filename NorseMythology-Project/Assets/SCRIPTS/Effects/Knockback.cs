using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Knockback : MonoBehaviour
{
    [System.Serializable]
    public class KnockbackSettings
    {
        [Header("Range & Damage")]
        public float maxRadius = 5f;
        public float maxDamage = 100f;
        public float minDamage = 20f;
        
        [Header("Knockback")]
        public float maxKnockbackDistance = 8f;
        public float minKnockbackDistance = 2f;
        public float knockbackSpeed = 15f;
        public float knockbackDuration = 0.5f;
        
        [Header("Stun")]
        public float maxStunDuration = 2f;
        public float minStunDuration = 0.5f;
        
        [Header("Variation")]
        [Range(0f, 0.5f)]
        public float damageVariation = 0.1f;
        [Range(0f, 0.5f)]
        public float knockbackVariation = 0.2f;
        
        [Header("Falloff")]
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        public AnimationCurve knockbackFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        public AnimationCurve stunFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    }

    [Header("Target Settings")]
    [SerializeField] private LayerMask enemyLayerMask = -1;
    [SerializeField] private string enemyTag = "Enemy";
    
    [Header("Visual Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color gizmoColor = Color.red;
    [SerializeField] private float gizmoAlpha = 0.3f;

    public static void ApplyRadialKnockback(Vector3 center, KnockbackSettings settings, 
        LayerMask enemyLayer = default, string enemyTag = "Enemy")
    {
        if (settings == null) return;
        
        // Use all layers if no specific layer mask is provided
        if (enemyLayer == default)
            enemyLayer = ~0; // All layers

        // Find all potential targets in radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, settings.maxRadius, enemyLayer);
        List<Enemy> validEnemies = new List<Enemy>();

        // Filter for valid enemies
        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag(enemyTag))
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead)
                {
                    validEnemies.Add(enemy);
                }
            }
        }

        // Apply knockback to each valid enemy
        foreach (Enemy enemy in validEnemies)
        {
            ApplyKnockbackToEnemy(enemy, center, settings);
        }
    }

    public static void ApplyDirectionalKnockback(Vector3 center, Vector2 direction, 
        KnockbackSettings settings, LayerMask enemyLayer = default, string enemyTag = "Enemy")
    {
        if (settings == null) return;
        
        // Use all layers if no specific layer mask is provided
        if (enemyLayer == default)
            enemyLayer = ~0; // All layers

        direction = direction.normalized;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, settings.maxRadius, enemyLayer);
        List<Enemy> validEnemies = new List<Enemy>();

        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag(enemyTag))
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead)
                {
                    validEnemies.Add(enemy);
                }
            }
        }

        foreach (Enemy enemy in validEnemies)
        {
            ApplyKnockbackToEnemy(enemy, center, settings, direction);
        }
    }

    private static void ApplyKnockbackToEnemy(Enemy enemy, Vector3 center, KnockbackSettings settings, 
        Vector2? forceDirection = null)
    {
        if (enemy == null || enemy.isDead || settings == null) return;

        Vector3 enemyPos = enemy.transform.position;
        float distance = Vector2.Distance(center, enemyPos);
        
        // Skip if outside radius
        if (distance > settings.maxRadius) return;

        // Calculate normalized distance (0 = center, 1 = edge)
        float normalizedDistance = distance / settings.maxRadius;

        // Calculate damage with falloff and variation
        float baseDamage = Mathf.Lerp(settings.maxDamage, settings.minDamage, 
            settings.damageFalloff.Evaluate(normalizedDistance));
        float damageVariation = Random.Range(-settings.damageVariation, settings.damageVariation);
        float finalDamage = baseDamage * (1f + damageVariation);

        // Calculate knockback distance with falloff and variation
        float baseKnockbackDistance = Mathf.Lerp(settings.maxKnockbackDistance, settings.minKnockbackDistance,
            settings.knockbackFalloff.Evaluate(normalizedDistance));
        float knockbackVariation = Random.Range(-settings.knockbackVariation, settings.knockbackVariation);
        float finalKnockbackDistance = baseKnockbackDistance * (1f + knockbackVariation);

        // Calculate stun duration with falloff
        float finalStunDuration = Mathf.Lerp(settings.maxStunDuration, settings.minStunDuration,
            settings.stunFalloff.Evaluate(normalizedDistance));

        // Determine knockback direction
        Vector2 knockbackDirection;
        if (forceDirection.HasValue)
        {
            knockbackDirection = forceDirection.Value;
        }
        else
        {
            // Radial direction from center
            if (distance < 0.1f)
            {
                // If too close to center, use random direction
                knockbackDirection = Random.insideUnitCircle.normalized;
            }
            else
            {
                knockbackDirection = (enemyPos - center).normalized;
            }
        }

        // Destroy enemy projectiles
        DestroyEnemyProjectiles(enemy);

        // Apply damage and stun
        enemy.TakeDamage(finalDamage, finalStunDuration);

        // Apply knockback if enemy is still alive
        if (!enemy.isDead)
        {
            ApplyPhysicsKnockback(enemy, knockbackDirection, finalKnockbackDistance, settings);
        }
    }

    private static void ApplyPhysicsKnockback(Enemy enemy, Vector2 direction, float distance, 
        KnockbackSettings settings)
    {
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // Store original movement state
        float originalMoveSpeed = enemy.moveSpeed;
        
        // Disable enemy movement during knockback
        enemy.moveSpeed = 0f;
        
        // Clear existing velocity
        rb.linearVelocity = Vector2.zero;
        
        // Calculate knockback force
        Vector2 knockbackForce = direction * settings.knockbackSpeed;
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        // Start coroutine to handle knockback duration and recovery
        MonoBehaviour monoBehaviour = enemy.GetComponent<MonoBehaviour>();
        if (monoBehaviour != null)
        {
            monoBehaviour.StartCoroutine(HandleKnockbackMotion(enemy, rb, originalMoveSpeed, 
                distance, settings.knockbackSpeed, settings.knockbackDuration));
        }
    }

    private static IEnumerator HandleKnockbackMotion(Enemy enemy, Rigidbody2D rb, 
        float originalMoveSpeed, float targetDistance, float knockbackSpeed, float duration)
    {
        if (enemy == null || rb == null) yield break;

        Vector2 startPosition = rb.position;
        float elapsedTime = 0f;
        
        // Monitor knockback progress
        while (elapsedTime < duration && enemy != null && !enemy.isDead)
        {
            elapsedTime += Time.fixedDeltaTime;
            
            // Apply gradual deceleration
            float decelerationFactor = 1f - (elapsedTime / duration);
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
            
            yield return new WaitForFixedUpdate();
        }

        // Restore enemy state
        if (enemy != null && !enemy.isDead)
        {
            enemy.moveSpeed = originalMoveSpeed;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private static void DestroyEnemyProjectiles(Enemy enemy)
    {
        if (enemy == null) return;

        // Destroy child projectiles
        Transform[] childTransforms = enemy.GetComponentsInChildren<Transform>();
        foreach (Transform child in childTransforms)
        {
            if (child != enemy.transform && child.CompareTag("Projectile"))
            {
                GameObject.Destroy(child.gameObject);
            }
        }
    }

    // Convenience methods for quick setup
    public static KnockbackSettings CreateExplosionSettings(float radius = 5f, float maxDamage = 100f)
    {
        var settings = new KnockbackSettings();
        settings.maxRadius = radius;
        settings.maxDamage = maxDamage;
        settings.minDamage = maxDamage * 0.3f;
        settings.maxKnockbackDistance = radius * 1.5f;
        settings.minKnockbackDistance = radius * 0.5f;
        settings.knockbackSpeed = 20f;
        settings.knockbackDuration = 0.4f;
        
        // Set up falloff curves for explosion-like behavior
        settings.damageFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);
        settings.knockbackFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f);
        settings.stunFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f);
        
        return settings;
    }

    public static KnockbackSettings CreatePushSettings(float radius = 3f, float force = 50f)
    {
        var settings = new KnockbackSettings();
        settings.maxRadius = radius;
        settings.maxDamage = force;
        settings.minDamage = force * 0.5f;
        settings.maxKnockbackDistance = radius * 2f;
        settings.minKnockbackDistance = radius * 0.8f;
        settings.knockbackSpeed = 15f;
        settings.knockbackDuration = 0.6f;
        
        // Linear falloff for consistent push
        settings.damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
        settings.knockbackFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.6f);
        settings.stunFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
        
        return settings;
    }

    // Debug visualisation
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // This would show the radius if this component was attached to a GameObject
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoAlpha);
        Gizmos.DrawSphere(transform.position, 5f); // Default radius for visualisation
    }
}