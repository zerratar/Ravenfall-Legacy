using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The old uGUI screen canvases take clicks before the UI Toolkit windows: their raycasters rank above the panel's, so
/// a leftover picture or label on them, even an invisible one, ate the press meant for a window drawn over it (the user,
/// 2026-11-04: windows at the top of the screen in free camera mode could not be moved). Their pictures and labels stop
/// catching clicks. Whatever handles a click keeps it: buttons, scroll views, the old draggable panels, the settings
/// cog, and the parts tagged to stop the cursor reaching the world (UIUtility.IsPointerOverUIElement).
/// </summary>
public sealed class LegacyCanvasClickThrough : MonoBehaviour
{
    /// <summary>The canvases make new rows (the old player list) as they go, so they are looked over now and then.</summary>
    private const float ScanInterval = 2f;

    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var go = new GameObject(nameof(LegacyCanvasClickThrough));
        DontDestroyOnLoad(go);
        go.AddComponent<LegacyCanvasClickThrough>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + ScanInterval;

        foreach (var canvas in FindObjectsByType<Canvas>())
        {
            if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.raycastTarget && !HandlesClicks(graphic.transform, canvas.transform))
                {
                    graphic.raycastTarget = false;
                }
            }
        }
    }

    private static bool HandlesClicks(Transform t, Transform canvas)
    {
        if (t.CompareTag("BuildingDialog") || t.CompareTag("StopCursorInteractions")) return true;
        for (; t != null; t = t.parent)
        {
            if (t.GetComponent<IEventSystemHandler>() != null) return true;
            if (t == canvas) break;
        }

        return false;
    }
}
