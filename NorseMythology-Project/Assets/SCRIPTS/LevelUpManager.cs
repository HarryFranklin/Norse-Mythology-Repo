using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUpManager : MonoBehaviour
{
    [Header("UI References")]
    public Button continueButton;
    public TextMeshProUGUI waveCompletedText;
    public TextMeshProUGUI nextWaveText;
    public TextMeshProUGUI playerStatsText;
    
    [Header("Player Stats Display")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI damageText;
    // Add more stat displays as needed
    
    private void Start()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {

    }
    
    private void DisplayPlayerStats()
    {
        
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