using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ClassGridItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image classIconImage;
    [SerializeField] private Image highlightImage;
    [SerializeField] private TextMeshProUGUI classNameText;

    [Header("Selection Visuals")]
    [SerializeField] private Vector2 selectedSize = new Vector2(110, 110);
    [SerializeField] private Vector2 deselectedSize = new Vector2(100, 100);

    public CharacterClass CharacterClass { get; private set; }
    private ClassSelectorUI selectorUI;
    private RectTransform iconRectTransform;

    public void Initialise(CharacterClass classData, ClassSelectorUI uiController)
    {
        CharacterClass = classData;
        selectorUI = uiController;

        if (classIconImage != null)
        {
            classIconImage.sprite = CharacterClass.classSprite;
            classIconImage.preserveAspect = true; 
            
            iconRectTransform = classIconImage.GetComponent<RectTransform>();
        }
        if (classNameText != null)
        {
            classNameText.text = CharacterClass.className;
        }

        GetComponent<Button>().onClick.AddListener(OnSelect);
        UpdateHighlight(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        selectorUI.DisplayClassInfo(CharacterClass);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        selectorUI.DisplayClassInfo(selectorUI.selectedClass);
    }

    private void OnSelect()
    {
        selectorUI.SelectClass(CharacterClass);
    }

    public void UpdateHighlight(bool isSelected)
    {
        if (highlightImage != null)
        {
            highlightImage.enabled = isSelected;
        }

        if (iconRectTransform != null)
        {
            iconRectTransform.sizeDelta = isSelected ? selectedSize : deselectedSize;
        }
    }
}