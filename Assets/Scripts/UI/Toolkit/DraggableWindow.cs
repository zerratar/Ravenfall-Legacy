using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Makes a UI Toolkit element draggable, remembers where the streamer left it, and keeps it on
    /// screen.
    /// </summary>
    /// <remarks>
    /// This replaces <c>Dragscript</c>, which had three faults that between them account for the
    /// windows people kept losing:
    ///
    /// <list type="number">
    /// <item>
    /// It clamped only while dragging. A saved position was applied on load with no clamping at
    /// all, so a window placed near the right edge of a 2560 wide screen was simply gone after
    /// relaunching at 1280, with no way to recover it short of clearing preferences.
    /// </item>
    /// <item>
    /// Nothing reacted to the game window being resized. Same outcome, without even needing a
    /// restart.
    /// </item>
    /// <item>
    /// Reset mixed coordinate spaces. It restored world position but saved through keys that were
    /// read back as local position, so resetting a window could move it somewhere new on the next
    /// launch rather than back to its default.
    /// </item>
    /// </list>
    ///
    /// Here a position is only ever stored as the top left corner in panel pixels, and it is
    /// re-clamped on load, during drag, and on every geometry change of the container. The
    /// invariant is that a window cannot end up off screen, whatever the preferences say.
    /// </remarks>
    public sealed class DraggableWindow
    {
        /// <summary>Prefix for saved coordinates, also used to clear them on reset.</summary>
        private const string KeyPrefix = "rf_win_";

        /// <summary>
        /// Index of every window id ever saved, so <see cref="ResetAllPositions"/> can clear entries
        /// for windows that are not currently open. PlayerPrefs cannot enumerate its keys, so the
        /// list has to be kept by hand.
        /// </summary>
        private const string IndexKey = "rf_win_index";

        /// <summary>
        /// How much of a window must remain inside the container. A margin rather than requiring the
        /// whole window to fit, so a large panel on a small screen stays usable: it may hang off an
        /// edge, but never far enough to lose the bar you grab it by.
        /// </summary>
        private const float MinVisible = 64f;

        private static readonly List<DraggableWindow> live = new List<DraggableWindow>();

        private readonly VisualElement window;
        private readonly VisualElement handle;
        private readonly string id;

        private Vector2 pointerStart;
        private Vector2 windowStart;
        private bool dragging;
        private bool restored;

        private DraggableWindow(VisualElement window, VisualElement handle, string id)
        {
            this.window = window;
            this.handle = handle;
            this.id = id;
        }

        /// <summary>
        /// Makes <paramref name="window"/> draggable by <paramref name="handle"/>.
        /// </summary>
        /// <param name="window">The element that moves. Switched to absolute positioning.</param>
        /// <param name="handle">
        /// The grab area, usually a title bar. Passing the window itself makes the whole surface
        /// draggable, which is wrong for anything holding a scroll view or buttons.
        /// </param>
        /// <param name="id">
        /// Stable identifier for the saved position. Deliberately not derived from the element name:
        /// the old system keyed on GameObject name, so different windows could collide.
        /// </param>
        public static DraggableWindow Attach(VisualElement window, VisualElement handle, string id)
        {
            if (window == null || handle == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            var w = new DraggableWindow(window, handle, id);

            window.style.position = Position.Absolute;

            handle.RegisterCallback<PointerDownEvent>(w.OnPointerDown);
            handle.RegisterCallback<PointerMoveEvent>(w.OnPointerMove);
            handle.RegisterCallback<PointerUpEvent>(w.OnPointerUp);

            // Restoring needs a resolved size, which does not exist until the first layout pass.
            window.RegisterCallback<GeometryChangedEvent>(w.OnGeometryChanged);

            live.Add(w);
            RegisterId(id);
            return w;
        }

        /// <summary>
        /// Detaches and forgets this window. Call when its screen goes away, otherwise the static
        /// list holds the element alive across scene loads.
        /// </summary>
        public void Detach()
        {
            handle.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            handle.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            handle.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            window.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            live.Remove(this);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!restored)
            {
                restored = true;
                Restore();
                return;
            }

            // Also fires when the container resizes, which is the case the old system missed.
            ApplyClamped(new Vector2(window.resolvedStyle.left, window.resolvedStyle.top));
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            dragging = true;
            pointerStart = evt.position;
            windowStart = new Vector2(window.resolvedStyle.left, window.resolvedStyle.top);

            // Captured so the drag survives the pointer leaving the handle. Without this, moving
            // faster than layout updates drops the drag.
            handle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!dragging || !handle.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            var delta = (Vector2)evt.position - pointerStart;
            ApplyClamped(windowStart + delta);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            if (handle.HasPointerCapture(evt.pointerId))
            {
                handle.ReleasePointer(evt.pointerId);
            }

            Save();
            evt.StopPropagation();
        }

        /// <summary>
        /// Writes a position after forcing it inside the container.
        /// </summary>
        private void ApplyClamped(Vector2 position)
        {
            var container = window.parent;
            if (container == null)
            {
                return;
            }

            var bounds = container.contentRect;
            var size = window.contentRect;

            // Before the first layout these are NaN or zero. Writing then would pin the window to
            // the top left corner and look like the saved position had been lost.
            if (float.IsNaN(bounds.width) || float.IsNaN(size.width) || bounds.width <= 0)
            {
                return;
            }

            var maxX = Mathf.Max(0f, bounds.width - MinVisible);
            var maxY = Mathf.Max(0f, bounds.height - MinVisible);
            var minX = Mathf.Min(0f, MinVisible - size.width);
            const float minY = 0f; // The title bar must stay reachable, so never above the top edge.

            var x = Mathf.Clamp(position.x, minX, maxX);
            var y = Mathf.Clamp(position.y, minY, maxY);

            window.style.left = new Length(x, LengthUnit.Pixel);
            window.style.top = new Length(y, LengthUnit.Pixel);
        }

        private void Restore()
        {
            var kx = KeyPrefix + id + "_x";
            var ky = KeyPrefix + id + "_y";

            if (!PlayerPrefs.HasKey(kx) || !PlayerPrefs.HasKey(ky))
            {
                // No saved position. Clamp whatever the stylesheet chose, so a default that does not
                // fit the current window gets corrected rather than trusted.
                ApplyClamped(new Vector2(window.resolvedStyle.left, window.resolvedStyle.top));
                return;
            }

            ApplyClamped(new Vector2(PlayerPrefs.GetFloat(kx), PlayerPrefs.GetFloat(ky)));
        }

        private void Save()
        {
            PlayerPrefs.SetFloat(KeyPrefix + id + "_x", window.resolvedStyle.left);
            PlayerPrefs.SetFloat(KeyPrefix + id + "_y", window.resolvedStyle.top);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Returns every window to the position its stylesheet gives it and forgets the saved
        /// coordinates. This is what the settings menu calls.
        /// </summary>
        public static void ResetAllPositions()
        {
            var ids = PlayerPrefs.GetString(IndexKey, string.Empty);
            if (!string.IsNullOrEmpty(ids))
            {
                var saved = ids.Split('|');
                for (var i = 0; i < saved.Length; ++i)
                {
                    if (string.IsNullOrEmpty(saved[i]))
                    {
                        continue;
                    }

                    PlayerPrefs.DeleteKey(KeyPrefix + saved[i] + "_x");
                    PlayerPrefs.DeleteKey(KeyPrefix + saved[i] + "_y");
                }
            }

            PlayerPrefs.Save();

            // Open windows move now rather than on next launch. Clearing the inline values lets the
            // stylesheet position apply again, and it is then clamped.
            for (var i = 0; i < live.Count; ++i)
            {
                var w = live[i];
                w.window.style.left = StyleKeyword.Null;
                w.window.style.top = StyleKeyword.Null;
                w.ApplyClamped(new Vector2(w.window.resolvedStyle.left, w.window.resolvedStyle.top));
            }
        }

        private static void RegisterId(string id)
        {
            var ids = PlayerPrefs.GetString(IndexKey, string.Empty);
            if (ids.Length == 0)
            {
                PlayerPrefs.SetString(IndexKey, id);
                return;
            }

            var existing = ids.Split('|');
            for (var i = 0; i < existing.Length; ++i)
            {
                if (string.Equals(existing[i], id, StringComparison.Ordinal))
                {
                    return;
                }
            }

            PlayerPrefs.SetString(IndexKey, ids + "|" + id);
        }
    }
}
