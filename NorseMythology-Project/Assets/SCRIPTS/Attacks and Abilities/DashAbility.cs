using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DashAbility", menuName = "Abilities/Defend/Dash")]
public class DashAbility : DefendAbility
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 8f;
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float invincibilityDuration = 0.3f; // Total invincibility time (dash + extra)
    [SerializeField] private float postDashInvincibilityTime = 0.1f; // Extra invincibility after dash ends
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject dashTrailPrefab;
    [SerializeField] private Color dashTrailColor = Color.cyan;
    [SerializeField] private float trailDuration = 0.5f;
    
    [Header("Sprite Trail Settings")]
    [SerializeField] private float trailSpawnInterval = 0.05f; // How often to spawn trail sprites
    [SerializeField] private float trailFadeDuration = 0.3f; // How long each trail sprite takes to fade
    [SerializeField] private int maxTrailSprites = 10; // Maximum number of trail sprites at once
    [SerializeField] private Color trailStartColor = Color.white; // Starting color (usually white for full opacity)
    [SerializeField] private Color trailEndColor = Color.clear; // Ending color (transparent)

    private void Awake()
    {
        abilityName = "Dash";
        description = "Dash quickly in a target direction, becoming invincible for a brief moment.";
        cooldown = 4f;
        activationMode = ActivationMode.ClickToTarget;
        maxTargetingRange = dashDistance;
        showTargetingLine = true;
        targetingLineColor = Color.cyan;
        duration = invincibilityDuration;
        effectStrength = dashSpeed;
    }

    public override bool CanActivate(Player player)
    {
        return player != null && !player.isDead && !player.isInvincible;
    }

    public override void Activate(Player player, PlayerMovement playerMovement = null)
    {
        // This shouldn't be called for click-to-target abilities, but just in case
        Debug.LogWarning("DashAbility: Activate called instead of ActivateWithTarget");
    }

    public override void ActivateWithTarget(Player player, PlayerMovement playerMovement, Vector2 targetDirection, Vector2 worldPosition)
    {
        if (player == null || playerMovement == null) 
        {
            Debug.LogError("DashAbility: Player or PlayerMovement is null");
            return;
        }

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
        
        // NEW: Set dash state for animation and facing direction
        playerMovement.SetDashState(true, dashDirection);
        
        // Calculate dash parameters
        float dashTime = dashDistance / dashSpeed;
        Vector2 dashVelocity = dashDirection.normalized * dashSpeed;
        
        // Create visual effects
        GameObject trailEffect = CreateDashTrail(player.transform);
        
        // Start sprite trail coroutine
        Coroutine spriteTrailCoroutine = player.StartCoroutine(CreateSpriteTrail(player));
        
        // Perform the dash
        float elapsedTime = 0f;
        Vector2 startPosition = player.transform.position;
        
        while (elapsedTime < dashTime)
        {
            // Move the player directly via rigidbody
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
        
        // Restore movement capability immediately after dash ends
        playerMovement.isMovementLocked = originalMovementLocked;
        
        // Wait for any additional invincibility time
        if (postDashInvincibilityTime > 0f)
        {
            yield return new WaitForSeconds(postDashInvincibilityTime);
        }
        
        // Clean up trail effect
        if (trailEffect != null)
        {
            StartTrailFadeOut(trailEffect);
        }
        
        Debug.Log($"Dash completed! Dashed {dashDistance} units in direction {dashDirection}");
    }

    private IEnumerator CreateSpriteTrail(Player player)
    {
        List<GameObject> activeTrailSprites = new List<GameObject>();
        
        while (true) // This will run until the coroutine is stopped
        {
            // Create a new trail sprite
            GameObject trailSprite = CreateTrailSprite(player);
            if (trailSprite != null)
            {
                activeTrailSprites.Add(trailSprite);
                
                // Start fading this sprite
                player.StartCoroutine(FadeTrailSprite(trailSprite, trailFadeDuration));
                
                // Remove old sprites to prevent memory leaks
                if (activeTrailSprites.Count > maxTrailSprites)
                {
                    GameObject oldestSprite = activeTrailSprites[0];
                    activeTrailSprites.RemoveAt(0);
                    if (oldestSprite != null)
                    {
                        Destroy(oldestSprite);
                    }
                }
            }
            
            yield return new WaitForSeconds(trailSpawnInterval);
        }
    }

    private GameObject CreateTrailSprite(Player player)
    {
        // Get the player's sprite renderer
        SpriteRenderer playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer == null)
        {
            Debug.LogWarning("Player doesn't have a SpriteRenderer component!");
            return null;
        }

        // Create a new GameObject for the trail sprite
        GameObject trailSpriteObj = new GameObject("DashTrailSprite");
        trailSpriteObj.transform.position = player.transform.position;
        trailSpriteObj.transform.rotation = player.transform.rotation;
        trailSpriteObj.transform.localScale = player.transform.localScale;

        // Add and configure the sprite renderer
        SpriteRenderer trailSpriteRenderer = trailSpriteObj.AddComponent<SpriteRenderer>();
        trailSpriteRenderer.sprite = playerSpriteRenderer.sprite;
        trailSpriteRenderer.color = trailStartColor;
        trailSpriteRenderer.sortingLayerName = playerSpriteRenderer.sortingLayerName;
        trailSpriteRenderer.sortingOrder = playerSpriteRenderer.sortingOrder - 1; // Behind the player
        
        // Copy the flip state
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
        Color startColor = trailStartColor;
        Color endColor = trailEndColor;

        while (elapsedTime < fadeDuration && trailSprite != null)
        {
            float t = elapsedTime / fadeDuration;
            spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the sprite is destroyed
        if (trailSprite != null)
        {
            Destroy(trailSprite);
        }
    }

    private GameObject CreateDashTrail(Transform playerTransform)
    {
        if (dashTrailPrefab != null)
        {
            GameObject trail = Instantiate(dashTrailPrefab, playerTransform.position, Quaternion.identity);
            
            // Try to set trail color if it has a renderer
            Renderer trailRenderer = trail.GetComponent<Renderer>();
            if (trailRenderer != null)
            {
                trailRenderer.material.color = dashTrailColor;
            }
            
            // Try to set trail color if it has a particle system
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
        
        // Simple approach: destroy after trail duration
        Destroy(trailEffect, trailDuration);
        
        // You could also implement a fade-out coroutine here for smoother visuals
    }

    public override void EnterTargetingMode(Player player)
    {
        Debug.Log("Enter dash targeting mode - click to dash in that direction!");
        // Could add targeting UI elements here if needed
    }

    public override void ExitTargetingMode(Player player)
    {
        Debug.Log("Exit dash targeting mode");
        // Clean up any targeting UI elements here if needed
    }
}