using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Knockback : MonoBehaviour
{
    [System.Serializable]
    public class DamageZone
    {
        [SerializeField] public float radius;
        [SerializeField] public float damage;
        [SerializeField] public float knockbackDistance;
        [SerializeField] public float stunDuration;
        [SerializeField] public Color gizmoColor = Color.red;

        public DamageZone(float radius, float damage, float knockbackDistance, float stunDuration)
        {
            this.radius = radius;
            this.damage = damage;
            this.knockbackDistance = knockbackDistance;
            this.stunDuration = stunDuration;
        }
    }

    public enum KnockbackType
    {
        Radial,
        Directional
    }

    [Header("Screen Bounds")]
    [SerializeField] private Vector2 screenBounds = new Vector2(10f, 6f);
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private string projectileTag = "Projectile";
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Knockback Variation")]
    [SerializeField] private float knockbackVariation = 0.3f;
    [SerializeField] private float knockbackForceMultiplier = 1.5f;

    [Header("Visual Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private float gizmoAlpha = 0.3f;

    public static void ApplyRadialKnockback(Vector3 center, DamageZone[] zones, 
        string enemyTag = "Enemy", float knockbackDuration = 0.2f, Vector2 screenBounds = default)
    {
        if (screenBounds == default)
            screenBounds = new Vector2(10f, 6f);
            
        ApplyKnockback(center, zones, KnockbackType.Radial, Vector2.zero, enemyTag, knockbackDuration, screenBounds);
    }

    public static void ApplyDirectionalKnockback(Vector3 center, DamageZone[] zones, Vector2 direction,
        string enemyTag = "Enemy", float knockbackDuration = 0.2f, Vector2 screenBounds = default)
    {
        if (screenBounds == default)
            screenBounds = new Vector2(10f, 6f);
            
        ApplyKnockback(center, zones, KnockbackType.Directional, direction, enemyTag, knockbackDuration, screenBounds);
    }

    private static void ApplyKnockback(Vector3 center, DamageZone[] zones, KnockbackType type, 
        Vector2 direction, string enemyTag, float knockbackDuration, Vector2 screenBounds)
    {
        if (zones == null || zones.Length == 0) return;

        float maxRadius = 0f;
        foreach (var zone in zones)
        {
            if (zone.radius > maxRadius)
                maxRadius = zone.radius;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, maxRadius);
        List<Enemy> enemies = new List<Enemy>();

        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag(enemyTag))
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead)
                {
                    enemies.Add(enemy);
                }
            }
        }

        foreach (Enemy enemy in enemies)
        {
            float distance = Vector2.Distance(center, enemy.transform.position);
            
            DamageZone applicableZone = null;
            foreach (var zone in zones)
            {
                if (distance <= zone.radius)
                {
                    if (applicableZone == null || zone.radius < applicableZone.radius)
                    {
                        applicableZone = zone;
                    }
                }
            }

            if (applicableZone != null)
            {
                DestroyEnemyProjectiles(enemy);
                
                enemy.TakeDamage(applicableZone.damage, applicableZone.stunDuration);

                if (!enemy.isDead)
                {
                    Vector2 knockbackDir = CalculateKnockbackDirection(center, enemy.transform.position, type, direction);
                    ApplyRigidbodyKnockback(enemy, center, knockbackDir, applicableZone, distance, knockbackDuration, screenBounds);
                }
            }
        }
    }

    private static Vector2 CalculateKnockbackDirection(Vector3 center, Vector3 enemyPosition, 
        KnockbackType type, Vector2 direction)
    {
        switch (type)
        {
            case KnockbackType.Radial:
                return (enemyPosition - center).normalized;
            case KnockbackType.Directional:
                return direction.normalized;
            default:
                return Vector2.zero;
        }
    }

    private static void DestroyEnemyProjectiles(Enemy enemy)
    {
        if (enemy == null) return;

        Transform[] childTransforms = enemy.GetComponentsInChildren<Transform>();
        foreach (Transform child in childTransforms)
        {
            if (child != enemy.transform && child.CompareTag("Projectile"))
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        GameObject[] projectilesOnEnemy = GameObject.FindGameObjectsWithTag("Projectile");
        foreach (GameObject projectile in projectilesOnEnemy)
        {
            if (projectile.transform.IsChildOf(enemy.transform) || projectile.transform == enemy.transform)
            {
                GameObject.Destroy(projectile);
            }
        }
    }

    private static void ApplyRigidbodyKnockback(Enemy enemy, Vector3 center, Vector2 direction, 
        DamageZone zone, float currentDistance, float duration, Vector2 screenBounds)
    {
        if (enemy == null || enemy.isDead) return;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        float normalisedDistance = Mathf.Clamp01(currentDistance / zone.radius);
        float knockbackMultiplier = Mathf.Lerp(1f, 0.3f, normalisedDistance);
        
        float variation = Random.Range(-0.3f, 0.3f);
        float finalKnockbackDistance = zone.knockbackDistance * knockbackMultiplier * (1f + variation);
        
        float knockbackForce = finalKnockbackDistance * knockbackMultiplier;
        
        Vector2 knockbackVelocity = direction * knockbackForce * 1.5f;
        
        float originalMoveSpeed = enemy.moveSpeed;
        enemy.moveSpeed = 0f;
        
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackVelocity, ForceMode2D.Impulse);
        
        MonoBehaviour monoBehaviour = enemy.GetComponent<MonoBehaviour>();
        if (monoBehaviour != null)
        {
            monoBehaviour.StartCoroutine(RestoreMovementAfterKnockback(enemy, originalMoveSpeed, duration));
        }
    }

    private static IEnumerator RestoreMovementAfterKnockback(Enemy enemy, float originalMoveSpeed, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (enemy != null && !enemy.isDead)
        {
            enemy.moveSpeed = originalMoveSpeed;
            
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.5f);
            }
        }
    }

    public static DamageZone[] CreateStandardZones(float innerRadius, float outerRadius, 
        float innerDamage, float outerDamage, float innerKnockbackDistance, float outerKnockbackDistance,
        float innerStun, float outerStun)
    {
        return new DamageZone[]
        {
            new DamageZone(innerRadius, innerDamage, innerKnockbackDistance, innerStun) { gizmoColor = Color.red },
            new DamageZone(outerRadius, outerDamage, outerKnockbackDistance, outerStun) { gizmoColor = Color.yellow }
        };
    }

    public static DamageZone[] CreateSingleZone(float radius, float damage, float knockbackDistance, float stun)
    {
        return new DamageZone[]
        {
            new DamageZone(radius, damage, knockbackDistance, stun) { gizmoColor = Color.red }
        };
    }
}