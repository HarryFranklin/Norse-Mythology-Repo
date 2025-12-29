using UnityEngine;
using System.Collections;

public class Idol : Entity
{
    // ------------------- ENUMS -------------------
    public enum BuffType { Health, Speed, Damage }
    public enum HostilityType { None, SlowingAura, BlindingAura, Turret }

    // ------------------- SETTINGS -------------------
    [Header("Idol Identity")]
    [SerializeField] private string displayName = "Hörgr";
    [SerializeField] private BuffType rewardType;
    [SerializeField] private HostilityType hostilityBehavior = HostilityType.None;

    [Header("Visuals & Animation")]
    [SerializeField] private Animator animator;
    [Tooltip("Health percentage to trigger the 'Partial' damage phase (e.g. 0.5 for 50%)")]
    [SerializeField] private float damagedThreshold = 0.5f;
    [SerializeField] private float brokenStateDuration = 1.0f; // How long to show the 'Broken' animation before fading/hiding

    [Header("Damage and Reward Settings")]
    [SerializeField] private bool isRewardInstant = false; 
    [SerializeField] private float rewardValue = 20f;    
    [SerializeField] private float rewardDuration = 10f;

    [Header("Combat Settings")]
    [SerializeField] private float effectRadius = 6f;
    [SerializeField] private float attackRate = 2f;
    [SerializeField] private GameObject projectilePrefab; 

    // ------------------- INTERNAL -------------------
    private Transform playerTransform;
    private Entity playerEntity;
    private float attackTimer;

    // Aura tracking
    private bool isAuraApplied = false;
    private float cachedOriginalValue; 

    // Visual State Tracking
    private int animStateParamID;
    
    // ------------------- ENTITY IMPLEMENTATION -------------------

    protected override void Awake()
    {
        base.Awake();
        if (animator == null) animator = GetComponent<Animator>();
        animStateParamID = Animator.StringToHash("DamageState");
        // DamageState 0 = Full
        // DamageState 1 = Partial
        // DamageState 2 = Broken
    }

    protected override void ApplyLevelStats(int level)
    {
        if (level == 1)
        {
            maxHealth = 35f;    
            hostilityBehavior = HostilityType.None; 
        }
        else if (level == 2)
        {
            maxHealth = 70f;
            hostilityBehavior = HostilityType.SlowingAura;
        }
        else if (level == 3)
        {
            maxHealth = 100f;
            hostilityBehavior = HostilityType.Turret;
        }
        else
        {
            maxHealth = 35f;
            hostilityBehavior = HostilityType.None;
        }

        currentHealth = maxHealth;
    }

    // Triggered whenever damage is taken (from Entity.cs)
    protected override void OnDamageTaken(float amount)
    {
        UpdateVisualState();
    }

    protected override void OnDeath()
    {
        // 1. Stop hostile logic immediately
        CleanupEffects();
        this.enabled = false; 

        // 2. Start the destruction sequence (Broken Anim -> Reward -> Fade)
        StartCoroutine(DestructionSequence());
    }

    private void UpdateVisualState()
    {
        if (animator == null) return;

        float healthPercent = currentHealth / maxHealth;

        if (healthPercent <= 0f)
        {
            // Handled in OnDeath usually, but safety check here
            animator.SetInteger(animStateParamID, 2); 
        }
        else if (healthPercent <= damagedThreshold)
        {
            // Phase 2: Partial Health
            animator.SetInteger(animStateParamID, 1);
        }
        else
        {
            // Phase 1: Full Health
            animator.SetInteger(animStateParamID, 0);
        }
    }

    private IEnumerator DestructionSequence()
    {
        // --- PHASE 3: BROKEN ---
        if (animator != null)
        {
            animator.SetInteger(animStateParamID, 2); // Trigger "Broken" animation
        }

        // Wait for the breaking animation to finish playing
        yield return new WaitForSeconds(brokenStateDuration);

        // --- DEATH SEQUENCE START ---
        
        // Disable Collider so it's no longer a physical obstacle
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Grant the reward
        yield return StartCoroutine(GrantRewardRoutine());

        // Finally, destroy the object
        Destroy(gameObject);
    }

    // ------------------- LOGIC -------------------

    protected override void Start()
    {
        base.Start();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerEntity = player.GetComponent<Entity>();
        }
        
        // Ensure we start in State 0
        if (animator != null) animator.SetInteger(animStateParamID, 0);
    }

    private void Update()
    {
        if (isDead || playerTransform == null) return;
        if (isStunned || isFrozen) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        switch (hostilityBehavior)
        {
            case HostilityType.SlowingAura:
                HandleSlowingAura(distance);
                break;
            case HostilityType.Turret:
                HandleTurretLogic(distance);
                break;
        }
    }

    // --- Combat Logic ---

    private void HandleSlowingAura(float distance)
    {
        if (playerEntity == null) return;

        bool isInRange = distance <= effectRadius;

        if (isInRange && !isAuraApplied)
        {
            cachedOriginalValue = playerEntity.moveSpeed;
            playerEntity.moveSpeed = cachedOriginalValue * 0.5f; 
            isAuraApplied = true;
        }
        else if (!isInRange && isAuraApplied)
        {
            playerEntity.moveSpeed = cachedOriginalValue;
            isAuraApplied = false;
        }
    }

    private void CleanupEffects()
    {
        if (hostilityBehavior == HostilityType.SlowingAura && isAuraApplied && playerEntity != null)
        {
            playerEntity.moveSpeed = cachedOriginalValue;
            isAuraApplied = false;
        }
    }

    private void HandleTurretLogic(float distance)
    {
        if (distance > effectRadius) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackRate)
        {
            FireProjectile();
            attackTimer = 0f;
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null) return;

        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Vector2 dir = (playerTransform.position - transform.position).normalized;

        EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
        if (ep != null)
        {
            ep.Initialise(dir, this.damage); 
        }
        else
        {
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * 10f; 
        }
    }

    // --- Reward Logic ---

    private IEnumerator GrantRewardRoutine()
    {
        if (playerEntity == null) yield break;

        Debug.Log($"Granted {rewardType} from Level {currentLevel} Idol!");

        // If the reward is NOT instant (like a 10s buff), we hide the sprite but keep the logic running
        if (!isRewardInstant)
        {
            SpriteRenderer spr = GetComponent<SpriteRenderer>();
            if (spr != null) spr.enabled = false;
        }

        switch (rewardType)
        {
            case BuffType.Health:
                playerEntity.Heal(rewardValue * currentLevel); 
                break;

            case BuffType.Speed:
                float originalSpeed = playerEntity.moveSpeed;
                playerEntity.moveSpeed += rewardValue;
                yield return new WaitForSeconds(rewardDuration);
                if (playerEntity != null) playerEntity.moveSpeed = originalSpeed;
                break;

            case BuffType.Damage:
                float originalDamage = playerEntity.damage;
                playerEntity.damage += rewardValue;
                yield return new WaitForSeconds(rewardDuration);
                if (playerEntity != null) playerEntity.damage = originalDamage;
                break;
        }
    }

    // Unused Entity hooks
    protected override void OnHealed(float amount) { }
    protected override void OnStunEnded() { }
    protected override void OnFreezeEnded() { }
    protected override void InitialiseFromCodeMatrix() { } 
}