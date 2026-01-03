using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsMenuUI : MonoBehaviour
{
    public static OptionsMenuUI Instance { get; private set; }

    [Header("Settings Toggles")]
    public Toggle abilitySwapToggle;

    [Header("Description Panel")]
    [Tooltip("The background object for the description text.")]
    public GameObject infoPanel;
    
    [Tooltip("The actual text component.")]
    public TextMeshProUGUI descriptionText;

    [Header("Description Settings")]
    [TextArea] public string defaultDescription = "Hover over an option to see details.";

    private void Awake()
    {
        // Singleton setup so Triggers can find this script
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        // 1. Setup the Logic (Sync with OptionsManager)
        if (OptionsManager.Instance != null && abilitySwapToggle != null)
        {
            // Load saved value
            abilitySwapToggle.isOn = OptionsManager.Instance.EnableAbilitySwapping;
            
            // Listen for changes
            abilitySwapToggle.onValueChanged.AddListener(OnSwapToggleChanged);
        }

        // 2. Setup the Visuals (Clear text)
        ClearDescription();
    }

    // --- Logic Section ---

    public void OnSwapToggleChanged(bool value)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.SetAbilitySwapping(value);
        }
    }

    // --- Visuals Section (Called by Triggers) ---

    public void ShowDescription(string text)
    {
        if (descriptionText != null) descriptionText.text = text;
        if (infoPanel != null) infoPanel.SetActive(true);
    }

    public void ClearDescription()
    {
        if (!string.IsNullOrEmpty(defaultDescription))
        {
            // Show default text
            if (descriptionText != null) descriptionText.text = defaultDescription;
            if (infoPanel != null) infoPanel.SetActive(true);
        }
        else
        {
            // Hide everything
            if (descriptionText != null) descriptionText.text = "";
            if (infoPanel != null) infoPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (abilitySwapToggle != null)
        {
            abilitySwapToggle.onValueChanged.RemoveListener(OnSwapToggleChanged);
        }
    }
}