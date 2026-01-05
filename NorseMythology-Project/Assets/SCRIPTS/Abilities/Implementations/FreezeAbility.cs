using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "FreezeAbility", menuName = "Abilities/Freeze")]
public class FreezeAbility : Ability
{
    [Header("Freeze Effects")]
    [SerializeField] private GameObject freezeEffectPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip freezeSound;
    [Range(0f, 1f)]
    [SerializeField] private float freezeVolume = 1f;
    [Tooltip("If true, pitch will vary slightly to sound more organic.")]
    [SerializeField] private bool useRandomPitch = true;

    private void Awake()
    {
        abilityName = "Freeze";
        description = "Freeze nearby enemies in place for a short duration.";
        activationMode = ActivationMode.ClickToTarget;
        showTargetingLine = true;
        targetingLineColor = Color.cyan;
        
        // Set up targeting range based on level 1 radius
        maxTargetingRange = GetStatsForLevel(1).radius * 2f;
    }

    public override void InitialiseFromCodeMatrix()
    {
        // Define freeze ability values via code matrix
        // Level, cooldown, damage, duration(freeze time), radius, speed, distance, specialValue1(effect scale), specialValue2, specialValue3, maxStacks, stackRegenTime
        
        // Level 1: Basic freeze
        SetLevelData(1, cooldown: 12f, damage: 0f, duration: 2f, radius: 2.5f, speed: 0f, distance: 0f, specialValue1: 1f, specialValue2: 0f, specialValue3: 0f, maxStacks: 1, stackRegenTime: 12f);
        
        // Level 2: Longer duration and bigger area
        SetLevelData(2, cooldown: 11f, damage: 0f, duration: 2.5f, radius: 3f, speed: 0f, distance: 0f, specialValue1: 1.2f, specialValue2: 0f, specialValue3: 0f, maxStacks: 1, stackRegenTime: 11f);
        
        // Level 3: Even better freeze
        SetLevelData(3, cooldown: 10f, damage: 0f, duration: 3f, radius: 3.5f, speed: 0f, distance: 0f, specialValue1: 1.4f, specialValue2: 0f, specialValue3: 0f, maxStacks: 1, stackRegenTime: 10f);
        
        // Level 4: Major improvements
        SetLevelData(4, cooldown: 8f, damage: 0f, duration: 3.5f, radius: 4f, speed: 0f, distance: 0f, specialValue1: 1.6f, specialValue2: 0f, specialValue3: 0f, maxStacks: 2, stackRegenTime: 8f);
        
        // Level 5: Maximum freeze power with 2 charges
        SetLevelData(5, cooldown: 6f, damage: 0f, duration: 4f, radius: 4.5f, speed: 0f, distance: 0f, specialValue1: 1.8f, specialValue2: 0f, specialValue3: 0f, maxStacks: 2, stackRegenTime: 6f);
        
        Debug.Log($"FreezeAbility initialised from code matrix. Level 1: {StackedDuration}s freeze, {StackedRadius}u radius");
    }

    public override bool CanActivate(Player player)
    {
        bool canActivate = player != null && !player.isDead && CurrentStacks > 0;
        
        if (!canActivate)
        {
            Debug.Log($"Freeze cannot activate - Player null: {player == null}, Dead: {player?.isDead}, Stacks: {CurrentStacks}/{MaxStacksAtCurrentLevel}");
        }
        
        return canActivate;
    }

    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        if (player == null) return;
        
        RemoveStack();
        FreezeEnemies(player.transform.position);
        
        Debug.Log($"Freeze activated instantly! Level {CurrentLevel} (Stack {AbilityStacks}): {StackedDuration}s freeze, {StackedRadius}u radius, {CurrentStacks}/{MaxStacksAtCurrentLevel} charges remaining");
    }

    public override void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        if (player == null) return;
        
        RemoveStack();
        FreezeEnemies(worldPosition);
        
        Debug.Log($"Freeze activated at target! Level {CurrentLevel} (Stack {AbilityStacks}): {StackedDuration}s freeze, {StackedRadius}u radius, {CurrentStacks}/{MaxStacksAtCurrentLevel} charges remaining");
    }

    private void FreezeEnemies(Vector3 center)
    {
        // --- AUDIO START ---
        if (AudioManager.Instance != null && freezeSound != null)
        {
            // Plays the sound with optional random pitch for an "ice shattering" variance
            AudioManager.Instance.PlaySFX(freezeSound, freezeVolume, useRandomPitch);
        }
        // -------------------

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, StackedRadius);
        int enemiesFrozen = 0;
        
        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // Apply freeze effect using stacked duration
                    enemy.Freeze(StackedDuration);
                    enemiesFrozen++;
                    
                    // Instantiate freeze effect and start fade coroutine
                    if (freezeEffectPrefab != null)
                    {
                        GameObject freezeEffect = Instantiate(freezeEffectPrefab, collider.transform.position, Quaternion.identity);
                        
                        // Scale effect based on stacked specialValue1
                        if (StackedSpecialValue1 > 0)
                        {
                            freezeEffect.transform.localScale = Vector3.one * StackedSpecialValue1;
                        }
                        
                        enemy.StartCoroutine(FadeAndDestroyEffect(freezeEffect, StackedDuration));
                    }
                }
            }
        }
        
        Debug.Log($"Freeze Level {CurrentLevel}: Froze {enemiesFrozen} enemies for {StackedDuration}s in {StackedRadius}u radius");
    }

    private IEnumerator FadeAndDestroyEffect(GameObject effectObject, float duration)
    {
        if (effectObject == null) yield break;

        // Get renderers
        SpriteRenderer spriteRenderer = effectObject.GetComponent<SpriteRenderer>();
        ParticleSystem particleSystem = effectObject.GetComponent<ParticleSystem>();
        
        Color originalColor = Color.white;
        ParticleSystem.MainModule particleMain = default;
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        else if (particleSystem != null)
        {
            particleMain = particleSystem.main;
            originalColor = particleMain.startColor.color;
        }

        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float alpha = Mathf.Lerp(1f, 0f, progress);
            
            // Update sprite renderer alpha
            if (spriteRenderer != null)
            {
                Color newColor = originalColor;
                newColor.a = alpha;
                spriteRenderer.color = newColor;
            }
            
            // Update particle system alpha
            if (particleSystem != null)
            {
                Color newColor = originalColor;
                newColor.a = alpha;
                particleMain.startColor = newColor;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (effectObject != null)
            Destroy(effectObject);
    }

    public override void EnterTargetingMode(Player player)
    {
        Debug.Log($"Freeze targeting: Level {CurrentLevel} (Stack {AbilityStacks}) - {StackedRadius}u radius, {StackedDuration}s duration, {CurrentStacks}/{MaxStacksAtCurrentLevel} charges");
        maxTargetingRange = StackedRadius * 2f; // Allow targeting a bit beyond the freeze radius
    }

    public override void ExitTargetingMode(Player player)
    {
        Debug.Log("Exit freeze targeting mode");
    }
}