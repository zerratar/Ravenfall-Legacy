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

        private static readonly List<DraggableWindow> live = new List<DraggableWindow>();

        /// <summary>Windows the pointer is currently over.</summary>
        private static readonly HashSet<VisualElement> hovered = new HashSet<VisualElement>();

        /// <summary>
        /// True while the pointer is over a visible HUD window. The cameras read this to leave the
        /// scroll wheel alone, so scrolling a list does not also zoom the world behind it.
        /// </summary>
        public static bool IsPointerOverWindow
        {
            get
            {
                foreach (var w in hovered)
                {
                    if (w.panel != null && w.resolvedStyle.display == DisplayStyle.Flex && w.resolvedStyle.visibility == Visibility.Visible)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private readonly VisualElement window;
        private readonly VisualElement handle;
        private readonly string id;

        private Vector2 pointerStart;
        private Vector2 windowStart;
        private bool dragging;
        private bool restored;

        private VisualElement container;
        private VisualElement grip;
        private Vector2 minSize;
        private bool resizing;
        private bool resizeWidthOnly;
        private Vector2 resizePointerStart;
        private Vector2 resizeSizeStart;
        private bool collapsed;

        /// <summary>The position last written, so a hidden and shown window returns to it.</summary>
        private Vector2? lastPosition;

        /// <summary>Raised when the window is collapsed or expanded, with the new state.</summary>
        public event Action<bool> CollapsedChanged;

        /// <summary>Whether the window is folded down to its header.</summary>
        public bool IsCollapsed => collapsed;

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

            window.RegisterCallback<PointerEnterEvent>(w.OnPointerEnterWindow);
            window.RegisterCallback<PointerLeaveEvent>(w.OnPointerLeaveWindow);

            // Restoring needs a resolved size, which does not exist until the first layout pass.
            window.RegisterCallback<GeometryChangedEvent>(w.OnGeometryChanged);

            // The container is watched as well as the window. A window with a fixed pixel size and
            // position does not change geometry when the game window is resized, only its container
            // does, so listening on the window alone meant the resize re-clamp never fired.
            w.container = window.parent;
            w.container?.RegisterCallback<GeometryChangedEvent>(w.OnContainerGeometryChanged);

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
            window.UnregisterCallback<PointerEnterEvent>(OnPointerEnterWindow);
            window.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveWindow);
            hovered.Remove(window);
            container?.UnregisterCallback<GeometryChangedEvent>(OnContainerGeometryChanged);

            if (grip != null)
            {
                grip.UnregisterCallback<PointerDownEvent>(OnGripDown);
                grip.UnregisterCallback<PointerMoveEvent>(OnGripMove);
                grip.UnregisterCallback<PointerUpEvent>(OnGripUp);
            }

            live.Remove(this);
        }

        /// <summary>
        /// Lets the window be resized by dragging <paramref name="resizeGrip"/>, usually a corner
        /// element. The size is saved per window id alongside the position.
        /// </summary>
        /// <param name="resizeGrip">The element to drag. Must sit inside the window.</param>
        /// <param name="minimum">
        /// Smallest allowed size. Below a certain width the content stops being legible on stream,
        /// so each window decides its own floor rather than sharing one.
        /// </param>
        /// <param name="widthOnly">
        /// Only the width follows the grip. For windows whose height comes from their content, such
        /// as the details windows, where a fixed height would clip a section that opens later.
        /// </param>
        public void EnableResize(VisualElement resizeGrip, Vector2 minimum, bool widthOnly = false)
        {
            if (resizeGrip == null || grip != null)
            {
                return;
            }

            grip = resizeGrip;
            minSize = minimum;
            resizeWidthOnly = widthOnly;
            grip.tooltip = widthOnly ? Localization.UI_WINDOW_RESIZE_WIDTH : Localization.UI_WINDOW_RESIZE;
            grip.EnableInClassList("rf-window__grip--width", widthOnly);
            grip.RegisterCallback<PointerDownEvent>(OnGripDown);
            grip.RegisterCallback<PointerMoveEvent>(OnGripMove);
            grip.RegisterCallback<PointerUpEvent>(OnGripUp);
        }

        /// <summary>
        /// Folds the window down to its header, or opens it again. The state is remembered.
        /// </summary>
        public void SetCollapsed(bool value)
        {
            ApplyCollapsed(value);
            PlayerPrefs.SetInt(Key("c"), value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ToggleCollapsed()
        {
            SetCollapsed(!collapsed);
        }

        private void ApplyCollapsed(bool value)
        {
            collapsed = value;
            window.EnableInClassList("rf-window--collapsed", value);

            if (value)
            {
                // Auto rather than a remembered pixel height: the header decides how tall a folded
                // window is, and the saved height is kept for when it opens again.
                window.style.height = StyleKeyword.Auto;
            }
            else if (PlayerPrefs.HasKey(Key("h")))
            {
                window.style.height = new Length(PlayerPrefs.GetFloat(Key("h")), LengthUnit.Pixel);
            }
            else
            {
                window.style.height = StyleKeyword.Null;
            }

            CollapsedChanged?.Invoke(value);
        }

        private string Key(string suffix)
        {
            return KeyPrefix + id + "_" + suffix;
        }

        private void OnGripDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || (collapsed && !resizeWidthOnly))
            {
                return;
            }

            resizing = true;
            resizePointerStart = evt.position;
            resizeSizeStart = new Vector2(window.resolvedStyle.width, window.resolvedStyle.height);
            grip.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnGripMove(PointerMoveEvent evt)
        {
            if (!resizing || !grip.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            var delta = (Vector2)evt.position - resizePointerStart;
            ApplySize(resizeSizeStart + delta);
            evt.StopPropagation();
        }

        private void OnGripUp(PointerUpEvent evt)
        {
            if (!resizing)
            {
                return;
            }

            resizing = false;
            if (grip.HasPointerCapture(evt.pointerId))
            {
                grip.ReleasePointer(evt.pointerId);
            }

            PlayerPrefs.SetFloat(Key("w"), window.resolvedStyle.width);
            if (!resizeWidthOnly)
            {
                PlayerPrefs.SetFloat(Key("h"), window.resolvedStyle.height);
            }
            PlayerPrefs.Save();
            evt.StopPropagation();
        }

        /// <summary>
        /// Writes a size after limiting it to the minimum and to what fits between the window's
        /// position and the container's far edges.
        /// </summary>
        private void ApplySize(Vector2 size)
        {
            if (container == null)
            {
                return;
            }

            var bounds = container.contentRect;
            if (float.IsNaN(bounds.width) || bounds.width <= 0)
            {
                return;
            }

            var left = window.resolvedStyle.left;
            var top = window.resolvedStyle.top;
            var maxW = Mathf.Max(minSize.x, bounds.width - left);
            var maxH = Mathf.Max(minSize.y, bounds.height - top);

            window.style.width = new Length(Mathf.Clamp(size.x, minSize.x, maxW), LengthUnit.Pixel);
            if (!collapsed && !resizeWidthOnly)
            {
                window.style.height = new Length(Mathf.Clamp(size.y, minSize.y, maxH), LengthUnit.Pixel);
            }
        }

        private void OnContainerGeometryChanged(GeometryChangedEvent evt)
        {
            if (!restored || !IsShown())
            {
                return;
            }

            ApplyClamped(CurrentPosition());

            // A window resized to fill a large screen would otherwise stay that size on a smaller
            // one and run off the edge.
            if (grip != null && !resizing)
            {
                ApplySize(new Vector2(window.resolvedStyle.width, window.resolvedStyle.height));
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // A hidden window lays out at zero size and position. Clamping that wrote 0,0 back as
            // the window's position, so a window hidden and shown again (player details, every time
            // the camera moved between a player and the free camera) came back in the top left
            // corner. Hidden windows are left alone, and come back where they were.
            if (!IsShown())
            {
                return;
            }

            if (!restored)
            {
                restored = true;
                Restore();
                return;
            }

            // Also fires when the container resizes, which is the case the old system missed.
            ApplyClamped(CurrentPosition());
        }

        /// <summary>Whether the window is displayed and has been laid out at a real size.</summary>
        private bool IsShown()
        {
            if (window.panel == null || window.resolvedStyle.display == DisplayStyle.None)
            {
                return false;
            }

            var size = window.layout.size;
            return !float.IsNaN(size.x) && size.x > 0f && size.y > 0f;
        }

        /// <summary>
        /// Where the window should be: the last position it was clamped to, when it has one. The
        /// resolved style is only trusted before that, since a hide and show passes through zero.
        /// </summary>
        private Vector2 CurrentPosition()
        {
            return lastPosition ?? new Vector2(window.resolvedStyle.left, window.resolvedStyle.top);
        }

        private void OnPointerEnterWindow(PointerEnterEvent evt)
        {
            hovered.Add(window);
        }

        private void OnPointerLeaveWindow(PointerLeaveEvent evt)
        {
            hovered.Remove(window);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || IsOnControl(evt.target as VisualElement))
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

        /// <summary>
        /// True when a press landed on a control inside the handle, such as a collapse button.
        /// </summary>
        /// <remarks>
        /// Without this, pressing a header button started a drag: the handle captured the pointer on
        /// the way up the tree, and the button never received the release it needs to count a click.
        /// </remarks>
        private bool IsOnControl(VisualElement target)
        {
            for (var e = target; e != null && e != handle; e = e.parent)
            {
                if (e is Button || e is Toggle || e is TextField)
                {
                    return true;
                }
            }

            return false;
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
            var size = window.layout.size;

            // Before the first layout these are NaN or zero. Writing then would pin the window to
            // the top left corner and look like the saved position had been lost.
            if (float.IsNaN(bounds.width) || float.IsNaN(size.x) || bounds.width <= 0)
            {
                return;
            }

            // The whole window stays on screen. An earlier version let it hang off the left, right
            // and bottom edges as long as a strip stayed visible, which read as a bug rather than a
            // feature. When the window is larger than the container, it pins to the top left so the
            // title bar is still reachable.
            var maxX = Mathf.Max(0f, bounds.width - size.x);
            var maxY = Mathf.Max(0f, bounds.height - size.y);
            const float minX = 0f;
            const float minY = 0f;

            var x = Mathf.Clamp(position.x, minX, maxX);
            var y = Mathf.Clamp(position.y, minY, maxY);

            window.style.left = new Length(x, LengthUnit.Pixel);
            window.style.top = new Length(y, LengthUnit.Pixel);
            lastPosition = new Vector2(x, y);
        }

        private void Restore()
        {
            if (grip != null && PlayerPrefs.HasKey(Key("w")))
            {
                window.style.width = new Length(Mathf.Max(minSize.x, PlayerPrefs.GetFloat(Key("w"))), LengthUnit.Pixel);
            }

            // Applies the saved height too, unless the window was left collapsed.
            ApplyCollapsed(PlayerPrefs.GetInt(Key("c"), 0) == 1);

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
        /// Returns every window to the position and size its stylesheet gives it, expands any that
        /// were collapsed, and forgets the saved values. This is what the settings menu calls.
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
                    PlayerPrefs.DeleteKey(KeyPrefix + saved[i] + "_w");
                    PlayerPrefs.DeleteKey(KeyPrefix + saved[i] + "_h");
                    PlayerPrefs.DeleteKey(KeyPrefix + saved[i] + "_c");
                }
            }

            PlayerPrefs.Save();

            // Open windows move now rather than on next launch. Clearing the inline values lets the
            // stylesheet position apply again, and it is then clamped. The clamp waits for the next
            // layout: the resolved style still holds the old position until then, and clamping
            // straight away wrote that old position back, so the reset looked like it did nothing.
            // (A scheduled callback is no good either; the scheduler runs before layout.) A window
            // already at its stylesheet place does not move, and needs no clamp.
            for (var i = 0; i < live.Count; ++i)
            {
                var w = live[i];
                w.window.style.left = StyleKeyword.Null;
                w.window.style.top = StyleKeyword.Null;
                w.lastPosition = null;
                w.window.style.width = StyleKeyword.Null;
                w.ApplyCollapsed(false);

                EventCallback<GeometryChangedEvent> once = null;
                once = e =>
                {
                    w.window.UnregisterCallback(once);
                    if (w.window.resolvedStyle.display == DisplayStyle.None) return;
                    w.ApplyClamped(new Vector2(w.window.resolvedStyle.left, w.window.resolvedStyle.top));
                };
                w.window.RegisterCallback(once);
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
