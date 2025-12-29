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

    [Header("Performance")]
    public float enemyScanInterval = 0.1f;

    private float lastAttackTime;
    private float lastEnemyScan;
    private Enemy closestEnemy;

    private void Start()
    {
        if (player == null)
            player = transform;
        if (playerComponent == null)
            playerComponent = GetComponent<Player>();

        weaponHolder = playerComponent.weaponHolder;
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

    private void UpdateClosestEnemy()
    {
        if (playerComponent == null || playerComponent.currentStats == null)
            return;

        Enemy potentialTarget = null;
        float closestDistSqr = float.MaxValue;
        Vector3 playerPosition = player.position;

        foreach (Enemy enemy in EnemySpawner.activeEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

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
            
        float attackRangeSqr = attackRange * attackRange;
        if (potentialTarget != null && closestDistSqr <= attackRangeSqr)
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
        if (playerComponent == null || playerComponent.currentStats == null)
            return;
            
        if (Time.time - lastAttackTime < (1f / playerComponent.currentStats.attackSpeed))
            return;

        if (closestEnemy != null)
        {
            PerformBasicAttack(closestEnemy);
            lastAttackTime = Time.time;
        }
    }

    private void PerformBasicAttack(Enemy target)
    {
        if (playerComponent.currentStats == null) return;
        
        switch (playerComponent.currentStats.attackType)
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
        if (playerComponent.currentStats.meleeWeaponPrefab == null || weaponHolder == null || target == null)
            yield break;
        
        GameObject weapon = Instantiate(playerComponent.currentStats.meleeWeaponPrefab, weaponHolder.position, Quaternion.identity, weaponHolder);

        Vector2 direction = (target.transform.position - weaponHolder.position).normalized;
        Vector2 startPos = weaponHolder.localPosition;
        Vector2 attackPos = startPos + (direction * 0.5f);
        
        weapon.transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(weapon.transform.localScale.x), weapon.transform.localScale.y, weapon.transform.localScale.z);
        
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            weapon.transform.localPosition = Vector2.Lerp(startPos, attackPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null && playerComponent != null && playerComponent.currentStats != null)
        {
            float damage = playerComponent.currentStats.attackDamage;
            float stun = doesMeleeWeaponStun ? meleeStunDuration : 0f;
            target.TakeDamage(damage, stun);
        }

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
        if (playerComponent.currentStats.projectilePrefab == null || playerComponent == null || playerComponent.currentStats == null)
            return;
        
        GameObject projectileGO = Instantiate(playerComponent.currentStats.projectilePrefab, player.position, Quaternion.identity);
        Projectile projectileScript = projectileGO.GetComponent<Projectile>();
        
        if (projectileScript == null)
            projectileScript = projectileGO.AddComponent<Projectile>();
            
        Vector2 direction = (target.transform.position - player.position).normalized;
        projectileScript.Initialise(direction, playerComponent.currentStats.projectileSpeed, 
                                  playerComponent.currentStats.projectileRange, 
                                  playerComponent.currentStats.attackDamage, returning, player);
    }
}