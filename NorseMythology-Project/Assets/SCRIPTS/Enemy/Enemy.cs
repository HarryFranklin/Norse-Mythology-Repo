using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public enum EnemyType { Melee, Projectile }
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Enemy Stats")]
    public float maxHealth = 30f;
    public float currentHealth;
    public float damage = 5f;

    [Header("XP")]
    public float xpValue = 10f; // XP value for the player when this enemy is defeated
    public int level = 1; // Level of the enemy, can be used for scaling difficulty

    [Header("Movement & Combat")]
    public float moveSpeed = 2f;
    public float meleeAttackRange = 0.5f;
    public float projectileMinRange = 2f;
    public float projectileMaxRange = 6f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    public bool isStunned = false;
    public float stunDuration = 2f; // Duration of stun effect

    [Header("Targeting")]
    public Transform target;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint; // A child object indicating where to shoot from
    
    // Track active projectiles spawned by this enemy
    private List<GameObject> activeProjectiles = new List<GameObject>();

    private void Start()
    {
        currentHealth = maxHealth;

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
    }

    private void Update()
    {
        if (isStunned || target == null) return; // Skip update if stunned or no target

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

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
            MoveTowardsTarget();
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
            MoveTowardsTarget();
        }
        else if (distance < projectileMinRange)
        {
            MoveAwayFromTarget();
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            ShootProjectile();
            lastAttackTime = Time.time;
        }
    }

    private void MoveTowardsTarget()
    {
        Vector2 direction = (target.position - transform.position).normalized;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    private void MoveAwayFromTarget()
    {
        Vector2 direction = (transform.position - target.position).normalized;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    private void Attack()
    {
        PlayerController playerController = target.GetComponent<PlayerController>(); // Get playerController from target
        if (playerController != null)
        {
            playerController.TakeDamage(damage); // Call its takeDamage method
        }
        else
        {
            Debug.LogWarning("Enemy attack failed: PlayerController not found on target.");
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

    public void TakeDamage(float damage, float stunDuration = 0f)
    {
        currentHealth -= damage;

        if (stunDuration > 0f)
        {
            Stun(stunDuration);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (enemyType != EnemyType.Melee) return;

        PlayerController player = collision.collider.GetComponent<PlayerController>();
        if (player != null && Time.time >= lastAttackTime + attackCooldown)
        {
            player.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
    }

    private void Die()
    {
        // Move any active projectiles to the enemies parent before this enemy is destroyed
        MoveProjectilesToParent();
        
        // Award XP to the player before destroying the enemy
        if (target != null)
        {
            PlayerController playerController = target.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.GainExperience(xpValue);
                Debug.Log($"Player gained {xpValue} XP from defeating {gameObject.name}");
            }
            else
            {
                Debug.LogWarning("Could not award XP: PlayerController not found on target.");
            }
        }

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

    public void Stun(float duration)
    {
        if (!isStunned)
            StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator<WaitForSeconds> StunRoutine(float duration)
    {
        isStunned = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;

        float elapsed = 0f;
        float flashInterval = 0.1f;

        while (elapsed < duration)
        {
            if (sr != null)
                sr.color = Color.white;

            yield return new WaitForSeconds(flashInterval / 2f);

            if (sr != null)
                sr.color = originalColor;

            yield return new WaitForSeconds(flashInterval / 2f);

            elapsed += flashInterval;
        }

        if (sr != null)
            sr.color = originalColor;

        isStunned = false;
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