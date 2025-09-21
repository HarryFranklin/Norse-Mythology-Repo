using UnityEngine;
using TMPro;
using System.Collections;

public class PopupText : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI textComponent;

    [Header("Animation Settings")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    public AnimationCurve moveCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    private Vector3 startPosition;
    private Color startColor;
    private float duration;
    private float moveDistance;

    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();
    }

    public void Initialise(string text, Color color, float fontSize, float animationDuration, float moveDistance)
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
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;

            // Apply fade
            float alpha = fadeCurve.Evaluate(normalizedTime);
            Color currentColor = startColor;
            currentColor.a = alpha;
            textComponent.color = currentColor;

            // Apply movement (directly in world space)
            float moveProgress = moveCurve.Evaluate(normalizedTime);
            Vector3 currentPosition = startPosition + Vector3.up * (moveDistance * moveProgress);
            transform.position = currentPosition;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}