using UnityEngine;
using System.Collections;

public class FadeAndDestroy : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] private bool willFade = true;
    [SerializeField] private float waitBeforeFade = 0.5f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Renderer objectRenderer;

    private void Start()
    {
        // Get the renderer (MeshRenderer or SpriteRenderer)
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer == null)
        {
            // If the script is on a parent, try finding the renderer in children
            objectRenderer = GetComponentInChildren<Renderer>();
        }

        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        // 1. Wait for the initial delay
        yield return new WaitForSeconds(waitBeforeFade);

        // 2. Prepare for fading
        if ((objectRenderer != null) && (willFade == true))
        {
            Color startColour = objectRenderer.material.color;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                
                // Calculate how far along we are (0.0 to 1.0)
                float progress = timer / fadeDuration;

                // Interpolate the Alpha (Transparency) from current to 0
                float currentAlpha = Mathf.Lerp(startColour.a, 0f, progress);

                // Apply the new colour
                Color newColour = startColour;
                newColour.a = currentAlpha;
                objectRenderer.material.color = newColour;

                yield return null; // Wait for next frame
            }
        }

        // 3. Destroy the object
        Destroy(gameObject);
    }
}