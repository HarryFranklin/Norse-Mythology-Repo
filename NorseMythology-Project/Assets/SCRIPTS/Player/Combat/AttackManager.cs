using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AttackManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Player playerComponent;
    public Transform weaponHolder;

    [Header("Basic Attack Settings")]
    public bool hasBasicAttack = true;
    public bool doesMeleeWeaponStun = true;
    public float meleeStunDuration = 0.2f;

    [Header("Weapon Animation")]
    public float walkCycleDuration = 0.417f; // Time for one full step cycle
    public float bobAmount = 0.02f;           // How high/low it goes
    public float bobSmoothSpeed = 10f;        // How fast it returns to center

    [Header("Performance")]
    public float enemyScanInterval = 0.1f;

    // --- State Variables ---
    private float attackCooldownTimer = 0f;
    private float lastEnemyScan;
    private Enemy closestEnemy;

    // --- Persistent Weapon Variables ---
    private GameObject activeWeapon;
    private Vector3 defaultWeaponScale;
    private Vector3 weaponRestingPos = new Vector3(0.26f, 0f, 0f); // Default "Idle"
    private bool isAttacking = false; 
    private bool isFacingRight = true; // Tracks last known direction

    private void Start()
    {
        if (player == null) player = transform;
        if (playerComponent == null) playerComponent = GetComponent<Player>();
        
        // Ensure weaponHolder is assigned from Player if missing
        if (weaponHolder == null && playerComponent != null) 
            weaponHolder = playerComponent.weaponHolder;

        SpawnPersistentWeapon();
    }

    private void SpawnPersistentWeapon()
    {
        if (playerComponent?.currentStats?.meleeWeaponPrefab != null && weaponHolder != null)
        {
            foreach (Transform child in weaponHolder) Destroy(child.gameObject);

            activeWeapon = Instantiate(playerComponent.currentStats.meleeWeaponPrefab, weaponHolder);
            
            activeWeapon.transform.localPosition = weaponRestingPos;
            activeWeapon.transform.localRotation = Quaternion.identity;
            
            defaultWeaponScale = activeWeapon.transform.localScale;
        }
    }

    private void Update()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.unscaledDeltaTime;

        // Only animate if NOT attacking
        if (activeWeapon != null && !isAttacking)
        {
            HandleWeaponAnimation();
        }

        if (hasBasicAttack && playerComponent != null && !playerComponent.isDead)
        {
            if (Time.time - lastEnemyScan >= enemyScanInterval)
            {
                UpdateClosestEnemy(); // This caused the error before (missing method)
                lastEnemyScan = Time.time;
            }

            HandleBasicAttack(); // This caused the error before (missing method)
        }
    }

    private void HandleWeaponAnimation()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f);

        // 1. Update Facing Direction ONLY if moving horizontally
        if (h < 0) isFacingRight = false;
        else if (h > 0) isFacingRight = true;

        // 2. Determine Target X & Scale based on facing direction
        Vector3 currentPos = activeWeapon.transform.localPosition;
        Vector3 currentScale = activeWeapon.transform.localScale;
        
        float targetX;
        
        if (isFacingRight)
        {
            targetX = Mathf.Abs(weaponRestingPos.x);           // Right Side
            currentScale.x = Mathf.Abs(defaultWeaponScale.x);  // Normal Scale
        }
        else
        {
            targetX = -Mathf.Abs(weaponRestingPos.x);          // Left Side
            currentScale.x = -Mathf.Abs(defaultWeaponScale.x); // Flipped Scale
        }

        // 3. Bobbing Logic (Y Axis)
        // We only change Y if moving. If idle, return to resting Y (0).
        float targetY = weaponRestingPos.y; 

        if (isMoving)
        {
            float frequency = (2f * Mathf.PI) / walkCycleDuration;
            float bobOffset = Mathf.Sin(Time.time * frequency) * bobAmount;
            targetY += bobOffset;
        }

        // 4. Apply
        // Snap X and Scale immediately for responsive turning
        currentPos.x = targetX;
        activeWeapon.transform.localScale = currentScale;

        // Smoothly Lerp Y for the bobbing effect
        currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * bobSmoothSpeed);
        
        activeWeapon.transform.localPosition = currentPos;
    }

    private void UpdateClosestEnemy()
    {
        if (playerComponent == null || playerComponent.currentStats == null)
            return;

        Enemy potentialTarget = null;
        float closestDistSqr = float.MaxValue;
        Vector3 playerPosition = player.position;

        // Ensure EnemySpawner.activeEnemies is public static or accessible
        foreach (Enemy enemy in EnemySpawner.activeEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            float distSqr = (enemy.transform.position - playerPosition).sqrMagnitude;

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                potentialTarget = enemy;
            }
        }

        float attackRange = (playerComponent.currentStats.attackType == AttackType.Melee)
            ? playerComponent.currentStats.meleeRange
            : playerComponent.currentStats.projectileRange;

        if (potentialTarget != null && closestDistSqr <= (attackRange * attackRange))
        {
            closestEnemy = potentialTarget;
        }
        else
        {
            closestEnemy = null;
        }
    }

    private void HandleBasicAttack()
    {
        if (playerComponent == null || playerComponent.currentStats == null) return;
        
        // Don't attack if on cooldown
        if (attackCooldownTimer > 0f) return;

        if (closestEnemy != null)
        {
            PerformBasicAttack(closestEnemy);

            float attackSpeed = playerComponent.currentStats.attackSpeed;
            if (attackSpeed > 0)
            {
                attackCooldownTimer = 1f / attackSpeed;
            }
        }
    }

    private void PerformBasicAttack(Enemy target)
    {
        if (playerComponent.currentStats == null) return;

        switch (playerComponent.currentStats.attackType)
        {
            case AttackType.Melee:
                if (activeWeapon != null)
                {
                    StartCoroutine(MeleeAttack(target));
                }
                break;
            case AttackType.Projectile:
                LaunchProjectile(target, false);
                break;
            case AttackType.ReturningProjectile:
                LaunchProjectile(target, true);
                break;
        }
    }

    private IEnumerator MeleeAttack(Enemy target)
    {
        isAttacking = true;

        Vector2 direction = (target.transform.position - weaponHolder.position).normalized;
        Vector2 startPos = activeWeapon.transform.localPosition;
        
        Vector3 worldStartPos = activeWeapon.transform.position;
        Vector3 worldEndPos = worldStartPos + (Vector3)(direction * 0.5f);
        Vector3 localEndPos = weaponHolder.InverseTransformPoint(worldEndPos);

        // Visual Flip for attack
        if (direction.x < 0)
        {
            Vector3 s = activeWeapon.transform.localScale;
            s.x = -Mathf.Abs(defaultWeaponScale.x);
            activeWeapon.transform.localScale = s;
        }
        else
        {
            Vector3 s = activeWeapon.transform.localScale;
            s.x = Mathf.Abs(defaultWeaponScale.x);
            activeWeapon.transform.localScale = s;
        }

        float duration = 0.1f;
        float elapsed = 0f;

        // Thrust
        while (elapsed < duration)
        {
            if (activeWeapon == null) yield break;
            activeWeapon.transform.localPosition = Vector2.Lerp(startPos, localEndPos, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Damage
        if (target != null && playerComponent != null && playerComponent.currentStats != null)
        {
            float damage = playerComponent.currentStats.attackDamage;
            float stun = doesMeleeWeaponStun ? meleeStunDuration : 0f;
            target.TakeDamage(damage, stun);
        }

        // Return
        elapsed = 0f;
        while (elapsed < duration)
        {
            if (activeWeapon == null) yield break;
            activeWeapon.transform.localPosition = Vector2.Lerp(localEndPos, startPos, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        if (activeWeapon != null) activeWeapon.transform.localPosition = startPos;

        isAttacking = false;
    }

    private void LaunchProjectile(Enemy target, bool returning)
    {
        if (playerComponent.currentStats.projectilePrefab == null) return;

        GameObject projectileGO = Instantiate(playerComponent.currentStats.projectilePrefab, player.position, Quaternion.identity);
        Projectile projectileScript = projectileGO.GetComponent<Projectile>();

        if (projectileScript == null) projectileScript = projectileGO.AddComponent<Projectile>();

        Vector2 direction = (target.transform.position - player.position).normalized;
        projectileScript.Initialise(direction, playerComponent.currentStats.projectileSpeed,
                                    playerComponent.currentStats.projectileRange,
                                    playerComponent.currentStats.attackDamage, returning, player);
    }
}