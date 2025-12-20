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
    public GameObject levelUpPanel;
    
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
    public bool showUpgradeAmountsOnHoverOnly = true;

    // --- NEW: Visual Feedback Settings ---
    [Header("Visual Feedback")]
    public Color normalTextColor = Color.white;
    public Color disabledTextColor = new Color(1f, 1f, 1f, 0.3f); 
    
    [Header("Ability Selector Reference")]
    public GameObject abilitySelectorScreen;
    
    [Header("Debug Settings")]
    [Tooltip("Force debug mode even if GameManager exists (useful for testing UI in main game).")]
    public bool forceDebugMode = false;

    private PlayerStats currentPlayerStats;
    private int availableUpgradePoints;
    
    private string[] originalButtonTexts;
    private float[] upgradeAmounts;
    private string[] statNames;
    
    private void Start()
    {
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (abilitySelectorScreen != null) abilitySelectorScreen.SetActive(true);

        InitialiseButtonData();
        SetupUpgradeButtons();
        GetPlayerStatsFromGameManager();
        UpdateUI();
    }
    
    private void InitialiseButtonData()
    {
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
                int index = i; 
                buttons[i].onClick.AddListener(() => UpgradeStat(statNames[index], upgradeAmounts[index]));
                if (showUpgradeAmountsOnHoverOnly) AddHoverEvents(buttons[i], index);
            }
        }
    }
    
    private void AddHoverEvents(Button button, int index)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => {
            // Only show hover text if button is interactable (has points)
            if (button.interactable)
            {
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = $"(+{FormatUpgradeAmount(upgradeAmounts[index])})"; 
            }
        });
        trigger.triggers.Add(pointerEnter);
        
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => {
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = originalButtonTexts[index];
        });
        trigger.triggers.Add(pointerExit);
    }
    
    private string FormatUpgradeAmount(float amount)
    {
        return (amount >= 1f) ? amount.ToString("F0") : amount.ToString("F1");
    }
    
    private void GetPlayerStatsFromGameManager()
    {
        if (GameManager.Instance == null || forceDebugMode)
        {
            Debug.LogWarning("LevelUpManager: Launching in Debug Mode with dummy stats.");
            GenerateDebugStats();
        }
        else
        {
            currentPlayerStats = GameManager.Instance.GetCurrentPlayerStats();
            availableUpgradePoints = GameManager.Instance.GetUpgradePoints();
        }
    }

    private void GenerateDebugStats()
    {
        currentPlayerStats = new PlayerStats
        {
            level = 10, experience = 500, experienceToNextLevel = 1000,
            maxHealth = 100, healthRegen = 1.5f, moveSpeed = 5.0f,
            attackDamage = 10f, attackSpeed = 1.0f, meleeRange = 2.0f,
            projectileSpeed = 15f, projectileRange = 10f
        };
        availableUpgradePoints = 10;
    }
    
    private void UpdateUI()
    {
        int waveNum = (WaveManager.Instance != null) ? WaveManager.Instance.GetCurrentWave() : 99;

        if (waveCompletedText != null) waveCompletedText.text = $"Wave {waveNum} Completed!";
        if (nextWaveText != null) nextWaveText.text = $"Prepare for Wave {waveNum + 1}";
        
        DisplayPlayerStats();
        UpdateUpgradePointsDisplay();
        UpdateButtonStates(); // This now handles colors
        UpdateButtonTexts();
    }
    
    private void UpdateButtonTexts()
    {
        Button[] buttons = {
            upgradeHealthButton, upgradeDamageButton, upgradeSpeedButton, 
            upgradeAttackSpeedButton, upgradeHealthRegenButton, upgradeMeleeRangeButton,
            upgradeProjectileSpeedButton, upgradeProjectileRangeButton
        };

        if (!showUpgradeAmountsOnHoverOnly)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    TextMeshProUGUI buttonText = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                        buttonText.text = $"{originalButtonTexts[i]} (+{FormatUpgradeAmount(upgradeAmounts[i])})";
                }
            }
        }
        else
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    TextMeshProUGUI buttonText = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null) buttonText.text = originalButtonTexts[i];
                }
            }
        }
    }
    
    private void DisplayPlayerStats()
    {
        if (currentPlayerStats == null) return;
        if (playerStatsText != null) playerStatsText.text = GenerateStatsText();
    }
    
    private string GenerateStatsText()
    {
        if (currentPlayerStats == null) return "No stats available";
        return $"Level: {currentPlayerStats.level}\n\n\n" +
               $"Experience: {currentPlayerStats.experience:F0} / {currentPlayerStats.experienceToNextLevel:F0}\n\n\n" +
               $"Max Health: {currentPlayerStats.maxHealth:F0}\n\n\n" +
               $"Health Regen: {currentPlayerStats.healthRegen:F1}/sec\n\n\n" +
               $"Move Speed: {currentPlayerStats.moveSpeed:F1}\n\n\n" +
               $"Attack Damage: {currentPlayerStats.attackDamage:F1}\n\n\n" +
               $"Attack Speed: {currentPlayerStats.attackSpeed:F1}\n\n\n" +
               $"Melee Range: {currentPlayerStats.meleeRange:F1}\n\n\n" +
               $"Projectile Speed: {currentPlayerStats.projectileSpeed:F1}\n\n\n" +
               $"Projectile Range: {currentPlayerStats.projectileRange:F1}";
    }
    
    private void UpdateUpgradePointsDisplay()
    {
        if (upgradePointsText != null) upgradePointsText.text = $"Upgrade Points: {availableUpgradePoints}";
    }
    
    private void UpdateButtonStates()
    {
        bool hasPoints = availableUpgradePoints > 0;
        
        Button[] buttons = {
            upgradeHealthButton, upgradeDamageButton, upgradeSpeedButton, 
            upgradeAttackSpeedButton, upgradeHealthRegenButton, upgradeMeleeRangeButton,
            upgradeProjectileSpeedButton, upgradeProjectileRangeButton
        };
        
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                // Disable the button interaction
                button.interactable = hasPoints;

                // Change Text Colour so it looks disabled
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.color = hasPoints ? normalTextColor : disabledTextColor;
                }
            }
        }
    }
    
    public void UpgradeStat(string statName, float amount)
    {
        if (currentPlayerStats == null || availableUpgradePoints <= 0) return;
        
        bool success = (GameManager.Instance != null && !forceDebugMode) ? 
                       GameManager.Instance.SpendUpgradePoint() : true;

        if (success)
        {
            availableUpgradePoints--; 
            
            switch (statName.ToLower())
            {
                case "health": currentPlayerStats.maxHealth += amount; break;
                case "damage": currentPlayerStats.attackDamage += amount; break;
                case "speed": currentPlayerStats.moveSpeed += amount; break;
                case "attackspeed": currentPlayerStats.attackSpeed += amount; break;
                case "healthregen": currentPlayerStats.healthRegen += amount; break;
                case "meleerange": currentPlayerStats.meleeRange += amount; break;
                case "projectilespeed": currentPlayerStats.projectileSpeed += amount; break;
                case "projectilerange": currentPlayerStats.projectileRange += amount; break;
                default: availableUpgradePoints++; break;
            }
            UpdateUI();
        }
    }
    
    private void ShowAbilitySelectorScreen()
    {
        if (abilitySelectorScreen != null)
        {
            levelUpPanel.SetActive(false);
            abilitySelectorScreen.SetActive(true);
        }
        else
        {
            Debug.LogError("AbilitySelectorManager reference is null! Please assign it in the inspector.");
        }
    }

    public void OnAbilitySelectionCompleted()
    {
        levelUpPanel.SetActive(true);
        if (continueButton != null)
        {
            continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue";
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(ContinueToNextWave);
        }
        UpdateUI();
    }

    private void ContinueToNextWave()
    {
        if (GameManager.Instance != null) GameManager.Instance.ContinueToNextWave();
        else Debug.Log("Debug Mode: Continue button clicked (No scene to load).");
    }
}