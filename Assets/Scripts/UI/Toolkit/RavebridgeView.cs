using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The Ravebridge bar (phase 6b of docs/ravebridge-design.md): while the event runs, under the stream quest bar in
    /// the top column, who is there and how long it has been open, with the streamer's buttons: Play (by hand, 6c),
    /// Stay (a forced event otherwise closes by itself) and End. Opening it is in the quest window's Village tab and
    /// !ravebridge start.
    /// </summary>
    public class RavebridgeView : MonoBehaviour
    {
        private VisualElement bar;
        private Label text;
        private Label meta;
        private Button stay;
        private Button play;
        private Button end;
        private bool bound;
        private float nextSecond;

        public static System.Action PlayClicked;

        private void OnEnable()
        {
            bound = false;
            RavebridgeManager.Changed += Redraw;
            Languages.Changed += Rebuild;
        }

        private void OnDisable()
        {
            RavebridgeManager.Changed -= Redraw;
            Languages.Changed -= Rebuild;
            bound = false;
        }

        private void Rebuild() => bound = false;

        private void TryBind()
        {
            VisualElement column = null;
            foreach (var d in FindObjectsByType<UIDocument>())
            {
                column = d.rootVisualElement?.Q<VisualElement>("top-column");
                if (column != null) break;
            }
            if (column == null) return;

            bar?.RemoveFromHierarchy();
            bar = new VisualElement { name = "ravebridge-bar" };
            bar.AddToClassList("rf-squest-bar");
            bar.AddToClassList("rf-ravebridge-bar");
            var icon = new Label("") { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-squest-bar__icon");
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("rf-squest-bar__body");
            text = new Label { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-squest-bar__text");
            meta = new Label { pickingMode = PickingMode.Ignore };
            meta.AddToClassList("rf-squest-bar__meta");
            body.Add(text);
            body.Add(meta);
            var buttons = new VisualElement();
            buttons.AddToClassList("rf-squest-chips");
            stay = Chip(Localization.UI_RAVEBRIDGE_STAY, true, () => { var rb = RavebridgeManager.Instance; if (rb) rb.Attended = true; Redraw(); });
            play = Chip(Localization.UI_RAVEBRIDGE_PLAY, true, () => PlayClicked?.Invoke());
            end = Chip(Localization.UI_RAVEBRIDGE_END, false, () => { var rb = RavebridgeManager.Instance; if (rb) rb.End(); });
            buttons.Add(stay);
            buttons.Add(play);
            buttons.Add(end);
            bar.Add(icon);
            bar.Add(body);
            bar.Add(buttons);
            column.Add(bar);
            bound = true;
            Redraw();
        }

        private static Button Chip(string label, bool primary, System.Action clicked)
        {
            var button = new Button(clicked) { text = label };
            button.AddToClassList("rf-squest-chip");
            button.EnableInClassList("rf-squest-chip--active", primary);
            return button;
        }

        private void Update()
        {
            if (bound && bar.panel == null) bound = false;
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }
            if (Time.unscaledTime >= nextSecond)
            {
                nextSecond = Time.unscaledTime + 1f;
                Redraw();
            }
        }

        private void Redraw()
        {
            if (!bound) return;
            var rb = RavebridgeManager.Instance;
            var shown = rb && rb.Active && !(VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive);
            bar.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown) return;

            text.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_BAR, rb.PlayerCount, Clock(Time.time - rb.StartedAt));
            var left = rb.SecondsLeft;
            meta.text = left >= 0f ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_BAR_FORCED, Clock(left)) : (rb.Forced && !string.IsNullOrEmpty(rb.ForcedReason) ? rb.ForcedReason : string.Empty);
            meta.style.display = string.IsNullOrEmpty(meta.text) ? DisplayStyle.None : DisplayStyle.Flex;
            stay.style.display = left >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            play.style.display = PlayClicked != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static string Clock(float seconds)
        {
            var s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return s >= 3600 ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", s / 3600, s / 60 % 60, s % 60) : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", s / 60, s % 60);
        }
    }
}
