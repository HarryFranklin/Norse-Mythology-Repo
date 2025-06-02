using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PopupText : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI textComponent;

    [Header("Animation Settings")]
    public float duration = 1.5f;
    public float moveDistance = 50f;

    // Use smooth fade-out
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    // Simulated ease-out: fast at the beginning, slow at the end
    public AnimationCurve moveCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    private Vector3 startPosition;
    private Color startColor;
    private bool isAnimating = false;

    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();
    }

    public void Initialise(string text, Color color, int fontSize, float animationDuration = 1.5f, float moveDistance = 50f)
    {
        if (textComponent == null)
        {
            Debug.LogError("PopupText: Text component not found!");
            return;
        }

        textComponent.text = text;
        textComponent.color = color;
        textComponent.fontSize = fontSize;

        this.duration = animationDuration;
        this.moveDistance = moveDistance;

        startPosition = transform.position;
        startColor = color;

        StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        isAnimating = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;

            // Apply fade curve
            float alpha = fadeCurve.Evaluate(normalizedTime);
            Color currentColor = startColor;
            currentColor.a = alpha;
            textComponent.color = currentColor;

            // Apply movement curve
            float moveProgress = moveCurve.Evaluate(normalizedTime);
            Vector3 currentPosition = startPosition + Vector3.up * (moveDistance * moveProgress);
            transform.position = currentPosition;

            elapsed += Time.deltaTime;
            yield return null;
        }

        isAnimating = false;
        Destroy(gameObject);
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }
}
