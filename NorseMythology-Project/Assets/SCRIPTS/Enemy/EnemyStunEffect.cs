using UnityEngine;
using System.Collections;

public class EnemyStunEffect : MonoBehaviour
{
    private Enemy enemy;
    private bool isStunned = false;
    private float stunEndTime;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;
    
    // Store original enemy behavior state
    private float originalMoveSpeed;
    private float originalAttackCooldown;
    private bool wasStunnedBefore = false;
    
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (enemy != null)
        {
            originalMoveSpeed = enemy.moveSpeed;
            originalAttackCooldown = enemy.attackCooldown;
        }
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    private void Update()
    {
        if (isStunned && Time.time >= stunEndTime)
        {
            RemoveStun();
        }
    }
    
    public void ApplyStun(float duration)
    {
        if (enemy == null) return;
        
        // If already stunned, extend the duration if new duration is longer
        if (isStunned)
        {
            float remainingTime = stunEndTime - Time.time;
            if (duration > remainingTime)
            {
                stunEndTime = Time.time + duration;
            }
            return;
        }
        
        isStunned = true;
        stunEndTime = Time.time + duration;
        wasStunnedBefore = true;
        
        // Disable enemy movement and attacks
        enemy.moveSpeed = 0f;
        enemy.attackCooldown = float.MaxValue; // Prevent attacks by setting cooldown to max
        
        // Visual feedback - tint the enemy
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.yellow; // Yellow tint for stunned state
        }
        
        // Start stun visual effects
        StartCoroutine(StunVisualEffect());
        
        Debug.Log($"{enemy.name} is now stunned for {duration} seconds");
    }
    
    private void RemoveStun()
    {
        if (!isStunned || enemy == null) return;
        
        isStunned = false;
        
        // Restore original enemy behavior
        enemy.moveSpeed = originalMoveSpeed;
        enemy.attackCooldown = originalAttackCooldown;
        
        // Restore original color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        Debug.Log($"{enemy.name} is no longer stunned");
        
        // Remove this component after a short delay to avoid issues
        StartCoroutine(RemoveComponentAfterDelay());
    }
    
    private IEnumerator StunVisualEffect()
    {
        // Create a pulsing effect while stunned
        float pulseSpeed = 3f;
        Color stunColor = Color.yellow;
        
        while (isStunned && spriteRenderer != null)
        {
            float alpha = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            Color currentColor = Color.Lerp(originalColor, stunColor, alpha);
            spriteRenderer.color = currentColor;
            yield return null;
        }
    }
    
    private IEnumerator RemoveComponentAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        
        // Only destroy this component if we're not stunned anymore
        if (!isStunned)
        {
            Destroy(this);
        }
    }
    
    public bool IsStunned()
    {
        return isStunned;
    }
    
    public float GetRemainingStunTime()
    {
        if (!isStunned) return 0f;
        return Mathf.Max(0f, stunEndTime - Time.time);
    }
    
    private void OnDestroy()
    {
        // Ensure we restore the enemy state if this component is destroyed while stunned
        if (isStunned && enemy != null)
        {
            enemy.moveSpeed = originalMoveSpeed;
            enemy.attackCooldown = originalAttackCooldown;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }
}