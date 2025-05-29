using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthXPUIManager : MonoBehaviour
{
    [Header("Player Reference")]
    public Player player;
    
    [Header("Health Bar")]
    public Image healthFillImage;
    
    [Header("XP Bar")]
    public Image xpFillImage;
    
    [Header("Optional Text Labels")]
    public TextMeshProUGUI healthText; // Optional: displays current/max health as text
    public TextMeshProUGUI xpText; // Optional: displays current XP progress as text
    public TextMeshProUGUI levelText; // Optional: displays current level

    private void Start()
    {
        // Try to find Player if not assigned
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null)
            {
                Debug.LogError("HealthXPUIManager: Player not found! Please assign it in the inspector.");
                return;
            }
        }

        // Set this UI manager as a reference in the Player
        if (player.healthXPUIManager == null)
        {
            player.healthXPUIManager = this;
        }

        // Wait a frame before updating UI to ensure Player is fully initialized
        StartCoroutine(DelayedInitialization());
    }
    
    private System.Collections.IEnumerator DelayedInitialization()
    {
        yield return null; // Wait one frame
        
        // Now update the UI
        UpdateHealthBar();
        UpdateXPBar();
        UpdateTextLabels();
    }
    
    // Called by Player when health changes
    public void OnHealthChanged()
    {
        UpdateHealthBar();
        if (healthText != null && player != null && player.currentStats != null)
        {
            healthText.text = $"{Mathf.Ceil(player.currentHealth)}/{player.currentStats.maxHealth}";
        }
    }
    
    // Called by Player when XP changes
    public void OnXPChanged()
    {
        UpdateXPBar();
        
        // Add null checks before accessing player data
        if (player == null || player.currentStats == null) return;
        
        if (xpText != null)
        {
            // Use the current experience directly (not total) since we reset it on level up
            float currentXP = player.GetCurrentExperience();
            float pendingXP = player.GetPendingExperience();
            float displayXP = currentXP + pendingXP;
            
            xpText.text = $"{Mathf.Floor(displayXP)}/{player.currentStats.experienceToNextLevel}";
        }
        
        if (levelText != null)
        {
            string levelUpIndicator = player.isLevelUpPending ? " (!)" : ""; // if need to level up, show !
            levelText.text = $"LEVEL: {player.currentStats.level}{levelUpIndicator}";
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthFillImage == null || player == null || player.currentStats == null) return;
        
        float healthPercentage = player.currentHealth / player.currentStats.maxHealth;
        healthFillImage.fillAmount = Mathf.Clamp01(healthPercentage);
    }
    
    private void UpdateXPBar()
    {
        if (xpFillImage == null || player == null || player.currentStats == null) return;
        
        // Calculate XP progress using current experience + pending experience
        float currentXP = player.GetCurrentExperience();
        float pendingXP = player.GetPendingExperience();
        float displayXP = currentXP + pendingXP;
        float xpPercentage = displayXP / player.currentStats.experienceToNextLevel;
        
        xpFillImage.fillAmount = Mathf.Clamp01(xpPercentage);
    }
    
    private void UpdateTextLabels()
    {
        // Only update if player and its stats are available
        if (player != null && player.currentStats != null)
        {
            OnHealthChanged();
            OnXPChanged();
        }
    }
    
    // Public methods for manual UI updates if needed
    public void ForceUpdateUI()
    {
        UpdateHealthBar();
        UpdateXPBar();
        UpdateTextLabels();
    }
}