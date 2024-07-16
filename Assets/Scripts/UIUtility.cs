using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class UIUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }

    ///Returns 'true' if we touched or hovering on Unity UI element.
    ///
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                var mouseOverUI = curRaysastResult.gameObject.tag == "BuildingDialog" ||
                curRaysastResult.gameObject.tag == "StopCursorInteractions";
//#if UNITY_EDITOR
//                UnityEngine.Debug.Log("UI Element: " + curRaysastResult.gameObject.name + " Mouse Over: " + mouseOverUI);
//#endif

                return mouseOverUI;
            }
        }
        return false;
    }

    ///Gets all event systen raycast results of current mouse or touch position.
    ///
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}
