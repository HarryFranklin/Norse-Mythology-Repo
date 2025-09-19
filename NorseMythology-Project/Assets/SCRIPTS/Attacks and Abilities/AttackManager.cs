using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AttackManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Player playerComponent;
    public GameObject meleeWeaponPrefab;
    public GameObject projectilePrefab;
    public Transform weaponHolder;

    [Header("Basic Attack Settings")]
    public bool hasBasicAttack = true;
    public AttackType basicAttackType = AttackType.Melee;
    public bool doesMeleeWeaponStun = true;
    public float meleeStunDuration = 0.2f;

    [Header("Performance")]
    public float enemyScanInterval = 0.1f;

    private float lastAttackTime;
    private float lastEnemyScan;
    private Enemy closestEnemy;

    public enum AttackType
    {
        Melee,
        Projectile,
        ReturningProjectile
    }

    private void Start()
    {
        if (player == null)
            player = transform;
        if (playerComponent == null)
            playerComponent = GetComponent<Player>();
    }

    private void Update()
    {
        if (hasBasicAttack && playerComponent != null && !playerComponent.isDead)
        {
            if (Time.time - lastEnemyScan >= enemyScanInterval)
            {
                UpdateClosestEnemy();
                lastEnemyScan = Time.time;
            }

            HandleBasicAttack();
        }
    }

    /// <summary>
    /// Finds the closest active enemy and checks if it's within melee range.
    /// This method is now more robust and efficient.
    /// </summary>
    private void UpdateClosestEnemy()
    {
        if (playerComponent == null || playerComponent.currentStats == null)
            return;

        Enemy potentialTarget = null;
        float closestDistSqr = float.MaxValue;
        Vector3 playerPosition = player.position;

        // Iterate through the static list of active enemies from the spawner
        foreach (Enemy enemy in EnemySpawner.activeEnemies)
        {
            // Skip any enemies that might be null or inactive in the list
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            // Using sqrMagnitude is faster than Vector3.Distance as it avoids a square root calculation
            float distSqr = (enemy.transform.position - playerPosition).sqrMagnitude;

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                potentialTarget = enemy;
            }
        }

        // After finding the closest enemy, check if it's within attack range.
        float meleeRangeSqr = playerComponent.currentStats.meleeRange * playerComponent.currentStats.meleeRange;
        if (potentialTarget != null && closestDistSqr <= meleeRangeSqr)
        {
            // We have a valid target in range
            closestEnemy = potentialTarget;
        }
        else
        {
            // No enemies in range, so clear the target
            closestEnemy = null;
        }
    }

    private void HandleBasicAttack()
    {
        if (playerComponent == null || playerComponent.currentStats == null)
            return;
            
        // Check attack cooldown
        if (Time.time - lastAttackTime < (1f / playerComponent.currentStats.attackSpeed))
            return;

        // Only attack if we have a valid target
        if (closestEnemy != null)
        {
            PerformBasicAttack(closestEnemy);
            lastAttackTime = Time.time;
        }
    }

    private void PerformBasicAttack(Enemy target)
    {
        switch (basicAttackType)
        {
            case AttackType.Melee:
                StartCoroutine(MeleeAttack(target));
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
        if (meleeWeaponPrefab == null || weaponHolder == null || target == null)
            yield break;
        
        GameObject weapon = Instantiate(meleeWeaponPrefab, weaponHolder.position, Quaternion.identity, weaponHolder);

        Vector2 direction = (target.transform.position - weaponHolder.position).normalized;
        Vector2 startPos = weaponHolder.localPosition;
        Vector2 attackPos = startPos + (direction * 0.5f);
        
        // Flip weapon sprite based on direction
        weapon.transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(weapon.transform.localScale.x), weapon.transform.localScale.y, weapon.transform.localScale.z);
        
        float duration = 0.1f; // Faster swing
        float elapsed = 0f;

        // Swing out
        while (elapsed < duration)
        {
            weapon.transform.localPosition = Vector2.Lerp(startPos, attackPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Deal damage (check if target is still valid)
        if (target != null && playerComponent != null && playerComponent.currentStats != null)
        {
            float damage = playerComponent.currentStats.attackDamage;
            float stun = doesMeleeWeaponStun ? meleeStunDuration : 0f;
            target.TakeDamage(damage, stun);
        }

        // Swing back
        elapsed = 0f;
        while (elapsed < duration)
        {
            weapon.transform.localPosition = Vector2.Lerp(attackPos, startPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(weapon);
    }

    private void LaunchProjectile(Enemy target, bool returning)
    {
        if (projectilePrefab == null || playerComponent == null || playerComponent.currentStats == null)
            return;
        
        GameObject projectileGO = Instantiate(projectilePrefab, player.position, Quaternion.identity);
        Projectile projectileScript = projectileGO.GetComponent<Projectile>();
        
        if (projectileScript == null)
            projectileScript = projectileGO.AddComponent<Projectile>();
            
        Vector2 direction = (target.transform.position - player.position).normalized;
        projectileScript.Initialise(direction, playerComponent.currentStats.projectileSpeed, 
                                  playerComponent.currentStats.projectileRange, 
                                  playerComponent.currentStats.attackDamage, returning, player);
    }
}
