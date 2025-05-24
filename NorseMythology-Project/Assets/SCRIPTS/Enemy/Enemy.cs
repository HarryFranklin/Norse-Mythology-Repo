using UnityEngine;

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

    [Header("Targeting")]
    public Transform target;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint; // A child object indicating where to shoot from

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
        if (target == null) return;

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

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

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

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
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

        // Add death effects, drops, etc.
        Destroy(gameObject);
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