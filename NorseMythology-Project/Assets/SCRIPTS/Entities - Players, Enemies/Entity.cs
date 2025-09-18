using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class Entity : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead = false;

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Combat")]
    public float damage = 5f;

    [Header("Status Effects")]
    public bool isStunned = false;
    public bool isFrozen = false;
    public bool isInvincible = false;
    protected float lastDamageTime = 0f;

    [Header("Level System")]
    public bool useInspectorLevels = false; 
    [SerializeField] protected int currentLevel = 1;

    protected virtual void Awake()
    {
        // Awake() is called immediately when an object is instantiated,
        // ensuring data is ready before other scripts can access it.
        InitialiseEntity();
    }

    protected virtual void Start()
    {
        // Start can be used for logic that depends on other objects being ready.
        // The core initialization is now handled in Awake().
    }

    protected virtual void InitialiseEntity()
    {
        if (!useInspectorLevels)
        {
            InitialiseFromCodeMatrix();
        }

        ApplyLevelStats(currentLevel);
        currentHealth = maxHealth;
    }

    protected virtual void InitialiseFromCodeMatrix()
    {
        // Base implementation - subclasses should override
    }

    public virtual void SetLevel(int newLevel)
    {
        currentLevel = Mathf.Max(1, newLevel);
        ApplyLevelStats(currentLevel);
    }

    protected abstract void ApplyLevelStats(int level);

    public virtual void TakeDamage(float damageAmount, float stunDuration = 0f)
    {
        if (isDead || isInvincible) return;

        currentHealth -= damageAmount;
        lastDamageTime = Time.time;

        PopupManager.Instance?.ShowDamage(damageAmount, transform.position);

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

        float healedAmount = Mathf.Min(maxHealth - currentHealth, amount);
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (healedAmount > 0)
        {
            PopupManager.Instance?.ShowHeal(healedAmount, transform.position);
        }

        OnHealed(amount);
    }

    public virtual void Stun(float duration)
    {
        if (!isStunned)
            StartCoroutine(StunRoutine(duration));
    }

    public virtual void Freeze(float duration)
    {
        if (!isFrozen)
            StartCoroutine(FreezeRoutine(duration));
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

    protected virtual IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;

        Color frozenColor = new Color(0.7f, 0.9f, 1f, originalColor.a);
        if (sr != null)
            sr.color = frozenColor;

        float immobilizeDuration = duration * 0.8f;
        yield return new WaitForSeconds(immobilizeDuration);

        isFrozen = false;
        OnFreezeEnded();

        float remainingDuration = duration * 0.2f;
        yield return new WaitForSeconds(remainingDuration);

        if (sr != null)
            sr.color = originalColor;
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
    protected virtual void OnFreezeEnded() { }
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

    // Getter methods
    public int GetCurrentLevel() => currentLevel;
}