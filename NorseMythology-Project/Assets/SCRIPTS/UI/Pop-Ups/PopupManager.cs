using UnityEngine;
using UnityEngine.UI;

public class PopupManager : MonoBehaviour
{
    [Header("Popup Settings")]
    public GameObject popupTextPrefab;
    public Canvas uiCanvas;
    public Camera mainCamera;

    [Header("Popup Offset")]
    public Vector3 popupOffset = new Vector3(-2, 5, 0); // Offset to position the popup above the entity

    [Header("Default Settings")]
    public float defaultDuration = 1.5f;
    public float defaultMoveDistance = 50f;
    public int defaultFontSize = 28;

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
                    Debug.LogError("PopupManager not found in scene! Make sure there's a PopupManager in your scene.");
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
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Auto-find components if not assigned
        if (uiCanvas == null)
            uiCanvas = GetComponent<Canvas>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public void ShowPopup(string text, Vector3 worldPosition, Color color, int fontSize = 0, float duration = 0f, float moveDistance = 0f)
    {
        if (popupTextPrefab == null || uiCanvas == null || mainCamera == null)
        {
            Debug.LogError("PopupManager: Missing required components (prefab, canvas, or camera)!");
            return;
        }

        // Use default values if not specified
        if (fontSize <= 0) fontSize = defaultFontSize;
        if (duration <= 0) duration = defaultDuration;
        if (moveDistance <= 0) moveDistance = defaultMoveDistance;

        // Adjust world position with offset
        worldPosition += popupOffset;

        // Convert world position to screen space
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        // Create popup
        GameObject popupObj = Instantiate(popupTextPrefab, uiCanvas.transform);
        popupObj.transform.position = screenPosition;

        // Initialise the popup
        PopupText popup = popupObj.GetComponent<PopupText>();
        if (popup != null)
        {
            popup.Initialise(text, color, fontSize, duration, moveDistance);
        }
        else
        {
            Debug.LogError("PopupText component not found on popup prefab!");
            Destroy(popupObj);
        }
    }

    // Convenience methods for common popup types
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
        ShowPopup($"+{regenAmount:F1}", worldPosition, regenColor, 20); // Slightly smaller for regen
    }

    public void ShowCustom(string text, Vector3 worldPosition, Color color)
    {
        ShowPopup(text, worldPosition, color);
    }
}

// Example usage in an entity script
// Basic usage anywhere in your code:
// PopupManager.Instance.ShowDamage(25f, enemy.transform.position);
// PopupManager.Instance.ShowHeal(15f, player.transform.position);
// PopupManager.Instance.ShowXP(100f, player.transform.position);

// Custom popup:
// PopupManager.Instance.ShowPopup("Critical Hit!", 
//     enemy.transform.position, 
//     Color.magenta, 
//     fontSize: 32, 
//     duration: 2f, 
//     moveDistance: 80f);