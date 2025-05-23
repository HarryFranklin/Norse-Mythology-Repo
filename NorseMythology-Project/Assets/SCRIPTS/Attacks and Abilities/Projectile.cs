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
    
    private Vector2 startPosition;
    private float traveledDistance;
    private bool returningToPlayer;
    
    // Track enemies hit to prevent multiple damage instances
    private HashSet<Enemy> enemiesHitOutbound = new HashSet<Enemy>();
    private HashSet<Enemy> enemiesHitInbound = new HashSet<Enemy>();
    
    public void Initialise(Vector2 dir, float spd, float range, float dmg, bool returning, Transform playerTransform)
    {
        direction = dir;
        speed = spd;
        maxRange = range;
        damage = dmg;
        isReturning = returning;
        player = playerTransform;
        startPosition = transform.position;
        
        // Rotate projectile to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        Debug.Log($"Projectile initialised with damage: {damage}");
    }
    
    private void Update()
    {
        if (!returningToPlayer)
        {
            // Move forward
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
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
            // Return to player
            Vector2 returnDirection = (player.position - transform.position).normalized;
            transform.Translate(returnDirection * speed * Time.deltaTime, Space.World);
            
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
                Debug.Log($"Collision detected with {enemy.name} - Current Health: {enemy.currentHealth}, Returning: {returningToPlayer}");
                
                // Check if we should damage this enemy
                bool shouldDamage = false;
                
                if (!returningToPlayer)
                {
                    // Outbound journey - check if we haven't hit this enemy yet
                    if (!enemiesHitOutbound.Contains(enemy))
                    {
                        enemiesHitOutbound.Add(enemy);
                        shouldDamage = true;
                        Debug.Log($"NEW HIT: Axe hit {enemy.name} on outbound journey");
                    }
                    else
                    {
                        Debug.Log($"DUPLICATE: Already hit {enemy.name} on outbound journey");
                    }
                }
                else
                {
                    // Return journey - check if we haven't hit this enemy on return yet
                    if (!enemiesHitInbound.Contains(enemy))
                    {
                        enemiesHitInbound.Add(enemy);
                        shouldDamage = true;
                        Debug.Log($"NEW HIT: Axe hit {enemy.name} on return journey");
                    }
                    else
                    {
                        Debug.Log($"DUPLICATE: Already hit {enemy.name} on return journey");
                    }
                }
                
                if (shouldDamage)
                {
                    Debug.Log($"BEFORE DAMAGE: {enemy.name} health = {enemy.currentHealth}");
                    Debug.Log($"Dealing {damage} damage to {enemy.name}");
                    enemy.TakeDamage(damage);
                    Debug.Log($"AFTER DAMAGE: {enemy.name} health = {enemy.currentHealth}");
                    
                    // Only destroy on outbound journey if not returning
                    if (!returningToPlayer && !isReturning)
                    {
                        Debug.Log("Projectile destroyed after hitting enemy");
                        Destroy(gameObject);
                    }
                }
                else
                {
                    Debug.Log($"SKIPPED: Not damaging {enemy.name} (already hit)");
                }
            }
        }
    }
}