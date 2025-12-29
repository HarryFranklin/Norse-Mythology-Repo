using UnityEngine;
using UnityEngine.UI; // Needed for Image
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Controls Panel")]
    [SerializeField] private RectTransform controlsPanel;
    [Tooltip("The black Image covering the controls content.")]
    [SerializeField] private Image controlsCurtain;

    [Header("Main Panel")]
    [SerializeField] private RectTransform mainPanel;
    [SerializeField] private Image mainCurtain;

    [Header("Options Panel")]
    [SerializeField] private RectTransform optionsPanel;
    [SerializeField] private Image optionsCurtain;

    [Header("Settings")]
    [SerializeField] private float slideDistance = 1088f; 
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Fade Settings")]
    [Tooltip("0.9 means the curtain stays opaque until the last 10% of the movement.")]
    [SerializeField, Range(0f, 0.99f)] private float fadeStartPercentage = 0.9f;

    private float controlsStartX, mainStartX, optionsStartX;
    private Coroutine currentSlideRoutine;

    private void Start()
    {
        if (controlsPanel) controlsStartX = controlsPanel.anchoredPosition.x;
        if (mainPanel)     mainStartX = mainPanel.anchoredPosition.x;
        if (optionsPanel)  optionsStartX = optionsPanel.anchoredPosition.x;

        UpdateAllCurtains();
    }

    public void OnStartNewGameClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
    }
    public void OnQuitGameClicked() => Application.Quit();
    public void OnOptionsClicked() => SlideToOffset(-slideDistance);
    public void OnControlsClicked() => SlideToOffset(slideDistance);
    public void OnBackClicked() => SlideToOffset(0);

    private void SlideToOffset(float targetOffsetX)
    {
        if (currentSlideRoutine != null) StopCoroutine(currentSlideRoutine);
        currentSlideRoutine = StartCoroutine(SlideRoutine(targetOffsetX));
    }

    private IEnumerator SlideRoutine(float targetOffset)
    {
        float currentOffset = mainPanel.anchoredPosition.x - mainStartX;
        float time = 0f;

        while (time < slideDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = slideCurve.Evaluate(time / slideDuration);
            float frameOffset = Mathf.Lerp(currentOffset, targetOffset, t);

            // Move Panels
            UpdatePanelPos(controlsPanel, controlsStartX, frameOffset);
            UpdatePanelPos(mainPanel, mainStartX, frameOffset);
            UpdatePanelPos(optionsPanel, optionsStartX, frameOffset);

            // Update Curtain Transparency
            UpdateAllCurtains();

            yield return null;
        }

        UpdatePanelPos(controlsPanel, controlsStartX, targetOffset);
        UpdatePanelPos(mainPanel, mainStartX, targetOffset);
        UpdatePanelPos(optionsPanel, optionsStartX, targetOffset);
        UpdateAllCurtains();

        currentSlideRoutine = null;
    }

    private void UpdatePanelPos(RectTransform panel, float startX, float offset)
    {
        Vector2 pos = panel.anchoredPosition;
        pos.x = startX + offset;
        panel.anchoredPosition = pos;
    }

    private void UpdateAllCurtains()
    {
        UpdateCurtainImage(controlsPanel, controlsCurtain);
        UpdateCurtainImage(mainPanel, mainCurtain);
        UpdateCurtainImage(optionsPanel, optionsCurtain);
    }

    private void UpdateCurtainImage(RectTransform panelRoot, Image curtain)
    {
        if (panelRoot == null || curtain == null) return;

        // 1. Calculate Distance from Center
        float dist = Mathf.Abs(panelRoot.anchoredPosition.x);
        
        // Calculate the fading range based on percentage
        float fadeRange = slideDistance * (1f - fadeStartPercentage);
        if (fadeRange < 1f) fadeRange = 1f;

        // 2. Calculate Alpha
        float alpha = Mathf.Clamp01(dist / fadeRange);

        // 3. Apply Color
        Color c = curtain.color;
        c.a = alpha;
        curtain.color = c;

        // 4. Curtain Blocking
        curtain.raycastTarget = alpha > 0.1f;

        // 5. Background Image Blocking
        Image backgroundImage = panelRoot.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = dist < 10f;
        }
    }
}