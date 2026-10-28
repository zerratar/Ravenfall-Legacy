using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// A hover tooltip for HUD windows: a title, rows of label and value, and an optional note.
    /// </summary>
    /// <remarks>
    /// Rows are separate elements rather than one block of rich text, so labels and values line up
    /// in columns with the game's proportional font. The old uGUI tooltip padded names with spaces
    /// inside a monospace tag to get the same effect.
    ///
    /// <para>
    /// The content is built when the pointer arrives, by a callback, rather than kept up to date in
    /// the background. A tooltip is open for a few seconds at most, so filling it on demand costs
    /// nothing the rest of the time.
    /// </para>
    /// </remarks>
    public sealed class HudTooltip
    {
        /// <summary>Gap between the tooltip and the element it describes.</summary>
        private const float Offset = 10f;

        private readonly VisualElement layer;
        private readonly VisualElement root;
        private readonly Label title;
        private readonly VisualElement rows;
        private readonly Label note;

        private VisualElement target;

        private HudTooltip(VisualElement layer)
        {
            this.layer = layer;

            root = new VisualElement { name = "hud-tooltip", pickingMode = PickingMode.Ignore };
            root.AddToClassList("rf-tooltip");
            root.style.display = DisplayStyle.None;

            title = new Label { pickingMode = PickingMode.Ignore };
            title.AddToClassList("rf-tooltip__title");

            rows = new VisualElement { pickingMode = PickingMode.Ignore };
            rows.AddToClassList("rf-tooltip__rows");

            note = new Label { pickingMode = PickingMode.Ignore };
            note.AddToClassList("rf-tooltip__note");

            root.Add(title);
            root.Add(rows);
            root.Add(note);
            layer.Add(root);

            // Positioned once the content has been measured, which is only known after layout.
            root.RegisterCallback<GeometryChangedEvent>(_ => Place());
        }

        /// <summary>
        /// Creates the tooltip inside <paramref name="layer"/>, usually the full screen root of a
        /// document, so it can sit outside the window that owns it.
        /// </summary>
        public static HudTooltip Create(VisualElement layer)
        {
            return layer != null ? new HudTooltip(layer) : null;
        }

        /// <summary>
        /// Shows the tooltip while the pointer is over <paramref name="element"/>. The callback fills
        /// it each time; leaving the title empty shows nothing.
        /// </summary>
        public void Attach(VisualElement element, Action<HudTooltip> fill)
        {
            if (element == null || fill == null)
            {
                return;
            }

            element.RegisterCallback<PointerEnterEvent>(_ =>
            {
                Clear();
                fill(this);
                if (string.IsNullOrEmpty(title.text))
                {
                    Hide();
                    return;
                }

                target = element;
                note.style.display = string.IsNullOrEmpty(note.text) ? DisplayStyle.None : DisplayStyle.Flex;
                rows.style.display = rows.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

                // Shown invisible first; Place makes it visible once it knows where it goes, so it
                // never flashes in the wrong spot for a frame.
                root.style.visibility = Visibility.Hidden;
                root.style.display = DisplayStyle.Flex;
                root.BringToFront();
                Place();
            });

            element.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (target == element)
                {
                    Hide();
                }
            });
        }

        public void SetTitle(string text)
        {
            title.text = text ?? string.Empty;
        }

        /// <summary>Adds a row. <paramref name="valueClass"/> colours the value, for example rf-tooltip__value--good.</summary>
        public void AddRow(string label, string value, string valueClass = null)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("rf-tooltip__row");

            var l = new Label(label) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("rf-tooltip__label");

            var v = new Label(value) { pickingMode = PickingMode.Ignore };
            v.AddToClassList("rf-tooltip__value");
            if (!string.IsNullOrEmpty(valueClass))
            {
                v.AddToClassList(valueClass);
            }

            row.Add(l);
            row.Add(v);
            rows.Add(row);
        }

        public void SetNote(string text)
        {
            note.text = text ?? string.Empty;
        }

        public void Hide()
        {
            target = null;
            root.style.display = DisplayStyle.None;
        }

        private void Clear()
        {
            title.text = string.Empty;
            note.text = string.Empty;
            rows.Clear();
        }

        /// <summary>
        /// Puts the tooltip beside its target: to the left by preference, since the windows that
        /// use it live on the right of the screen, otherwise to the right. Kept inside the layer.
        /// </summary>
        private void Place()
        {
            if (target == null || root.style.display == DisplayStyle.None)
            {
                return;
            }

            var size = root.layout.size;
            var bounds = layer.contentRect;
            if (float.IsNaN(size.x) || size.x <= 0f || float.IsNaN(bounds.width))
            {
                return;
            }

            var anchor = layer.WorldToLocal(target.worldBound);

            var x = anchor.xMin - Offset - size.x;
            if (x < 0f)
            {
                x = anchor.xMax + Offset;
            }

            x = Mathf.Clamp(x, 0f, Mathf.Max(0f, bounds.width - size.x));
            var y = Mathf.Clamp(anchor.yMin, 0f, Mathf.Max(0f, bounds.height - size.y));

            // laid out at the corner and moved by a translate, which layout ignores: placed by left, it was measured
            // against the room right of that spot, so a long name (an enchanted item's) wrapped instead of the
            // tooltip growing wider
            root.style.left = 0f;
            root.style.top = 0f;
            root.style.translate = new Translate(x, y);
            root.style.visibility = Visibility.Visible;
        }
    }
}
