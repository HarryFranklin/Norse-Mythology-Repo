using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicSpriteSorter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Tooltip("An offset to adjust the pivot point for sorting. For a tree, you'd want this to be negative to put the pivot at its base.")]
    [SerializeField] private float sortingOrderOffset = 0f;

    [Tooltip("Higher numbers make the sorting more granular but can lead to very large sorting order numbers.")]
    [SerializeField] private int precisionMultiplier = 100;

    void Awake()
    {
        // Get the SpriteRenderer component attached to this GameObject.
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        // Calculate the new sorting order.
        // We multiply by a negative number so that a higher Y-position (further up the screen)
        // results in a lower sorting order (drawn further back).
        float newYPosition = transform.position.y + sortingOrderOffset;
        spriteRenderer.sortingOrder = -(int)(newYPosition * precisionMultiplier);
    }

    // This is a helper function to draw a visual guide in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 pivotPosition = transform.position + new Vector3(0, sortingOrderOffset, 0);
        Gizmos.DrawLine(pivotPosition - Vector3.right * 0.25f, pivotPosition + Vector3.right * 0.25f);
    }
}