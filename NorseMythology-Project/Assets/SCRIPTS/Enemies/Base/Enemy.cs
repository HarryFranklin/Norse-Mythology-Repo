using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class EnemyLevelData
{
    [Header("Level Info")]
    public int level = 1;

    [Header("Health")]
    public float maxHealth = 50f;

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Combat")]
    public float attackDamage = 5f;
    public float attackCooldown = 1f;
    public float meleeAttackRange = 0.5f;
    public float projectileMinRange = 2f;
    public float projectileMaxRange = 6f;

    [Header("Rewards")]
    public float xpValue = 10f;
}

[System.Serializable]
public class EnemyTypeModifiers
{
    public float healthMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float attackCooldownMultiplier = 1f;
    public float attackRangeMultiplier = 1f;
}

public class Enemy : Entity
{
    public enum EnemyType { Melee, Projectile }

    public EnemySpawner spawner;
    
    [Header("Enemy Type")]
    public EnemyType enemyType = EnemyType.Melee;
    public EnemyTypeModifiers meleeModifiers = new EnemyTypeModifiers
    {
        healthMultiplier = 1.5f,
        moveSpeedMultiplier = 0.7f,
        damageMultiplier = 1.5f,
        attackCooldownMultiplier = 1.2f
    };
    public EnemyTypeModifiers projectileModifiers = new EnemyTypeModifiers
    {
        healthMultiplier = 1f,
        moveSpeedMultiplier = 1f,
        damageMultiplier = 1f,
        attackCooldownMultiplier = 1f
    };

    [Header("Enemy XP & Level")]
    public GameObject xpOrbPrefab;

    [Header("Level Configuration")]
    public List<EnemyLevelData> levelData = new List<EnemyLevelData>();
    
    protected Dictionary<int, EnemyLevelData> codeLevelData;

    [Header("Combat Settings")]
    private float attackCooldown = 1f;
    private float meleeAttackRange = 0.5f;
    private float projectileMinRange = 2f;
    private float projectileMaxRange = 6f;
    [HideInInspector] public float xpValue = 10f;
    private float lastAttackTime;

    [Header("Targeting")]
    public Transform target;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    private List<GameObject> activeProjectiles = new List<GameObject>();

    // --- NEW ANIMATION VARIABLES ---
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Time to wait after death before disabling object (should match death anim length)")]
    [SerializeField] private float deathAnimationDuration = 1f;

    private Vector3 lastPosition;

    protected override void Awake()
    {
        codeLevelData = new Dictionary<int, EnemyLevelData>();
        base.Awake();

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnObjectSpawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        // Re-enable collider if it was disabled during death
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        
        // Reset animation state
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (spawner == null)
        {
            spawner = FindFirstObjectByType<EnemySpawner>();
        }
        
        lastPosition = transform.position;
    }

    protected override void InitialiseFromCodeMatrix()
    {
        float baseHealth = 5f;
        float baseSpeed = 2f;
        float baseDamage = 4f;
        float baseCooldown = 1f;
        float baseMeleeRange = 0.5f;
        float baseProjMinRange = 2f;
        float baseProjMaxRange = 6f;
        float baseXP = 8f;

        for (int level = 1; level <= 10; level++)
        {
            float levelScale = 1 + (level - 1) * 0.2f;

            SetEnemyLevelData(
                level,
                maxHealth: baseHealth * levelScale,
                moveSpeed: baseSpeed * levelScale,
                attackDamage: baseDamage * levelScale,
                attackCooldown: Mathf.Max(0.5f, baseCooldown * (1 / levelScale)),
                meleeAttackRange: baseMeleeRange * levelScale,
                projectileMinRange: baseProjMinRange * levelScale,
                projectileMaxRange: baseProjMaxRange * levelScale,
                xpValue: baseXP * levelScale
            );
        }
    }
    
    protected void SetEnemyLevelData(int level, float maxHealth, float moveSpeed, float attackDamage, 
        float attackCooldown, float meleeAttackRange, float projectileMinRange, float projectileMaxRange, float xpValue)
    {
        EnemyLevelData data = new EnemyLevelData
        {
            level = level,
            maxHealth = maxHealth,
            moveSpeed = moveSpeed,
            attackDamage = attackDamage,
            attackCooldown = attackCooldown,
            meleeAttackRange = meleeAttackRange,
            projectileMinRange = projectileMinRange,
            projectileMaxRange = projectileMaxRange,
            xpValue = xpValue
        };
        
        codeLevelData[level] = data;
    }
    
