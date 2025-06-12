using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FreezeAbility", menuName = "Abilities/Defend/Freeze")]
public class FreezeAbility : DefendAbility
{
    [Header("Freeze Settings")]
    public float freezeDuration = 3f;
    public float freezeRadius = 5f;
    public GameObject freezeEffectPrefab;

    private void Awake()
    {
        abilityName = "Freeze";
        description = "Freeze nearby enemies in place for a short duration.";
        cooldown = 5f;
    }

    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        // This is called for instant activation mode
        FreezeEnemies(player.transform.position);
    }

    public override void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        // This is called for click-to-target activation mode
        FreezeEnemies(worldPosition);
    }

    private void FreezeEnemies(Vector3 center)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, freezeRadius);
        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                // Apply freeze effect to the enemy
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.Freeze(freezeDuration);
                }
                
                // Instantiate freeze effect and start fade coroutine
                if (freezeEffectPrefab != null)
                {
                    GameObject freezeEffect = Instantiate(freezeEffectPrefab, collider.transform.position, Quaternion.identity);
                    
                    // Start the fade coroutine on a MonoBehaviour (use the enemy as host)
                    if (enemy != null)
                    {
                        enemy.StartCoroutine(FadeAndDestroyEffect(freezeEffect, freezeDuration));
                    }
                }
            }
        }
    }

    private IEnumerator FadeAndDestroyEffect(GameObject effectObject, float duration)
    {
        if (effectObject == null) yield break;

        // Get all renderers (SpriteRenderer, ParticleSystem, etc.)
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
        
        // Destroy the effect when fade is complete
        if (effectObject != null)
        {
            Destroy(effectObject);
        }
    }
}