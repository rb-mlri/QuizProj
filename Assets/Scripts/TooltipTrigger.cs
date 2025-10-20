using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea] public string tooltipMessage;

    public void OnPointerEnter(PointerEventData eventData)
    {
        FindObjectOfType<MenuManager>().ShowTooltip(tooltipMessage);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        FindObjectOfType<MenuManager>().HideTooltip();
    }
}
