using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerStats baseStats; // ScriptableObject reference
    
    [Header("Runtime Stats")]
    public PlayerStats currentStats; // Runtime copy
    public float currentHealth;
    public bool isDead = false;
    
    private void Start()
    {
        InitialisePlayer();
    }
    
    private void InitialisePlayer()
    {
        // Create runtime copy of base stats
        currentStats = baseStats.CreateRuntimeCopy();
        currentHealth = currentStats.maxHealth;
        StartCoroutine(HealthRegeneration());
    }
    
    private System.Collections.IEnumerator HealthRegeneration()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(1f);
            if (currentHealth < currentStats.maxHealth)
            {
                currentHealth = Mathf.Min(currentStats.maxHealth, currentHealth + currentStats.healthRegen);
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentStats.maxHealth, currentHealth + amount);
    }
    
    public void GainExperience(float xp)
    {
        if (isDead) return;
        
        currentStats.experience += xp;
        CheckLevelUp();
    }
    
    private void CheckLevelUp()
    {
        while (currentStats.experience >= currentStats.experienceToNextLevel)
        {
            LevelUp();
        }
    }
    
    private void LevelUp()
    {
        currentStats.experience -= currentStats.experienceToNextLevel;
        currentStats.level++;
        
        // Increase stats on level up (customise as needed)
        currentStats.maxHealth += 10f;
        currentStats.attackDamage += 2f;
        currentStats.experienceToNextLevel = Mathf.Floor(currentStats.experienceToNextLevel * 1.2f);
        
        // Heal to full on level up
        currentHealth = currentStats.maxHealth;
        
        Debug.Log($"Level Up! Now level {currentStats.level}");
    }
    
    private void Die()
    {
        isDead = true;
        Debug.Log("Player died!");
        // Handle death logic (restart level, game over screen, etc.)
    }
    
    // Method to save current stats back to base stats (for persistence between levels)
    public void SaveStatsToBase()
    {
        baseStats.level = currentStats.level;
        baseStats.experience = currentStats.experience;
        baseStats.experienceToNextLevel = currentStats.experienceToNextLevel;
        baseStats.moveSpeed = currentStats.moveSpeed;
        baseStats.maxHealth = currentStats.maxHealth;
        baseStats.healthRegen = currentStats.healthRegen;
        baseStats.meleeRange = currentStats.meleeRange;
        baseStats.attackDamage = currentStats.attackDamage;
        baseStats.attackSpeed = currentStats.attackSpeed;
        baseStats.projectileSpeed = currentStats.projectileSpeed;
        baseStats.projectileRange = currentStats.projectileRange;
        baseStats.abilityCooldownReduction = currentStats.abilityCooldownReduction;
    }
}