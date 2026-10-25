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

            if (Focus == null) content.Add(Text(Localization.UI_STORY_INTRO, "rf-squest__section"));
            var anyActive = log.Stories.Any(s => s.State == StoryLog.Active);
            foreach (var story in log.Stories)
            {
                if (Focus != null && story.Key != Focus) continue;
                content.Add(Card(story, anyActive));
            }

            if (Focus != null)
            {
                var all = new Button(() => { Focus = null; redraw(); }) { text = Localization.UI_STORY_ALL };
                all.AddToClassList("rf-button");
                all.AddToClassList("rf-squest-form__post");
                content.Add(all);
            }
        }

        private VisualElement Card(VillageStoryData s, bool anyActive)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-squest-card");
            card.AddToClassList("rf-story");
            card.EnableInClassList("rf-squest-card--active", s.State == StoryLog.Active);
            var body = new VisualElement();
            body.AddToClassList("rf-squest-card__body");
            body.Add(Text(StoryLog.Name(s.Key), "rf-squest-card__title"));

            switch (s.State)
            {
                case StoryLog.Active:
                    {
                        body.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STORY_STEP, s.Step + 1, s.Steps), "rf-squest-card__meta"));
                        body.Add(Speech(StoryLog.Npc(s.Key), StoryLog.Line(s.Key, s.Step)));
                        body.Add(Text(StoryLog.Task(s), "rf-squest-card__reward"));
                        if (s.Kind != "talk" && s.Kind != "choice" && s.Amount > 1) body.Add(Track(s.Amount > 0 ? (float)s.Progress / s.Amount : 0f));
                        if (s.Helpers != null && s.Helpers.Length > 0)
                        {
                            var label = s.Kind == "find" ? Localization.UI_STORY_FOUND_BY : Localization.UI_STREAM_QUEST_TOP;
                            var names = s.Kind == "find" ? string.Join(", ", s.Helpers.Select(h => h.Name + " (" + StoryLog.Number(h.Amount) + ")")) : StreamQuests.Helpers(s.Helpers, 5);
                            body.Add(Text(string.Format(CultureInfo.InvariantCulture, label, names), "rf-squest-card__helpers"));
                        }
                        body.Add(Actions(s));
                        break;
                    }

                case StoryLog.Done:
                    body.Add(Speech(StoryLog.Npc(s.Key), StoryLog.Ending(s.Key)));
                    body.Add(Text(Localization.UI_STORY_DONE, "rf-squest-card__reward"));
                    break;

                default:
                    {
                        body.Add(Text(StoryLog.Hook(s.Key), "rf-squest-card__meta"));
                        var begin = Button(Localization.UI_STORY_BEGIN, true, () => Run(game => StoryLog.BeginAsync(game, s.Key)));
                        begin.SetEnabled(!Busy && !anyActive);
                        if (anyActive) begin.tooltip = Localization.UI_STORY_ONE_AT_A_TIME;
                        var row = new VisualElement();
                        row.AddToClassList("rf-squest-chips");
                        row.Add(begin);
                        body.Add(row);
                        break;
                    }
            }

            card.Add(body);
            return card;
        }

        /// <summary>The streamer's part at this step: talk, the choice (or chat's vote), and giving up.</summary>
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

            var giveUp = Button(Localization.UI_STORY_ABANDON, false, () => Run(g => StoryLog.AbandonAsync(g, s.Key)));
            giveUp.AddToClassList("rf-story__abandon");
            row.Add(giveUp);
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
                default: return Localization.UI_STREAM_QUEST_FAILED;
            }
        }

        // ---- Pieces ----------------------------------------------------------------------------------------

        /// <summary>The NPC's name and what they say, as a speech box.</summary>
        private static VisualElement Speech(string npc, string line)
        {
            var box = new VisualElement();
            box.AddToClassList("rf-story__speech");
            box.Add(Text(npc, "rf-story__npc"));
            box.Add(Text(line, "rf-story__line"));
            return box;
        }

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
