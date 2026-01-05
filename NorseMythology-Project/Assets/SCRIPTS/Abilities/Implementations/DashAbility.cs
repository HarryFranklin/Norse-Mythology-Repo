using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DashAbility", menuName = "Abilities/Dash")]
public class DashAbility : Ability
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject dashTrailPrefab;
    [SerializeField] private Color dashTrailColor = Color.cyan;
    [SerializeField] private float trailDuration = 0.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip dashSound;
    [Range(0f, 1f)]
    [SerializeField] private float dashVolume = 1f;
    [Tooltip("If true, pitch will vary slightly each dash to prevent repetition.")]
    [SerializeField] private bool useRandomPitch = true;
    
    [Header("Sprite Trail Settings")]
    [SerializeField] private float trailSpawnInterval = 0.05f;
    [SerializeField] private float trailFadeDuration = 0.3f;
    [SerializeField] private int maxTrailSprites = 10;
    [SerializeField] private Color trailStartColor = Color.white;
    [SerializeField] private Color trailEndColor = Color.clear;

    private void Awake()
    {
        abilityName = "Dash";
        description = "Dash quickly in a target direction, becoming invincible for a brief moment.";
        activationMode = ActivationMode.ClickToTarget;
        showTargetingLine = true;
        targetingLineColor = Color.cyan;
        
        maxTargetingRange = GetStatsForLevel(1).distance;
    }

    public override void InitialiseFromCodeMatrix()
    {
        SetLevelData(1, cooldown: 4f, speed: 15f, distance: 3f, specialValue1: 0.1f, maxStacks: 1, stackRegenTime: 2f);
        SetLevelData(2, cooldown: 3.25f, speed: 16f, distance: 4.5f, specialValue1: 0.15f, maxStacks: 2, stackRegenTime: 1.5f);
        SetLevelData(3, cooldown: 2.5f, speed: 17f, distance: 5.25f, specialValue1: 0.2f, maxStacks: 2, stackRegenTime: 1.2f);
        SetLevelData(4, cooldown: 1.75f, speed: 18f, distance: 6.5f, specialValue1: 0.25f, maxStacks: 3, stackRegenTime: 1f);
        SetLevelData(5, cooldown: 1.5f, speed: 20f, distance: 7.5f, specialValue1: 0.3f, maxStacks: 4, stackRegenTime: 0.8f);
    }

    public override bool CanActivate(Player player)
    {
        return player != null && !player.isDead && !player.isInvincible && CurrentStacks > 0;
    }

    public override void Activate(Player player, PlayerMovement playerMovement = null)
    {
        Debug.LogWarning("DashAbility: Activate called instead of ActivateWithTarget");
    }

    public override void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        if (player == null || playerMovement == null) return;

        RemoveStack();
        player.StartCoroutine(PerformDash(player, playerMovement, targetDirection));
    }

    private IEnumerator PerformDash(Player player, PlayerMovement playerMovement, Vector2 dashDirection)
    {
        // --- AUDIO START ---
        if (AudioManager.Instance != null && dashSound != null)
        {
            // We use the boolean overload we created earlier.
            // true = slight random pitch variation (Great for repetitive actions like dashing)
            AudioManager.Instance.PlaySFX(dashSound, dashVolume, useRandomPitch);
        }
        // -------------------

        bool originalMovementLocked = playerMovement.isMovementLocked;
        
        playerMovement.isMovementLocked = true;
        playerMovement.SetDashState(true, dashDirection);
        
        float dashTime = StackedDistance / StackedSpeed;
        Vector2 dashVelocity = dashDirection.normalized * StackedSpeed;
        
        GameObject trailEffect = CreateDashTrail(player.transform);
        Coroutine spriteTrailCoroutine = player.StartCoroutine(CreateSpriteTrail(player));
        
        float elapsedTime = 0f;
        
        while (elapsedTime < dashTime)
        {
            player.rigidBody.linearVelocity = dashVelocity;
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        
        player.rigidBody.linearVelocity = Vector2.zero;
        
        if (spriteTrailCoroutine != null) player.StopCoroutine(spriteTrailCoroutine);
        
        playerMovement.SetDashState(false);
        playerMovement.isMovementLocked = originalMovementLocked;
        
        if (StackedSpecialValue1 > 0f) yield return new WaitForSeconds(StackedSpecialValue1);
        
        if (trailEffect != null) StartTrailFadeOut(trailEffect);
    }

    private IEnumerator CreateSpriteTrail(Player player)
    {
        List<GameObject> activeTrailSprites = new List<GameObject>();
        
        while (true)
        {
            GameObject trailSprite = CreateTrailSprite(player);
            if (trailSprite != null)
            {
                activeTrailSprites.Add(trailSprite);
                player.StartCoroutine(FadeTrailSprite(trailSprite, trailFadeDuration));
                
                if (activeTrailSprites.Count > maxTrailSprites)
                {
                    GameObject oldestSprite = activeTrailSprites[0];
                    activeTrailSprites.RemoveAt(0);
                    if (oldestSprite != null) Destroy(oldestSprite);
                }
            }
            
            yield return new WaitForSeconds(trailSpawnInterval);
        }
    }

    private GameObject CreateTrailSprite(Player player)
    {
        SpriteRenderer playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer == null) return null;

        GameObject trailSpriteObj = new GameObject("DashTrailSprite");
        trailSpriteObj.transform.position = player.transform.position;
        trailSpriteObj.transform.rotation = player.transform.rotation;
        trailSpriteObj.transform.localScale = player.transform.localScale;

        SpriteRenderer trailSpriteRenderer = trailSpriteObj.AddComponent<SpriteRenderer>();
        trailSpriteRenderer.sprite = playerSpriteRenderer.sprite;
        trailSpriteRenderer.color = trailStartColor;
        trailSpriteRenderer.sortingLayerName = playerSpriteRenderer.sortingLayerName;
        trailSpriteRenderer.sortingOrder = playerSpriteRenderer.sortingOrder - 1;
        trailSpriteRenderer.flipX = playerSpriteRenderer.flipX;
        trailSpriteRenderer.flipY = playerSpriteRenderer.flipY;

        return trailSpriteObj;
    }

    private IEnumerator FadeTrailSprite(GameObject trailSprite, float fadeDuration)
    {
        if (trailSprite == null) yield break;
        
        SpriteRenderer spriteRenderer = trailSprite.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration && trailSprite != null)
        {
            float t = elapsedTime / fadeDuration;
            spriteRenderer.color = Color.Lerp(trailStartColor, trailEndColor, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (trailSprite != null) Destroy(trailSprite);
    }

    private GameObject CreateDashTrail(Transform playerTransform)
    {
        if (dashTrailPrefab != null)
        {
            GameObject trail = Instantiate(dashTrailPrefab, playerTransform.position, Quaternion.identity);
            
            Renderer trailRenderer = trail.GetComponent<Renderer>();
            if (trailRenderer != null) trailRenderer.material.color = dashTrailColor;
            
            ParticleSystem particles = trail.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                var main = particles.main;
                main.startColor = dashTrailColor;
            }
            
            return trail;
        }
        return null;
    }

    private void StartTrailFadeOut(GameObject trailEffect)
    {
        if (trailEffect == null) return;
        Destroy(trailEffect, trailDuration);
    }

    public override void EnterTargetingMode(Player player)
    {
        maxTargetingRange = StackedDistance; 
    }

    public override void ExitTargetingMode(Player player) { }
}