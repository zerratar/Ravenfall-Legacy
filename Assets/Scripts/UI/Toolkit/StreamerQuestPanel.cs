using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The quest window's Streamer quests tab (phase 4 of docs/quests-design.md): the streamer's running bounties and
    /// stream goal with a stop button each, and the form to post a new one: a bounty or a stream goal, a name and a
    /// description of their own, what it asks and how much, how long, how many winners, and the reward (treasury or
    /// character coins, an item from the stash or a character, loyalty points from the week's budget). The server sets
    /// the reward aside when it is posted.
    /// </summary>
    /// <remarks>
    /// Built into StreamQuestView's window. The form keeps its values here, so it can be rebuilt when a chip is picked;
    /// typing only updates the summary, so a field never loses its focus.
    /// </remarks>
    public sealed class StreamerQuestPanel
    {
        private const int ItemsShown = 40;

        private struct ObjectiveChoice
        {
            public string Objective;
            public Skill Skill;
            public Func<string> Label;
        }

        private static readonly ObjectiveChoice[] Choices =
        {
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Woodcutting, Label = () => Localization.UI_STREAMER_OBJ_WOOD },
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Mining, Label = () => Localization.UI_STREAMER_OBJ_ORE },
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Fishing, Label = () => Localization.UI_STREAMER_OBJ_FISH },
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Farming, Label = () => Localization.UI_STREAMER_OBJ_WHEAT },
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Gathering, Label = () => Localization.UI_STREAMER_OBJ_HERBS },
            new ObjectiveChoice { Objective = "kill", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_KILL },
            new ObjectiveChoice { Objective = "craft", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_CRAFT },
            new ObjectiveChoice { Objective = "cook", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_COOK },
            new ObjectiveChoice { Objective = "brew", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_BREW },
            new ObjectiveChoice { Objective = "levels", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_LEVELS },
            new ObjectiveChoice { Objective = "raid", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_RAID },
            new ObjectiveChoice { Objective = "dungeon", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_DUNGEON },
        };

        private static readonly int[] Durations = { 30, 60, 120, 240, 720, 1440, 4320, 10080 };

        private readonly Action redraw;
        private readonly Action<string> showError;

        // the form
        private int kind = StreamerQuests.Bounty;
        private string questName = string.Empty;
        private string description = string.Empty;
        private int choice;
        private long amount = 100;
        private int minutes = 120;
        private long winners = 3;
        private long coins;
        private int coinSource; // 0 the treasury, 1.. the streamer's characters
        private int itemSource;
        private string itemFilter = string.Empty;
        private StreamerShopSellableItem item;
        private long itemAmount = 1;
        private long loyalty;

        private StreamerShopSources sources;
        private bool loadingSources;
        private Label summary;
        private VisualElement itemList;

        public StreamerQuestPanel(Action redraw, Action<string> showError)
        {
            this.redraw = redraw;
            this.showError = showError;
        }

        /// <summary>The form is open: the window does not rebuild for refreshes then.</summary>
        public bool Editing { get; private set; }

        public bool Busy { get; private set; }

        public void Build(VisualElement content)
        {
            var list = StreamerQuests.Latest;
            if (list == null)
            {
                content.Add(Text(Localization.UI_STREAM_QUEST_LOADING, "rf-squest__empty"));
                return;
            }

            if (Editing) BuildForm(content, list);
            else BuildList(content, list);
        }

        // ---- The running quests ----------------------------------------------------------------------------

        private void BuildList(VisualElement content, StreamerQuestListData list)
        {
            content.Add(Text(Localization.UI_STREAMER_QUEST_INTRO, "rf-squest__section"));
            var quests = list.Quests ?? Array.Empty<StreamerQuestData>();
            if (quests.Length == 0) content.Add(Text(Localization.UI_STREAMER_QUEST_NONE, "rf-squest__empty"));
            foreach (var q in quests) content.Add(QuestCard(q));

            content.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_BUDGET,
                StreamerQuests.Number(list.LoyaltyBudgetLeft), StreamerQuests.Number(list.LoyaltyBudgetWeekly)), "rf-squest-form__note"));

            var post = new Button(OpenForm) { text = Localization.UI_STREAMER_QUEST_NEW };
            post.AddToClassList("rf-button");
            post.AddToClassList("rf-button--primary");
            post.AddToClassList("rf-squest-form__post");
            post.SetEnabled(!Busy);
            content.Add(post);
        }

        private VisualElement QuestCard(StreamerQuestData q)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-squest-card");
            card.AddToClassList("rf-squest-card--active");
            var body = new VisualElement();
            body.AddToClassList("rf-squest-card__body");
            var kindText = q.Kind == StreamerQuests.Goal ? Localization.UI_STREAMER_QUEST_GOAL : Localization.UI_STREAMER_QUEST_BOUNTY;
            body.Add(Text(kindText + " - " + StreamerQuests.Title(q), "rf-squest-card__title"));
            if (!string.IsNullOrEmpty(q.Description)) body.Add(Text(q.Description, "rf-squest-card__meta"));

            if (q.Kind == StreamerQuests.Goal)
            {
                body.Add(Track(q.Amount > 0 ? (float)q.Progress / q.Amount : 0f));
                body.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_ACTIVE_META,
                    StreamerQuests.Number(q.Progress), StreamerQuests.Number(q.Amount), StreamerQuests.TimeLeft(q), q.Helpers) + " - " + StreamerQuests.Text(q), "rf-squest-card__meta"));
                body.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_SHARED, StreamerQuests.Reward(q)), "rf-squest-card__reward"));
            }
            else
            {
                body.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_BOUNTY_META,
                    StreamerQuests.Text(q), q.Claimed, q.Winners, StreamerQuests.TimeLeft(q), q.Helpers), "rf-squest-card__meta"));
                body.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_EACH, StreamerQuests.Reward(q)), "rf-squest-card__reward"));
            }

            if (q.TopHelpers != null && q.TopHelpers.Length > 0)
            {
                var label = q.Kind == StreamerQuests.Goal ? Localization.UI_STREAM_QUEST_TOP : Localization.UI_STREAMER_QUEST_WINNERS;
                var names = q.Kind == StreamerQuests.Goal ? StreamQuests.Helpers(q.TopHelpers, 5) : string.Join(", ", q.TopHelpers.Select(h => h.Name));
                body.Add(Text(string.Format(CultureInfo.InvariantCulture, label, names), "rf-squest-card__helpers"));
            }
            card.Add(body);

            var id = q.Id;
            var stop = new Button(() => Stop(id)) { text = Localization.UI_STREAM_QUEST_STOP };
            stop.AddToClassList("rf-button");
            stop.AddToClassList("rf-squest-card__button");
            stop.SetEnabled(!Busy);
            card.Add(stop);
            return card;
        }

        private async void Stop(Guid id)
        {
            var game = GameManager.Instance;
            if (Busy || !game) return;
            Busy = true;
            showError(string.Empty);
            redraw();
            try
            {
                var result = await StreamerQuests.StopAsync(game, id);
                if (result == null || !result.Ok) showError(result?.Error ?? Localization.UI_STREAM_QUEST_FAILED);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("The streamer quest could not be stopped: " + exc.Message);
                showError(Localization.UI_STREAM_QUEST_FAILED);
            }
            finally
            {
                Busy = false;
                redraw();
            }
        }

        // ---- The form --------------------------------------------------------------------------------------

        private void OpenForm()
        {
            Editing = true;
            showError(string.Empty);
            if (sources == null && !loadingSources) LoadSources();
            redraw();
        }

        private async void LoadSources()
        {
            var game = GameManager.Instance;
            if (!game || game.RavenNest == null) return;
            loadingSources = true;
            try
            {
                sources = await game.RavenNest.Shop.GetSourcesAsync();
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("The streamer's items could not be loaded: " + exc.Message);
            }
            finally
            {
                loadingSources = false;
                if (Editing) redraw();
            }
        }

        private void BuildForm(VisualElement content, StreamerQuestListData list)
        {
            var goal = kind == StreamerQuests.Goal;

            content.Add(Label(Localization.UI_STREAMER_QUEST_KIND));
            var kinds = Row();
            kinds.Add(Chip(Localization.UI_STREAMER_QUEST_BOUNTY, !goal, () => { kind = StreamerQuests.Bounty; redraw(); }));
            kinds.Add(Chip(Localization.UI_STREAMER_QUEST_GOAL, goal, () => { kind = StreamerQuests.Goal; redraw(); }));
            content.Add(kinds);
            content.Add(Text(goal ? Localization.UI_STREAMER_QUEST_GOAL_HINT : Localization.UI_STREAMER_QUEST_BOUNTY_HINT, "rf-squest-form__note"));

            content.Add(Label(Localization.UI_STREAMER_QUEST_NAME));
            var nameField = Field(questName, Localization.UI_STREAMER_QUEST_NAME_HINT, v => questName = v);
            nameField.maxLength = 40;
            content.Add(nameField);
            var descriptionField = Field(description, Localization.UI_STREAMER_QUEST_DESCRIPTION_HINT, v => description = v);
            descriptionField.maxLength = 200;
            descriptionField.multiline = true;
            descriptionField.AddToClassList("rf-squest-field--tall");
            content.Add(descriptionField);

            content.Add(Label(Localization.UI_STREAMER_QUEST_WHAT));
            var objectives = Row();
            for (var i = 0; i < Choices.Length; ++i)
            {
                var index = i;
                objectives.Add(Chip(Choices[i].Label(), i == choice, () => { choice = index; redraw(); }));
            }
            content.Add(objectives);

            content.Add(Label(goal ? Localization.UI_STREAMER_QUEST_AMOUNT_GOAL : Localization.UI_STREAMER_QUEST_AMOUNT_BOUNTY));
            content.Add(NumberField(amount, v => amount = v));

            content.Add(Label(Localization.UI_STREAMER_QUEST_TIME));
            var times = Row();
            foreach (var d in Durations)
            {
                var value = d;
                times.Add(Chip(StreamerQuests.Duration(d), d == minutes, () => { minutes = value; redraw(); }));
            }
            content.Add(times);

            content.Add(Label(goal ? Localization.UI_STREAMER_QUEST_WINNERS_GOAL : Localization.UI_STREAMER_QUEST_WINNERS_BOUNTY));
            content.Add(NumberField(winners, v => winners = v));

            // the reward: coins and where from
            content.Add(Label(goal ? Localization.UI_STREAMER_QUEST_COINS_GOAL : Localization.UI_STREAMER_QUEST_COINS_BOUNTY));
            content.Add(NumberField(coins, v => coins = v));
            var coinRow = Row();
            coinRow.Add(Chip(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_TREASURY, StreamerQuests.Number(list.TreasuryCoins)), coinSource == 0, () => { coinSource = 0; redraw(); }));
            var characters = list.Characters ?? Array.Empty<StreamerQuestCoinSourceData>();
            for (var i = 0; i < characters.Length; ++i)
            {
                var index = i + 1;
                coinRow.Add(Chip(characters[i].Name + " (" + StreamerQuests.Number(characters[i].Coins) + ")", coinSource == index, () => { coinSource = index; redraw(); }));
            }
            content.Add(coinRow);

            // an item, from the stash or a character
            content.Add(Label(goal ? Localization.UI_STREAMER_QUEST_ITEM_GOAL : Localization.UI_STREAMER_QUEST_ITEM_BOUNTY));
            BuildItems(content);

            content.Add(Label(goal ? Localization.UI_STREAMER_QUEST_LOYALTY_GOAL : Localization.UI_STREAMER_QUEST_LOYALTY_BOUNTY));
            content.Add(NumberField(loyalty, v => loyalty = v));
            content.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_BUDGET,
                StreamerQuests.Number(list.LoyaltyBudgetLeft), StreamerQuests.Number(list.LoyaltyBudgetWeekly)), "rf-squest-form__note"));

            summary = Text(string.Empty, "rf-squest-form__summary");
            content.Add(summary);
            UpdateSummary();

            var buttons = new VisualElement();
            buttons.AddToClassList("rf-squest-form__buttons");
            var back = new Button(() => { Editing = false; showError(string.Empty); redraw(); }) { text = Localization.UI_STREAMER_QUEST_BACK };
            back.AddToClassList("rf-button");
            var post = new Button(Post) { text = Localization.UI_STREAMER_QUEST_POST };
            post.AddToClassList("rf-button");
            post.AddToClassList("rf-button--primary");
            post.SetEnabled(!Busy);
            buttons.Add(back);
            buttons.Add(new VisualElement { style = { flexGrow = 1 } });
            buttons.Add(post);
            content.Add(buttons);
        }

        private void BuildItems(VisualElement content)
        {
            if (sources == null)
            {
                content.Add(Text(loadingSources ? Localization.UI_STREAM_QUEST_LOADING : Localization.UI_STREAMER_QUEST_NO_ITEMS, "rf-squest-form__note"));
                return;
            }

            var sourceRow = Row();
            sourceRow.Add(Chip(Localization.UI_STREAMER_QUEST_NO_ITEM, item == null, () => { item = null; redraw(); }));
            for (var i = 0; i < sources.Sources.Count; ++i)
            {
                var index = i;
                var s = sources.Sources[i];
                var name = s.Kind == StreamerShopSourceKind.Stash ? Localization.UI_STREAMER_QUEST_STASH : s.Name;
                sourceRow.Add(Chip(name, i == itemSource, () => { itemSource = index; item = null; redraw(); }));
            }
            content.Add(sourceRow);

            var filter = Field(itemFilter, Localization.UI_STREAMER_QUEST_FILTER, v => { itemFilter = v; FillItems(); });
            content.Add(filter);
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rf-squest-items");
            itemList = scroll.contentContainer;
            content.Add(scroll);
            FillItems();

            if (item != null)
            {
                content.Add(Label(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_ITEM_AMOUNT, ItemName(item))));
                content.Add(NumberField(itemAmount, v => itemAmount = v));
            }
        }

        private void FillItems()
        {
            if (itemList == null || sources == null || itemSource >= sources.Sources.Count) return;
            itemList.Clear();
            var shown = 0;
            foreach (var it in sources.Sources[itemSource].Items)
            {
                if (!it.Sellable) continue;
                var name = ItemName(it);
                if (!string.IsNullOrEmpty(itemFilter) && name.IndexOf(itemFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (++shown > ItemsShown) break;
                var picked = it;
                var row = new Button(() => { item = picked; if (itemAmount < 1) itemAmount = 1; redraw(); }) { text = name + "  x" + StreamerQuests.Number(it.Amount) };
                row.AddToClassList("rf-squest-item");
                row.EnableInClassList("rf-squest-item--selected", item != null && Same(item, it));
                itemList.Add(row);
            }
            if (shown == 0) itemList.Add(Text(Localization.UI_STREAMER_QUEST_NO_ITEMS, "rf-squest-form__note"));
        }

        /// <summary>"Sets aside: 15,000 coins, 15 Raw Shrimp and 300 loyalty points".</summary>
        private void UpdateSummary()
        {
            if (summary == null) return;
            var n = kind == StreamerQuests.Goal ? 1 : Math.Max(1, winners);
            var setAside = StreamerQuests.Reward(coins * n, item != null ? ItemName(item) : null, item != null ? itemAmount * Math.Max(1, winners) : 0, loyalty * n);
            summary.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_SETS_ASIDE, setAside);
        }

        private async void Post()
        {
            var game = GameManager.Instance;
            var list = StreamerQuests.Latest;
            if (Busy || !game || list == null) return;

            var c = Choices[choice];
            var characters = list.Characters ?? Array.Empty<StreamerQuestCoinSourceData>();
            var source = sources != null && itemSource < sources.Sources.Count ? sources.Sources[itemSource] : null;
            var request = new StreamerQuestRequestData
            {
                Kind = kind,
                Name = questName,
                Description = description,
                Objective = c.Objective,
                Skill = (int)c.Skill,
                Amount = amount,
                Minutes = minutes,
                Winners = (int)Math.Min(int.MaxValue, winners),
                RewardCoins = coins,
                CoinSource = coinSource == 0 ? 1 : 2,
                CoinCharacterId = coinSource > 0 && coinSource <= characters.Length ? characters[coinSource - 1].CharacterId : (Guid?)null,
                RewardLoyalty = loyalty,
            };
            if (item != null && source != null)
            {
                request.ItemId = item.ItemId;
                request.ItemTag = item.Tag;
                request.ItemEnchantment = item.Enchantment;
                request.ItemTransmogrificationId = item.TransmogrificationId;
                request.ItemName = item.Name;
                request.ItemFlags = item.Flags;
                request.ItemAmount = itemAmount;
                request.ItemSourceKind = source.Kind;
                request.ItemSourceCharacterId = source.CharacterId;
            }

            Busy = true;
            showError(string.Empty);
            redraw();
            try
            {
                var result = await StreamerQuests.PostAsync(game, request);
                if (result != null && result.Ok)
                {
                    Editing = false;
                    sources = null; // items were set aside: ask again next time
                    questName = string.Empty;
                    description = string.Empty;
                    item = null;
                }
                else showError(result?.Error ?? Localization.UI_STREAM_QUEST_FAILED);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("The streamer quest could not be posted: " + exc.Message);
                showError(Localization.UI_STREAM_QUEST_FAILED);
            }
            finally
            {
                Busy = false;
                redraw();
            }
        }

        // ---- Pieces ----------------------------------------------------------------------------------------

        private static string ItemName(StreamerShopSellableItem it) => !string.IsNullOrEmpty(it.Name) ? it.Name : it.ItemName;

        private static bool Same(StreamerShopSellableItem a, StreamerShopSellableItem b) =>
            a.ItemId == b.ItemId && a.Tag == b.Tag && a.Enchantment == b.Enchantment && a.TransmogrificationId == b.TransmogrificationId && a.Name == b.Name && a.Flags == b.Flags;

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

        private static Label Label(string text) => Text(text, "rf-squest-form__label");

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.AddToClassList("rf-squest-chips");
            return row;
        }

        private static Button Chip(string text, bool active, Action clicked)
        {
            var chip = new Button(clicked) { text = text };
            chip.AddToClassList("rf-squest-chip");
            chip.EnableInClassList("rf-squest-chip--active", active);
            return chip;
        }

        private TextField Field(string value, string placeholder, Action<string> changed)
        {
            var field = new TextField { value = value ?? string.Empty };
            field.AddToClassList("rf-squest-field");
            field.textEdition.placeholder = placeholder;
            field.textEdition.hidePlaceholderOnFocus = true;
            field.RegisterValueChangedCallback(e => { changed(e.newValue); UpdateSummary(); });
            return field;
        }

        private TextField NumberField(long value, Action<long> changed)
        {
            var field = new TextField { value = value.ToString(CultureInfo.InvariantCulture) };
            field.AddToClassList("rf-squest-field");
            field.RegisterValueChangedCallback(e =>
            {
                var digits = new string((e.newValue ?? "").Where(char.IsDigit).ToArray());
                if (digits != e.newValue) field.SetValueWithoutNotify(digits);
                changed(long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0);
                UpdateSummary();
            });
            return field;
        }
    }
}