    protected override void ApplyLevelStats(int level)
    {
        EnemyLevelData data = GetEnemyLevelData(level);
        if (data == null)
        {
            Debug.LogWarning($"No enemy level data found for level {level}");
            return;
        }
        
        EnemyTypeModifiers modifiers = enemyType == EnemyType.Melee ? meleeModifiers : projectileModifiers;

        maxHealth = data.maxHealth * modifiers.healthMultiplier;
        moveSpeed = data.moveSpeed * modifiers.moveSpeedMultiplier;
        damage = data.attackDamage * modifiers.damageMultiplier;
        attackCooldown = data.attackCooldown * modifiers.attackCooldownMultiplier;
        meleeAttackRange = data.meleeAttackRange * modifiers.attackRangeMultiplier;
        projectileMinRange = data.projectileMinRange * modifiers.attackRangeMultiplier;
        projectileMaxRange = data.projectileMaxRange * modifiers.attackRangeMultiplier;
        xpValue = data.xpValue;
    }

    protected EnemyLevelData GetEnemyLevelData(int level)
    {
        if (useInspectorLevels && levelData.Count > 0)
        {
            EnemyLevelData fallback = null;
            foreach (var data in levelData)
            {
                if (data.level == level) return data;
                if (data.level <= level && (fallback == null || data.level > fallback.level))
                    fallback = data;
            }
            return fallback ?? levelData[levelData.Count - 1];
        }
        else
        {
            if (codeLevelData != null && codeLevelData.ContainsKey(level))
                return codeLevelData[level];
                
            EnemyLevelData fallback = null;
            if(codeLevelData != null)
            {
                foreach (var kvp in codeLevelData)
                {
                    if (kvp.Key <= level && (fallback == null || kvp.Key > fallback.level))
                        fallback = kvp.Value;
                }
            }
            return fallback;
        }
    }

    protected override void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
        lastPosition = transform.position;
    }

    private void Update()
    {
        // Don't update behavior if dead
        if (isDead || isStunned || isFrozen || target == null) 
        {
            // If dead or stunned, we are not moving
            if (animator != null) animator.SetBool("IsMoving", false);
            return;
        }

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
        
        UpdateAnimationState();
        CleanupDestroyedProjectiles();
    }

    private void UpdateAnimationState()
    {
        // Calculate movement
        float movementThreshold = 0.001f;
        Vector3 displacement = transform.position - lastPosition;
        bool isMoving = displacement.sqrMagnitude > movementThreshold;

        // Update Animator
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }

        // Flip Sprite based on X direction
        if (spriteRenderer != null && Mathf.Abs(displacement.x) > movementThreshold)
        {
            // If moving left (negative x), flip. If moving right (positive x), don't flip.
            spriteRenderer.flipX = displacement.x < 0;
        }

        lastPosition = transform.position;
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
        if (animator != null) animator.SetTrigger("Attack");

        Player player = target.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        
        if (animator != null) animator.SetTrigger("Attack");

        Vector2 direction = (target.position - firePoint.position).normalized;
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity, transform);
        activeProjectiles.Add(projectile);

        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            enemyProjectile.Initialise(direction, damage);
        }
    }
    
    private void CleanupDestroyedProjectiles()
    {
        activeProjectiles.RemoveAll(projectile => projectile == null);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (enemyType != EnemyType.Melee) return;

        Player player = collision.collider.GetComponent<Player>();
        if (player != null && Time.time >= lastAttackTime + attackCooldown)
        {
            // Trigger attack animation even on collision hit
            if (animator != null) animator.SetTrigger("Attack");
            
            player.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
    }
    
    public new void SetLevel(int level)
    {
        level = Mathf.Max(1, level);
        currentLevel = level;
        ApplyLevelStats(level);
        currentHealth = maxHealth;
    }

    protected override void OnDeath()
    {
        // Tell spawner immediately so count is updated
        spawner?.EnemyDied(this);
        SpawnXPOrb();

        // Start the death sequence
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 1. Play animation
        if (animator != null) animator.SetTrigger("Die");

        // 2. Disable collider so player can't hit dead body
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 3. Disable Rigidbody physics to stop sliding
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        // 4. Wait for animation to finish
        yield return new WaitForSeconds(deathAnimationDuration);

        // 5. Restore Rigidbody (important for object pooling re-use)
        if (rb != null) rb.simulated = true;

        // 6. "Destroy" (return to pool)
        gameObject.SetActive(false);
    }

    private void SpawnXPOrb()
    {
        if (xpOrbPrefab == null) return;

        Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0f);
        GameObject xpOrb = Instantiate(xpOrbPrefab, spawnPosition, Quaternion.identity);
        
        XPOrb xpOrbScript = xpOrb.GetComponent<XPOrb>();
        if (xpOrbScript != null)
        {
            xpOrbScript.value = xpValue;
        }
    }
    
    private void MoveProjectilesToParent()
    {
        Transform enemiesParent = transform.parent;
        if (enemiesParent == null)
        {
            GameObject enemiesObject = GameObject.Find("Enemies Parent");
            if (enemiesObject == null)
            {
                enemiesObject = new GameObject("Enemies Parent");
            }
            enemiesParent = enemiesObject.transform;
        }

        foreach (GameObject projectile in activeProjectiles)
        {
            if (projectile != null)
            {
                projectile.transform.SetParent(enemiesParent);
            }
        }
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