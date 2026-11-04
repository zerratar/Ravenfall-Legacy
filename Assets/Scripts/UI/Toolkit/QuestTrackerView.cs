using System;
using System.Globalization;
using RavenNest.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The quest tracker (docs/mockups/quest-ui): the stream's quests under way and what to do next, always on screen as
    /// online games do it: the stream quest, the village story's step (with chat's votes while it chooses) and the
    /// streamer's goals and bounties. Streamers kept the quest window open and shrunk into a corner to show this on
    /// stream (the user, 2026-11-04). Movable by its header, foldable, and switched on or off in the settings or with
    /// its key; the background is none, soft or solid. A click on a quest opens the quest window there.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class QuestTrackerView : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;

        private VisualElement tracker, list;
        private Label title;
        private Button fold;
        private DraggableWindow drag;
        private bool bound;
        private float nextRefresh;
        private string shown;

        private void OnEnable()
        {
            bound = false;
            Languages.Changed += Rebind;
        }

        private void OnDisable()
        {
            Languages.Changed -= Rebind;
            drag?.Detach();
            drag = null;
            tracker?.RemoveFromHierarchy();
            bound = false;
        }

        private void Rebind() => bound = false;

        private void Update()
        {
            if (bound && tracker.panel == null) bound = false;
            if (!bound) TryBind();
            if (!bound || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            Refresh();
        }

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            if (layer == null) return;

            drag?.Detach();
            tracker?.RemoveFromHierarchy();
            tracker = new VisualElement { name = "quest-tracker" };
            tracker.AddToClassList("rf-tracker");

            var head = new VisualElement();
            head.AddToClassList("rf-tracker__head");
            var grip = Text("", "rf-tracker__grip");
            grip.AddToClassList("rf-icon");
            head.Add(grip);
            title = Text(Localization.UI_TRACKER_TITLE.ToUpperInvariant(), "rf-tracker__title");
            head.Add(title);
            var hide = new Button(() => { SettingsApplier.SetQuestTracker(false); PlayerSettings.Save(); Refresh(); }) { text = "", tooltip = Localization.UI_TRACKER_HIDE };
            hide.AddToClassList("rf-icon");
            hide.AddToClassList("rf-tracker__button");
            head.Add(hide);
            fold = new Button(() => drag?.ToggleCollapsed()) { text = "", tooltip = Localization.UI_TRACKER_FOLD };
            fold.AddToClassList("rf-icon");
            fold.AddToClassList("rf-tracker__button");
            head.Add(fold);
            tracker.Add(head);

            list = new VisualElement();
            list.AddToClassList("rf-tracker__list");
            tracker.Add(list);

            // under every window in the layer: the tracker is part of the scene, the windows come and go over it
            layer.Insert(0, tracker);
            drag = DraggableWindow.Attach(tracker, head, "questtracker");
            if (drag != null)
            {
                drag.CollapsedChanged += folded =>
                {
                    list.style.display = folded ? DisplayStyle.None : DisplayStyle.Flex;
                    fold.text = folded ? "" : "";
                };
            }

            shown = null;
            bound = true;
            Refresh();
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            var on = SettingsApplier.QuestTracker && gm && gm.RavenNest != null && gm.RavenNest.SessionStarted;
            var background = SettingsApplier.QuestTrackerBackground;
            tracker.EnableInClassList("rf-tracker--none", background == 0);
            tracker.EnableInClassList("rf-tracker--soft", background == 1);
            tracker.EnableInClassList("rf-tracker--solid", background == 2);
            tracker.EnableInClassList("rf-tracker--large", SettingsApplier.QuestTrackerLargeText);

            var key = on ? Signature() : string.Empty;
            if (key != shown)
            {
                // rebuilt only when what it says changes: the clocks tick every second, the rest seldom
                shown = key;
                list.Clear();
                if (on) Build();
            }

            tracker.style.display = on && list.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (drag != null) list.style.display = drag.IsCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Everything the tracker shows, as text: when it changes, the tracker is drawn again.</summary>
        private static string Signature()
        {
            var sb = new System.Text.StringBuilder();
            var stream = SettingsApplier.QuestTrackerStream ? StreamQuests.Active : null;
            if (stream != null) sb.Append(stream.Id).Append(stream.Progress).Append(StreamQuests.TimeLeft(stream)).Append(stream.Helpers);
            var story = SettingsApplier.QuestTrackerStory ? StoryLog.ActiveStory : null;
            if (story != null)
            {
                sb.Append(story.Key).Append(story.Step).Append(story.Progress).Append(story.Kind);
                if (StoryLog.VoteStory == story.Key)
                {
                    sb.Append(Mathf.CeilToInt(StoryLog.VoteEnds - Time.unscaledTime));
                    for (var i = 1; i <= story.Options; ++i) sb.Append(',').Append(StoryLog.VotesFor(i));
                }
            }
            if (SettingsApplier.QuestTrackerGoals && StreamerQuests.Latest?.Quests != null)
            {
                foreach (var q in StreamerQuests.Latest.Quests)
                {
                    if (q.State == StreamerQuests.Running) sb.Append(q.Id).Append(q.Progress).Append(q.Claimed).Append(StreamerQuests.TimeLeft(q));
                }
            }
            sb.Append(Localization.UI_TRACKER_TITLE);
            return sb.ToString();
        }

        private void Build()
        {
            var stream = SettingsApplier.QuestTrackerStream ? StreamQuests.Active : null;
            if (stream != null)
            {
                var entry = Entry("stream", "", Localization.UI_TRACKER_STREAM, StreamQuests.Text(stream), () => OpenWindow(0, null));
                entry.Body.Add(Bar(stream.Amount > 0 ? (float)stream.Progress / stream.Amount : 0f));
                var meta = Meta(entry.Body);
                MetaPart(meta, null, StreamQuests.Number(stream.Progress) + " / " + StreamQuests.Number(stream.Amount));
                MetaPart(meta, "", string.Format(CultureInfo.InvariantCulture, Localization.UI_TRACKER_LEFT, StreamQuests.TimeLeft(stream)));
                if (stream.Helpers > 0) MetaPart(meta, "", string.Format(CultureInfo.InvariantCulture, Localization.UI_TRACKER_HELPING, stream.Helpers));
            }

            var story = SettingsApplier.QuestTrackerStory ? StoryLog.ActiveStory : null;
            if (story != null) BuildStory(story);

            if (SettingsApplier.QuestTrackerGoals && StreamerQuests.Latest?.Quests != null)
            {
                foreach (var q in StreamerQuests.Latest.Quests)
                {
                    if (q.State != StreamerQuests.Running) continue;
                    if (q.Kind == StreamerQuests.Goal)
                    {
                        var entry = Entry("goal", "", Localization.UI_TRACKER_GOAL, StreamerQuests.Title(q), () => OpenWindow(1, null));
                        entry.Body.Add(Bar(q.Amount > 0 ? (float)q.Progress / q.Amount : 0f));
                        var meta = Meta(entry.Body);
                        MetaPart(meta, null, StreamerQuests.Number(q.Progress) + " / " + StreamerQuests.Number(q.Amount));
                        MetaPart(meta, "", string.Format(CultureInfo.InvariantCulture, Localization.UI_TRACKER_LEFT, StreamerQuests.TimeLeft(q)));
                    }
                    else
                    {
                        var kind = Localization.UI_TRACKER_BOUNTY + " - " + StreamerQuests.Reward(q);
                        var entry = Entry("bounty", "", kind, StreamerQuests.Title(q), () => OpenWindow(1, null));
                        var meta = Meta(entry.Body);
                        MetaPart(meta, null, string.Format(CultureInfo.InvariantCulture, Localization.UI_TRACKER_CLAIMED, q.Claimed, q.Winners));
                        MetaPart(meta, "", string.Format(CultureInfo.InvariantCulture, Localization.UI_TRACKER_LEFT, StreamerQuests.TimeLeft(q)));
                    }
                }
            }
        }

        private void BuildStory(VillageStoryInfo s)
        {
            var kind = Localization.UI_TRACKER_VILLAGE + " - " + StoryLog.Name(s.Key) + " " + (s.Step + 1) + "/" + s.Steps;
            var voting = s.Kind == "choice" && StoryLog.VoteStory == s.Key;
            var entry = Entry("story", "", kind, voting ? Localization.UI_STORY_VOTE_TITLE : StoryLog.Task(s, false), () => OpenWindow(2, s.Key));

            if (voting)
            {
                var total = 0;
                for (var i = 1; i <= s.Options; ++i) total += StoryLog.VotesFor(i);
                var best = 0;
                var bestVotes = 0;
                for (var i = 1; i <= s.Options; ++i)
                {
                    if (StoryLog.VotesFor(i) > bestVotes) { bestVotes = StoryLog.VotesFor(i); best = i; }
                }

                for (var i = 1; i <= s.Options; ++i)
                {
                    var votes = StoryLog.VotesFor(i);
                    var row = new VisualElement();
                    row.AddToClassList("rf-tracker__vote");
                    row.EnableInClassList("rf-tracker__vote--lead", i == best);
                    row.Add(Text(i.ToString(CultureInfo.InvariantCulture), "rf-tracker__vote-num"));
                    row.Add(Text(StoryLog.Option(s.Key, i), "rf-tracker__vote-name"));
                    var track = new VisualElement();
                    track.AddToClassList("rf-tracker__vote-track");
                    var fill = new VisualElement();
                    fill.AddToClassList("rf-tracker__vote-fill");
                    fill.style.width = Length.Percent(total > 0 ? 100f * votes / total : 0f);
                    track.Add(fill);
                    row.Add(track);
                    row.Add(Text(votes.ToString(CultureInfo.InvariantCulture), "rf-tracker__vote-count"));
                    entry.Body.Add(row);
                }

                var left = Mathf.Max(0, Mathf.CeilToInt(StoryLog.VoteEnds - Time.unscaledTime));
                var meta = Meta(entry.Body);
                MetaPart(meta, null, string.Format(CultureInfo.InvariantCulture, Localization.UI_TRACKER_VOTE, s.Options));
                MetaPart(meta, "", (left / 60).ToString(CultureInfo.InvariantCulture) + ":" + (left % 60).ToString("00", CultureInfo.InvariantCulture));
                return;
            }

            var counted = s.Kind != "talk" && s.Kind != "choice" && s.Amount > 0;
            if (counted)
            {
                entry.Body.Add(Bar((float)s.Progress / s.Amount));
                MetaPart(Meta(entry.Body), null, StoryLog.Number(s.Progress) + " / " + StoryLog.Number(s.Amount));
            }
        }

        private static void OpenWindow(int tab, string story)
        {
            var view = FindAnyObjectByType<StreamQuestView>();
            if (!view) return;
            if (story != null) view.OpenStory(story);
            else view.OpenTab(tab);
        }

        // ---- Pieces --------------------------------------------------------------------------------------

        private sealed class EntryParts
        {
            public VisualElement Body;
        }

        private EntryParts Entry(string variant, string glyph, string kind, string task, Action clicked)
        {
            var entry = new VisualElement();
            entry.AddToClassList("rf-tracker__entry");
            entry.AddToClassList("rf-tracker__entry--" + variant);
            entry.tooltip = Localization.UI_TRACKER_OPEN;
            entry.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => clicked());

            var icon = Text(glyph, "rf-tracker__icon");
            icon.AddToClassList("rf-icon");
            entry.Add(icon);

            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("rf-tracker__body");
            body.Add(Text(kind.ToUpperInvariant(), "rf-tracker__kind"));
            body.Add(Text(task, "rf-tracker__task"));
            entry.Add(body);
            list.Add(entry);
            return new EntryParts { Body = body };
        }

        private static VisualElement Bar(float share)
        {
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("rf-tracker__bar");
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("rf-tracker__bar-fill");
            fill.style.width = Length.Percent(Mathf.Clamp01(share) * 100f);
            track.Add(fill);
            return track;
        }

        private static VisualElement Meta(VisualElement body)
        {
            var meta = new VisualElement { pickingMode = PickingMode.Ignore };
            meta.AddToClassList("rf-tracker__meta");
            body.Add(meta);
            return meta;
        }

        private static void MetaPart(VisualElement meta, string glyph, string text)
        {
            var part = new VisualElement { pickingMode = PickingMode.Ignore };
            part.AddToClassList("rf-tracker__meta-part");
            if (glyph != null)
            {
                var icon = Text(glyph, "rf-tracker__meta-icon");
                icon.AddToClassList("rf-icon");
                part.Add(icon);
            }
            part.Add(Text(text, "rf-tracker__meta-text"));
            meta.Add(part);
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }
    }
}
