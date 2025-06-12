using UnityEngine;
using System.Collections.Generic;

public class Enemy : Entity
{
    public enum EnemyType { Melee, Projectile }
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Enemy XP & Level")]
    public float xpValue = 10f; // XP value for the player when this enemy is defeated and XP is collected
    public int level = 1; // Level of the enemy, can be used for scaling difficulty

    [Header("XP Orb")]
    public GameObject xpOrbPrefab; // 

    [Header("Combat Settings")]
    public float meleeAttackRange = 0.5f;
    public float projectileMinRange = 2f;
    public float projectileMaxRange = 6f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("Targeting")]
    public Transform target;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint; // A child object indicating where to shoot from
    
    // Track active projectiles spawned by this enemy
    private List<GameObject> activeProjectiles = new List<GameObject>();

    protected override void Start()
    {
        base.Start(); // Call Entity's Start method
        
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
    }

    private void Update()
    {
        if (isStunned || isFrozen || target == null) return;

        float distanceToTarget = GetDistanceTo(target);

        switch (enemyType)
        {
            case EnemyType.Melee:
                HandleMeleeBehavior(distanceToTarget);
                break;

            case EnemyType.Projectile:
                HandleProjectileBehavior(distanceToTarget);
                break;
        }
        
        // Clean up destroyed projectiles from our tracking list
        CleanupDestroyedProjectiles();
    }

    private void HandleMeleeBehavior(float distance)
    {
        if (distance > meleeAttackRange)
        {
            MoveTowards(target.position);
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    private void HandleProjectileBehavior(float distance)
    {
        if (distance > projectileMaxRange)
        {
            MoveTowards(target.position);
        }
        else if (distance < projectileMinRange)
        {
            MoveAwayFrom(target.position);
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            ShootProjectile();
            lastAttackTime = Time.time;
        }
    }

    private void Attack()
    {
        Player player = target.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(damage); // Call Entity's TakeDamage method
        }
        else
        {
            Debug.LogWarning("Enemy attack failed: Player not found on target.");
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("Missing projectilePrefab or firePoint on enemy.");
            return;
        }

        Vector2 direction = (target.position - firePoint.position).normalized;

        // Spawn projectile as child of this enemy initially
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity, transform);
        
        // Track this projectile
        activeProjectiles.Add(projectile);

        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            enemyProjectile.Initialise(direction, damage);
        }
        else
        {
            Debug.LogWarning("Projectile prefab missing EnemyProjectile component.");
        }
    }
    
    private void CleanupDestroyedProjectiles()
    {
        // Remove null references (destroyed projectiles) from our tracking list
        activeProjectiles.RemoveAll(projectile => projectile == null);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (enemyType != EnemyType.Melee) return;

        Player player = collision.collider.GetComponent<Player>();
        if (player != null && Time.time >= lastAttackTime + attackCooldown)
        {
            player.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
    }

    protected override void OnDeath()
    {
        // Move any active projectiles to the enemies parent before this enemy is destroyed
        MoveProjectilesToParent();
        
        // Spawn XP orb instead of directly giving XP to player
        SpawnXPOrb();

        // Notify the WaveManager that this enemy was killed
        // Find a better way to reference WaveManager
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnEnemyKilled();
        }

        // Add drop effects before death, animations, ...
        Destroy(gameObject);
    }

    private void SpawnXPOrb()
    {
        if (xpOrbPrefab == null)
        {
            Debug.LogWarning($"XP Orb prefab not assigned on enemy {gameObject.name}");
            return;
        }

        // Spawn XP orb at enemy's position with slight random offset
        Vector3 spawnPosition = transform.position + new Vector3(
            Random.Range(-0.25f, 0.25f), 
            Random.Range(-0.25f, 0.25f), 
            0f
        );

        GameObject xpOrb = Instantiate(xpOrbPrefab, spawnPosition, Quaternion.identity);
        
        // Set the XP value on the orb
        XPOrb xpOrbScript = xpOrb.GetComponent<XPOrb>();
        if (xpOrbScript != null)
        {
            xpOrbScript.value = xpValue;
        }
        else
        {
            Debug.LogWarning("XP Orb prefab missing XPOrb component!");
        }
    }
    
    private void MoveProjectilesToParent()
    {
        // Find the enemies parent object (should be the same parent this enemy is under)
        Transform enemiesParent = transform.parent;

        if (enemiesParent == null)
        {
            // If no parent, try to find or create the "Enemies Parent" object
            GameObject enemiesObject = GameObject.Find("Enemies Parent");
            if (enemiesObject == null)
            {
                enemiesObject = new GameObject("Enemies Parent");
                enemiesObject.transform.position = Vector3.zero;
            }
            enemiesParent = enemiesObject.transform;
        }

        // Move all active projectiles to the enemies parent
        foreach (GameObject projectile in activeProjectiles)
        {
            if (projectile != null)
            {
                projectile.transform.SetParent(enemiesParent);
            }
        }

        // Clear the list since we're about to be destroyed
        activeProjectiles.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, projectileMinRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, projectileMaxRange);
    }
}