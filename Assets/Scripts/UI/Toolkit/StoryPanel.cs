using System;
using System.Globalization;
using System.Linq;
using RavenNest.SDK.Endpoints;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The quest window's Village tab (phase 5 of docs/quests-design.md): the streamer's story lines (StoryLog), each
    /// told as a talk with its NPC: what they say at this step, what the stream must do and how far it is, who helped
    /// or found what, and the streamer's part: begin, talk, make the choice or let chat vote. Clicking the NPC in the
    /// world (the runestone, the mangrove island) opens the window here on that story alone.
    /// </summary>
    public sealed class StoryPanel
    {
        private readonly Action redraw;
        private readonly Action<string> showError;

        public StoryPanel(Action redraw, Action<string> showError)
        {
            this.redraw = redraw;
            this.showError = showError;
        }

        /// <summary>The story the window was opened on from the world, or null for all.</summary>
        public string Focus { get; set; }

        public bool Busy { get; private set; }

        public void Build(VisualElement content)
        {
            var log = StoryLog.Latest;
            if (log?.Stories == null)
            {
                content.Add(Text(Localization.UI_STREAM_QUEST_LOADING, "rf-squest__empty"));
                return;
            }

            if (Focus == null) QuestArt.Heading(content, Localization.UI_QUESTWIN_STORY, Localization.UI_QUESTWIN_STORY_SUB);
            var stories = log.Stories.Where(s => Focus == null || s.Key == Focus).ToList();
            var anyActive = log.Stories.Any(s => s.State == StoryLog.Active);

            foreach (var story in stories.Where(s => s.State == StoryLog.Active)) content.Add(ActiveCard(story));

            if (Focus == null && (RavebridgeManager.Unlocked || (RavebridgeManager.Instance && RavebridgeManager.Instance.Active)))
            {
                content.Add(Text(Localization.UI_QUESTWIN_EVENTS.ToUpperInvariant(), "rf-qsection"));
                content.Add(RavebridgeCard());
            }

            var toBegin = stories.Where(s => s.State != StoryLog.Active && s.State != StoryLog.Done).ToList();
            if (toBegin.Count > 0)
            {
                content.Add(Text(Localization.UI_QUESTWIN_TO_BEGIN.ToUpperInvariant(), "rf-qsection"));
                foreach (var story in toBegin) content.Add(BeginCard(story, anyActive));
            }

            var done = stories.Where(s => s.State == StoryLog.Done).ToList();
            if (done.Count > 0)
            {
                content.Add(Text(Localization.UI_QUESTWIN_COMPLETED.ToUpperInvariant(), "rf-qsection"));
                foreach (var story in done) content.Add(DoneRow(story));
            }

            if (Focus != null)
            {
                var all = new Button(() => { Focus = null; redraw(); }) { text = Localization.UI_STORY_ALL };
                all.AddToClassList("rf-button");
                all.AddToClassList("rf-squest-form__post");
                content.Add(all);
            }
        }

        /// <summary>The tab's footer: the village and a nudge to talk to its people.</summary>
        public void BuildFooter(VisualElement footer)
        {
            footer.Add(QuestArt.Picture("village-houses", "\uf015", "rf-squest__footer-picture"));
            footer.Add(Text(Localization.UI_QUESTWIN_VILLAGERS, "rf-squest__footer-text"));
        }

        /// <summary>
        /// The story under way: its name and steps, the NPC's line beside its picture, and the current objective with how
        /// far it is, who helped, the streamer's part (talk, choose, let chat vote) and giving up.
        /// </summary>
        private VisualElement ActiveCard(VillageStoryData s)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-qstory");

            var head = new VisualElement();
            head.AddToClassList("rf-qstory__head");
            var names = new VisualElement();
            names.AddToClassList("rf-qstory__names");
            var badge = Text(Localization.UI_QUESTWIN_ACTIVE_STORY.ToUpperInvariant(), "rf-qbadge");
            badge.AddToClassList("rf-qbadge--solid");
            names.Add(badge);
            names.Add(Text(StoryLog.Name(s.Key), "rf-qstory__title"));
            head.Add(names);
            var steps = new VisualElement();
            steps.AddToClassList("rf-qstory__steps");
            steps.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STORY_STEP, s.Step + 1, s.Steps), "rf-qstory__step"));
            steps.Add(Dots(s.Step, s.Steps));
            head.Add(steps);
            card.Add(head);

            var body = new VisualElement();
            body.AddToClassList("rf-qstory__body");
            body.Add(QuestArt.Picture(QuestArt.StoryPicture(s.Key), "\uf70e", "rf-qstory__picture"));
            var right = new VisualElement();
            right.AddToClassList("rf-qstory__right");
            right.Add(Text(StoryLog.Npc(s.Key), "rf-qstory__npc"));
            right.Add(Text(StoryLog.Line(s.Key, s.Step), "rf-qstory__line"));

            var objective = new VisualElement();
            objective.AddToClassList("rf-qobjective");
            objective.Add(Text(Localization.UI_QUESTWIN_OBJECTIVE.ToUpperInvariant(), "rf-qlabel"));
            var counted = s.Kind != "talk" && s.Kind != "choice" && s.Amount > 0;
            var taskRow = new VisualElement();
            taskRow.AddToClassList("rf-qobjective__row");
            taskRow.Add(Text(StoryLog.Task(s, false), "rf-qobjective__task"));
            if (counted) taskRow.Add(Text(StoryLog.Number(s.Progress) + " / " + StoryLog.Number(s.Amount), "rf-qobjective__count"));
            objective.Add(taskRow);
            if (counted) objective.Add(Track((float)s.Progress / s.Amount));

            var actions = Actions(s);
            if (actions.childCount > 0) objective.Add(actions);

            var foot = new VisualElement();
            foot.AddToClassList("rf-qobjective__foot");
            if (s.Helpers != null && s.Helpers.Length > 0)
            {
                var label = s.Kind == "find" ? Localization.UI_STORY_FOUND_BY : Localization.UI_STREAM_QUEST_TOP;
                var who = s.Kind == "find" ? string.Join(", ", s.Helpers.Select(h => h.Name + " (" + StoryLog.Number(h.Amount) + ")")) : StreamQuests.Helpers(s.Helpers, 5);
                var helpers = QuestArt.Chip("\uf007", string.Format(CultureInfo.InvariantCulture, label, who));
                helpers.AddToClassList("rf-qobjective__helpers");
                foot.Add(helpers);
            }
            foot.Add(new VisualElement { style = { flexGrow = 1 } });
            var giveUp = Button(Localization.UI_STORY_ABANDON, false, () => Run(g => StoryLog.AbandonAsync(g, s.Key)));
            giveUp.AddToClassList("rf-qghost");
            foot.Add(giveUp);
            objective.Add(foot);

            right.Add(objective);
            body.Add(right);
            card.Add(body);
            return card;
        }

        /// <summary>One dot per step: done ones ticked, the current one lit, joined by a line.</summary>
        private static VisualElement Dots(int step, int count)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-qdots");
            for (var i = 0; i < count; ++i)
            {
                if (i > 0)
                {
                    var line = new VisualElement();
                    line.AddToClassList("rf-qdots__line");
                    line.EnableInClassList("rf-qdots__line--done", i <= step);
                    row.Add(line);
                }
                var dot = new VisualElement();
                dot.AddToClassList("rf-qdot");
                dot.EnableInClassList("rf-qdot--done", i < step);
                dot.EnableInClassList("rf-qdot--current", i == step);
                if (i < step) dot.Add(QuestArt.Glyph("\uf00c", "rf-qdot__tick"));
                row.Add(dot);
            }
            return row;
        }

        /// <summary>A story not begun (or given up): its picture, what it is about and Begin.</summary>
        private VisualElement BeginCard(VillageStoryData s, bool anyActive)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-qcard");
            card.Add(QuestArt.Picture(QuestArt.StoryPicture(s.Key), "\uf70e", "rf-qcard__picture rf-qcard__picture--plain"));
            var body = new VisualElement();
            body.AddToClassList("rf-qcard__body");
            body.Add(Text(StoryLog.Name(s.Key), "rf-qcard__title"));
            body.Add(Text(StoryLog.Npc(s.Key), "rf-qstory__npc"));
            body.Add(Text(StoryLog.Hook(s.Key), "rf-qcard__line"));
            card.Add(body);

            // the road west begins where the runestone's story ends
            if (s.Key == "roadwest" && !StoryLog.IsDone("runestone"))
            {
                body.Add(Text(Localization.UI_STORY_REQUIRES, "rf-qcard__reward"));
                return card;
            }
            var begin = new Button(() => Run(game => StoryLog.BeginAsync(game, s.Key))) { text = Localization.UI_STORY_BEGIN };
            begin.AddToClassList("rf-button");
            begin.AddToClassList("rf-button--primary");
            begin.AddToClassList("rf-qcard__button");
            begin.SetEnabled(!Busy && !anyActive);
            if (anyActive) begin.tooltip = Localization.UI_STORY_ONE_AT_A_TIME;
            card.Add(begin);
            return card;
        }

        /// <summary>A finished story: a small picture, its name and NPC, and a green tick. Its ending as the tooltip.</summary>
        private static VisualElement DoneRow(VillageStoryData s)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-qdone");
            row.tooltip = StoryLog.Ending(s.Key);
            row.Add(QuestArt.Picture(QuestArt.StoryPicture(s.Key), "\uf70e", "rf-qdone__picture"));
            var names = new VisualElement();
            names.AddToClassList("rf-qdone__names");
            names.Add(Text(StoryLog.Name(s.Key), "rf-qdone__title"));
            names.Add(Text(StoryLog.Npc(s.Key), "rf-qdone__npc"));
            row.Add(names);
            row.Add(QuestArt.Glyph("\uf058", "rf-qdone__tick"));
            row.Add(Text(Localization.UI_STORY_DONE, "rf-qdone__state"));
            return row;
        }

        /// <summary>The streamer's part at this step: talk, the choice, or chat's vote (giving up sits under the objective).</summary>
        private VisualElement Actions(VillageStoryData s)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-squest-chips");
            row.AddToClassList("rf-story__actions");
            var game = GameManager.Instance;

            if (s.Kind == "talk")
            {
                row.Add(Button(string.Format(CultureInfo.InvariantCulture, Localization.UI_STORY_TALK, StoryLog.Npc(s.Key)), true, () => Run(g => StoryLog.TalkAsync(g, s.Key))));
            }
            else if (s.Kind == "choice")
            {
                var voting = StoryLog.VoteStory == s.Key;
                for (var i = 1; i <= s.Options; ++i)
                {
                    var option = i;
                    var text = StoryLog.Option(s.Key, i) + (voting ? " (" + StoryLog.VotesFor(i) + ")" : string.Empty);
                    row.Add(Button(text, true, () => Run(g => StoryLog.ChooseAsync(g, s.Key, option))));
                }

                if (voting)
                {
                    var left = Mathf.Max(0, Mathf.CeilToInt(StoryLog.VoteEnds - Time.unscaledTime));
                    row.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STORY_VOTING, left), "rf-squest-form__note"));
                }
                else if (!StoryLog.Voting)
                {
                    row.Add(Button(Localization.UI_STORY_LET_CHAT_VOTE, false, () => { if (game) StoryLog.StartVote(game, s); }));
                }
            }
            return row;
        }

        private async void Run(Func<GameManager, System.Threading.Tasks.Task<VillageStoryResultData>> action)
        {
            var game = GameManager.Instance;
            if (Busy || !game) return;
            Busy = true;
            showError(string.Empty);
            redraw();
            try
            {
                var result = await action(game);
                if (result == null || !result.Ok) showError(ErrorText(result?.Error));
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("The story action failed: " + exc.Message);
                showError(Localization.UI_STREAM_QUEST_FAILED);
            }
            finally
            {
                Busy = false;
                redraw();
            }
        }

        private static string ErrorText(string error)
        {
            switch (error)
            {
                case "busy": return Localization.UI_STORY_ONE_AT_A_TIME;
                case "done": return Localization.UI_STORY_DONE;
                case "requires": return Localization.UI_STORY_REQUIRES;
                default: return Localization.UI_STREAM_QUEST_FAILED;
            }
        }

        /// <summary>The Ravebridge event (docs/ravebridge-design.md): open it, or how it goes and end it.</summary>
        private VisualElement RavebridgeCard()
        {
            var rb = RavebridgeManager.Instance;
            var card = new VisualElement();
            card.AddToClassList("rf-qcard");
            card.EnableInClassList("rf-qcard--active", rb && rb.Active);
            card.Add(QuestArt.Picture("story-ravebridge", "\uf6d9", "rf-qcard__picture rf-qcard__picture--plain"));
            var body = new VisualElement();
            body.AddToClassList("rf-qcard__body");
            body.Add(Text(Localization.UI_RAVEBRIDGE_TITLE, "rf-qcard__title"));
            card.Add(body);
            if (rb && rb.Active)
            {
                body.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_CARD_OPEN, rb.PlayerCount), "rf-qcard__line"));
                var end = new Button(() => { rb.End(); redraw(); }) { text = Localization.UI_RAVEBRIDGE_END };
                end.AddToClassList("rf-button");
                end.AddToClassList("rf-qcard__button");
                card.Add(end);
            }
            else
            {
                body.Add(Text(Localization.UI_RAVEBRIDGE_START_HINT, "rf-qcard__line"));
                var busy = rb && GameManager.Instance && GameManager.Instance.Events.IsActive;
                var open = new Button(() =>
                {
                    if (rb && rb.Begin(false) == RavebridgeManager.StartResult.Busy) showError(Localization.UI_RAVEBRIDGE_BUSY);
                    redraw();
                }) { text = Localization.UI_RAVEBRIDGE_START };
                open.AddToClassList("rf-button");
                open.AddToClassList("rf-button--primary");
                open.AddToClassList("rf-qcard__button");
                open.SetEnabled(rb && !busy);
                if (busy) open.tooltip = Localization.UI_RAVEBRIDGE_BUSY;
                card.Add(open);
            }
            return card;
        }

        // ---- Pieces ----------------------------------------------------------------------------------------

        private Button Button(string text, bool primary, Action clicked)
        {
            var button = new Button(clicked) { text = text };
            button.AddToClassList("rf-squest-chip");
            button.EnableInClassList("rf-squest-chip--active", primary);
            button.SetEnabled(!Busy);
            return button;
        }

        private static VisualElement Track(float share)
        {
            var track = new VisualElement();
            track.AddToClassList("rf-squest-track");
            track.AddToClassList("rf-squest-track--large");
            var fill = new VisualElement();
            fill.AddToClassList("rf-squest-track__fill");
            fill.style.width = new Length(Mathf.Clamp01(share) * 100f, LengthUnit.Percent);
            track.Add(fill);
            return track;
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }
    }
}
