using UnityEngine;
using TMPro;
using System.Collections;

public class WaveAnnouncementUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign the Panel object here. MUST have a CanvasGroup component attached!")]
    [SerializeField] private GameObject panel; 
    [SerializeField] private TextMeshProUGUI titleText; 
    [SerializeField] private TextMeshProUGUI objectiveText;

    [Header("Timing Settings")]
    public float fadeInTime = 1.0f;
    public float displayTime = 2.0f;
    public float fadeOutTime = 1.0f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            panel.SetActive(false);
        }
    }

    public IEnumerator ShowWaveStart(int waveNumber, string objective)
    {
        if (panel == null) yield break;

        titleText.text = $"WAVE {waveNumber}";
        objectiveText.text = objective;

        yield return StartCoroutine(AnimatePanel());
    }

    public IEnumerator ShowWaveCompleted()
    {
        if (panel == null) yield break;

        titleText.text = "WAVE COMPLETED";
        objectiveText.text = "Well fought";

        yield return StartCoroutine(AnimatePanel());
    }

    private IEnumerator AnimatePanel()
    {
        panel.SetActive(true);
        canvasGroup.alpha = 0f;

        // 1. Fade In (Using Unscaled Time)
        yield return StartCoroutine(FadeRoutine(0f, 1f, fadeInTime));

        // 2. Display Wait (Using Realtime)
        yield return new WaitForSecondsRealtime(displayTime);

        // 3. Fade Out (Using Unscaled Time)
        yield return StartCoroutine(FadeRoutine(1f, 0f, fadeOutTime));

        panel.SetActive(false);
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Use unscaledDeltaTime so this runs smoothly even during slow-mo
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(elapsed / duration);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }
}