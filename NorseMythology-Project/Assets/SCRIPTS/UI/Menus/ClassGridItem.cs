using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ClassGridItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CharacterClass myClass;
    private ClassSelectionManager manager;

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    public void Setup(CharacterClass c, ClassSelectionManager m)
    {
        myClass = c;
        manager = m;

        if (nameText != null) nameText.text = c.className;
        
        if (iconImage != null && c.classSprite != null)
        {
            iconImage.sprite = c.classSprite;
            iconImage.preserveAspect = true;
            iconImage.gameObject.SetActive(true);
        }
    }

    // We still need these to trigger the comparison panel updates
    public void OnPointerEnter(PointerEventData eventData) 
    {
        if (manager != null) manager.OnHoverEnter(myClass);
    }

    public void OnPointerExit(PointerEventData eventData) 
    {
        if (manager != null) manager.OnHoverExit();
    }

    // Link this to the Button's OnClick() event in the Inspector
    public void OnClick() 
    {
        if (manager != null) manager.SelectClass(myClass);
    }

    // Keep for manager compatibility, but logic is handled by Button Transition
    public void UpdateVisuals(bool isSelected) { }
}