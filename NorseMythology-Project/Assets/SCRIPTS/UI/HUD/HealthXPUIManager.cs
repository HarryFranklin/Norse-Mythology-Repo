using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthXPUIManager : MonoBehaviour
{
    [Header("Player Reference")]
    public Player player;

    [Header("Health Bar")]
    [Tooltip("The RectTransform of the health bar's background or border.")]
    public RectTransform healthBarBackground;
    [Tooltip("The 1-pixel wide Image for the health bar's front fill.")]
    public Image healthFillImage;
    [Tooltip("The color to use for the health bar fill.")]
    public Color healthColor = Color.red;
    private RectTransform healthFillRect;

    [Header("XP Bar")]
    [Tooltip("The RectTransform of the XP bar's background or border.")]
    public RectTransform xpBarBackground;
    [Tooltip("The 1-pixel wide Image for the XP bar's front fill.")]
    public Image xpFillImage;
    [Tooltip("The color to use for the XP bar fill.")]
    public Color xpColor = Color.yellow;
    private RectTransform xpFillRect;
    
    [Header("Optional Text Labels")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null)
            {
                Debug.LogError("HealthXPUIManager: Player not found! Please assign it in the inspector.");
                return;
            }
        }

        if (player.healthXPUIManager == null)
        {
            player.healthXPUIManager = this;
        }

        // Cache RectTransforms and apply initial colors
        if (healthFillImage != null)
        {
            healthFillRect = healthFillImage.rectTransform;
            healthFillImage.color = healthColor;
        }
        if (xpFillImage != null)
        {
            xpFillRect = xpFillImage.rectTransform;
            xpFillImage.color = xpColor;
        }

        StartCoroutine(DelayedInitialisation());
    }
    
    private System.Collections.IEnumerator DelayedInitialisation()
    {
        yield return null; // Wait one frame
        UpdateHealthBar();
        UpdateXPBar();
        UpdateTextLabels();
    }
    
    public void OnHealthChanged()
    {
        UpdateHealthBar();
        if (healthText != null && player != null && player.currentStats != null)
        {
            healthText.text = $"{Mathf.Ceil(player.currentHealth)}/{player.currentStats.maxHealth}";
        }
    }
    
    public void OnXPChanged()
    {
        UpdateXPBar();
        
        if (player == null || player.currentStats == null) return;
        
        if (xpText != null)
        {
            float currentXP = player.GetCurrentExperience();
            float pendingXP = player.GetPendingExperience();
            float displayXP = currentXP + pendingXP;
            
            xpText.text = $"{Mathf.Floor(displayXP)}/{player.currentStats.experienceToNextLevel}";
        }
        
        if (levelText != null)
        {
            string levelUpIndicator = player.isLevelUpPending ? " (!)" : "";
            levelText.text = $"LEVEL: {player.currentStats.level}{levelUpIndicator}";
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthFillRect == null || healthBarBackground == null || player == null || player.currentStats == null) return;
        
        float healthPercentage = Mathf.Clamp01(player.currentHealth / player.currentStats.maxHealth);
        float backgroundWidth = healthBarBackground.rect.width;
        float targetWidth = backgroundWidth * healthPercentage;
        
        // This scales the bar from the left, assuming the pivot is set correctly.
        healthFillRect.sizeDelta = new Vector2(targetWidth, healthFillRect.sizeDelta.y);
    }
    
    private void UpdateXPBar()
    {
        if (xpFillRect == null || xpBarBackground == null || player == null || player.currentStats == null) return;
        
        float currentXP = player.GetCurrentExperience();
        float pendingXP = player.GetPendingExperience();
        float displayXP = currentXP + pendingXP;
        float xpPercentage = Mathf.Clamp01(displayXP / player.currentStats.experienceToNextLevel);
        
        float backgroundWidth = xpBarBackground.rect.width;
        float targetWidth = backgroundWidth * xpPercentage;
        
        // This scales the bar from the left.
        xpFillRect.sizeDelta = new Vector2(targetWidth, xpFillRect.sizeDelta.y);
    }
    
    private void UpdateTextLabels()
    {
        if (player != null && player.currentStats != null)
        {
            OnHealthChanged();
            OnXPChanged();
        }
    }
    
    public void ForceUpdateUI()
    {
        UpdateHealthBar();
        UpdateXPBar();
        UpdateTextLabels();
    }
}