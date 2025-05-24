using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Game/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Character")]
    public int level = 1;
    public float experience = 0f;
    public float experienceToNextLevel = 100f;
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    
    [Header("Health")]
    public float maxHealth = 100f;
    public float healthRegen = 1f; // per second
    public float healthRegenDelay = 2f; // seconds before health starts regenerating after taking damage
    
    [Header("Combat")]
    public float meleeRange = 2f;
    public float attackDamage = 10f;
    public float attackSpeed = 1f; // attacks per second
    public float projectileSpeed = 8f;
    public float projectileRange = 10f;
    
    [Header("Abilities")]
    public float abilityCooldownReduction = 0f; // percentage
    
    // Method to create a runtime copy of stats
    public PlayerStats CreateRuntimeCopy()
    {
        PlayerStats copy = CreateInstance<PlayerStats>();
        copy.level = this.level;
        copy.experience = this.experience;
        copy.experienceToNextLevel = this.experienceToNextLevel;
        copy.moveSpeed = this.moveSpeed;
        copy.maxHealth = this.maxHealth;
        copy.healthRegen = this.healthRegen;
        copy.meleeRange = this.meleeRange;
        copy.attackDamage = this.attackDamage;
        copy.attackSpeed = this.attackSpeed;
        copy.projectileSpeed = this.projectileSpeed;
        copy.projectileRange = this.projectileRange;
        copy.abilityCooldownReduction = this.abilityCooldownReduction;
        return copy;
    }
}