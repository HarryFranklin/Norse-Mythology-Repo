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
    public Transform weaponHolder; // Empty GameObject child of player for weapon positioning
    
    [Header("Basic Attack Settings")]
    public bool hasBasicAttack = true;
    public AttackType basicAttackType = AttackType.Melee;
    public bool doesMeleeWeaponStun = true; // Whether the melee weapon can stun enemies
    public float meleeStunDuration = 0.2f; // Duration of stun effect from melee weapon
    
    [Header("Performance")]
    public float enemyScanInterval = 0.1f; // How often to scan for enemies (in seconds)
    
    private float lastAttackTime;
    private float lastEnemyScan;
    private Enemy closestEnemy;
    private float closestEnemyDistance;
    
    public enum AttackType
    {
        Melee,
        Projectile,
        ReturningProjectile
    }
    
    private void Start()
    {
        if (player == null)
            player = GetComponent<Transform>();
        if (playerComponent == null)
            playerComponent = GetComponent<Player>();
    }
    
    private void Update()
    {
        if (hasBasicAttack && playerComponent != null && !playerComponent.isDead)
        {
            // Only scan for enemies at intervals for performance
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
        // Add null check for playerComponent and currentStats
        if (playerComponent == null || playerComponent.currentStats == null)
            return;
            
        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        
        closestEnemy = null;
        closestEnemyDistance = playerComponent.currentStats.meleeRange;
        
        foreach (GameObject enemyObject in enemyObjects)
        {
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            if (enemy == null) continue;
            
            float distance = Vector2.Distance(player.position, enemy.transform.position);
            if (distance <= playerComponent.currentStats.meleeRange && distance < closestEnemyDistance)
            {
                closestEnemy = enemy;
                closestEnemyDistance = distance;
            }
        }
    }
    
    private void HandleBasicAttack()
    {
        // Add null check for playerComponent and currentStats
        if (playerComponent == null || playerComponent.currentStats == null)
            return;
            
        if (Time.time - lastAttackTime >= 1f / playerComponent.currentStats.attackSpeed)
        {
            if (closestEnemy != null && IsEnemyStillValid())
            {
                PerformBasicAttack(closestEnemy);
                lastAttackTime = Time.time;
            }
        }
    }
    
    private bool IsEnemyStillValid()
    {
        if (closestEnemy == null || playerComponent == null || playerComponent.currentStats == null) 
            return false;
        
        // Quick distance check without recalculating all enemies
        float currentDistance = Vector2.Distance(player.position, closestEnemy.transform.position);
        return currentDistance <= playerComponent.currentStats.meleeRange;
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
    
    private System.Collections.IEnumerator MeleeAttack(Enemy target)
    {
        if (meleeWeaponPrefab != null && weaponHolder != null)
        {
            GameObject weapon = Instantiate(meleeWeaponPrefab, weaponHolder.position, Quaternion.identity, weaponHolder);
            
            Vector2 direction = (target.transform.position - weaponHolder.position).normalized;
            Vector2 startPos = weaponHolder.localPosition;
            Vector2 attackPos = startPos + direction * 0.5f;

            Vector3 scale = weapon.transform.localScale; // Consider re-writing this with a flip function

            if (direction.x < 0)
            {
                // Flip weapon for left direction
                scale.x *= -1;
            }
            else
            {
                // Reset scale for right direction
                scale.x = Mathf.Abs(scale.x);    
            }

            weapon.transform.localScale = scale;
            
            // Move weapon towards enemy
            float duration = 0.2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                weapon.transform.localPosition = Vector2.Lerp(startPos, attackPos, t);
                yield return null;
            }
            
            // Deal damage + optional stun
            if (target != null && playerComponent != null && playerComponent.currentStats != null)
            {
                float damage = playerComponent.currentStats.attackDamage;
                float stun = doesMeleeWeaponStun ? meleeStunDuration : 0f;
                target.TakeDamage(damage, stun);
            }
            
            // Return weapon
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                weapon.transform.localPosition = Vector2.Lerp(attackPos, startPos, t);
                yield return null;
            }
            
            Destroy(weapon);
        }
        else
        {
            // Direct damage if no weapon prefab
            if (target != null && playerComponent != null && playerComponent.currentStats != null)
            {
                target.TakeDamage(playerComponent.currentStats.attackDamage);
            }
        }
    }
    
    private void LaunchProjectile(Enemy target, bool returning)
    {
        if (projectilePrefab != null && playerComponent != null && playerComponent.currentStats != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, player.position, Quaternion.identity);
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            
            if (projectileScript == null)
                projectileScript = projectile.AddComponent<Projectile>();
                
            Vector2 direction = (target.transform.position - player.position).normalized;
            projectileScript.Initialise(direction, playerComponent.currentStats.projectileSpeed, 
                                      playerComponent.currentStats.projectileRange, 
                                      playerComponent.currentStats.attackDamage, returning, player);
        }
    }
    
    public void ActivateAbility(int abilityIndex)
    {
        // This will be expanded with specific abilities
        Debug.Log($"Ability {abilityIndex} activated!");
    }
}