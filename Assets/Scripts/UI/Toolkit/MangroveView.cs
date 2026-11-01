using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The Mangrove island on screen (docs/banana-island.md): the feed of what its monkeys, treasure hunters and racers
    /// get up to (MangroveFeed), low on the left, each line fading after a while; and a bar under the village bar while
    /// something runs there (a treasure dig, the regatta, the pirates' raid), saying what and how to join. It rides on the
    /// stream quest view's document (StreamQuestView adds it), so it hides with the rest of the HUD.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MangroveView : MonoBehaviour
    {
        /// <summary>What an island event shows in the bar; Progress below 0 hides the track.</summary>
        public sealed class Status
        {
            public string Glyph = "";
            public string Title;
            public string Line;
            public float Progress = -1f;
        }

        private const float FadeSeconds = 1.5f;

        private VisualElement feed;
        private readonly List<VisualElement> feedLines = new List<VisualElement>();
        private VisualElement bar;
        private Label barIcon;
        private Label barTitle;
        private Label barLine;
        private VisualElement barTrack;
        private VisualElement barFill;
        private bool bound;
        private bool feedDirty = true;
        private float nextBar;

        private void OnEnable()
        {
            bound = false;
            MangroveFeed.Changed += FeedChanged;
        }

        private void OnDisable()
        {
            MangroveFeed.Changed -= FeedChanged;
            feed?.RemoveFromHierarchy();
            bar?.RemoveFromHierarchy();
        }

        private void FeedChanged() => feedDirty = true;

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            VisualElement column = null;
            foreach (var d in FindObjectsByType<UIDocument>())
            {
                column = d.rootVisualElement?.Q<VisualElement>("top-column");
                if (column != null) break;
            }
            if (layer == null || column == null) return;

            feed?.RemoveFromHierarchy();
            bar?.RemoveFromHierarchy();

            feed = new VisualElement { name = "mangrove-feed", pickingMode = PickingMode.Ignore };
            feed.AddToClassList("rf-mfeed");
            layer.Add(feed);

            bar = new VisualElement { name = "mangrove-bar" };
            // during the pirates' raid a click takes the streamer's character by hand to defend (PirateRaid)
            bar.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => { if (PirateRaid.Running) PirateRaid.BarClicked(); });
            bar.AddToClassList("rf-squest-bar");
            bar.AddToClassList("rf-mbar");
            barIcon = new Label { pickingMode = PickingMode.Ignore };
            barIcon.AddToClassList("rf-icon");
            barIcon.AddToClassList("rf-squest-bar__icon");
            barIcon.AddToClassList("rf-mbar__icon");
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("rf-squest-bar__body");
            barTitle = new Label { pickingMode = PickingMode.Ignore };
            barTitle.AddToClassList("rf-squest-bar__text");
            barTrack = new VisualElement { pickingMode = PickingMode.Ignore };
            barTrack.AddToClassList("rf-squest-track");
            barFill = new VisualElement { pickingMode = PickingMode.Ignore };
            barFill.AddToClassList("rf-squest-track__fill");
            barTrack.Add(barFill);
            barLine = new Label { pickingMode = PickingMode.Ignore };
            barLine.AddToClassList("rf-squest-bar__meta");
            body.Add(barTitle);
            body.Add(barTrack);
            body.Add(barLine);
            bar.Add(barIcon);
            bar.Add(body);
            bar.style.display = DisplayStyle.None;
            column.Add(bar);

            feedLines.Clear();
            feedDirty = true;
            bound = true;
        }

        private void Update()
        {
            if (bound && (feed.panel == null || bar.panel == null)) bound = false;
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (feedDirty) RebuildFeed();
            FadeFeed();

            if (Time.unscaledTime >= nextBar)
            {
                nextBar = Time.unscaledTime + 0.5f;
                UpdateBar();
            }
        }

        private void RebuildFeed()
        {
            feedDirty = false;
            feed.Clear();
            feedLines.Clear();
            foreach (var line in MangroveFeed.Lines)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("rf-mfeed__line");
                var glyph = new Label(line.Glyph) { pickingMode = PickingMode.Ignore };
                glyph.AddToClassList("rf-icon");
                glyph.AddToClassList("rf-mfeed__glyph");
                var text = new Label(line.Text) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("rf-mfeed__text");
                row.Add(glyph);
                row.Add(text);
                row.userData = line.At;
                feed.Add(row);
                feedLines.Add(row);
            }
        }

        private void FadeFeed()
        {
            foreach (var row in feedLines)
            {
                var age = Time.unscaledTime - (float)row.userData;
                var left = MangroveFeed.LineSeconds - age;
                var opacity = Mathf.Clamp01(left / FadeSeconds);
                row.style.opacity = opacity;
                var display = opacity > 0f ? DisplayStyle.Flex : DisplayStyle.None;
                if (row.style.display != display) row.style.display = display;
            }
        }

        private void UpdateBar()
        {
            var status = TreasureHunt.CurrentStatus() ?? Regatta.CurrentStatus() ?? PirateRaid.CurrentStatus();
            var building = VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive;
            var show = status != null && !building;
            var display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (bar.style.display != display) bar.style.display = display;
            if (!show) return;

            barIcon.text = status.Glyph;
            barTitle.text = status.Title;
            barLine.text = status.Line;
            var track = status.Progress >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            if (barTrack.style.display != track) barTrack.style.display = track;
            if (status.Progress >= 0f) barFill.style.width = new Length(Mathf.Clamp01(status.Progress) * 100f, LengthUnit.Percent);
        }
    }
}
