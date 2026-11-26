using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    ///     Keeps a HUD window from lying over others (the owner dislikes stacked popups): where it would, it stands at the
    ///     nearest free place instead, and goes back to its own place when that is free again; a drag by hand makes the
    ///     new place its own. Which windows it stays clear of is the caller's (<c>avoids</c>; every other window when
    ///     null), and optionally the tower panel and the pills in the bottom left corner. Call <see cref="Update"/> on the
    ///     window's refresh while it shows, and <see cref="GoHome"/> as it hides. The meters window's way of keeping clear
    ///     (DungeonMetersWindow), shared with the player details since 2026-11-27: picked from the players list, the
    ///     details opened over the list (both top left).
    /// </summary>
    public sealed class WindowKeepClear
    {
        private const float ClearGap = 8f, ClearStep = 24f;
        private static readonly List<VisualElement> foundWindows = new List<VisualElement>();

        private readonly VisualElement window;
        private readonly Func<DraggableWindow> drag;
        private readonly Func<VisualElement, bool> avoids;
        private readonly bool panelAndPills;
        private readonly List<Rect> blocked = new List<Rect>();
        private VisualElement cornerPills;
        private float nextPillSearch;
        // where the window stood (its remembered place) before it was moved out of something's way, and where it
        // was moved to; it goes back when its place is free again
        private Vector2? home;
        private Vector2 nudgedTo;

        public WindowKeepClear(VisualElement window, Func<DraggableWindow> drag, Func<VisualElement, bool> avoids = null, bool panelAndPills = true)
        {
            this.window = window;
            this.drag = drag;
            this.avoids = avoids;
            this.panelAndPills = panelAndPills;
        }

        /// <summary>Out of the way if something is in it; back home when home is free. The search runs only when needed.</summary>
        public void Update()
        {
            var panel = window != null ? window.panel : null;
            if (panel == null) return;
            var root = panel.visualTree;
            var screen = root.worldBound;
            var box = window.worldBound;
            if (float.IsNaN(box.width) || box.width < 2f || box.height < 2f || float.IsNaN(screen.width) || screen.width < 2f) return;

            // moved by hand since it was put somewhere: that is its place now
            if (home.HasValue && (box.position - nudgedTo).sqrMagnitude > 9f) home = null;

            blocked.Clear();
            foundWindows.Clear();
            root.Query<VisualElement>(className: "rf-window").ToList(foundWindows);
            foreach (var other in foundWindows)
            {
                if (other == window || !Visible(other) || (avoids != null && !avoids(other))) continue;
                Add(other.worldBound);
            }
            if (panelAndPills)
            {
                if (TowerHudView.PanelOwner == panel) Add(TowerHudView.PanelBounds);
                if (cornerPills == null || cornerPills.panel == null) cornerPills = FindCornerPills(root);
                if (cornerPills != null && Visible(cornerPills))
                {
                    // the stack is as big as the pills it shows
                    foreach (var pill in cornerPills.Children())
                        if (Visible(pill)) Add(pill.worldBound);
                }
            }

            var want = home ?? box.position;
            if (Free(new Rect(want, box.size)))
            {
                if (home.HasValue) { MoveTo(want); home = null; }
                return;
            }
            if (!home.HasValue && Free(box)) return;

            // the nearest free place to its own, on a coarse grid over the screen
            var best = Vector2.zero;
            var bestCost = float.MaxValue;
            for (var y = screen.yMin; y <= screen.yMax - box.height; y += ClearStep)
            {
                for (var x = screen.xMin; x <= screen.xMax - box.width; x += ClearStep)
                {
                    var cost = (new Vector2(x, y) - want).sqrMagnitude;
                    if (cost >= bestCost || !Free(new Rect(x, y, box.width, box.height))) continue;
                    bestCost = cost;
                    best = new Vector2(x, y);
                }
            }
            if (bestCost == float.MaxValue) return; // no room anywhere at this size: it stays
            if (!home.HasValue) home = box.position;
            MoveTo(best);
        }

        /// <summary>Back to its own place (as it hides): the next time it shows it starts from there.</summary>
        public void GoHome()
        {
            if (!home.HasValue) return;
            MoveTo(home.Value);
            home = null;
        }

        private void Add(Rect r)
        {
            if (!float.IsNaN(r.width) && r.width > 2f && r.height > 2f) blocked.Add(r);
        }

        private bool Free(Rect rect)
        {
            for (var i = 0; i < blocked.Count; ++i)
            {
                var b = blocked[i];
                if (rect.xMin < b.xMax + ClearGap && rect.xMax > b.xMin - ClearGap && rect.yMin < b.yMax + ClearGap && rect.yMax > b.yMin - ClearGap) return false;
            }
            return true;
        }

        private void MoveTo(Vector2 position)
        {
            var origin = window.parent != null ? window.parent.worldBound.position : Vector2.zero;
            // through the window's own keeper of its place: it puts the window back where it last had it on every
            // change of layout, so a place written beside it did not hold
            var d = drag?.Invoke();
            if (d != null) d.Nudge(position - origin);
            else
            {
                window.style.left = new Length(position.x - origin.x, LengthUnit.Pixel);
                window.style.top = new Length(position.y - origin.y, LengthUnit.Pixel);
            }
            nudgedTo = position;
        }

        public static bool Visible(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
            {
                var style = e.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden) return false;
            }
            return true;
        }

        // The pills in the bottom left corner ("Bot offline", the server's state): on this panel, or on a panel of
        // their own (the same screen measure, so their places compare). Looked for until found.
        private VisualElement FindCornerPills(VisualElement root)
        {
            var stack = root.Q<VisualElement>(className: "rf-conn-stack");
            if (stack != null || Time.unscaledTime < nextPillSearch) return stack;
            nextPillSearch = Time.unscaledTime + 5f;
            foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var other = doc.rootVisualElement;
                if (other == null) continue;
                stack = other.Q<VisualElement>(className: "rf-conn-stack");
                if (stack != null) return stack;
            }
            return null;
        }
    }
}
