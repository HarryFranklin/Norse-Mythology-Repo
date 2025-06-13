using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "FreezeAbility", menuName = "Abilities/Freeze")]
public class FreezeAbility : Ability
{
    [Header("Freeze Effects")]
    [SerializeField] private GameObject freezeEffectPrefab;

    private void Awake()
    {
        abilityName = "Freeze";
        description = "Freeze nearby enemies in place for a short duration.";
        activationMode = ActivationMode.ClickToTarget;
        showTargetingLine = true;
        targetingLineColor = Color.cyan;
        maxStacks = 1;
    }

    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        // For instant activation mode
        FreezeEnemies(player.transform.position);
    }

    public override void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        // For click-to-target activation mode
        FreezeEnemies(worldPosition);
    }

    private void FreezeEnemies(Vector3 center)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, CurrentRadius);
        int enemiesFrozen = 0;
        
        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // Apply freeze effect using current duration
                    enemy.Freeze(CurrentDuration);
                    enemiesFrozen++;
                    
                    // Instantiate freeze effect and start fade coroutine
                    if (freezeEffectPrefab != null)
                    {
                        GameObject freezeEffect = Instantiate(freezeEffectPrefab, collider.transform.position, Quaternion.identity);
                        
                        // Scale effect based on current level (using specialValue1 as scale multiplier)
                        if (CurrentSpecialValue1 > 0)
                        {
                            freezeEffect.transform.localScale = Vector3.one * CurrentSpecialValue1;
                        }
                        
                        enemy.StartCoroutine(FadeAndDestroyEffect(freezeEffect, CurrentDuration));
                    }
                }
            }
        }
        
        Debug.Log($"Freeze Level {CurrentLevel}: Froze {enemiesFrozen} enemies for {CurrentDuration}s in {CurrentRadius}u radius");
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
        Debug.Log($"Freeze targeting: Level {CurrentLevel} - {CurrentRadius}u radius, {CurrentDuration}s duration");
        maxTargetingRange = CurrentRadius * 2f; // Allow targeting a bit beyond the freeze radius
    }

    public override void ExitTargetingMode(Player player)
    {
        Debug.Log("Exit freeze targeting mode");
    }
}