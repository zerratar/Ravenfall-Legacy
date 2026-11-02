using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class UIUtility
{
    /// <summary>
    /// True when a click at the mouse is meant for the interface, not the world: any UGUI element (the settings cog and
    /// other old canvases), any UI Toolkit element that takes clicks, a draggable window or the admin debug buttons.
    /// </summary>
    public static bool IsPointerOverAnyUI()
    {
        if (Shinobytes.UI.HudPointer.IsOverHud() || Shinobytes.UI.DraggableWindow.IsPointerOverWindow) return true;
        var mouse = Input.mousePosition;
        if (GameManager.DebugMenuArea.Contains(new Vector2(mouse.x, Screen.height - mouse.y))) return true;
        var events = EventSystem.current;
        if (!events) return false;
        var results = GetEventSystemRaycastResults();
        for (var i = 0; i < results.Count; ++i)
        {
            // a button or other control on a screen canvas; not a physics raycaster's hit (the world), a name tag over a
            // player or an invisible overlay image
            var hit = results[i];
            if (!(hit.module is UnityEngine.UI.GraphicRaycaster) || !hit.gameObject) continue;
            if (IsTagged(hit.gameObject)) return true;
            var canvas = hit.gameObject.GetComponentInParent<Canvas>();
            if (canvas && canvas.rootCanvas.renderMode == RenderMode.WorldSpace) continue;
            if (hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>()) return true;
        }
        return false;
    }

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
                var mouseOverUI = IsTagged(curRaysastResult.gameObject);
//#if UNITY_EDITOR
//                UnityEngine.Debug.Log("UI Element: " + curRaysastResult.gameObject.name + " Mouse Over: " + mouseOverUI);
//#endif

                return mouseOverUI;
            }
        }
        return false;
    }

    private static bool IsTagged(GameObject go) => go.CompareTag("BuildingDialog") || go.CompareTag("StopCursorInteractions");

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
