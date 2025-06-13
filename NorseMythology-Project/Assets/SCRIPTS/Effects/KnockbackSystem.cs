using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class KnockbackSystem
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

    public static void ApplyRadialKnockback(Vector3 center, KnockbackSettings settings, 
        LayerMask enemyLayer = default, string enemyTag = "Enemy")
    {
        if (settings == null) return;
        
        if (enemyLayer == default)
            enemyLayer = ~0;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, settings.maxRadius, enemyLayer);
        List<Entity> validEntities = new List<Entity>();

        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag(enemyTag))
            {
                Entity entity = collider.GetComponent<Entity>();
                if (entity != null && !entity.isDead)
                {
                    validEntities.Add(entity);
                }
            }
        }

        foreach (Entity entity in validEntities)
        {
            ApplyKnockbackToEntity(entity, center, settings);
        }
    }

    public static void ApplyDirectionalKnockback(Vector3 center, Vector2 direction, 
        KnockbackSettings settings, LayerMask enemyLayer = default, string enemyTag = "Enemy")
    {
        if (settings == null) return;
        
        if (enemyLayer == default)
            enemyLayer = ~0;

        direction = direction.normalized;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, settings.maxRadius, enemyLayer);
        List<Entity> validEntities = new List<Entity>();

        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag(enemyTag))
            {
                Entity entity = collider.GetComponent<Entity>();
                if (entity != null && !entity.isDead)
                {
                    validEntities.Add(entity);
                }
            }
        }

        foreach (Entity entity in validEntities)
        {
            ApplyKnockbackToEntity(entity, center, settings, direction);
        }
    }

    // Simple method that works with your existing Entity system
    public static void ApplySimpleKnockback(Entity entity, Vector2 direction, float distance, float speed)
    {
        if (entity == null || entity.isDead) return;

        MonoBehaviour monoBehaviour = entity.GetComponent<MonoBehaviour>();
        if (monoBehaviour != null)
        {
            monoBehaviour.StartCoroutine(SimpleKnockbackCoroutine(entity, direction, distance, speed));
        }
    }

    private static void ApplyKnockbackToEntity(Entity entity, Vector3 center, KnockbackSettings settings, 
        Vector2? forceDirection = null)
    {
        if (entity == null || entity.isDead || settings == null) return;

        Vector3 entityPos = entity.transform.position;
        float distance = Vector2.Distance(center, entityPos);
        
        if (distance > settings.maxRadius) return;

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
            if (distance < 0.1f)
            {
                knockbackDirection = Random.insideUnitCircle.normalized;
            }
            else
            {
                knockbackDirection = (entityPos - center).normalized;
            }
        }

        // Apply damage and stun using Entity's built-in methods
        entity.TakeDamage(finalDamage, finalStunDuration);

        // Apply knockback if entity is still alive
        if (!entity.isDead)
        {
            ApplyPhysicsKnockback(entity, knockbackDirection, finalKnockbackDistance, settings);
        }
    }

    private static void ApplyPhysicsKnockback(Entity entity, Vector2 direction, float distance, 
        KnockbackSettings settings)
    {
        Rigidbody2D rb = entity.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // Store original movement state
        float originalMoveSpeed = entity.moveSpeed;
        
        // Disable entity movement during knockback
        entity.moveSpeed = 0f;
        
        // Clear existing velocity
        rb.linearVelocity = Vector2.zero;
        
        // Calculate knockback force
        Vector2 knockbackForce = direction * settings.knockbackSpeed;
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        // Start coroutine to handle knockback duration and recovery
        MonoBehaviour monoBehaviour = entity.GetComponent<MonoBehaviour>();
        if (monoBehaviour != null)
        {
            monoBehaviour.StartCoroutine(HandleKnockbackMotion(entity, rb, originalMoveSpeed, 
                distance, settings.knockbackSpeed, settings.knockbackDuration));
        }
    }

    private static IEnumerator HandleKnockbackMotion(Entity entity, Rigidbody2D rb, 
        float originalMoveSpeed, float targetDistance, float knockbackSpeed, float duration)
    {
        if (entity == null || rb == null) yield break;

        Vector2 startPosition = rb.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && entity != null && !entity.isDead)
        {
            elapsedTime += Time.fixedDeltaTime;
            
            // Apply gradual deceleration
            float decelerationFactor = 1f - (elapsedTime / duration);
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
            
            yield return new WaitForFixedUpdate();
        }

        // Restore entity state
        if (entity != null && !entity.isDead)
        {
            entity.moveSpeed = originalMoveSpeed;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private static IEnumerator SimpleKnockbackCoroutine(Entity entity, Vector2 direction, float distance, float speed)
    {
        if (entity == null || entity.isDead) yield break;

        float originalMoveSpeed = entity.moveSpeed;
        entity.moveSpeed = 0f; // Disable normal movement during knockback

        Vector3 startPos = entity.transform.position;
        Vector3 targetPos = startPos + (Vector3)(direction.normalized * distance);
        
        float elapsedTime = 0f;
        float knockbackDuration = distance / speed;

        while (elapsedTime < knockbackDuration && entity != null && !entity.isDead)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / knockbackDuration;
            
            // Use an easing curve for more natural knockback
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f); // Ease-out cubic
            
            entity.transform.position = Vector3.Lerp(startPos, targetPos, easedProgress);
            
            yield return null;
        }

        // Restore original move speed
        if (entity != null && !entity.isDead)
        {
            entity.moveSpeed = originalMoveSpeed;
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
        
        settings.damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
        settings.knockbackFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.6f);
        settings.stunFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
        
        return settings;
    }
}