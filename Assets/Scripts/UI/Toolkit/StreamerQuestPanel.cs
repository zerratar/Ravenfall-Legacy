using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
    /// Built into StreamQuestView's window. The form keeps its values here, so it can be rebuilt when a choice is made;
    /// typing only updates what posting sets aside, so a field never loses its focus. The dropdowns' lists and the item
    /// dialog are laid over the window itself (it has the room), and go when the form is rebuilt or Esc is pressed.
    /// </remarks>
    public sealed class StreamerQuestPanel
    {
        private const int ItemsShown = 40;

        private struct ObjectiveChoice
        {
            public string Objective;
            public Skill Skill;
            public Func<string> Label;
            public string Glyph;
        }

        private static readonly ObjectiveChoice[] Choices =
        {
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Woodcutting, Label = () => Localization.UI_STREAMER_OBJ_WOOD, Glyph = "\uf6b2" },
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Mining, Label = () => Localization.UI_STREAMER_OBJ_ORE, Glyph = "\uf3a5" },
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Fishing, Label = () => Localization.UI_STREAMER_OBJ_FISH, Glyph = "\uf578" },
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Farming, Label = () => Localization.UI_STREAMER_OBJ_WHEAT, Glyph = "\uf72c" },
            new ObjectiveChoice { Objective = "gather", Skill = Skill.Gathering, Label = () => Localization.UI_STREAMER_OBJ_HERBS, Glyph = "\uf06c" },
            new ObjectiveChoice { Objective = "kill", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_KILL, Glyph = "\uf71d" },
            new ObjectiveChoice { Objective = "craft", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_CRAFT, Glyph = "\uf6e3" },
            new ObjectiveChoice { Objective = "cook", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_COOK, Glyph = "\uf2e7" },
            new ObjectiveChoice { Objective = "brew", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_BREW, Glyph = "\uf0c3" },
            new ObjectiveChoice { Objective = "levels", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_LEVELS, Glyph = "\uf062" },
            new ObjectiveChoice { Objective = "raid", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_RAID, Glyph = "\uf6d5" },
            new ObjectiveChoice { Objective = "dungeon", Skill = (Skill)(-1), Label = () => Localization.UI_STREAMER_OBJ_DUNGEON, Glyph = "\uf6d9" },
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
        private StreamerShopSellableItem item;
        private long itemAmount = 1;
        private long loyalty;

        private StreamerShopSources sources;
        private bool loadingSources;

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
            CloseOverlays();
            window = WindowOf(content);
            var list = StreamerQuests.Latest;
            if (list == null)
            {
                content.Add(Text(Localization.UI_STREAM_QUEST_LOADING, "rf-squest__empty"));
                return;
            }

            if (!Editing)
            {
                BuildList(content, list);
                return;
            }
            BuildForm(content, list);
            if (picking) ShowPicker();
        }

        // ---- The running quests ----------------------------------------------------------------------------

        private void BuildList(VisualElement content, StreamerQuestListData list)
        {
            QuestArt.Heading(content, Localization.UI_QUESTWIN_CREATE, Localization.UI_QUESTWIN_CREATE_SUB);
            content.Add(Budget(list));

            var quests = list.Quests ?? Array.Empty<StreamerQuestData>();
            if (quests.Length == 0)
            {
                content.Add(Empty());
                return;
            }

            foreach (var q in quests) content.Add(QuestCard(q));
            var row = new VisualElement();
            row.AddToClassList("rf-qempty__post-row");
            row.Add(PostButton());
            content.Add(row);
        }

        /// <summary>The tab's footer: what posting does with the reward.</summary>
        public void BuildFooter(VisualElement footer)
        {
            footer.Add(QuestArt.Glyph("\uf05a", "rf-squest__footer-icon"));
            footer.Add(QuestArt.Text(Localization.UI_QUESTWIN_RESERVED, "rf-squest__footer-text"));
        }

        /// <summary>The loyalty points left to give this week, as a number and a bar.</summary>
        private static VisualElement Budget(StreamerQuestListData list)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-qbudget");
            card.Add(QuestArt.Picture("icon-loyalty", "\uf5a2", "rf-qbudget__icon"));
            var text = new VisualElement();
            text.AddToClassList("rf-qbudget__text");
            text.Add(Text(Localization.UI_QUESTWIN_WEEKLY, "rf-qbudget__label"));
            var numbers = new VisualElement();
            numbers.AddToClassList("rf-qbudget__numbers");
            numbers.Add(Text(StreamerQuests.Number(list.LoyaltyBudgetLeft), "rf-qbudget__value"));
            numbers.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_QUESTWIN_AVAILABLE, StreamerQuests.Number(list.LoyaltyBudgetWeekly)), "rf-qbudget__of"));
            text.Add(numbers);
            card.Add(text);
            var track = Track(list.LoyaltyBudgetWeekly > 0 ? (float)list.LoyaltyBudgetLeft / list.LoyaltyBudgetWeekly : 0f);
            track.AddToClassList("rf-qbudget__track");
            card.Add(track);
            return card;
        }

        /// <summary>No quest of the streamer's running: a picture, a nudge and the post button.</summary>
        private VisualElement Empty()
        {
            var empty = new VisualElement();
            empty.AddToClassList("rf-qempty");
            empty.Add(QuestArt.Picture("empty-board", "\uf70e", "rf-qempty__picture"));
            empty.Add(Text(Localization.UI_QUESTWIN_EMPTY, "rf-qempty__title"));
            empty.Add(Text(Localization.UI_QUESTWIN_EMPTY_SUB, "rf-qempty__sub"));
            empty.Add(PostButton());
            var kinds = new VisualElement();
            kinds.AddToClassList("rf-qempty__kinds");
            kinds.Add(QuestArt.Chip("\uf091", Localization.UI_QUESTWIN_FIRST));
            kinds.Add(QuestArt.Divider());
            kinds.Add(QuestArt.Chip("\uf0c0", Localization.UI_QUESTWIN_WHOLE));
            empty.Add(kinds);
            return empty;
        }

        private Button PostButton()
        {
            var post = new Button(OpenForm);
            post.AddToClassList("rf-button");
            post.AddToClassList("rf-button--primary");
            post.AddToClassList("rf-qpost");
            post.Add(QuestArt.Glyph("\uf067", "rf-qpost__icon"));
            post.Add(Text(Localization.UI_STREAMER_QUEST_NEW, "rf-qpost__text"));
            post.SetEnabled(!Busy);
            return post;
        }

        private VisualElement QuestCard(StreamerQuestData q)
        {
            var goal = q.Kind == StreamerQuests.Goal;
            var card = new VisualElement();
            card.AddToClassList("rf-qcard");
            card.AddToClassList("rf-qcard--active");
            card.Add(QuestArt.Picture(QuestArt.QuestPicture(q.Objective, q.Skill), QuestArt.QuestGlyph(q.Objective), "rf-qcard__picture"));

            var body = new VisualElement();
            body.AddToClassList("rf-qcard__body");
            body.Add(Text((goal ? Localization.UI_STREAMER_QUEST_GOAL : Localization.UI_STREAMER_QUEST_BOUNTY).ToUpperInvariant(), "rf-qbadge"));
            var titleRow = new VisualElement();
            titleRow.AddToClassList("rf-qcard__title-row");
            titleRow.Add(Text(StreamerQuests.Title(q), "rf-qcard__title"));
            if (goal) titleRow.Add(Text(StreamerQuests.Number(q.Progress) + " / " + StreamerQuests.Number(q.Amount), "rf-qcard__count"));
            body.Add(titleRow);
            body.Add(Text(string.IsNullOrEmpty(q.Description) ? StreamerQuests.Text(q) : q.Description, "rf-qcard__line"));
            if (goal) body.Add(Track(q.Amount > 0 ? (float)q.Progress / q.Amount : 0f));
            else body.Add(Track(q.Winners > 0 ? (float)q.Claimed / q.Winners : 0f));

            var chips = new VisualElement();
            chips.AddToClassList("rf-qcard__chips");
            chips.Add(QuestArt.Chip("\uf017", string.Format(CultureInfo.InvariantCulture, Localization.UI_QUESTWIN_LEFT, StreamerQuests.TimeLeft(q))));
            chips.Add(QuestArt.Chip("\uf0c0", string.Format(CultureInfo.InvariantCulture, Localization.UI_QUESTWIN_HELPING, q.Helpers)));
            if (!goal) chips.Add(QuestArt.Chip("\uf091", q.Claimed + " / " + q.Winners)); // the winners so far
            body.Add(chips);
            body.Add(Text(string.Format(CultureInfo.InvariantCulture, goal ? Localization.UI_STREAMER_QUEST_SHARED : Localization.UI_STREAMER_QUEST_EACH, StreamerQuests.Reward(q)), "rf-qcard__reward"));

            if (q.TopHelpers != null && q.TopHelpers.Length > 0)
            {
                var label = goal ? Localization.UI_STREAM_QUEST_TOP : Localization.UI_STREAMER_QUEST_WINNERS;
                var names = goal ? StreamQuests.Helpers(q.TopHelpers, 5) : string.Join(", ", q.TopHelpers.Select(h => h.Name));
                body.Add(Text(string.Format(CultureInfo.InvariantCulture, label, names), "rf-qcard__helpers"));
            }
            card.Add(body);

            var id = q.Id;
            var stop = new Button(() => Stop(id)) { text = Localization.UI_QUESTWIN_STOP };
            stop.AddToClassList("rf-button");
            stop.AddToClassList("rf-qcard__button");
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

        /// <summary>
        /// The form, after the user's mockup: a back arrow and the title; Quest details on the left (a bounty or a stream
        /// goal, the name, the chat description, the activity and its target, how long and how many win), the rewards on
        /// the right (coins and where they come from, an item picked in its own dialog and how many, loyalty points), what
        /// posting sets aside, then Back and Post quest.
        /// </summary>
        private void BuildForm(VisualElement content, StreamerQuestListData list)
        {
            var goal = kind == StreamerQuests.Goal;

            var top = new VisualElement();
            top.AddToClassList("rf-qform__top");
            var back = new Button(CloseForm) { text = "" };
            back.AddToClassList("rf-icon");
            back.AddToClassList("rf-qform__back");
            top.Add(back);
            var titles = new VisualElement();
            titles.Add(Text(Localization.UI_STREAMER_QUEST_NEW, "rf-qheading"));
            titles.Add(Text(Localization.UI_POSTQ_SUB, "rf-qheading__sub"));
            top.Add(titles);
            content.Add(top);

            var columns = new VisualElement();
            columns.AddToClassList("rf-qform__columns");
            columns.Add(Details(goal));
            columns.Add(Rewards(goal, list));
            content.Add(columns);

            content.Add(Reserved());
            UpdateSummary();

            var buttons = new VisualElement();
            buttons.AddToClassList("rf-squest-form__buttons");
            var backButton = new Button(CloseForm) { text = Localization.UI_STREAMER_QUEST_BACK };
            backButton.AddToClassList("rf-button");
            backButton.AddToClassList("rf-qform__button");
            var post = new Button(Post) { text = Localization.UI_STREAMER_QUEST_POST };
            post.AddToClassList("rf-button");
            post.AddToClassList("rf-button--primary");
            post.AddToClassList("rf-qform__button");
            post.SetEnabled(!Busy);
            buttons.Add(backButton);
            buttons.Add(new VisualElement { style = { flexGrow = 1 } });
            buttons.Add(post);
            content.Add(buttons);
        }

        private void CloseForm()
        {
            Editing = false;
            picking = false;
            CloseOverlays();
            showError(string.Empty);
            redraw();
        }

        private VisualElement Details(bool goal)
        {
            var card = Card(Localization.UI_POSTQ_DETAILS);
            var kinds = new VisualElement();
            kinds.AddToClassList("rf-qform__kinds");
            kinds.Add(KindCard(StreamerQuests.Bounty, "", Localization.UI_STREAMER_QUEST_BOUNTY, Localization.UI_POSTQ_BOUNTY_SUB));
            kinds.Add(KindCard(StreamerQuests.Goal, "", Localization.UI_STREAMER_QUEST_GOAL, Localization.UI_POSTQ_GOAL_SUB));
            card.Add(kinds);
            card.Add(Text(goal ? Localization.UI_POSTQ_GOAL_NOTE : Localization.UI_POSTQ_BOUNTY_NOTE, "rf-qform__note"));

            card.Add(FieldLabel(Localization.UI_POSTQ_NAME, true));
            var nameField = Field(questName, Localization.UI_STREAMER_QUEST_NAME_HINT, v => questName = v);
            nameField.maxLength = 40;
            card.Add(nameField);
            card.Add(FieldLabel(Localization.UI_POSTQ_DESCRIPTION, true));
            var descriptionField = Field(description, Localization.UI_STREAMER_QUEST_DESCRIPTION_HINT, v => description = v);
            descriptionField.maxLength = 200;
            descriptionField.multiline = true;
            descriptionField.AddToClassList("rf-squest-field--tall");
            card.Add(descriptionField);

            var activity = Choices[choice];
            var activities = Choices.Select((c, i) => new Option(c.Label(), c.Glyph, i == choice, () => { choice = i; redraw(); })).ToList();
            card.Add(Pair(
                Labelled(Localization.UI_POSTQ_ACTIVITY, Dropdown(activity.Glyph, activity.Label(), activities)),
                Labelled(goal ? Localization.UI_POSTQ_TARGET_GOAL : Localization.UI_POSTQ_TARGET_BOUNTY, Spinner(amount, v => amount = v))));
            var times = Durations.Select(d => new Option(StreamerQuests.Duration(d), null, d == minutes, () => { minutes = d; redraw(); })).ToList();
            card.Add(Pair(
                Labelled(Localization.UI_POSTQ_DURATION, Dropdown(null, StreamerQuests.Duration(minutes), times)),
                Labelled(goal ? Localization.UI_POSTQ_WINNERS_GOAL : Localization.UI_POSTQ_WINNERS_BOUNTY, Spinner(winners, v => winners = v))));
            return card;
        }

        /// <summary>A bounty or a stream goal, as a card with its icon, a line under its name and a radio dot.</summary>
        private Button KindCard(int value, string glyph, string title, string sub)
        {
            var card = new Button(() => { if (kind == value) return; kind = value; redraw(); });
            card.AddToClassList("rf-qkind");
            card.EnableInClassList("rf-qkind--active", kind == value);
            card.Add(QuestArt.Glyph(glyph, "rf-qkind__icon"));
            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-qkind__text");
            text.Add(Text(title, "rf-qkind__title"));
            text.Add(Text(sub, "rf-qkind__sub"));
            card.Add(text);
            var radio = new VisualElement { pickingMode = PickingMode.Ignore };
            radio.AddToClassList("rf-qkind__radio");
            card.Add(radio);
            return card;
        }

        private VisualElement Rewards(bool goal, StreamerQuestListData list)
        {
            var card = Card(goal ? Localization.UI_POSTQ_REWARDS_GOAL : Localization.UI_POSTQ_REWARDS_BOUNTY);
            card.AddToClassList("rf-qform__card--rewards");

            // coins, and where they come from: the treasury or one of the streamer's characters
            card.Add(FieldLabel(Localization.UI_POSTQ_COINS, true));
            card.Add(Spinner(coins, v => coins = v, QuestArt.Coin()));
            var characters = list.Characters ?? Array.Empty<StreamerQuestCoinSourceData>();
            if (coinSource > characters.Length) coinSource = 0;
            var from = coinSource == 0 ? Localization.UI_POSTQ_TREASURY : characters[coinSource - 1].Name;
            var available = coinSource == 0 ? list.TreasuryCoins : characters[coinSource - 1].Coins;
            var froms = new List<Option> { new Option(Localization.UI_POSTQ_TREASURY + " (" + Number(list.TreasuryCoins) + ")", "", coinSource == 0, () => { coinSource = 0; redraw(); }) };
            for (var i = 0; i < characters.Length; ++i)
            {
                var index = i + 1;
                froms.Add(new Option(characters[i].Name + " (" + Number(characters[i].Coins) + ")", "", coinSource == index, () => { coinSource = index; redraw(); }));
            }
            card.Add(Dropdown(null, string.Format(CultureInfo.InvariantCulture, Localization.UI_POSTQ_FROM, from, Number(available)), froms, link: true));
            card.Add(Rule());

            card.Add(FieldLabel(Localization.UI_POSTQ_ITEM, true));
            card.Add(ItemCard());
            if (item != null)
            {
                card.Add(FieldLabel(goal ? Localization.UI_POSTQ_QUANTITY_GOAL : Localization.UI_POSTQ_QUANTITY_BOUNTY, false));
                var quantity = Spinner(itemAmount, v => itemAmount = Math.Max(1, v));
                quantity.AddToClassList("rf-qnumber--short");
                card.Add(quantity);
            }
            card.Add(Rule());

            card.Add(FieldLabel(Localization.UI_POSTQ_LOYALTY, true));
            card.Add(Spinner(loyalty, v => loyalty = v, QuestArt.Icon("icon-loyalty", "")));
            card.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_POSTQ_LOYALTY_LEFT, Number(list.LoyaltyBudgetLeft)), "rf-qform__note"));
            return card;
        }

        /// <summary>The picked item with where it comes from, how many there are, Change and a cross; or a button to pick one.</summary>
        private VisualElement ItemCard()
        {
            if (item == null)
            {
                var choose = new Button(OpenPicker);
                choose.AddToClassList("rf-qitem-choose");
                choose.Add(QuestArt.Glyph("", "rf-qitem-choose__icon"));
                choose.Add(Text(Localization.UI_POSTQ_CHOOSE_ITEM, "rf-qitem-choose__text"));
                choose.SetEnabled(!Busy);
                return choose;
            }

            var card = new VisualElement();
            card.AddToClassList("rf-qitem");
            card.Add(ItemIcon(item, "rf-qitem__icon"));
            var text = new VisualElement();
            text.AddToClassList("rf-qitem__text");
            text.Add(Text(ItemName(item), "rf-qitem__name"));
            text.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_POSTQ_FROM_SOURCE, SourceName(itemSource)), "rf-qitem__line"));
            text.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_POSTQ_OWNED, Number(item.Amount)), "rf-qitem__line"));
            card.Add(text);
            var change = new Button(OpenPicker) { text = Localization.UI_POSTQ_CHANGE };
            change.AddToClassList("rf-button");
            change.AddToClassList("rf-qitem__change");
            var remove = new Button(() => { item = null; redraw(); }) { text = "" };
            remove.AddToClassList("rf-icon");
            remove.AddToClassList("rf-qitem__remove");
            card.Add(change);
            card.Add(remove);
            return card;
        }

        // ---- What posting sets aside -----------------------------------------------------------------------

        private Label reservedSub;
        private Label reservedCoins;
        private Label reservedItems;
        private Label reservedLoyalty;
        private Label reservedNothing;
        private VisualElement reservedCoinsPart;
        private VisualElement reservedItemsPart;
        private VisualElement reservedLoyaltyPart;

        private VisualElement Reserved()
        {
            var bar = new VisualElement();
            bar.AddToClassList("rf-qreserved");
            var head = new VisualElement();
            head.AddToClassList("rf-qreserved__head");
            head.Add(Text(Localization.UI_POSTQ_RESERVED, "rf-qreserved__title"));
            reservedSub = Text(string.Empty, "rf-qreserved__sub");
            head.Add(reservedSub);
            bar.Add(head);
            reservedCoinsPart = ReservedPart(QuestArt.Coin(), out reservedCoins);
            reservedItemsPart = ReservedPart(item != null ? ItemIcon(item, "rf-qart-icon") : new VisualElement(), out reservedItems);
            reservedLoyaltyPart = ReservedPart(QuestArt.Icon("icon-loyalty", ""), out reservedLoyalty);
            reservedNothing = Text(Localization.UI_POSTQ_NOTHING, "rf-qreserved__nothing");
            bar.Add(reservedCoinsPart);
            bar.Add(reservedItemsPart);
            bar.Add(reservedLoyaltyPart);
            bar.Add(reservedNothing);
            return bar;
        }

        private static VisualElement ReservedPart(VisualElement icon, out Label text)
        {
            var part = new VisualElement();
            part.AddToClassList("rf-qreserved__part");
            part.Add(QuestArt.Divider());
            part.Add(icon);
            text = Text(string.Empty, "rf-qreserved__amount");
            part.Add(text);
            return part;
        }

        /// <summary>
        /// What posting sets aside as the fields read now: a bounty's rewards times its winners; a goal's coins and points
        /// once (they are shared), its item once for each top helper.
        /// </summary>
        private void UpdateSummary()
        {
            if (reservedSub == null) return;
            var goal = kind == StreamerQuests.Goal;
            var n = Math.Max(1, winners);
            var shared = goal ? 1 : n;
            var totalCoins = coins * shared;
            var totalLoyalty = loyalty * shared;
            var totalItems = item != null ? itemAmount * n : 0;
            reservedSub.text = string.Format(CultureInfo.InvariantCulture, goal ? Localization.UI_POSTQ_RESERVED_GOAL : Localization.UI_POSTQ_RESERVED_BOUNTY, n);
            reservedCoins.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_COINS, Number(totalCoins));
            reservedItems.text = item != null ? Number(totalItems) + " " + ItemName(item) : string.Empty;
            reservedLoyalty.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAMER_QUEST_LOYALTY, Number(totalLoyalty));
            SetShown(reservedCoinsPart, totalCoins > 0);
            SetShown(reservedItemsPart, totalItems > 0);
            SetShown(reservedLoyaltyPart, totalLoyalty > 0);
            SetShown(reservedNothing, totalCoins <= 0 && totalItems <= 0 && totalLoyalty <= 0);
            UpdatePickNote();
        }

        // ---- The item dialog -------------------------------------------------------------------------------
        // Its own dialog over the window: search, where from (the stash or a character), a category, the items with
        // their icons and how many there are, and how many each winner gets. Use item puts it on the form.

        private bool picking;
        private VisualElement modal;
        private int pickSource;
        private string pickCategory;
        private string pickFilter = string.Empty;
        private StreamerShopSellableItem pickItem;
        private long pickAmount = 1;
        private VisualElement pickList;
        private VisualElement pickFoot;
        private Label pickNote;
        private Button useButton;

        private void OpenPicker()
        {
            picking = true;
            pickSource = itemSource;
            pickItem = item;
            pickAmount = Math.Max(1, itemAmount);
            pickFilter = string.Empty;
            pickCategory = null;
            if (sources == null && !loadingSources) LoadSources();
            ShowPicker();
        }

        private void ClosePicker()
        {
            picking = false;
            ClosePopup();
            modal?.RemoveFromHierarchy();
            modal = null;
            pickList = null;
            pickFoot = null;
            pickNote = null;
            useButton = null;
        }

        private void ShowPicker()
        {
            ClosePopup();
            modal?.RemoveFromHierarchy();
            if (window == null) return;

            modal = new VisualElement();
            modal.AddToClassList("rf-qmodal");
            var scrim = modal;
            modal.RegisterCallback<PointerDownEvent>(e => { if (e.target == scrim) ClosePicker(); });
            var dialog = new VisualElement();
            dialog.AddToClassList("rf-qmodal__dialog");

            var head = new VisualElement();
            head.AddToClassList("rf-qmodal__head");
            head.Add(Text(Localization.UI_POSTQ_CHOOSE_ITEM, "rf-qheading"));
            var close = new Button(ClosePicker) { text = "" };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-squest__close");
            head.Add(close);
            dialog.Add(head);
            dialog.Add(Text(Localization.UI_POSTQ_PICK_SUB, "rf-qheading__sub"));

            var tools = new VisualElement();
            tools.AddToClassList("rf-qmodal__tools");
            var search = new VisualElement();
            search.AddToClassList("rf-qsearch");
            search.Add(QuestArt.Glyph("", "rf-qsearch__icon"));
            var field = new TextField { value = pickFilter };
            field.AddToClassList("rf-squest-field");
            field.AddToClassList("rf-qsearch__field");
            field.textEdition.placeholder = Localization.UI_POSTQ_SEARCH;
            field.textEdition.hidePlaceholderOnFocus = true;
            field.RegisterValueChangedCallback(e => { pickFilter = e.newValue ?? string.Empty; FillPicker(); });
            search.Add(field);
            tools.Add(search);
            if (sources != null && sources.Sources.Count > 1)
            {
                var froms = sources.Sources.Select((s, i) => new Option(SourceTitle(i), s.Kind == StreamerShopSourceKind.Stash ? "" : "", i == pickSource,
                    () => { pickSource = i; pickCategory = null; pickItem = null; ShowPicker(); })).ToList();
                tools.Add(Dropdown(null, SourceTitle(pickSource), froms));
            }
            var categories = new List<Option> { new Option(Localization.UI_POSTQ_ALL_ITEMS, null, pickCategory == null, () => { pickCategory = null; ShowPicker(); }) };
            foreach (var c in Categories())
            {
                var category = c;
                categories.Add(new Option(category, null, category == pickCategory, () => { pickCategory = category; ShowPicker(); }));
            }
            tools.Add(Dropdown(null, pickCategory ?? Localization.UI_POSTQ_ALL_ITEMS, categories));
            dialog.Add(tools);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rf-qmodal__list");
            pickList = scroll.contentContainer;
            dialog.Add(scroll);
            pickFoot = new VisualElement();
            pickFoot.AddToClassList("rf-qmodal__foot");
            dialog.Add(pickFoot);

            var buttons = new VisualElement();
            buttons.AddToClassList("rf-squest-form__buttons");
            var cancel = new Button(ClosePicker) { text = Localization.UI_POSTQ_CANCEL };
            cancel.AddToClassList("rf-button");
            cancel.AddToClassList("rf-qform__button");
            useButton = new Button(UsePicked) { text = Localization.UI_POSTQ_USE };
            useButton.AddToClassList("rf-button");
            useButton.AddToClassList("rf-button--primary");
            useButton.AddToClassList("rf-qform__button");
            buttons.Add(cancel);
            buttons.Add(new VisualElement { style = { flexGrow = 1 } });
            buttons.Add(useButton);
            dialog.Add(buttons);

            modal.Add(dialog);
            window.Add(modal);
            FillPicker();
            FillPickFoot();
        }

        private void UsePicked()
        {
            if (pickItem == null) return;
            item = pickItem;
            itemSource = pickSource;
            itemAmount = Math.Max(1, pickAmount);
            ClosePicker();
            redraw();
        }

        private void FillPicker()
        {
            if (pickList == null) return;
            pickList.Clear();
            if (sources == null)
            {
                pickList.Add(Text(loadingSources ? Localization.UI_STREAM_QUEST_LOADING : Localization.UI_STREAMER_QUEST_NO_ITEMS, "rf-qform__note"));
                return;
            }
            if (pickSource >= sources.Sources.Count) pickSource = 0;
            if (sources.Sources.Count == 0) return;

            var shown = 0;
            foreach (var it in sources.Sources[pickSource].Items)
            {
                if (!it.Sellable) continue;
                var name = ItemName(it);
                if (pickFilter.Length > 0 && name.IndexOf(pickFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var category = Category(it);
                if (pickCategory != null && category != pickCategory) continue;
                if (++shown > ItemsShown) break;

                var picked = it;
                var selected = pickItem != null && Same(pickItem, it);
                var row = new Button(() => { pickItem = picked; FillPicker(); FillPickFoot(); });
                row.AddToClassList("rf-qpick");
                row.EnableInClassList("rf-qpick--selected", selected);
                row.Add(ItemIcon(it, "rf-qpick__icon"));
                var text = new VisualElement { pickingMode = PickingMode.Ignore };
                text.AddToClassList("rf-qpick__text");
                text.Add(Text(name, "rf-qpick__name"));
                if (category.Length > 0) text.Add(Text(category, "rf-qpick__category"));
                row.Add(text);
                row.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_POSTQ_OWNED, Number(it.Amount)), "rf-qpick__owned"));
                var tick = QuestArt.Glyph("", "rf-qpick__tick");
                tick.style.visibility = selected ? Visibility.Visible : Visibility.Hidden;
                row.Add(tick);
                pickList.Add(row);
            }
            if (shown == 0) pickList.Add(Text(Localization.UI_POSTQ_NONE_FOUND, "rf-qform__note"));
        }

        /// <summary>The picked item along the dialog's bottom, with how many each winner gets and what that sets aside.</summary>
        private void FillPickFoot()
        {
            if (pickFoot == null) return;
            pickFoot.Clear();
            pickNote = null;
            useButton?.SetEnabled(pickItem != null && !Busy);
            SetShown(pickFoot, pickItem != null);
            if (pickItem == null) return;

            pickFoot.Add(ItemIcon(pickItem, "rf-qmodal__foot-icon"));
            var text = new VisualElement();
            text.AddToClassList("rf-qmodal__foot-text");
            text.Add(Text(ItemName(pickItem), "rf-qitem__name"));
            text.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_POSTQ_OWNED, Number(pickItem.Amount)), "rf-qitem__line"));
            pickFoot.Add(text);

            var quantity = new VisualElement();
            quantity.AddToClassList("rf-qmodal__quantity");
            quantity.Add(FieldLabel(kind == StreamerQuests.Goal ? Localization.UI_POSTQ_QUANTITY_GOAL : Localization.UI_POSTQ_QUANTITY_BOUNTY, false));
            var stepper = new VisualElement();
            stepper.AddToClassList("rf-qstepper");
            TextField field = null;
            var minus = new Button(() => field.value = Math.Max(1, pickAmount - 1).ToString(CultureInfo.InvariantCulture)) { text = "" };
            field = NumberField(pickAmount, v => { pickAmount = Math.Max(1, v); UpdatePickNote(); });
            var plus = new Button(() => field.value = (pickAmount + 1).ToString(CultureInfo.InvariantCulture)) { text = "" };
            minus.AddToClassList("rf-icon");
            minus.AddToClassList("rf-qstepper__button");
            plus.AddToClassList("rf-icon");
            plus.AddToClassList("rf-qstepper__button");
            field.AddToClassList("rf-qstepper__field");
            stepper.Add(minus);
            stepper.Add(field);
            stepper.Add(plus);
            quantity.Add(stepper);
            pickNote = Text(string.Empty, "rf-qform__note");
            quantity.Add(pickNote);
            pickFoot.Add(quantity);
            UpdatePickNote();
        }

        private void UpdatePickNote()
        {
            if (pickNote == null || pickItem == null) return;
            var n = Math.Max(1, winners);
            pickNote.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_POSTQ_PICK_NOTE, n, Number(pickAmount), Number(n * pickAmount) + " " + ItemName(pickItem));
        }

        /// <summary>The categories of the items in the picked source, for the dialog's filter.</summary>
        private List<string> Categories()
        {
            if (sources == null || pickSource >= sources.Sources.Count) return new List<string>();
            return sources.Sources[pickSource].Items.Where(i => i.Sellable).Select(Category).Where(c => c.Length > 0).Distinct().OrderBy(c => c).ToList();
        }

        private static string Category(StreamerShopSellableItem it)
        {
            var game = GameManager.Instance;
            var definition = game && game.Items != null ? game.Items.Get(it.ItemId) : null;
            return definition != null ? Regex.Replace(definition.Category.ToString(), "(?<=[a-z])(?=[A-Z])", " ") : string.Empty;
        }

        private string SourceName(int index)
        {
            if (sources == null || index >= sources.Sources.Count) return Localization.UI_POSTQ_STASH;
            var s = sources.Sources[index];
            return s.Kind == StreamerShopSourceKind.Stash ? Localization.UI_POSTQ_STASH : s.Name;
        }

        private string SourceTitle(int index)
        {
            if (sources == null || index >= sources.Sources.Count) return Localization.UI_STREAMER_QUEST_STASH;
            var s = sources.Sources[index];
            return s.Kind == StreamerShopSourceKind.Stash ? Localization.UI_STREAMER_QUEST_STASH : s.Name;
        }

        // ---- Dropdowns -------------------------------------------------------------------------------------
        // Drawn here rather than with Unity's runtime dropdowns: a button with the value and a chevron opens a list
        // under it (over it when there is no room below) on top of the window; a click outside the list closes it.

        private sealed class Option
        {
            public readonly string Text;
            public readonly string Glyph;
            public readonly bool Active;
            public readonly Action Pick;

            public Option(string text, string glyph, bool active, Action pick)
            {
                Text = text;
                Glyph = glyph;
                Active = active;
                Pick = pick;
            }
        }

        private VisualElement window;
        private VisualElement popup;

        private Button Dropdown(string glyph, string text, List<Option> options, bool link = false)
        {
            var button = new Button();
            button.AddToClassList("rf-qdropdown");
            button.EnableInClassList("rf-qdropdown--link", link);
            if (glyph != null) button.Add(QuestArt.Glyph(glyph, "rf-qdropdown__icon"));
            button.Add(Text(text, "rf-qdropdown__text"));
            button.Add(QuestArt.Glyph("", "rf-qdropdown__chevron"));
            button.clicked += () => OpenPopup(button, options);
            return button;
        }

        private void OpenPopup(VisualElement anchor, List<Option> options)
        {
            ClosePopup();
            if (window == null) return;
            popup = new VisualElement();
            popup.AddToClassList("rf-qpopup-catcher");
            var catcher = popup;
            popup.RegisterCallback<PointerDownEvent>(e => { if (e.target == catcher) ClosePopup(); });

            var list = new ScrollView(ScrollViewMode.Vertical);
            list.AddToClassList("rf-qpopup");
            // absolute children are placed from the window's padding box, inside its border
            var a = anchor.worldBound;
            var w = window.worldBound;
            var x = w.x + window.resolvedStyle.borderLeftWidth;
            var y = w.y + window.resolvedStyle.borderTopWidth;
            list.style.left = a.x - x;
            list.style.width = Mathf.Max(a.width, 220f);
            var below = w.yMax - a.yMax;
            var above = a.yMin - w.y;
            if (below >= 240f || below >= above)
            {
                list.style.top = a.yMax - y + 4f;
                list.style.maxHeight = Mathf.Max(80f, below - 16f);
            }
            else
            {
                list.style.bottom = w.yMax - a.yMin + 4f;
                list.style.maxHeight = Mathf.Max(80f, above - 16f);
            }

            foreach (var o in options)
            {
                var option = o;
                var row = new Button(() => { ClosePopup(); option.Pick(); });
                row.AddToClassList("rf-qpopup__row");
                row.EnableInClassList("rf-qpopup__row--active", o.Active);
                if (o.Glyph != null) row.Add(QuestArt.Glyph(o.Glyph, "rf-qpopup__icon"));
                row.Add(Text(o.Text, "rf-qpopup__text"));
                list.Add(row);
            }
            popup.Add(list);
            window.Add(popup);
        }

        private void ClosePopup()
        {
            popup?.RemoveFromHierarchy();
            popup = null;
        }

        private void CloseOverlays()
        {
            ClosePopup();
            modal?.RemoveFromHierarchy();
            modal = null;
        }

        /// <summary>Esc: closes an open list or the item dialog, and says so; the window closes otherwise.</summary>
        public bool HideOverlay()
        {
            if (popup != null)
            {
                ClosePopup();
                return true;
            }
            if (picking)
            {
                ClosePicker();
                return true;
            }
            return false;
        }

        /// <summary>Leaving the tab: no list or dialog is left over the window.</summary>
        public void HideOverlays()
        {
            picking = false;
            CloseOverlays();
        }

        private static VisualElement WindowOf(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
            {
                if (e.ClassListContains("rf-squest--window")) return e;
            }
            return null;
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

        private static string Number(long value) => StreamerQuests.Number(value);

        private static void SetShown(VisualElement element, bool shown)
        {
            if (element != null) element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>One of the form's two columns, with its title.</summary>
        private static VisualElement Card(string title)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-qform__card");
            card.Add(Text(title, "rf-qform__title"));
            return card;
        }

        private static VisualElement Rule()
        {
            var rule = new VisualElement();
            rule.AddToClassList("rf-qform__rule");
            return rule;
        }

        /// <summary>A field's name, with a dim "Optional" after it where it may be left empty.</summary>
        private static VisualElement FieldLabel(string text, bool optional)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-qform__label-row");
            row.Add(Text(text, "rf-qform__label"));
            if (optional) row.Add(Text(Localization.UI_POSTQ_OPTIONAL, "rf-qform__optional"));
            return row;
        }

        private static VisualElement Labelled(string label, VisualElement control)
        {
            var cell = new VisualElement();
            cell.AddToClassList("rf-qform__cell");
            cell.Add(FieldLabel(label, false));
            cell.Add(control);
            return cell;
        }

        /// <summary>Two fields side by side: a wide one and a narrow one.</summary>
        private static VisualElement Pair(VisualElement wide, VisualElement narrow)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-qform__pair");
            wide.AddToClassList("rf-qform__cell--wide");
            narrow.AddToClassList("rf-qform__cell--narrow");
            row.Add(wide);
            row.Add(narrow);
            return row;
        }

        private static VisualElement ItemIcon(StreamerShopSellableItem it, string className)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(className);
            // set even before the element is on screen: a cached icon calls back at once
            ItemIcons.Get(it.ItemName, texture => { if (texture) icon.style.backgroundImage = new StyleBackground(texture); });
            return icon;
        }

        /// <summary>
        /// A number with arrows to step it, and an icon box before it when given. A step grows with the number: 1 under
        /// 100, then 10, 100, 1,000.
        /// </summary>
        private VisualElement Spinner(long value, Action<long> changed, VisualElement icon = null)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-qnumber");
            if (icon != null)
            {
                var box = new VisualElement();
                box.AddToClassList("rf-qnumber__iconbox");
                box.Add(icon);
                row.Add(box);
            }
            var field = NumberField(value, changed);
            field.AddToClassList("rf-qnumber__field");
            row.Add(field);
            var arrows = new VisualElement();
            arrows.AddToClassList("rf-qnumber__arrows");
            arrows.Add(Step("\uf077", field, 1));
            arrows.Add(Step("\uf078", field, -1));
            row.Add(arrows);
            return row;
        }

        private static Button Step(string glyph, TextField field, int direction)
        {
            var button = new Button(() =>
            {
                long.TryParse(field.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v);
                var by = v >= 10000 ? 1000 : v >= 1000 ? 100 : v >= 100 ? 10 : 1;
                field.value = Math.Max(0, v + direction * by).ToString(CultureInfo.InvariantCulture);
            }) { text = glyph };
            button.AddToClassList("rf-icon");
            button.AddToClassList("rf-qnumber__arrow");
            return button;
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
