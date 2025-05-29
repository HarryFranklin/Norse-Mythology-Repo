using UnityEngine;
using System.Collections;

public abstract class Entity : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f; // Default to 100 - the player's base health
    public float currentHealth;
    public bool isDead = false;
    
    [Header("Movement")]
    public float moveSpeed = 2f;
    
    [Header("Combat")]
    public float damage = 5f; // Might need to separate into melee and ranged later
    
    [Header("Status Effects")]
    public bool isStunned = false;
    protected float lastDamageTime = 0f; // Time since last damaged (for setting and checking stun duration)

    protected virtual void Start()
    {
        InitialiseEntity();
    }

    protected virtual void InitialiseEntity()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(float damageAmount, float stunDuration = 0f)
    {
        if (isDead) return;
        // later, check for invincibility frames or damage reduction here
        // if (isInvincible) return;
        
        currentHealth -= damageAmount;
        lastDamageTime = Time.time;

        if (stunDuration > 0f)
        {
            Stun(stunDuration);
        }

        OnDamageTaken(damageAmount);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealed(amount);
    }

    public virtual void Stun(float duration)
    {
        if (!isStunned)
            StartCoroutine(StunRoutine(duration));
    }

    protected virtual IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;

        float elapsed = 0f;
        float flashInterval = 0.1f;

        while (elapsed < duration)
        {
            if (sr != null)
                sr.color = Color.white;

            yield return new WaitForSeconds(flashInterval / 2f);

            if (sr != null)
                sr.color = originalColor;

            yield return new WaitForSeconds(flashInterval / 2f);

            elapsed += flashInterval;
        }

        if (sr != null)
            sr.color = originalColor;

        isStunned = false;
        OnStunEnded();
    }

    protected virtual void Die()
    {
        isDead = true;
        OnDeath();
    }

    // Virtual methods for subclasses to override
    protected virtual void OnDamageTaken(float damage) { }
    protected virtual void OnHealed(float amount) { }
    protected virtual void OnStunEnded() { }
    protected abstract void OnDeath();

    // Utility methods
    protected void MoveTowards(Vector3 targetPosition)
    {
        Vector2 direction = (targetPosition - transform.position).normalized;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    protected void MoveAwayFrom(Vector3 targetPosition)
    {
        Vector2 direction = (transform.position - targetPosition).normalized;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
    }

    protected float GetDistanceTo(Transform target)
    {
        return Vector2.Distance(transform.position, target.position);
    }
}