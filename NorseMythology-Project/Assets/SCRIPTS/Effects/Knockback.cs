using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CircleCollider2D))]
public class Knockback : MonoBehaviour
{
    public enum KnockbackType
    {
        Directional,
        Radial
    }

    public enum ZoneType
    {
        OneZone,
        TwoZone
    }

    [SerializeField] private CircleCollider2D collider2D;
    [SerializeField] private List<Enemy> enemiesHit = new List<Enemy>();
    [SerializeField] public float[] radii;
    private List<Enemy> zoneAEnemies = new List<Enemy>();
    private List<Enemy> zoneBEnemies = new List<Enemy>();

    [Header("Damage Settings")]
    [SerializeField] private float zoneADamage = 50f; // Inner zone damage
    [SerializeField] private float zoneBDamage = 25f; // Outer zone damage
    [SerializeField] private float zoneAStunDuration = 1.5f; // Inner zone stun
    [SerializeField] private float zoneBStunDuration = 0.75f; // Outer zone stun

    [Header("Knockback Settings")]
    [SerializeField] private KnockbackType knockbackType = KnockbackType.Radial;
    [SerializeField] private Vector2 knockbackDirection = Vector2.right; // For directional knockback
    [SerializeField] private float zoneAKnockbackForce = 15f;
    [SerializeField] private float zoneBKnockbackForce = 8f;
    [SerializeField] private float knockbackDuration = 0.5f;

