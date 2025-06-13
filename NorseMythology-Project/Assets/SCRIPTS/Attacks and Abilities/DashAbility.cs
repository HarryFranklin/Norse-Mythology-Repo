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
        maxStacks = 3; // Dash can be stacked up to 3 times
        
        // Set up targeting range based on level 1 distance
        maxTargetingRange = GetLevelData(1).distance;
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
        if (player == null || playerMovement == null) 
        {
            Debug.LogError("DashAbility: Player or PlayerMovement is null");
            return;
        }

        // Use a stack
        RemoveStack();
        
        // Start the dash coroutine
        player.StartCoroutine(PerformDash(player, playerMovement, targetDirection));
    }

    private IEnumerator PerformDash(Player player, PlayerMovement playerMovement, Vector2 dashDirection)
    {
        // Store original values
        bool originalMovementLocked = playerMovement.isMovementLocked;
        Vector2 originalVelocity = player.rigidBody.linearVelocity;

        // Set player state for dash
        playerMovement.isMovementLocked = true;
        playerMovement.SetDashState(true, dashDirection);
        
        // Calculate dash parameters using current level values
        float dashTime = CurrentDistance / CurrentSpeed;
        Vector2 dashVelocity = dashDirection.normalized * CurrentSpeed;
        
        // Create visual effects
        GameObject trailEffect = CreateDashTrail(player.transform);
        Coroutine spriteTrailCoroutine = player.StartCoroutine(CreateSpriteTrail(player));
        
        // Perform the dash
        float elapsedTime = 0f;
        
        while (elapsedTime < dashTime)
        {
            player.rigidBody.linearVelocity = dashVelocity;
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        
        // Stop dash movement
        player.rigidBody.linearVelocity = Vector2.zero;
        
        // Stop creating new trail sprites
        if (spriteTrailCoroutine != null)
        {
            player.StopCoroutine(spriteTrailCoroutine);
        }
        
        // Clear dash state
        playerMovement.SetDashState(false);
        playerMovement.isMovementLocked = originalMovementLocked;
        
        // Wait for any additional invincibility time (using specialValue1)
        if (CurrentSpecialValue1 > 0f)
        {
            yield return new WaitForSeconds(CurrentSpecialValue1);
        }
        
        // Clean up trail effect
        if (trailEffect != null)
        {
            StartTrailFadeOut(trailEffect);
        }
        
        Debug.Log($"Dash completed! Level {CurrentLevel} dash: {CurrentDistance}u at {CurrentSpeed} speed");
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
                    if (oldestSprite != null)
                        Destroy(oldestSprite);
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

        if (trailSprite != null)
            Destroy(trailSprite);
    }

    private GameObject CreateDashTrail(Transform playerTransform)
    {
        if (dashTrailPrefab != null)
        {
            GameObject trail = Instantiate(dashTrailPrefab, playerTransform.position, Quaternion.identity);
            
            Renderer trailRenderer = trail.GetComponent<Renderer>();
            if (trailRenderer != null)
                trailRenderer.material.color = dashTrailColor;
            
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
        Debug.Log($"Enter dash targeting mode - {CurrentStacks} dashes available!");
        maxTargetingRange = CurrentDistance; // Update targeting range based on current level
    }

    public override void ExitTargetingMode(Player player)
    {
        Debug.Log("Exit dash targeting mode");
    }
}