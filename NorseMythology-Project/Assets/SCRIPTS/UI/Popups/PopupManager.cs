using UnityEngine;
using UnityEngine.UI;

public class PopupManager : MonoBehaviour
{
    [Header("Popup Settings")]
    public GameObject popupTextPrefab;
    public Canvas canvas; // Set this to World Space in inspector

    [Header("Canvas Scale Settings")]
    [Tooltip("Scale factor for the world space canvas - adjust based on your camera setup")]
    public float canvasScale = 0.01f; // Start with 0.01 for typical 2D setups

    [Header("Popup Offset")]
    public Vector3 popupOffset = new Vector3(0, 1, 0); // Offset in world units

    [Header("Default Settings")]
    public float defaultDuration = 1.5f;
    public float defaultMoveDistance = 2f; // In world units
    public float defaultFontSize = 36f; // Will be scaled by canvas

    [Header("Preset Colors")]
    public Color damageColor = Color.red;
    public Color healColor = Color.green;
    public Color xpColor = Color.yellow;
    public Color regenColor = Color.cyan;

    private static PopupManager instance;
    public static PopupManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PopupManager>();
                if (instance == null)
                {
                    Debug.LogError("PopupManager not found in scene!");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            SetupCanvas();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void SetupCanvas()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (canvas != null)
        {
            // Ensure canvas is set to World Space
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Set the scale
            canvas.transform.localScale = Vector3.one * canvasScale;
            
            // Position at origin
            canvas.transform.position = Vector3.zero;
            
            // Set sorting layer to ensure it renders above game objects
            // Use the Inspector value or a specific layer like "POPUP" or "UI"
            // canvas.sortingLayerName = "UI"; 
            canvas.sortingOrder = 100;

            Debug.Log($"World Space Canvas setup complete. Scale: {canvasScale}");
        }
    }

    public void ShowPopup(string text, Vector3 worldPosition, Color color, float fontSize = 0f, float duration = 0f, float moveDistance = 0f)
    {
        if (popupTextPrefab == null || canvas == null)
        {
            Debug.LogError("PopupManager: Missing required components!");
            return;
        }

        // Use default values if not specified
        if (fontSize <= 0) fontSize = defaultFontSize;
        if (duration <= 0) duration = defaultDuration;
        if (moveDistance <= 0) moveDistance = defaultMoveDistance;

        // Apply offset to world position
        Vector3 finalPosition = worldPosition + popupOffset;

        // Create popup directly at world position
        GameObject popupObj = Instantiate(popupTextPrefab, canvas.transform);
        popupObj.transform.position = finalPosition;

        // Initialise the popup
        PopupText popup = popupObj.GetComponent<PopupText>();
        if (popup != null)
        {
            popup.Initialise(text, color, fontSize, duration, moveDistance);
        }
        else
        {
            Debug.LogError("WorldSpacePopupText component not found on popup prefab!");
            Destroy(popupObj);
        }
    }

    // Convenience methods
    public void ShowDamage(float damage, Vector3 worldPosition)
    {
        ShowPopup($"-{damage:F0}", worldPosition, damageColor);
    }

    public void ShowHeal(float healAmount, Vector3 worldPosition)
    {
        ShowPopup($"+{healAmount:F0}", worldPosition, healColor);
    }

    public void ShowXP(float xpAmount, Vector3 worldPosition)
    {
        ShowPopup($"+{xpAmount:F0} XP", worldPosition, xpColor);
    }

    public void ShowRegen(float regenAmount, Vector3 worldPosition)
    {
        // Changed to F0 to show as Integer (e.g., +1 instead of +1.0)
        ShowPopup($"+{regenAmount:F0}", worldPosition, regenColor, 24f); 
    }

    public void ShowLevelUp(int level, Vector3 worldPosition)
    {
        ShowPopup($"LEVEL {level}!", worldPosition, Color.magenta, 48f, 2f, 3f);
    }

    // Method to adjust canvas scale at runtime if needed
    public void SetCanvasScale(float newScale)
    {
        canvasScale = newScale;
        if (canvas != null)
        {
            canvas.transform.localScale = Vector3.one * canvasScale;
        }
    }
}