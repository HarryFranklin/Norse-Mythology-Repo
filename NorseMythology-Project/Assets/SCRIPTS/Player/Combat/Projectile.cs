using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxRange;
    private float damage;
    private bool isReturning;
    private Transform player;
    
    // Flag to determine if we respect the custom time scale
    private bool useAbilityTimeScale = false;
    
    private Vector2 startPosition;
    private float traveledDistance;
    private bool returningToPlayer;
    
    // Track enemies hit to prevent multiple damage instances
    private HashSet<Enemy> enemiesHitOutbound = new HashSet<Enemy>();
    private HashSet<Enemy> enemiesHitInbound = new HashSet<Enemy>();
    
    // Optional 'useTimeScale' parameter (defaults to false to keep basic attacks working as before)
    public void Initialise(Vector2 dir, float spd, float range, float dmg, bool returning, Transform playerTransform, bool useTimeScale = false)
    {
        direction = dir;
        speed = spd;
        maxRange = range;
        damage = dmg;
        isReturning = returning;
        player = playerTransform;
        useAbilityTimeScale = useTimeScale; // Store the setting
        startPosition = transform.position;
        
        // Rotate projectile to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        Debug.Log($"Projectile initialised with damage: {damage}");
    }
    
    private void Update()
    {
        // Calculate the appropriate Delta Time for this frame
        float deltaTime = Time.deltaTime;
        if (useAbilityTimeScale && FreezeTimeAbility.IsTimeFrozen)
        {
            // Use the multiplier defined by the FreezeTimeAbility level
            deltaTime = Time.unscaledDeltaTime * FreezeTimeAbility.GlobalRechargeMultiplier;
        }

        if (!returningToPlayer)
        {
            // Move forward using calculated deltaTime
            transform.Translate(direction * speed * deltaTime, Space.World);
            traveledDistance = Vector2.Distance(startPosition, transform.position);
            
            // Check if should return or be destroyed
            if (traveledDistance >= maxRange)
            {
                if (isReturning && player != null)
                {
                    returningToPlayer = true;
                    Debug.Log("Projectile returning to player");
                }
                else
                {
                    Debug.Log("Projectile destroyed at max range");
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // Return to player using calculated deltaTime
            Vector2 returnDirection = (player.position - transform.position).normalized;
            transform.Translate(returnDirection * speed * deltaTime, Space.World);
            
            // Check if reached player
            if (Vector2.Distance(transform.position, player.position) < 0.5f)
            {
                Debug.Log("Projectile returned to player");
                Destroy(gameObject);
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Check if we should damage this enemy
                bool shouldDamage = false;
                
                if (!returningToPlayer)
                {
                    if (!enemiesHitOutbound.Contains(enemy))
                    {
                        enemiesHitOutbound.Add(enemy);
                        shouldDamage = true;
                    }
                }
                else
                {
                    if (!enemiesHitInbound.Contains(enemy))
                    {
                        enemiesHitInbound.Add(enemy);
                        shouldDamage = true;
                    }
                }
                
                if (shouldDamage)
                {
                    enemy.TakeDamage(damage);
                    
                    if (!returningToPlayer && !isReturning)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}