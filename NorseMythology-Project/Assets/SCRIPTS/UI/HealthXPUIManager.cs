using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthXPUIManager : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerController playerController;
    
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
        // Try to find PlayerController if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("HealthXPUIManager: PlayerController not found! Please assign it in the inspector.");
                return;
            }
        }

        // Set this UI manager as a reference in the PlayerController
        if (playerController.healthXPUIManager == null)
        {
            playerController.healthXPUIManager = this;
        }

        UpdateHealthBar();
        UpdateXPBar();
        UpdateTextLabels();

    }
    
    // Called by PlayerController when health changes
    public void OnHealthChanged()
    {
        UpdateHealthBar();
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(playerController.currentHealth)}/{playerController.currentStats.maxHealth}";
        }
    }
    
    // Called by PlayerController when XP changes
    public void OnXPChanged()
    {
        UpdateXPBar();
        if (xpText != null)
        {
            // Use the current experience directly (not total) since we reset it on level up
            float currentXP = playerController.GetCurrentExperience();
            float pendingXP = playerController.GetPendingExperience();
            float displayXP = currentXP + pendingXP;
            
            xpText.text = $"{Mathf.Floor(displayXP)}/{playerController.currentStats.experienceToNextLevel}";
        }
        
        if (levelText != null)
        {
            string levelUpIndicator = playerController.isLevelUpPending ? " (!)" : "";
            levelText.text = $"Level {playerController.currentStats.level}{levelUpIndicator}";
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthFillImage == null || playerController == null || playerController.currentStats == null) return;
        
        float healthPercentage = playerController.currentHealth / playerController.currentStats.maxHealth;
        healthFillImage.fillAmount = Mathf.Clamp01(healthPercentage);
    }
    
    private void UpdateXPBar()
    {
        if (xpFillImage == null || playerController == null || playerController.currentStats == null) return;
        
        // Calculate XP progress using current experience + pending experience
        float currentXP = playerController.GetCurrentExperience();
        float pendingXP = playerController.GetPendingExperience();
        float displayXP = currentXP + pendingXP;
        float xpPercentage = displayXP / playerController.currentStats.experienceToNextLevel;
        
        xpFillImage.fillAmount = Mathf.Clamp01(xpPercentage);
    }
    
    private void UpdateTextLabels()
    {
        OnHealthChanged();
        OnXPChanged();
    }
    
    // Public methods for manual UI updates if needed
    public void ForceUpdateUI()
    {
        UpdateHealthBar();
        UpdateXPBar();
        UpdateTextLabels();
    }
}