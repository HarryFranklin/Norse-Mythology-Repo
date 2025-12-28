using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicSpriteSorter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Header("Sorting Settings")]
    [Tooltip("If true, sorting only runs once at Start. Use this for Trees/Rocks.")]
    [SerializeField] private bool isStatic = true;

    [Tooltip("An offset to adjust the pivot point for sorting.")]
    [SerializeField] private float sortingOrderOffset = 0f;

    [Tooltip("Higher numbers make the sorting more granular.")]
    [SerializeField] private int precisionMultiplier = 100;

    [Header("Fading Logic")]
    [Tooltip("Is this object a character (like the player) that should NOT fade?")]
    [SerializeField] private bool isCharacter = false;

    [Tooltip("The target alpha value when the player is behind a prop.")]
    [SerializeField] [Range(0f, 1f)] private float fadeAmount = 0.25f;

    [Tooltip("How quickly the object fades in and out.")]
    [SerializeField] private float fadeSpeed = 8f;

    [Tooltip("The maximum distance from the player at which a prop will fade.")]
    [SerializeField] private float fadeDistance = 1f;

    [Tooltip("Shrinks the detection box. 1.0 = Full Sprite Size. 0.5 = Inner 50% only.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float fadeBoundsScale = 0.5f;

    private Transform playerTransform;
    private DynamicSpriteSorter playerSorter;
    private Coroutine fadeCoroutine;
    private bool isPlayerBehind = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (!isCharacter)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                playerSorter = playerObject.GetComponent<DynamicSpriteSorter>();
            }
        }
    }

    void Start()
    {
        if (isStatic) UpdateSorting();
    }

    void LateUpdate()
    {
        // 1. Dynamic Sorting (Only for moving objects)
        if (!isStatic) UpdateSorting();

        // 2. Fading Logic (Only for props)
        if (isCharacter || playerTransform == null || playerSorter == null) return;

        HandleFading();
    }

    private void UpdateSorting()
    {
        // Sorting always uses the PIVOT (Feet) so feet overlay correctly
        float newYPosition = transform.position.y + sortingOrderOffset;
        spriteRenderer.sortingOrder = -(int)(newYPosition * precisionMultiplier);
    }

    private void HandleFading()
    {
        // FIX: Use bounds.center (Visual Middle) instead of transform.position (Feet)
        Vector3 visualCenter = spriteRenderer.bounds.center;

        // Optimisation: Quick distance check from the Visual Center
        float rawDistSqr = (playerTransform.position - visualCenter).sqrMagnitude;
        
        // Calculate the max reach of the sprite from its center
        Vector3 extents = spriteRenderer.bounds.extents;
        float maxDimension = Mathf.Max(extents.x, extents.y);
        float checkDistance = fadeDistance + maxDimension;

        if (rawDistSqr > checkDistance * checkDistance)
        {
            if (isPlayerBehind)
            {
                isPlayerBehind = false;
                StartFade(1.0f);
            }
            return;
        }

        bool shouldBeBehind = false;

        // Get bounds and shrink them relative to the Visual Center
        Bounds checkBounds = spriteRenderer.bounds;
        checkBounds.extents *= fadeBoundsScale;

        // Find closest point on the shrunk box to the player
        Vector3 closestPointOnBounds = checkBounds.ClosestPoint(playerTransform.position);
        float distanceToInnerBounds = Vector2.Distance(closestPointOnBounds, playerTransform.position);

        if (distanceToInnerBounds <= fadeDistance)
        {
            // Check if prop is visually "below" the player (render order)
            shouldBeBehind = spriteRenderer.sortingOrder > playerSorter.spriteRenderer.sortingOrder;
        }

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
        Color currentColour = spriteRenderer.color;
        float currentAlpha = currentColour.a;

        while (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            spriteRenderer.color = new Color(currentColour.r, currentColour.g, currentColour.b, currentAlpha);
            yield return null;
        }

        spriteRenderer.color = new Color(currentColour.r, currentColour.g, currentColour.b, targetAlpha);
    }

    private void OnValidate()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSorting();
    }

    private void OnDrawGizmosSelected()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Draw Sorting Pivot (Blue line at feet)
        Gizmos.color = Color.cyan;
        Vector3 pivotPosition = transform.position + new Vector3(0, sortingOrderOffset, 0);
        Gizmos.DrawLine(pivotPosition - Vector3.right * 0.25f, pivotPosition + Vector3.right * 0.25f);

        if (!isCharacter && spriteRenderer != null)
        {
            // Draw Fading Bounds (Yellow Box from Visual Center)
            Bounds drawBounds = spriteRenderer.bounds;
            drawBounds.extents *= fadeBoundsScale;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(drawBounds.center, drawBounds.size);
            
            // Draw Center Point check
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(drawBounds.center, 0.1f);
        }
    }
}