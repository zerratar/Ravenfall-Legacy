using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The Mangrove island on screen (docs/banana-island.md): the feed of what its monkeys, treasure hunters and racers
    /// get up to (MangroveFeed), low on the left, each line fading after a while; and a panel under the village bar while
    /// something runs there (a treasure dig, the regatta, the pirates' raid), saying what and how to join; during the
    /// regatta, every racer's place on the right (Regatta.Standings). The panel (the user's pick of two mockups,
    /// 2026-11-03): the title big with a progress line under it and a ring timer on the right, and the chat command
    /// that joins in a strip of its own below, so a viewer reads "Type !regatta in chat" at a glance. It rides on the
    /// stream quest view's document (StreamQuestView adds it), so it hides with the rest of the HUD.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MangroveView : MonoBehaviour
    {
        /// <summary>What an island event shows in the panel; Progress below 0 hides the track, an empty Timer the ring.</summary>
        public sealed class Status
        {
            public string Glyph = "";
            public string Title;
            public string Line;
            public float Progress = -1f;
            /// <summary>The ring on the right: its text ("83s") and how full it is, emptying as the time runs out.</summary>
            public string Timer;
            public float TimerFill;
            /// <summary>
            /// The chat command that joins ("!regatta"), in the strip at the bottom as a gold chip between
            /// <see cref="CommandHint"/> ("Own a boat? Type") and "in chat". None, no strip.
            /// </summary>
            public string Command;
            public string CommandHint;
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
        private VisualElement barRing;
        private Label barTimer;
        private float ringFill;
        private VisualElement barCta;
        private Label barCtaHint;
        private Label barCommand;
        private VisualElement standings;
        private VisualElement standingsRows;
        private readonly List<(Label Place, Label Name, Label Glyphs, Label Detail, VisualElement Row)> standingRows = new List<(Label, Label, Label, Label, VisualElement)>();
        private RegattaResultsPanel results;
        private bool bound;
        private bool feedDirty = true;
        private float nextBar;

        private void OnEnable()
        {
            bound = false;
            MangroveFeed.Changed += FeedChanged;
            Regatta.Results += ShowResults;
        }

        private void OnDisable()
        {
            MangroveFeed.Changed -= FeedChanged;
            Regatta.Results -= ShowResults;
            results?.Remove();
            feed?.RemoveFromHierarchy();
            bar?.RemoveFromHierarchy();
            standings?.RemoveFromHierarchy();
        }

        private void FeedChanged() => feedDirty = true;

        private void ShowResults(RegattaResult result)
        {
            if (!bound) TryBind();
            results?.Show(result);
        }

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
            standings?.RemoveFromHierarchy();

            standings = new VisualElement { name = "mangrove-standings", pickingMode = PickingMode.Ignore };
            standings.AddToClassList("rf-mstand");
            var head = new Label(Localization.UI_REGATTA_STANDINGS) { pickingMode = PickingMode.Ignore };
            head.AddToClassList("rf-mstand__title");
            standings.Add(head);
            standingsRows = new VisualElement { pickingMode = PickingMode.Ignore };
            standings.Add(standingsRows);
            standings.style.display = DisplayStyle.None;
            standingRows.Clear();
            layer.Add(standings);

            results?.Remove();
            results = new RegattaResultsPanel(layer);

            feed = new VisualElement { name = "mangrove-feed", pickingMode = PickingMode.Ignore };
            feed.AddToClassList("rf-mfeed");
            layer.Add(feed);

            bar = new VisualElement { name = "mangrove-bar" };
            // during the pirates' raid a click takes the streamer's character by hand to defend (PirateRaid)
            bar.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => { if (PirateRaid.Running) PirateRaid.BarClicked(); });
            bar.AddToClassList("rf-mbar");
            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("rf-mbar__top");
            barIcon = new Label { pickingMode = PickingMode.Ignore };
            barIcon.AddToClassList("rf-icon");
            barIcon.AddToClassList("rf-mbar__icon");
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("rf-mbar__body");
            barTitle = new Label { pickingMode = PickingMode.Ignore };
            barTitle.AddToClassList("rf-mbar__title");
            barTitle.AddToClassList("rf-font-eb");
            barLine = new Label { pickingMode = PickingMode.Ignore };
            barLine.AddToClassList("rf-mbar__line");
            barLine.AddToClassList("rf-font-b");
            barTrack = new VisualElement { pickingMode = PickingMode.Ignore };
            barTrack.AddToClassList("rf-mbar__track");
            barFill = new VisualElement { pickingMode = PickingMode.Ignore };
            barFill.AddToClassList("rf-mbar__fill");
            barTrack.Add(barFill);
            body.Add(barTitle);
            body.Add(barLine);
            body.Add(barTrack);
            // the ring timer: a gold arc that empties clockwise, the seconds in it
            barRing = new VisualElement { pickingMode = PickingMode.Ignore };
            barRing.AddToClassList("rf-mbar__ring");
            barRing.generateVisualContent += DrawRing;
            barTimer = new Label { pickingMode = PickingMode.Ignore };
            barTimer.AddToClassList("rf-mbar__timer");
            barTimer.AddToClassList("rf-font-eb");
            barRing.Add(barTimer);
            top.Add(barIcon);
            top.Add(body);
            top.Add(barRing);
            // the command to type is what the panel is for: a strip of its own, the command a gold chip in the sentence
            barCta = new VisualElement { pickingMode = PickingMode.Ignore };
            barCta.AddToClassList("rf-mbar__cta");
            barCtaHint = new Label { pickingMode = PickingMode.Ignore };
            barCtaHint.AddToClassList("rf-mbar__cta-text");
            barCtaHint.AddToClassList("rf-font-b");
            barCommand = new Label { pickingMode = PickingMode.Ignore };
            barCommand.AddToClassList("rf-mbar__cmd");
            barCommand.AddToClassList("rf-font-eb");
            var after = new Label(Localization.UI_MANGROVE_IN_CHAT) { pickingMode = PickingMode.Ignore };
            after.AddToClassList("rf-mbar__cta-text");
            after.AddToClassList("rf-font-b");
            barCta.Add(barCtaHint);
            barCta.Add(barCommand);
            barCta.Add(after);
            bar.Add(top);
            bar.Add(barCta);
            bar.style.display = DisplayStyle.None;
            column.Add(bar);

            feedLines.Clear();
            feedDirty = true;
            bound = true;
        }

        private void Update()
        {
            if (bound && (feed.panel == null || bar.panel == null || standings.panel == null || !results.Attached)) bound = false;
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (feedDirty) RebuildFeed();
            FadeFeed();
            results.Update(VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive);

            if (Time.unscaledTime >= nextBar)
            {
                nextBar = Time.unscaledTime + 0.25f;
                UpdateBar();
                UpdateStandings();
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

        /// <summary>The regatta's racers in order, with what each carries and its lap or time.</summary>
        private void UpdateStandings()
        {
            var list = Regatta.Standings();
            var building = VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive;
            var show = list != null && list.Count > 0 && !building;
            var display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (standings.style.display != display) standings.style.display = display;
            if (!show) return;

            while (standingRows.Count < list.Count)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("rf-mstand__row");
                var place = new Label { pickingMode = PickingMode.Ignore };
                place.AddToClassList("rf-mstand__place");
                var name = new Label { pickingMode = PickingMode.Ignore };
                name.AddToClassList("rf-mstand__name");
                var glyphs = new Label { pickingMode = PickingMode.Ignore };
                glyphs.AddToClassList("rf-icon");
                glyphs.AddToClassList("rf-mstand__glyphs");
                var detail = new Label { pickingMode = PickingMode.Ignore };
                detail.AddToClassList("rf-mstand__detail");
                row.Add(place);
                row.Add(name);
                row.Add(glyphs);
                row.Add(detail);
                standingsRows.Add(row);
                standingRows.Add((place, name, glyphs, detail, row));
            }
            for (var i = 0; i < standingRows.Count; ++i)
            {
                var (place, name, glyphs, detail, row) = standingRows[i];
                var on = i < list.Count;
                var rowDisplay = on ? DisplayStyle.Flex : DisplayStyle.None;
                if (row.style.display != rowDisplay) row.style.display = rowDisplay;
                if (!on) continue;
                var s = list[i];
                place.text = s.Place.ToString(System.Globalization.CultureInfo.InvariantCulture);
                name.text = s.Name;
                glyphs.text = s.Glyphs;
                detail.text = s.Detail;
                row.EnableInClassList("rf-mstand__row--lead", s.Place == 1);
                row.EnableInClassList("rf-mstand__row--done", s.Done);
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
            // a bare title (the chest is open) is a small panel, no empty line under it
            var line = string.IsNullOrEmpty(status.Line) ? DisplayStyle.None : DisplayStyle.Flex;
            if (barLine.style.display != line) barLine.style.display = line;
            var track = status.Progress >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            if (barTrack.style.display != track) barTrack.style.display = track;
            if (status.Progress >= 0f) barFill.style.width = new Length(Mathf.Clamp01(status.Progress) * 100f, LengthUnit.Percent);
            var ring = string.IsNullOrEmpty(status.Timer) ? DisplayStyle.None : DisplayStyle.Flex;
            if (barRing.style.display != ring) barRing.style.display = ring;
            if (ring == DisplayStyle.Flex)
            {
                barTimer.text = status.Timer;
                var fill = Mathf.Clamp01(status.TimerFill);
                if (!Mathf.Approximately(fill, ringFill)) { ringFill = fill; barRing.MarkDirtyRepaint(); }
            }
            var cta = string.IsNullOrEmpty(status.Command) ? DisplayStyle.None : DisplayStyle.Flex;
            if (barCta.style.display != cta) barCta.style.display = cta;
            if (cta == DisplayStyle.Flex)
            {
                barCommand.text = status.Command;
                barCtaHint.text = status.CommandHint ?? string.Empty;
            }
        }

        private static readonly Color RingTrack = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color RingGold = new Color32(236, 190, 52, 255);

        private void DrawRing(MeshGenerationContext context)
        {
            var rect = barRing.contentRect;
            var width = 5f;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f - width;
            if (radius <= 0f) return;
            var painter = context.painter2D;
            painter.lineWidth = width;
            painter.strokeColor = RingTrack;
            painter.BeginPath();
            painter.Arc(rect.center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();
            if (ringFill <= 0f) return;
            painter.strokeColor = RingGold;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.Arc(rect.center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * ringFill));
            painter.Stroke();
        }
    }
}
