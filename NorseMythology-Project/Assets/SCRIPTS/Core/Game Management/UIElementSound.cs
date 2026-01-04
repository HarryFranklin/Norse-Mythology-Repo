using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIElementSound : MonoBehaviour, IPointerEnterHandler
{
    [Header("Overrides (Optional)")]
    [Tooltip("If null, uses Default Button Sound from AudioManager")]
    [SerializeField] private AudioClip customClickSound;
    
    [Tooltip("If null, uses Default Toggle Sound from AudioManager")]
    [SerializeField] private AudioClip customToggleSound;

    [Header("Hover")]
    [SerializeField] private AudioClip hoverSound;

    private Button myButton;
    private Toggle myToggle;
    private Slider mySlider;

    private void Start()
    {
        // Priority 1: Check for Toggle (More specific than Button)
        myToggle = GetComponent<Toggle>();
        if (myToggle != null)
        {
            myToggle.onValueChanged.AddListener(OnToggleChanged);
            return; // Found our component, stop looking!
        }

        // Priority 2: Check for Slider (futureproofing)
        mySlider = GetComponent<Slider>();
        if (mySlider != null)
        {
            // Logic for later...
            return; 
        }

        // Priority 3: Check for Button (Most generic and catch-all for all buttons)
        myButton = GetComponent<Button>();
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnButtonClick);
            return;
        }
    }

    private void OnButtonClick()
    {
        // Use custom clip if exists, otherwise default
        if (customClickSound != null)
            AudioManager.Instance.PlaySFX(customClickSound);
        else
            AudioManager.Instance.PlayButtonSound();
    }

    private void OnToggleChanged(bool isOn)
    {
        // If we have a custom sound override, we just play that normally
        if (customToggleSound != null)
        {
            AudioManager.Instance.PlaySFX(customToggleSound);
        }
        else
        {
            // Otherwise, use the smart pitch-shifting logic in the manager
            AudioManager.Instance.PlayToggleSound(isOn);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        bool isInteractable = true;
        
        if (myButton) isInteractable = myButton.interactable;
        else if (myToggle) isInteractable = myToggle.interactable;
        else if (mySlider) isInteractable = mySlider.interactable;

        if (isInteractable && hoverSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound, 0.5f);
        }
    }
}