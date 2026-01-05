using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsMenuUI : MonoBehaviour
{
    public static OptionsMenuUI Instance { get; private set; }

    [Header("Settings Toggles")]
    public Toggle abilitySwapToggle;

    [Header("Audio Settings")]
    // Added Slider reference
    public Slider volumeSlider;

    [Header("Description Panel")]
    [Tooltip("The background object for the description text.")]
    public GameObject infoPanel;
    
    [Tooltip("The actual text component.")]
    public TextMeshProUGUI descriptionText;

    [Header("Description Settings")]
    [TextArea] public string defaultDescription = "Hover over an option to see details.";

    private void Awake()
    {
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
        // 1. Sync Ability Swap
        if (OptionsManager.Instance != null && abilitySwapToggle != null)
        {
            abilitySwapToggle.isOn = OptionsManager.Instance.EnableAbilitySwapping;
            abilitySwapToggle.onValueChanged.AddListener(OnSwapToggleChanged);
        }

        // 2. Sync Volume Slider
        if (OptionsManager.Instance != null && volumeSlider != null)
        {
            // Set slider position to match current volume
            volumeSlider.value = OptionsManager.Instance.MasterVolume;
            
            // Listen for player dragging the handle
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

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

    // Called when slider moves
    public void OnVolumeChanged(float value)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.SetMasterVolume(value);
        }
    }

    // --- Visuals Section ---

    public void ShowDescription(string text)
    {
        if (descriptionText != null) descriptionText.text = text;
        if (infoPanel != null) infoPanel.SetActive(true);
    }

    public void ClearDescription()
    {
        if (!string.IsNullOrEmpty(defaultDescription))
        {
            if (descriptionText != null) descriptionText.text = defaultDescription;
            if (infoPanel != null) infoPanel.SetActive(true);
        }
        else
        {
            if (descriptionText != null) descriptionText.text = "";
            if (infoPanel != null) infoPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (abilitySwapToggle != null)
            abilitySwapToggle.onValueChanged.RemoveListener(OnSwapToggleChanged);
            
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }
}