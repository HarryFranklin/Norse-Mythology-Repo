using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))] 
public class AbilityTooltipPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statsText; 

    [Header("Animation Settings")]
    [Tooltip("How fast the panel slides.")]
    [SerializeField] private float slideSpeed = 10f;

    [Header("Visible State (Fully Up)")]
    public float visibleTop = 200f;

    [Header("Hidden State (Collapsed)")]
    public float hiddenTop = 676f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 targetOffsetMin; 
    private Vector2 targetOffsetMax; 

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // Force anchors to Stretch/Stretch so offsets work correctly
        rectTransform.anchorMin = Vector2.zero; 
        rectTransform.anchorMax = Vector2.one;  
        rectTransform.pivot = new Vector2(0.5f, 0f);

        // Start hidden
        SetTargetState(hiddenTop, 0f);
        rectTransform.offsetMin = targetOffsetMin;
        rectTransform.offsetMax = targetOffsetMax;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        // Smooth slide
        rectTransform.offsetMin = Vector2.Lerp(
            rectTransform.offsetMin, targetOffsetMin, Time.unscaledDeltaTime * slideSpeed);
            
        rectTransform.offsetMax = Vector2.Lerp(
            rectTransform.offsetMax, targetOffsetMax, Time.unscaledDeltaTime * slideSpeed);
    }

    public void ShowTooltip(Ability ability, int currentLevel)
    {
        if (ability == null) return;

        // 1. Use the Main Description from the Ability Asset
        if (descriptionText != null) 
            descriptionText.text = ability.description;
        
        // 2. Simple Placeholder for Stats
        if (statsText != null) 
        {
            statsText.text = "L1: ...\nL2: ...\nL3: ...\nL4: ...\nL5: ...";
        }

        // Slide Up
        SetTargetState(visibleTop, 0f);
    }

    public void HideTooltip()
    {
        // Slide Down
        SetTargetState(hiddenTop, 0f);
    }

    private void SetTargetState(float top, float bottom)
    {
        targetOffsetMin = new Vector2(0f, bottom);
        targetOffsetMax = new Vector2(0f, -top); 
    }
}