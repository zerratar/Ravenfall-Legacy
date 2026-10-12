using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Answers "is the pointer over interface" for every UI Toolkit panel, so world clicks can be
    /// ignored when they land on a HUD element.
    /// </summary>
    /// <remarks>
    /// Asks the panels themselves through <see cref="IPanel.Pick"/>, which honours picking modes:
    /// layers and decorations that ignore picking do not count, only elements that would actually
    /// take the click. That makes it correct for any panel, including ones that are not windows.
    /// </remarks>
    public static class HudPointer
    {
        private static readonly List<UIDocument> documents = new List<UIDocument>();
        private static readonly HashSet<IPanel> seen = new HashSet<IPanel>();
        private static int cachedFrame = -1;
        private static bool cachedResult;

        /// <summary>True when the mouse is over a UI Toolkit element that takes clicks.</summary>
        public static bool IsOverHud()
        {
            if (cachedFrame == Time.frameCount)
            {
                return cachedResult;
            }

            cachedFrame = Time.frameCount;
            cachedResult = Check(Input.mousePosition);
            return cachedResult;
        }

        private static bool Check(Vector2 mouse)
        {
            documents.Clear();
            seen.Clear();
            documents.AddRange(Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));

            // Screen space has its origin at the bottom left, panels at the top left.
            var screen = new Vector2(mouse.x, Screen.height - mouse.y);

            for (var i = 0; i < documents.Count; ++i)
            {
                var root = documents[i].rootVisualElement;
                var panel = root?.panel;
                if (panel == null || !seen.Add(panel))
                {
                    continue;
                }

                var picked = panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, screen));
                if (picked != null && !IsDocumentRoot(picked))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDocumentRoot(VisualElement element)
        {
            for (var i = 0; i < documents.Count; ++i)
            {
                var root = documents[i].rootVisualElement;
                if (root == element || root?.parent == element)
                {
                    return true;
                }
            }

            return element.parent == null;
        }
    }
}
