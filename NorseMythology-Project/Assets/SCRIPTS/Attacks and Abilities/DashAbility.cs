using UnityEngine;
using System.Collections;

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
        
        // Make player temporarily invincible (you'll need to add this to your Player class)
        // For now, we'll just disable collision with enemies or add a flag
        
        // Calculate dash parameters
        float dashTime = dashDistance / dashSpeed;
        Vector2 dashVelocity = dashDirection.normalized * dashSpeed;
        
        // Create visual effects
        GameObject trailEffect = CreateDashTrail(player.transform);
        
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