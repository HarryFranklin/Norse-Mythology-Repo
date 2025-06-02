using UnityEngine;
using UnityEngine.UI;

public class PopupManager : MonoBehaviour
{
    [Header("Popup Settings")]
    public GameObject popupTextPrefab;
    public Canvas uiCanvas;
    public Camera mainCamera;

    [Header("Popup Offset")]
    public Vector3 popupOffset = new Vector3(0, 2, 0); // Offset in world units above the entity

    [Header("Default Settings")]
    public float defaultDuration = 1.5f;
    public float defaultMoveDistance = 1f; // World units for movement
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

        // Apply offset to world position
        Vector3 offsetWorldPosition = worldPosition + popupOffset;

        // Create popup
        GameObject popupObj = Instantiate(popupTextPrefab, uiCanvas.transform);

        // Initialize the popup with world position tracking
        PopupText popup = popupObj.GetComponent<PopupText>();
        if (popup != null)
        {
            popup.Initialise(text, color, fontSize, duration, moveDistance, offsetWorldPosition, mainCamera);
        }
        else
        {
            Debug.LogError("PopupText component not found on popup prefab!");
            Destroy(popupObj);
        }
    }

    // Overload for tracking a specific Transform (if you want the popup to follow a moving object)
    public PopupText ShowPopupTracking(string text, Transform targetTransform, Color color, int fontSize = 0, float duration = 0f, float moveDistance = 0f)
    {
        if (popupTextPrefab == null || uiCanvas == null || mainCamera == null)
        {
            Debug.LogError("PopupManager: Missing required components (prefab, canvas, or camera)!");
            return null;
        }

        // Use default values if not specified
        if (fontSize <= 0) fontSize = defaultFontSize;
        if (duration <= 0) duration = defaultDuration;
        if (moveDistance <= 0) moveDistance = defaultMoveDistance;

        // Create popup
        GameObject popupObj = Instantiate(popupTextPrefab, uiCanvas.transform);

        // Initialize the popup with world position tracking
        PopupText popup = popupObj.GetComponent<PopupText>();
        if (popup != null)
        {
            Vector3 offsetWorldPosition = targetTransform.position + popupOffset;
            popup.Initialise(text, color, fontSize, duration, moveDistance, offsetWorldPosition, mainCamera);
            
            // Start tracking the transform
            StartCoroutine(TrackTransform(popup, targetTransform));
            return popup;
        }
        else
        {
            Debug.LogError("PopupText component not found on popup prefab!");
            Destroy(popupObj);
            return null;
        }
    }

    private System.Collections.IEnumerator TrackTransform(PopupText popup, Transform target)
    {
        while (popup != null && popup.IsAnimating() && target != null)
        {
            popup.UpdateWorldPosition(target.position + popupOffset);
            yield return null;
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

    // Convenience methods for tracking moving objects
    public void ShowDamageTracking(float damage, Transform target)
    {
        ShowPopupTracking($"-{damage:F0}", target, damageColor);
    }

    public void ShowHealTracking(float healAmount, Transform target)
    {
        ShowPopupTracking($"+{healAmount:F0}", target, healColor);
    }

    public void ShowXPTracking(float xpAmount, Transform target)
    {
        ShowPopupTracking($"+{xpAmount:F0} XP", target, xpColor);
    }

    public void ShowRegenTracking(float regenAmount, Transform target)
    {
        ShowPopupTracking($"+{regenAmount:F1}", target, regenColor, 20);
    }

    // Overloaded methods that accept Vector3 positions and convert them
    public void ShowDamageTracking(float damage, Vector3 worldPosition)
    {
        ShowPopup($"-{damage:F0}", worldPosition, damageColor);
    }

    public void ShowHealTracking(float healAmount, Vector3 worldPosition)
    {
        ShowPopup($"+{healAmount:F0}", worldPosition, healColor);
    }

    public void ShowXPTracking(float xpAmount, Vector3 worldPosition)
    {
        ShowPopup($"+{xpAmount:F0} XP", worldPosition, xpColor);
    }

    public void ShowRegenTracking(float regenAmount, Vector3 worldPosition)
    {
        ShowPopup($"+{regenAmount:F1}", worldPosition, regenColor, 20);
    }
}

// Example usage:
// For static position (popup stays relative to world position):
// PopupManager.Instance.ShowDamage(25f, enemy.transform.position);

// For tracking a moving object:
// PopupManager.Instance.ShowDamageTracking(25f, enemy.transform);