    [Header("Gizmo Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color zoneAColor = Color.red;
    [SerializeField] private Color zoneBColor = Color.yellow;
    [SerializeField] private Color enemyZoneAColor = Color.magenta;
    [SerializeField] private Color enemyZoneBColor = Color.cyan;

    private void Start()
    {
        if (collider2D == null)
        {
            SetupCollider(5f);
        }

        DetectEnemies();
        CategoriseEnemies();
        ApplyKnockbackEffects();
    }

    private void SetupCollider(float? radius)
    {
        collider2D = GetComponent<CircleCollider2D>();
        if (collider2D == null)
        {
            collider2D = gameObject.AddComponent<CircleCollider2D>();
        }

        collider2D.radius = (float)radius;
        collider2D.isTrigger = true;
    }

    private void DetectEnemies()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, collider2D.radius);
        foreach (Collider2D hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null && !enemiesHit.Contains(enemy))
            {
                enemiesHit.Add(enemy);
            }
        }
    }

    private void CategoriseEnemies()
    {
        foreach(Enemy enemy in enemiesHit)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= radii[0])
            {
                zoneAEnemies.Add(enemy);
            }
            else if ((distance <= radii[1]) && (distance > radii[0]))
            {
                zoneBEnemies.Add(enemy);
            }
        }
    }

    private void ApplyKnockbackEffects()
    {
        // Apply Zone A effects (inner zone - higher damage)
        foreach (Enemy enemy in zoneAEnemies)
        {
            if (enemy != null && !enemy.isDead)
            {
                // Apply damage and stun
                enemy.TakeDamage(zoneADamage, zoneAStunDuration);
                
                // Apply knockback
                ApplyKnockbackToEnemy(enemy, zoneAKnockbackForce);
            }
        }

        // Apply Zone B effects (outer zone - lower damage)
        foreach (Enemy enemy in zoneBEnemies)
        {
            if (enemy != null && !enemy.isDead)
            {
                // Apply damage and stun
                enemy.TakeDamage(zoneBDamage, zoneBStunDuration);
                
                // Apply knockback
                ApplyKnockbackToEnemy(enemy, zoneBKnockbackForce);
            }
        }

        // Destroy the knockback object after a short delay to allow for visual effects
        StartCoroutine(DestroyAfterDelay(0.1f));
    }

    private void ApplyKnockbackToEnemy(Enemy enemy, float force)
    {
        Vector2 knockbackDir;
        
        switch (knockbackType)
        {
            case KnockbackType.Radial:
                // Knockback away from the center of the explosion
                knockbackDir = (enemy.transform.position - transform.position).normalized;
                break;
            case KnockbackType.Directional:
                // Knockback in a specified direction
                knockbackDir = knockbackDirection.normalized;
                break;
            default:
                knockbackDir = Vector2.zero;
                break;
        }

        // Apply the knockback force using Rigidbody2D
        ApplyKnockbackForce(enemy, knockbackDir, force);
    }

    private void ApplyKnockbackForce(Enemy enemy, Vector2 direction, float force)
    {
        if (enemy == null || enemy.isDead) return;

        // Get or add Rigidbody2D component
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();

        // Apply impulse force for instant knockback
        Vector2 knockbackForce = direction * force;
        enemyRb.AddForce(knockbackForce, ForceMode2D.Impulse);

        // Start coroutine to temporarily disable enemy movement
        StartCoroutine(DisableEnemyMovementDuringKnockback(enemy));
    }

    private IEnumerator DisableEnemyMovementDuringKnockback(Enemy enemy)
    {
        if (enemy == null || enemy.isDead) yield break;

        // Store original move speed to restore later
        float originalMoveSpeed = enemy.moveSpeed;
        enemy.moveSpeed = 0f; // Prevent enemy from moving during knockback

        // Wait for knockback duration
        yield return new WaitForSeconds(knockbackDuration);

        // Restore original move speed
        if (enemy != null && !enemy.isDead)
        {
            enemy.moveSpeed = originalMoveSpeed;
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // Public method to set up the knockback from external scripts
    public void Initialise(Vector3 position, float[] zones, float innerDamage = 50f, float outerDamage = 25f, 
                          KnockbackType kbType = KnockbackType.Radial, Vector2 direction = default)
    {
        transform.position = position;
        radii = zones;
        zoneADamage = innerDamage;
        zoneBDamage = outerDamage;
        knockbackType = kbType;
        
        if (direction != default)
            knockbackDirection = direction;

        // Set collider radius to outer zone
        if (zones.Length > 1)
            SetupCollider(zones[1]);
        else if (zones.Length > 0)
            SetupCollider(zones[0]);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Draw the detection zones
        DrawDetectionZones();
        
        // Draw enemy indicators
        DrawEnemyIndicators();
    }

    private void DrawDetectionZones()
    {
        Vector3 position = transform.position;

        // Draw Zone A (inner radius) - solid circle
        if (radii != null && radii.Length > 0)
        {
            Gizmos.color = new Color(zoneAColor.r, zoneAColor.g, zoneAColor.b, 0.3f);
            Gizmos.DrawSphere(position, radii[0]);
            
            Gizmos.color = zoneAColor;
            Gizmos.DrawWireSphere(position, radii[0]);
        }

        // Draw Zone B (outer radius) - wire circle only
        if (radii != null && radii.Length > 1)
        {
            Gizmos.color = new Color(zoneBColor.r, zoneBColor.g, zoneBColor.b, 0.15f);
            Gizmos.DrawSphere(position, radii[1]);
            
            Gizmos.color = zoneBColor;
            Gizmos.DrawWireSphere(position, radii[1]);
        }

        // Draw collider radius for reference
        if (collider2D != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(position, collider2D.radius);
        }

        // Draw knockback direction indicator for directional knockback
        if (knockbackType == KnockbackType.Directional)
        {
            Gizmos.color = Color.blue;
            Vector3 directionEnd = position + (Vector3)(knockbackDirection.normalized * 2f);
            Gizmos.DrawLine(position, directionEnd);
            Gizmos.DrawWireSphere(directionEnd, 0.2f);
        }
    }

    private void DrawEnemyIndicators()
    {
        // Draw Zone A enemies
        Gizmos.color = enemyZoneAColor;
        foreach (Enemy enemy in zoneAEnemies)
        {
            if (enemy != null)
            {
                Vector3 enemyPos = enemy.transform.position;
                Gizmos.DrawWireCube(enemyPos, Vector3.one * 0.5f);
                Gizmos.DrawLine(transform.position, enemyPos);
                
                // Draw a small sphere to make it more visible
                Gizmos.DrawSphere(enemyPos + Vector3.up * 0.3f, 0.1f);
            }
        }

        // Draw Zone B enemies
        Gizmos.color = enemyZoneBColor;
        foreach (Enemy enemy in zoneBEnemies)
        {
            if (enemy != null)
            {
                Vector3 enemyPos = enemy.transform.position;
                Gizmos.DrawWireCube(enemyPos, Vector3.one * 0.5f);
                Gizmos.DrawLine(transform.position, enemyPos);
                
                // Draw a small sphere to make it more visible
                Gizmos.DrawSphere(enemyPos + Vector3.up * 0.3f, 0.1f);
            }
        }

        // Draw all detected enemies (for debugging)
        Gizmos.color = Color.gray;
        foreach (Enemy enemy in enemiesHit)
        {
            if (enemy != null && !zoneAEnemies.Contains(enemy) && !zoneBEnemies.Contains(enemy))
            {
                Vector3 enemyPos = enemy.transform.position;
                Gizmos.DrawWireCube(enemyPos, Vector3.one * 0.3f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Draw more detailed info when selected
        Vector3 position = transform.position;
        
        // Draw distance measurements
        Gizmos.color = Color.white;
        if (radii != null && radii.Length > 0)
        {
            // Draw radius lines
            Gizmos.DrawLine(position, position + Vector3.right * radii[0]);
            if (radii.Length > 1)
            {
                Gizmos.DrawLine(position, position + Vector3.up * radii[1]);
            }
        }

        // Draw enemy count info
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(position + Vector3.up * 2f, Vector3.one * 0.2f);
    }
}