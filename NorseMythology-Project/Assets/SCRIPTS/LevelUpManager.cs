using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class LevelUpManager : MonoBehaviour
{
    [Header("UI References")]
    public Button continueButton;
    public TextMeshProUGUI waveCompletedText;
    public TextMeshProUGUI nextWaveText;
    public TextMeshProUGUI playerStatsText;
    public TextMeshProUGUI upgradePointsText;
    
    [Header("Upgrade Buttons")]
    public Button upgradeHealthButton;
    public Button upgradeDamageButton;
    public Button upgradeSpeedButton;
    public Button upgradeAttackSpeedButton;
    public Button upgradeHealthRegenButton;
    public Button upgradeMeleeRangeButton;
    public Button upgradeProjectileSpeedButton;
    public Button upgradeProjectileRangeButton;
    
    [Header("Upgrade Display Settings")]
    [Tooltip("If true, upgrade amounts show only on hover. If false, they show all the time.")]
    public bool showUpgradeAmountsOnHoverOnly = false;
    
    private PlayerStats currentPlayerStats;
    private int availableUpgradePoints;
    
    // Store original button texts and upgrade amounts
    private string[] originalButtonTexts;
    private float[] upgradeAmounts;
    private string[] statNames;
    
    private void Start()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        InitialiseButtonData();
        SetupUpgradeButtons();
        
        // Get the persistent player stats from GameManager
        GetPlayerStatsFromGameManager();
        
        UpdateUI();
    }
    
    private void InitialiseButtonData()
    {
        // Store original button texts and corresponding upgrade amounts
        originalButtonTexts = new string[8];
        upgradeAmounts = new float[8];
        statNames = new string[8];
        
        Button[] buttons = {
            upgradeHealthButton, upgradeDamageButton, upgradeSpeedButton, 
            upgradeAttackSpeedButton, upgradeHealthRegenButton, upgradeMeleeRangeButton,
            upgradeProjectileSpeedButton, upgradeProjectileRangeButton
        };
        
        string[] buttonTexts = {
            "Health", "Damage", "Speed",
            "Attack Speed", "Health Regen", "Melee Range",
            "Projectile Speed", "Projectile Range"
        };
        
        float[] amounts = { 25f, 5f, 1f, 0.2f, 0.5f, 0.5f, 2f, 2f };
        string[] stats = { "health", "damage", "speed", "attackspeed", "healthregen", "meleerange", "projectilespeed", "projectilerange" };
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                originalButtonTexts[i] = buttonTexts[i];
                upgradeAmounts[i] = amounts[i];
                statNames[i] = stats[i];
                
                // Set initial button text
                TextMeshProUGUI buttonText = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = originalButtonTexts[i];
                }
            }
        }
    }
    
    private void SetupUpgradeButtons()
    {
        Button[] buttons = {
            upgradeHealthButton, upgradeDamageButton, upgradeSpeedButton, 
            upgradeAttackSpeedButton, upgradeHealthRegenButton, upgradeMeleeRangeButton,
            upgradeProjectileSpeedButton, upgradeProjectileRangeButton
        };
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                int index = i; // Capture for closure
                buttons[i].onClick.AddListener(() => UpgradeStat(statNames[index], upgradeAmounts[index]));
                
                // Add hover functionality if needed
                if (showUpgradeAmountsOnHoverOnly)
                {
                    AddHoverEvents(buttons[i], index);
                }
            }
        }
    }
    
    private void AddHoverEvents(Button button, int index)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // On hover enter
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => {
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{originalButtonTexts[index]} (+{FormatUpgradeAmount(upgradeAmounts[index])})";
            }
        });
        trigger.triggers.Add(pointerEnter);
        
        // On hover exit
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => {
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = originalButtonTexts[index];
            }
        });
        trigger.triggers.Add(pointerExit);
    }
    
    private string FormatUpgradeAmount(float amount)
    {
        // Format the upgrade amount nicely
        if (amount >= 1f)
        {
            return amount.ToString("F0"); // No decimals for whole numbers
        }
        else
        {
            return amount.ToString("F1"); // One decimal for fractional numbers
        }
    }
    
    private void GetPlayerStatsFromGameManager()
    {
        if (GameManager.Instance != null)
        {
            currentPlayerStats = GameManager.Instance.GetCurrentPlayerStats();
            availableUpgradePoints = GameManager.Instance.GetUpgradePoints();
            
            if (currentPlayerStats != null)
            {
                Debug.Log($"Retrieved player stats - Level: {currentPlayerStats.level}, Health: {currentPlayerStats.maxHealth}, Upgrade Points: {availableUpgradePoints}");
            }
            else
            {
                Debug.LogWarning("Could not retrieve player stats from GameManager!");
            }
        }
        else
        {
            Debug.LogError("GameManager.Instance is null in LevelUpManager!");
        }
    }
    
    private void UpdateUI()
    {
        if (GameManager.Instance != null)
        {
            WaveManager waveManager = GameManager.Instance.GetWaveManager();
            
            if (waveCompletedText != null && waveManager != null)
            {
                waveCompletedText.text = $"Wave {waveManager.GetCurrentWave()} Completed!";
            }
            
            if (nextWaveText != null && waveManager != null)
            {
                nextWaveText.text = $"Prepare for Wave {waveManager.GetCurrentWave() + 1}";
            }
        }
        
        DisplayPlayerStats();
        UpdateUpgradePointsDisplay();
        UpdateButtonStates();
        UpdateButtonTexts();
    }
    
    private void UpdateButtonTexts()
    {
        if (!showUpgradeAmountsOnHoverOnly)
        {
            // Show upgrade amounts all the time
            Button[] buttons = {
                upgradeHealthButton, upgradeDamageButton, upgradeSpeedButton, 
                upgradeAttackSpeedButton, upgradeHealthRegenButton, upgradeMeleeRangeButton,
                upgradeProjectileSpeedButton, upgradeProjectileRangeButton
            };
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    TextMeshProUGUI buttonText = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = $"{originalButtonTexts[i]} (+{FormatUpgradeAmount(upgradeAmounts[i])})";
                    }
                }
            }
        }
        else
        {
            // Reset to original texts when hover-only mode is enabled
            Button[] buttons = {
                upgradeHealthButton, upgradeDamageButton, upgradeSpeedButton, 
                upgradeAttackSpeedButton, upgradeHealthRegenButton, upgradeMeleeRangeButton,
                upgradeProjectileSpeedButton, upgradeProjectileRangeButton
            };
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    TextMeshProUGUI buttonText = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = originalButtonTexts[i];
                    }
                }
            }
        }
    }
    
    private void DisplayPlayerStats()
    {
        if (currentPlayerStats == null)
        {
            Debug.LogWarning("No player stats available to display");
            return;
        }
        
        // Update the combined stats text
        if (playerStatsText != null)
        {
            playerStatsText.text = GenerateStatsText();
        }
    }
    
    private string GenerateStatsText()
    {
        if (currentPlayerStats == null) return "No stats available";
        
        return $"Level: {currentPlayerStats.level}\n\n" +
               $"Experience: {currentPlayerStats.experience:F0} / {currentPlayerStats.experienceToNextLevel:F0}\n\n" +
               $"Max Health: {currentPlayerStats.maxHealth:F0}\n\n" +
               $"Health Regen: {currentPlayerStats.healthRegen:F1}/sec\n\n" +
               $"Move Speed: {currentPlayerStats.moveSpeed:F1}\n\n" +
               $"Attack Damage: {currentPlayerStats.attackDamage:F1}\n\n" +
               $"Attack Speed: {currentPlayerStats.attackSpeed:F1}\n\n" +
               $"Melee Range: {currentPlayerStats.meleeRange:F1}\n\n" +
               $"Projectile Speed: {currentPlayerStats.projectileSpeed:F1}\n\n" +
               $"Projectile Range: {currentPlayerStats.projectileRange:F1}";
    }
    
    private void UpdateUpgradePointsDisplay()
    {
        if (upgradePointsText != null)
        {
            upgradePointsText.text = $"Upgrade Points: {availableUpgradePoints}";
        }
    }
    
    private void UpdateButtonStates()
    {
        bool hasPoints = availableUpgradePoints > 0;
        
        // Enable/disable all upgrade buttons based on available points
        Button[] buttons = {
            upgradeHealthButton, upgradeDamageButton, upgradeSpeedButton, 
            upgradeAttackSpeedButton, upgradeHealthRegenButton, upgradeMeleeRangeButton,
            upgradeProjectileSpeedButton, upgradeProjectileRangeButton
        };
        
        // This should fix the attack speed button issue by treating all buttons the same way
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.interactable = hasPoints;
            }
        }
    }
    
    // Method to upgrade a specific stat
    public void UpgradeStat(string statName, float amount)
    {
        if (currentPlayerStats == null || availableUpgradePoints <= 0) return;
        
        // Spend an upgrade point through GameManager
        if (GameManager.Instance != null && GameManager.Instance.SpendUpgradePoint())
        {
            availableUpgradePoints--; // Update local copy
            
            switch (statName.ToLower())
            {
                case "health":
                    currentPlayerStats.maxHealth += amount;
                    Debug.Log($"Upgraded Max Health by {amount}. New value: {currentPlayerStats.maxHealth}");
                    break;
                case "damage":
                    currentPlayerStats.attackDamage += amount;
                    Debug.Log($"Upgraded Attack Damage by {amount}. New value: {currentPlayerStats.attackDamage}");
                    break;
                case "speed":
                    currentPlayerStats.moveSpeed += amount;
                    Debug.Log($"Upgraded Move Speed by {amount}. New value: {currentPlayerStats.moveSpeed}");
                    break;
                case "attackspeed":
                    currentPlayerStats.attackSpeed += amount;
                    Debug.Log($"Upgraded Attack Speed by {amount}. New value: {currentPlayerStats.attackSpeed}");
                    break;
                case "healthregen":
                    currentPlayerStats.healthRegen += amount;
                    Debug.Log($"Upgraded Health Regen by {amount}. New value: {currentPlayerStats.healthRegen}");
                    break;
                case "meleerange":
                    currentPlayerStats.meleeRange += amount;
                    Debug.Log($"Upgraded Melee Range by {amount}. New value: {currentPlayerStats.meleeRange}");
                    break;
                case "projectilespeed":
                    currentPlayerStats.projectileSpeed += amount;
                    Debug.Log($"Upgraded Projectile Speed by {amount}. New value: {currentPlayerStats.projectileSpeed}");
                    break;
                case "projectilerange":
                    currentPlayerStats.projectileRange += amount;
                    Debug.Log($"Upgraded Projectile Range by {amount}. New value: {currentPlayerStats.projectileRange}");
                    break;
                default:
                    Debug.LogWarning($"Unknown stat name: {statName}");
                    // Refund the point if stat name is invalid
                    if (GameManager.Instance != null)
                    {
                        // This would need a refund method in GameManager
                        availableUpgradePoints++;
                    }
                    break;
            }
            
            // Refresh the display after upgrade
            UpdateUI();
        }
        else
        {
            Debug.LogWarning("Cannot upgrade: No upgrade points available or GameManager not found");
        }
    }
    
    private void OnContinueClicked()
    {
        Debug.Log("Continue button clicked");
        
        // This will transition back to the main game with the next wave
        if (GameManager.Instance != null)
        {
            Debug.Log("GameManager found, calling ContinueToNextWave");
            GameManager.Instance.ContinueToNextWave();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null! Attempting fallback...");
            // Fallback if GameManager instance is not available
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
        }
    }
}