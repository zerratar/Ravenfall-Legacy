using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Keeps the top status column clear of the windows that stand at the screen's sides (the player list on the left,
    /// the player details on the right, or wherever the streamer dragged them). The column is centred on the screen;
    /// a wide panel in it (the tower panel grows with its text, and wider still with the floor map open) reached under
    /// the player details window, which covered its right edge. So while such a panel shows, the column is centred in
    /// the room the windows leave instead, and the panel is told how wide it may be.
    /// </summary>
    public static class TopColumnFit
    {
        /// <summary>The gap kept between the column and a window beside it.</summary>
        public const float Margin = 16f;

        /// <summary>A window wider than this share of the screen is not one to step aside for (it covers the middle anyway).</summary>
        public const float WidestWindow = 0.45f;

        private static readonly List<VisualElement> windows = new List<VisualElement>();

        /// <summary>
        /// Moves the column sideways so that it stands between the windows that reach into its height, and answers the
        /// width that is free for it there (a panel wider than that has to give).
        /// </summary>
        public static float Apply(VisualElement column)
        {
            var panel = column?.panel;
            if (panel == null) return float.MaxValue;
            var root = panel.visualTree;
            var width = root.worldBound.width;
            var band = column.worldBound;
            if (float.IsNaN(width) || width <= 0f || float.IsNaN(band.height) || band.height <= 0f) return float.MaxValue;

            var left = 0f;
            var right = width;
            windows.Clear();
            root.Query<VisualElement>(className: "rf-window").ToList(windows);
            foreach (var window in windows)
            {
                if (!Shown(window)) continue;
                var box = window.worldBound;
                if (float.IsNaN(box.width) || box.width <= 1f || box.height <= 1f) continue;
                if (box.yMin >= band.yMax || box.yMax <= band.yMin) continue;
                if (box.width > width * WidestWindow) continue;
                if (box.center.x < width * 0.5f) left = Mathf.Max(left, box.xMax);
                else right = Mathf.Min(right, box.xMin);
            }

            var free = Mathf.Max(0f, right - left - 2f * Margin);
            var half = Mathf.Min(band.width, free) * 0.5f;
            var middle = width * 0.5f;
            // as near the screen's middle as the windows allow
            var centre = Mathf.Clamp(middle, left + Margin + half, Mathf.Max(left + Margin + half, right - Margin - half));
            var shift = Mathf.Round(centre - middle);
            column.style.translate = Mathf.Abs(shift) < 1f ? new StyleTranslate(StyleKeyword.Null) : new StyleTranslate(new Translate(shift, 0f));
            return free;
        }

        /// <summary>The column back in the middle of the screen (no wide panel shows).</summary>
        public static void Reset(VisualElement column)
        {
            if (column != null) column.style.translate = new StyleTranslate(StyleKeyword.Null);
        }

        private static bool Shown(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
            {
                var style = e.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden) return false;
            }
            return element.resolvedStyle.opacity > 0.01f;
        }
    }
}
