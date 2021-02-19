using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TownHouseScrollButton : Button
{
    public bool IsPointerDown { get; private set; }
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        IsPointerDown = true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        IsPointerDown = false;
    }
}
