using UnityEngine;
using UnityEngine.EventSystems;

public class OptionTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [TextArea(3, 10)] 
    public string description;

    public void OnPointerEnter(PointerEventData eventData) => UpdateTooltip();
    public void OnPointerExit(PointerEventData eventData) => ClearTooltip();
    public void OnSelect(BaseEventData eventData) => UpdateTooltip();
    public void OnDeselect(BaseEventData eventData) => ClearTooltip();

    private void UpdateTooltip()
    {
        if (OptionsMenuUI.Instance != null)
        {
            OptionsMenuUI.Instance.ShowDescription(description);
        }
    }

    private void ClearTooltip()
    {
        if (OptionsMenuUI.Instance != null)
        {
            OptionsMenuUI.Instance.ClearDescription();
        }
    }
}