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

    // World position tracking
    private Vector3 worldStartPosition;
    private Vector3 worldOffset;
    private Camera trackingCamera;
    private Color startColor;
    private bool isAnimating = false;

    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();
    }

    public void Initialise(string text, Color color, int fontSize, float animationDuration = 1.5f, float moveDistance = 50f, Vector3 worldPosition = default, Camera camera = null)
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

        // Store world position and camera for tracking
        worldStartPosition = worldPosition;
        worldOffset = Vector3.zero;
        trackingCamera = camera;
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

            // Calculate movement offset in world space
            float moveProgress = moveCurve.Evaluate(normalizedTime);
            worldOffset = Vector3.up * (moveDistance * moveProgress * 0.01f); // Scale down for world units

            // Update screen position based on current world position
            UpdateScreenPosition();

            elapsed += Time.deltaTime;
            yield return null;
        }

        isAnimating = false;
        Destroy(gameObject);
    }

    private void UpdateScreenPosition()
    {
        if (trackingCamera != null)
        {
            Vector3 currentWorldPos = worldStartPosition + worldOffset;
            Vector3 screenPosition = trackingCamera.WorldToScreenPoint(currentWorldPos);
            transform.position = screenPosition;
        }
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }

    // Method to update the world position if the tracked object moves
    public void UpdateWorldPosition(Vector3 newWorldPosition)
    {
        worldStartPosition = newWorldPosition;
    }
}