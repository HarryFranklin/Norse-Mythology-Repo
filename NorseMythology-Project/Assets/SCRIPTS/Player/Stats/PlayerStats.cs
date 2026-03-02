using UnityEngine;

// 1. We define the Enum here so it is shared globally
public enum AttackType
{
    Melee,
    Projectile,
    ReturningProjectile
}

[System.Serializable]
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Game/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Core Character")]
    public int level = 1;
    public float experience = 0f;
    public float experienceToNextLevel = 100f;
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    
    [Header("Health")]
    public float maxHealth = 100f;
    public float healthRegen = 1f; 
    public float healthRegenDelay = 2f; 
    
    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackSpeed = 1f; 
    
    [Header("Range")]
    public float meleeRange = 1.5f;     
    public float projectileRange = 8f; 
    
    [Header("Projectiles")]
    public float projectileSpeed = 10f; 

    [Header("Abilities")]
    public float abilityCooldownReduction = 0f;

    [Header("Class Settings")]
    public AttackType attackType = AttackType.Melee;
    public GameObject meleeWeaponPrefab;
    public GameObject projectilePrefab;

    // Creates a clean copy so we don't overwrite the asset file during gameplay
    public PlayerStats CreateRuntimeCopy()
    {
        PlayerStats copy = Instantiate(this);
        return copy;
    }
}