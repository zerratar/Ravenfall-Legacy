using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The quest window's Ravebridge tab: the island quests (RavebridgeQuests) of a character the streamer plays by hand
    /// there, the Ravebridge character or one of their own. Which character, its quest points toward the Duke's title,
    /// the quests under way with their current objective (how far, and who to see), the quests and bounties to begin
    /// and whom to ask, and the finished ones. Quests are begun and handed in by talking to the NPCs; this only shows
    /// where each stands.
    /// </summary>
    /// <remarks>Built into StreamQuestView's window, shown while Ravebridge is open to the streamer.</remarks>
    public sealed class RavebridgeQuestPanel
    {
        private readonly Action redraw;

        /// <summary>The character picked, by id; the one played by hand when nothing was picked.</summary>
        private Guid? picked;
        private string shownSignature;

        public RavebridgeQuestPanel(Action redraw)
        {
            this.redraw = redraw;
        }

        /// <summary>Whether the tab is offered: Ravebridge is open to the streamer, or its event is on.</summary>
        public static bool Available => RavebridgeManager.Unlocked || (RavebridgeManager.Instance && RavebridgeManager.Instance.Active);

        private sealed class Character
        {
            public Guid Id;
            public string Name;
            public PlayerController Player;
            public RavebridgeQuests.State State;
        }

        /// <summary>The Ravebridge character (saved, whether it is out or not) and the streamer's own characters in the game.</summary>
        private static List<Character> Characters()
        {
            var list = new List<Character>();
            var local = RavebridgeCharacter.Current;
            if (local != null)
            {
                var spawned = RavebridgeCharacter.Spawned;
                list.Add(new Character { Id = local.Id, Name = local.Name, Player = spawned, State = local.Quest ??= new RavebridgeQuests.State() });
            }
            var game = GameManager.Instance;
            if (game && game.Players != null)
            {
                foreach (var p in game.Players.GetAllPlayers())
                {
                    if (!p || p.IsBot || p.IsLocal || !RavebridgeManager.IsStreamers(p)) continue;
                    list.Add(new Character { Id = p.Id, Name = p.Name, Player = p, State = RavebridgeQuests.StateOf(p) });
                }
            }
            return list;
        }

        private Character Current(List<Character> characters)
        {
            if (characters.Count == 0) return null;
            var chosen = picked.HasValue ? characters.FirstOrDefault(c => c.Id == picked.Value) : null;
            return chosen ?? characters.FirstOrDefault(c => c.Player && c.Player.Controlled) ?? characters[0];
        }

        public void Build(VisualElement content)
        {
            QuestArt.Heading(content, Localization.UI_RBQ_TITLE, Localization.UI_RBQ_SUB);
            var data = RavebridgeQuests.Data;
            var characters = Characters();
            var current = Current(characters);
            shownSignature = Signature(current);
            if (data?.quests == null || current == null)
            {
                content.Add(Empty());
                return;
            }

            if (characters.Count > 1)
            {
                var row = new VisualElement();
                row.AddToClassList("rf-squest-chips");
                row.AddToClassList("rf-rbq__characters");
                foreach (var c in characters)
                {
                    var id = c.Id;
                    var chip = new Button(() => { picked = id; redraw(); }) { text = c.Name };
                    chip.AddToClassList("rf-squest-chip");
                    chip.EnableInClassList("rf-squest-chip--active", c == current);
                    row.Add(chip);
                }
                content.Add(row);
            }
            content.Add(Points(current));

            var state = current.State;
            var quests = data.quests;
            var underWay = quests.Where(q => { var at = RavebridgeQuests.StepOf(state, q.key); return at > 0 && at < q.steps.Count; }).ToList();
            var toBegin = quests.Where(q => !q.bounty && RavebridgeQuests.StepOf(state, q.key) == 0).ToList();
            var bounties = quests.Where(q => q.bounty && RavebridgeQuests.StepOf(state, q.key) == 0).ToList();
            var done = quests.Where(q => !q.bounty && RavebridgeQuests.IsDone(state, q.key)).ToList();

            if (underWay.Count > 0)
            {
                content.Add(QuestArt.Text(Localization.UI_RBQ_UNDER_WAY.ToUpperInvariant(), "rf-qsection"));
                foreach (var q in underWay) content.Add(ActiveCard(current, q));
            }
            if (toBegin.Count > 0)
            {
                content.Add(QuestArt.Text(Localization.UI_RBQ_TO_BEGIN.ToUpperInvariant(), "rf-qsection"));
                foreach (var q in toBegin) content.Add(BeginCard(current, q));
            }
            if (bounties.Count > 0)
            {
                content.Add(QuestArt.Text(Localization.UI_RBQ_BOUNTIES.ToUpperInvariant(), "rf-qsection"));
                foreach (var q in bounties) content.Add(BeginCard(current, q));
            }
            if (done.Count > 0)
            {
                content.Add(QuestArt.Text(Localization.UI_QUESTWIN_COMPLETED.ToUpperInvariant(), "rf-qsection"));
                foreach (var q in done) content.Add(DoneRow(q));
            }
        }

        /// <summary>The tab's footer: quests go through the people of Ravebridge.</summary>
        public void BuildFooter(VisualElement footer)
        {
            footer.Add(QuestArt.Glyph("\uf4ad", "rf-squest__footer-icon"));
            footer.Add(QuestArt.Text(Localization.UI_RBQ_FOOTER, "rf-squest__footer-text"));
        }

        /// <summary>Whether what the tab shows has changed (a step, an item, the loot): checked every few seconds while it is open.</summary>
        public bool Changed() => Signature(Current(Characters())) != shownSignature;

        private static string Signature(Character c)
        {
            if (c?.State == null) return "none";
            var s = c.Id + ":" + c.State.Points;
            foreach (var kv in c.State.Steps) s += "|" + kv.Key + kv.Value;
            foreach (var kv in c.State.Items) s += "|" + kv.Key + kv.Value;
            if (c.Player)
            {
                foreach (var q in RavebridgeQuests.Data?.quests ?? new List<RavebridgeQuests.Quest>())
                {
                    RavebridgeQuests.Objective(c.Player, c.State, q, out var have, out _);
                    s += "|" + have;
                }
            }
            return s;
        }

        private static VisualElement Empty()
        {
            var empty = new VisualElement();
            empty.AddToClassList("rf-qempty");
            empty.Add(QuestArt.Picture("story-ravebridge", "\uf6d9", "rf-qempty__picture"));
            empty.Add(QuestArt.Text(Localization.UI_RBQ_NOBODY, "rf-qempty__title"));
            empty.Add(QuestArt.Text(Localization.UI_RBQ_NOBODY_SUB, "rf-qempty__sub"));
            return empty;
        }

        /// <summary>Quest points toward the Duke's title, as a number and a bar.</summary>
        private static VisualElement Points(Character c)
        {
            var count = RavebridgeQuests.QuestCount;
            var card = new VisualElement();
            card.AddToClassList("rf-qbudget");
            card.Add(QuestArt.Picture("story-ravebridge", "\uf6d9", "rf-qbudget__icon"));
            var text = new VisualElement();
            text.AddToClassList("rf-qbudget__text");
            text.Add(QuestArt.Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_RBQ_POINTS, c.Name), "rf-qbudget__label"));
            var numbers = new VisualElement();
            numbers.AddToClassList("rf-qbudget__numbers");
            numbers.Add(QuestArt.Text(c.State.Points.ToString(CultureInfo.InvariantCulture), "rf-qbudget__value"));
            numbers.Add(QuestArt.Text("/ " + count, "rf-qbudget__of"));
            text.Add(numbers);
            card.Add(text);
            var track = Track(count > 0 ? (float)c.State.Points / count : 0f);
            track.AddToClassList("rf-qbudget__track");
            card.Add(track);
            if (c.State.Titled || (count > 0 && c.State.Points >= count))
                card.Add(QuestArt.Chip("\uf091", Localization.UI_RBQ_TITLED));
            return card;
        }

        /// <summary>A quest under way: its picture, its name, the step it is on, what that asks and how far, and who it is with.</summary>
        private static VisualElement ActiveCard(Character c, RavebridgeQuests.Quest q)
        {
            var state = c.State;
            var at = RavebridgeQuests.StepOf(state, q.key);
            var card = new VisualElement();
            card.AddToClassList("rf-qcard");
            card.AddToClassList("rf-qcard--active");
            card.Add(QuestArt.Picture(Picture(q), "\uf70e", "rf-qcard__picture rf-qcard__picture--plain"));
            var body = new VisualElement();
            body.AddToClassList("rf-qcard__body");
            body.Add(QuestArt.Text((q.bounty ? Localization.UI_STREAMER_QUEST_BOUNTY : string.Format(CultureInfo.InvariantCulture, Localization.UI_STORY_STEP, at + 1, q.steps.Count)).ToUpperInvariant(), "rf-qbadge"));
            body.Add(QuestArt.Text(q.name.Get(), "rf-qcard__title"));

            var objective = new VisualElement();
            objective.AddToClassList("rf-qobjective");
            objective.AddToClassList("rf-rbq__objective");
            var task = RavebridgeQuests.Objective(c.Player, state, q, out var have, out var need);
            var row = new VisualElement();
            row.AddToClassList("rf-qobjective__row");
            row.Add(QuestArt.Text(task, "rf-qobjective__task"));
            if (need > 0) row.Add(QuestArt.Text(have + " / " + need, "rf-qobjective__count"));
            objective.Add(row);
            if (need > 0) objective.Add(Track((float)have / need));
            var npc = RavebridgeQuests.NpcOf(state, q);
            if (npc != null) objective.Add(QuestArt.Chip("\uf007", RavebridgeQuests.NpcName(npc)));
            body.Add(objective);
            var reward = RavebridgeQuests.RewardText(q, IsRavebridgeCharacter(c));
            if (reward.Length > 0) body.Add(QuestArt.Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_RBQ_REWARD, reward), "rf-qcard__reward"));
            card.Add(body);
            return card;
        }

        /// <summary>A quest or bounty not begun: whom to ask, or what it waits for (the quest it follows, a bounty's cooldown).</summary>
        private static VisualElement BeginCard(Character c, RavebridgeQuests.Quest q)
        {
            var state = c.State;
            var card = new VisualElement();
            card.AddToClassList("rf-qcard");
            card.AddToClassList("rf-rbq__begin");
            card.Add(QuestArt.Picture(Picture(q), "\uf70e", "rf-qcard__picture rf-qcard__picture--plain rf-rbq__picture--small"));
            var body = new VisualElement();
            body.AddToClassList("rf-qcard__body");
            body.Add(QuestArt.Text(q.name.Get(), "rf-qcard__title"));
            var npc = RavebridgeQuests.NpcName(q.steps.FirstOrDefault()?.npc);
            string line;
            if (RavebridgeQuests.CanBegin(state, q)) line = string.Format(CultureInfo.InvariantCulture, Localization.UI_RBQ_ASK, npc);
            else if (q.bounty && RavebridgeQuests.BountyMinutes(state, q) > 0f)
                line = string.Format(CultureInfo.InvariantCulture, Localization.UI_RBQ_AGAIN, Mathf.CeilToInt(RavebridgeQuests.BountyMinutes(state, q)), npc);
            else
            {
                var after = RavebridgeQuests.Data.quests.FirstOrDefault(o => o.key == q.steps.FirstOrDefault()?.requires);
                line = string.Format(CultureInfo.InvariantCulture, Localization.UI_RBQ_AFTER, after?.name.Get() ?? "?");
            }
            body.Add(QuestArt.Text(line, "rf-qcard__line"));
            var reward = RavebridgeQuests.RewardText(q, IsRavebridgeCharacter(c));
            if (reward.Length > 0) body.Add(QuestArt.Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_RBQ_REWARD, reward), "rf-qcard__reward"));
            card.Add(body);
            return card;
        }

        private static VisualElement DoneRow(RavebridgeQuests.Quest q)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-qdone");
            row.Add(QuestArt.Picture(Picture(q), "\uf70e", "rf-qdone__picture"));
            var names = new VisualElement();
            names.AddToClassList("rf-qdone__names");
            names.Add(QuestArt.Text(q.name.Get(), "rf-qdone__title"));
            names.Add(QuestArt.Text(RavebridgeQuests.NpcName(q.steps.FirstOrDefault()?.npc), "rf-qdone__npc"));
            row.Add(names);
            row.Add(QuestArt.Glyph("\uf058", "rf-qdone__tick"));
            row.Add(QuestArt.Text(Localization.UI_STORY_DONE, "rf-qdone__state"));
            return row;
        }

        /// <summary>The Ravebridge character (out or not), not one of the streamer's own.</summary>
        private static bool IsRavebridgeCharacter(Character c) => c.Player == null || c.Player.IsLocal;

        /// <summary>Each quest's picture (rb-key); every bounty shares one.</summary>
        private static string Picture(RavebridgeQuests.Quest q) => q.bounty ? "rb-bounty" : "rb-" + q.key;

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
    }
}
