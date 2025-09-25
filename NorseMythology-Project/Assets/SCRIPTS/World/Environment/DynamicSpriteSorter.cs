using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicSpriteSorter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Tooltip("An offset to adjust the pivot point for sorting.")]
    [SerializeField] private float sortingOrderOffset = 0f;

    [Tooltip("Higher numbers make the sorting more granular.")]
    [SerializeField] private int precisionMultiplier = 100;

    [Header("Fading Logic")]
    [Tooltip("Is this object a character (like the player or an enemy) that should NOT fade?")]
    [SerializeField] private bool isCharacter = false;

    [Tooltip("The target alpha value when the player is behind a prop.")]
    [SerializeField] [Range(0f, 1f)] private float fadeAmount = 0.25f;

    [Tooltip("How quickly the object fades in and out.")]
    [SerializeField] private float fadeSpeed = 8f;

    [Tooltip("The maximum distance from the player at which a prop will fade.")]
    [SerializeField] private float fadeDistance = 5f;

    private Transform playerTransform;
    private DynamicSpriteSorter playerSorter;
    private Coroutine fadeCoroutine;
    private bool isPlayerBehind = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (!isCharacter) // Only props need to find the player
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                playerSorter = playerObject.GetComponent<DynamicSpriteSorter>();
            }
            else
            {
                Debug.LogWarning("DynamicSpriteSorter: Player object not found. Disabling fading for this object.", this);
            }
        }
    }

    void LateUpdate()
    {
        // --- Sorting Logic (runs for everyone) ---
        float newYPosition = transform.position.y + sortingOrderOffset;
        spriteRenderer.sortingOrder = -(int)(newYPosition * precisionMultiplier);

        // --- Fading Logic (skips characters and objects without a player reference) ---
        if (isCharacter || playerTransform == null || playerSorter == null)
        {
            return;
        }

        bool shouldBeBehind = false;
        // Check distance to player first for performance
        if (Vector2.Distance(transform.position, playerTransform.position) <= fadeDistance)
        {
            // A higher sortingOrder means it's rendered ON TOP (in front)
            shouldBeBehind = spriteRenderer.sortingOrder > playerSorter.spriteRenderer.sortingOrder;
        }

        // If the state has changed (e.g., player just moved behind or in front)
        if (shouldBeBehind != isPlayerBehind)
        {
            isPlayerBehind = shouldBeBehind;
            StartFade(isPlayerBehind ? fadeAmount : 1.0f);
        }
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(targetAlpha));
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        Color currentColor = spriteRenderer.color;
        float currentAlpha = currentColor.a;

        while (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            spriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, currentAlpha);
            yield return null;
        }

        spriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 pivotPosition = transform.position + new Vector3(0, sortingOrderOffset, 0);
        Gizmos.DrawLine(pivotPosition - Vector3.right * 0.25f, pivotPosition + Vector3.right * 0.25f);

        if(!isCharacter)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, fadeDistance);
        }
    }
}