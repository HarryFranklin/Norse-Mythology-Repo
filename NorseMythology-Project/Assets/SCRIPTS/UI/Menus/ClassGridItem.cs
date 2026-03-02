using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ClassGridItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public CharacterClass myClass;
    private ClassSelectionManager manager;

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image borderImage;
    [SerializeField] private GameObject selectedHighlight;

    [Header("Selection Visuals")]
    [SerializeField] private bool scaleIconOnSelect = false; // Disable this to stop the squashing
    [SerializeField] private Vector2 selectedSize = new Vector2(110, 110);
    [SerializeField] private Vector2 deselectedSize = new Vector2(100, 100);

    public void Setup(CharacterClass c, ClassSelectionManager m)
{
    myClass = c;
    manager = m;

    if (nameText) nameText.text = c.className;
    
    if (iconImage != null)
    {
        if (c.classSprite != null)
        {
            iconImage.sprite = c.classSprite;
            iconImage.preserveAspect = true; // Keeps the pixel art proportions
            iconImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"Sprite missing for class: {c.className}");
            iconImage.gameObject.SetActive(false);
        }
    }

    UpdateVisuals(false);
}

    public void OnPointerEnter(PointerEventData eventData) => manager.OnHoverEnter(myClass);
    public void OnPointerExit(PointerEventData eventData) => manager.OnHoverExit();
    public void OnPointerClick(PointerEventData eventData) => manager.SelectClass(myClass);

    public void UpdateVisuals(bool isSelected)
    {
        if (selectedHighlight) selectedHighlight.SetActive(isSelected);
        
        if (borderImage) 
            borderImage.color = isSelected ? Color.yellow : Color.white;

        // Only scale the icon if explicitly enabled in inspector
        if (scaleIconOnSelect && iconImage != null)
        {
            iconImage.rectTransform.sizeDelta = isSelected ? selectedSize : deselectedSize;
        }
    }
}