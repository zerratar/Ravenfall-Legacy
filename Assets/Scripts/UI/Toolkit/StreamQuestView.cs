using System.Globalization;
using RavenNest.SDK.Endpoints;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The stream quest on screen (StreamQuests): a bar at the top center while one runs (what, how far, time left,
    /// helpers), a small pill there when none does, and the quest window that either opens: today's three offers to
    /// start, or the running quest with its top helpers and a stop button. Clicking a quest board in the world (by the
    /// town hall, or in the tavern) opens the window too, so it is found without build mode or chat. A second tab holds
    /// the streamer's own quests (StreamerQuestPanel); a stream goal the streamer wrote shows on the bar as well. A third,
    /// Village, holds the streamer's story lines (StoryPanel); clicking a story's NPC in the world (the Ironhill
    /// runestone, the mangrove island) opens it on that story. A fourth, Ravebridge, shows where the island quests of a
    /// character played by hand there stand (RavebridgeQuestPanel), while Ravebridge is open to the streamer.
    /// </summary>
    /// <remarks>
    /// Shares the connection warnings' document; ConnectionStatusView adds it. Built in code. The window is dragged by its
    /// header and resized from its corner (DraggableWindow, saved as "streamquest"); each tab draws its own footer. The
    /// pictures come from QuestArt.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class StreamQuestView : MonoBehaviour
    {
        private const string BoardPrefix = "SM_Prop_Quest_Board";
        private const string RunestoneName = "Runestone Event";
        private const string MangroveName = "Mangrove";

        private enum Clicked { Nothing, Board, Runestone, Mangrove, Smith }
        private const int TopHelpersShown = 5;

        private static readonly RaycastHit[] hits = new RaycastHit[32];

        private VisualElement bar;
        private Label barIcon;
        private Label barText;
        private VisualElement barTrack;
        private VisualElement barFill;
        private Label barMeta;

        private VisualElement window;
        private Label windowTitle;
        private VisualElement tabs;
        private ScrollView scroll;
        private VisualElement content;
        private StreamerQuestPanel streamerPanel;
        private StoryPanel storyPanel;
        private RavebridgeQuestPanel ravebridgePanel;
        private int tab;
        private VisualElement footer;
        private Label error;
        private DraggableWindow drag;

        private bool bound;
        private bool windowOpen;
        private bool busy;
        private float nextSecond;
        private float nextRavebridgeCheck;

        public static StreamQuestView Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
            bound = false;
            StreamQuests.Changed += DataChanged;
            StreamerQuests.Changed += DataChanged;
            StoryLog.Changed += DataChanged;
            Languages.Changed += Redraw;
            streamerPanel ??= new StreamerQuestPanel(Redraw, ShowError);
            storyPanel ??= new StoryPanel(Redraw, ShowError);
            ravebridgePanel ??= new RavebridgeQuestPanel(Redraw);
        }

        private void OnDisable()
        {
            StreamQuests.Changed -= DataChanged;
            StreamerQuests.Changed -= DataChanged;
            StoryLog.Changed -= DataChanged;
            Languages.Changed -= Redraw;
            bound = false;
        }

        public void Open()
        {
            if (!bound) return;
            windowOpen = true;
            error.text = string.Empty;
            StreamQuests.RefreshSoon();
            StreamerQuests.RefreshSoon();
            StoryLog.RefreshSoon();
            Redraw();
        }

        /// <summary>Opens the window on one of the streamer's story lines, as its NPC would.</summary>
        public void OpenStory(string key)
        {
            if (!bound) return;
            tab = 2;
            storyPanel.Focus = key;
            Open();
        }

        public void Close()
        {
            windowOpen = false;
            Redraw();
        }

        /// <summary>The shortcut bar's quest button: opens the window, or closes it when it is open.</summary>
        public void Toggle()
        {
            if (windowOpen) Close();
            else
            {
                storyPanel.Focus = null;
                Open();
            }
        }

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            var column = TopColumn();
            if (layer == null || column == null) return;

            // again after a document was rebuilt: the old pieces go first
            bar?.RemoveFromHierarchy();
            window?.RemoveFromHierarchy();
            drag?.Detach();
            drag = null;

            bar = new VisualElement { name = "squest-bar" };
            bar.AddToClassList("rf-squest-bar");
            barIcon = new Label("\uf024") { pickingMode = PickingMode.Ignore };
            barIcon.AddToClassList("rf-icon");
            barIcon.AddToClassList("rf-squest-bar__icon");
            var barBody = new VisualElement { pickingMode = PickingMode.Ignore };
            barBody.AddToClassList("rf-squest-bar__body");
            barText = new Label { pickingMode = PickingMode.Ignore };
            barText.AddToClassList("rf-squest-bar__text");
            barTrack = new VisualElement { pickingMode = PickingMode.Ignore };
            barTrack.AddToClassList("rf-squest-track");
            barFill = new VisualElement { pickingMode = PickingMode.Ignore };
            barFill.AddToClassList("rf-squest-track__fill");
            barTrack.Add(barFill);
            barMeta = new Label { pickingMode = PickingMode.Ignore };
            barMeta.AddToClassList("rf-squest-bar__meta");
            barBody.Add(barText);
            barBody.Add(barTrack);
            barBody.Add(barMeta);
            bar.Add(barIcon);
            bar.Add(barBody);
            bar.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => { if (windowOpen) Close(); else Open(); });
            column.Add(bar);

            window = new VisualElement { name = "squest-window" };
            window.AddToClassList("rf-squest");
            window.AddToClassList("rf-squest--window");
            var header = new VisualElement();
            header.AddToClassList("rf-squest__header");
            windowTitle = new Label();
            windowTitle.AddToClassList("rf-squest__title");
            var close = new Button(Close) { text = "\uf00d" };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-squest__close");
            header.Add(windowTitle);
            header.Add(close);
            tabs = new VisualElement();
            tabs.AddToClassList("rf-squest-tabs");
            scroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            scroll.AddToClassList("rf-squest__scroll");
            content = scroll.contentContainer;
            content.AddToClassList("rf-squest__content");
            error = new Label();
            error.AddToClassList("rf-squest__error");
            footer = new VisualElement();
            footer.AddToClassList("rf-squest__footer");
            var grip = new VisualElement();
            grip.AddToClassList("rf-window__grip");
            window.Add(header);
            window.Add(tabs);
            window.Add(scroll);
            window.Add(error);
            window.Add(footer);
            window.Add(grip);
            window.style.display = DisplayStyle.None;
            layer.Add(window);
            drag = DraggableWindow.Attach(window, header, "streamquest");
            drag?.EnableResize(grip, new Vector2(620f, 420f));

            bound = true;
            Redraw();
        }

        private void Update()
        {
            if (bound && (bar.panel == null || window.panel == null)) bound = false;
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            var game = GameManager.Instance;
            StreamQuests.Tick(game, Time.unscaledTime);
            StreamerQuests.Tick(game, Time.unscaledTime);
            StoryLog.Tick(game, Time.unscaledTime);
            QuestTitles.Tick(game, Time.unscaledTime);

            // the clock and the bar's numbers once a second
            if (Time.unscaledTime >= nextSecond)
            {
                nextSecond = Time.unscaledTime + 1f;
                UpdateBar();
                if (windowOpen && StreamQuests.Active != null) UpdateActiveTimes();
                if (windowOpen && tab == 2 && StoryLog.Voting) Redraw(); // the vote's countdown
                if (windowOpen && tab == 3 && Time.unscaledTime >= nextRavebridgeCheck)
                {
                    // the character gathers and hands in while the window is open: again when what it shows changed
                    nextRavebridgeCheck = Time.unscaledTime + 2f;
                    if (ravebridgePanel.Changed()) Redraw();
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                switch (UnderMouse())
                {
                    case Clicked.Board: storyPanel.Focus = null; Open(); break;
                    case Clicked.Runestone: OpenStory(StoryLog.IsDone("runestone") ? "roadwest" : "runestone"); break;
                    case Clicked.Mangrove: OpenStory("bananas"); break;
                    case Clicked.Smith: OpenStory(StoryLog.SmithClicked()); break;
                }
            }
            // Esc closes the post form's open list or item dialog first, then the window
            if (windowOpen && Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping && !(tab == 1 && streamerPanel.HideOverlay())) Close();
        }

        // ---- The bar ---------------------------------------------------------------------------------------

        private void UpdateBar()
        {
            var game = GameManager.Instance;
            var online = game && game.RavenNest != null && game.RavenNest.SessionStarted && StreamQuests.Latest != null;
            var q = online ? StreamQuests.Active : null;
            var goal = online && q == null ? StreamerQuests.RunningGoal : null;
            // the idle pill only invites a start: none while today's offers are used up (the window is in the shortcut bar)
            var offers = online && StreamQuests.Latest.Offers != null && StreamQuests.Latest.Offers.Length > 0;
            var hidden = !online || !SettingsApplier.StreamQuestBar || (VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive)
                || (q == null && goal == null && !offers);
            SetDisplay(bar, !hidden);
            if (hidden) return;

            bar.EnableInClassList("rf-squest-bar--idle", q == null && goal == null);
            SetDisplay(barTrack, q != null || goal != null);
            SetDisplay(barMeta, q != null || goal != null);
            if (goal != null)
            {
                // the streamer's own goal: their name for it, what it asks underneath
                bar.tooltip = Localization.UI_STREAM_QUEST_BAR_HINT;
                barText.text = StreamerQuests.Title(goal);
                barFill.style.width = new Length((goal.Amount > 0 ? Mathf.Clamp01((float)goal.Progress / goal.Amount) : 0f) * 100f, LengthUnit.Percent);
                barMeta.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_BAR_META,
                    StreamerQuests.Number(goal.Progress), StreamerQuests.Number(goal.Amount), StreamerQuests.TimeLeft(goal), goal.Helpers);
                return;
            }

            if (q == null)
            {
                barText.text = Localization.UI_STREAM_QUEST_TITLE;
                bar.tooltip = Localization.UI_STREAM_QUEST_PILL_HINT;
                return;
            }

            bar.tooltip = Localization.UI_STREAM_QUEST_BAR_HINT;
            barText.text = StreamQuests.Text(q);
            barFill.style.width = new Length(Progress(q) * 100f, LengthUnit.Percent);
            barMeta.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_BAR_META,
                StreamQuests.Number(q.Progress), StreamQuests.Number(q.Amount), StreamQuests.TimeLeft(q), q.Helpers);
        }

        // ---- The window ------------------------------------------------------------------------------------

        private Label activeTime;

        /// <summary>New numbers from the server: the window again, unless the streamer is filling in the form.</summary>
        private void DataChanged()
        {
            if (windowOpen && tab == 1 && streamerPanel != null && streamerPanel.Editing) UpdateBar();
            else Redraw();
        }

        private void ShowError(string text)
        {
            if (error != null) error.text = text ?? string.Empty;
        }

        private void Redraw()
        {
            if (!bound) return;
            UpdateBar();
            SetDisplay(window, windowOpen);
            if (!windowOpen) return;

            windowTitle.text = tab == 2 && storyPanel.Focus != null ? StoryLog.Npc(storyPanel.Focus) : Localization.UI_STREAM_QUEST_TITLE;
            content.Clear();
            footer.Clear();
            activeTime = null;
            activeFill = null;
            activeCount = null;

            tabs.Clear();
            tabs.Add(Tab(Localization.UI_STREAM_QUEST_TAB, 0));
            tabs.Add(Tab(Localization.UI_STREAMER_QUEST_TAB, 1));
            tabs.Add(Tab(Localization.UI_STORY_TAB, 2));
            if (RavebridgeQuestPanel.Available) tabs.Add(Tab(Localization.UI_RBQ_TAB, 3));
            else if (tab == 3) tab = 0;
            if (tab == 3)
            {
                ravebridgePanel.Build(content);
                ravebridgePanel.BuildFooter(footer);
                return;
            }
            if (tab == 1)
            {
                streamerPanel.Build(content);
                streamerPanel.BuildFooter(footer);
                return;
            }
            if (tab == 2)
            {
                storyPanel.Build(content);
                storyPanel.BuildFooter(footer);
                return;
            }

            // the chat command does the same as this tab
            footer.Add(QuestArt.Glyph("\uf4ad", "rf-squest__footer-icon"));
            footer.Add(Text(Localization.UI_QUESTWIN_CHAT, "rf-squest__footer-strong"));
            footer.Add(QuestArt.Divider());
            footer.Add(Text(Localization.UI_QUESTWIN_DAILY, "rf-squest__footer-text"));

            var state = StreamQuests.Latest;
            if (state == null)
            {
                content.Add(Text(Localization.UI_STREAM_QUEST_LOADING, "rf-squest__empty"));
                return;
            }

            var q = StreamQuests.Active;
            if (q != null)
            {
                DrawActive(q);
                return;
            }

            QuestArt.Heading(content, Localization.UI_QUESTWIN_CHOOSE, Localization.UI_QUESTWIN_CHOOSE_SUB);
            if (state.Offers == null || state.Offers.Length == 0)
            {
                content.Add(Text(Localization.UI_QUEST_NO_OFFERS, "rf-squest__empty"));
                return;
            }

            foreach (var offer in state.Offers) content.Add(OfferCard(offer));
        }

        /// <summary>An offer: its picture, what it asks and how long, what the village and the helpers get, and Start.</summary>
        private VisualElement OfferCard(StreamQuestData offer)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-qcard");
            card.Add(QuestArt.Picture(QuestArt.QuestPicture(offer.Objective, offer.Skill), QuestArt.QuestGlyph(offer.Objective), "rf-qcard__picture"));

            var body = new VisualElement();
            body.AddToClassList("rf-qcard__body");
            body.Add(Text(StreamQuests.Text(offer), "rf-qcard__title"));
            body.Add(QuestArt.Chip("\uf017", string.Format(CultureInfo.InvariantCulture, Localization.UI_QUESTWIN_MIN, offer.Minutes)));
            body.Add(Rewards(offer));
            card.Add(body);

            var slot = offer.Slot;
            var start = new Button(() => StartQuest(slot)) { text = Localization.UI_QUESTWIN_START };
            start.AddToClassList("rf-button");
            start.AddToClassList("rf-button--primary");
            start.AddToClassList("rf-qcard__button");
            start.SetEnabled(!busy);
            card.Add(start);
            return card;
        }

        /// <summary>VILLAGE: coins (and a resource) | HELPERS: coins shared by everyone who helped.</summary>
        private static VisualElement Rewards(StreamQuestData q)
        {
            var village = new VisualElement();
            village.AddToClassList("rf-qrewards__row");
            village.Add(QuestArt.Amount(QuestArt.Coin(), StreamQuests.Number(q.RewardVillageCoins)));
            if (q.RewardResource > 0 && q.RewardResourceAmount > 0)
                village.Add(QuestArt.Amount(QuestArt.Resource(q.RewardResource), StreamQuests.Number(q.RewardResourceAmount)));
            var helpers = new VisualElement();
            helpers.AddToClassList("rf-qrewards__row");
            helpers.Add(QuestArt.Amount(QuestArt.Coin(), StreamQuests.Number(q.RewardHelperCoins)));
            return QuestArt.Rewards((Localization.UI_QUESTWIN_VILLAGE, village), (Localization.UI_QUESTWIN_HELPERS, helpers));
        }

        private VisualElement activeFill;
        private Label activeCount;

        /// <summary>The quest under way: how far, how long is left, who helps most, what it gives, and Stop.</summary>
        private void DrawActive(StreamQuestData q)
        {
            QuestArt.Heading(content, Localization.UI_QUESTWIN_RUNNING, Localization.UI_QUESTWIN_RUNNING_SUB);
            var card = new VisualElement();
            card.AddToClassList("rf-qcard");
            card.AddToClassList("rf-qcard--active");
            card.Add(QuestArt.Picture(QuestArt.QuestPicture(q.Objective, q.Skill), QuestArt.QuestGlyph(q.Objective), "rf-qcard__picture"));

            var body = new VisualElement();
            body.AddToClassList("rf-qcard__body");
            var titleRow = new VisualElement();
            titleRow.AddToClassList("rf-qcard__title-row");
            titleRow.Add(Text(StreamQuests.Text(q), "rf-qcard__title"));
            activeCount = Text(string.Empty, "rf-qcard__count");
            titleRow.Add(activeCount);
            body.Add(titleRow);

            var track = new VisualElement();
            track.AddToClassList("rf-squest-track");
            track.AddToClassList("rf-squest-track--large");
            activeFill = new VisualElement();
            activeFill.AddToClassList("rf-squest-track__fill");
            track.Add(activeFill);
            body.Add(track);

            var chips = new VisualElement();
            chips.AddToClassList("rf-qcard__chips");
            var time = QuestArt.Chip("\uf017", string.Empty);
            activeTime = time.Q<Label>(className: "rf-qchip__text");
            chips.Add(time);
            chips.Add(QuestArt.Chip("\uf0c0", string.Format(CultureInfo.InvariantCulture, Localization.UI_QUESTWIN_HELPING, q.Helpers)));
            body.Add(chips);
            body.Add(Rewards(q));

            if (q.TopHelpers != null && q.TopHelpers.Length > 0)
            {
                body.Add(Text(Localization.UI_QUESTWIN_TOP, "rf-qlabel"));
                body.Add(Text(StreamQuests.Helpers(q.TopHelpers, TopHelpersShown), "rf-qcard__helpers"));
            }
            card.Add(body);

            var stop = new Button(StopQuest) { text = Localization.UI_QUESTWIN_STOP };
            stop.AddToClassList("rf-button");
            stop.AddToClassList("rf-qcard__button");
            stop.SetEnabled(!busy);
            card.Add(stop);
            content.Add(card);
            UpdateActiveTimes();
        }

        private void UpdateActiveTimes()
        {
            var q = StreamQuests.Active;
            if (activeTime == null || q == null) return;
            activeTime.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_QUESTWIN_LEFT, StreamQuests.TimeLeft(q));
            if (activeCount != null) activeCount.text = StreamQuests.Number(q.Progress) + " / " + StreamQuests.Number(q.Amount);
            if (activeFill != null) activeFill.style.width = new Length(Progress(q) * 100f, LengthUnit.Percent);
        }

        private async void StartQuest(int slot)
        {
            var game = GameManager.Instance;
            if (busy || !game) return;
            busy = true;
            error.text = string.Empty;
            Redraw();
            try
            {
                var result = await StreamQuests.StartAsync(game, slot);
                if (result == null || !result.Ok)
                    error.text = result?.Error == "running" ? Localization.UI_STREAM_QUEST_ALREADY
                        : result?.Error == "goal" ? Localization.UI_STREAM_QUEST_GOAL_RUNNING
                        : Localization.UI_STREAM_QUEST_FAILED;
            }
            catch (System.Exception exc)
            {
                Shinobytes.Debug.LogWarning("The stream quest could not be started: " + exc.Message);
                error.text = Localization.UI_STREAM_QUEST_FAILED;
            }
            finally
            {
                busy = false;
                Redraw();
            }
        }

        private async void StopQuest()
        {
            var game = GameManager.Instance;
            if (busy || !game) return;
            busy = true;
            error.text = string.Empty;
            Redraw();
            try
            {
                var result = await StreamQuests.StopAsync(game);
                if (result == null || !result.Ok) error.text = Localization.UI_STREAM_QUEST_FAILED;
            }
            catch (System.Exception exc)
            {
                Shinobytes.Debug.LogWarning("The stream quest could not be stopped: " + exc.Message);
                error.text = Localization.UI_STREAM_QUEST_FAILED;
            }
            finally
            {
                busy = false;
                Redraw();
            }
        }

        // ---- Helpers ---------------------------------------------------------------------------------------

        private Button Tab(string text, int index)
        {
            var button = new Button(() =>
            {
                if (tab == index) return;
                streamerPanel.HideOverlays();
                tab = index;
                error.text = string.Empty;
                scroll.scrollOffset = Vector2.zero;
                Redraw();
            }) { text = text };
            button.AddToClassList("rf-squest-tab");
            button.EnableInClassList("rf-squest-tab--active", tab == index);
            return button;
        }

        /// <summary>The top status column (the village bar, then the raid and dungeon bars): the bar goes last in it.</summary>
        private static VisualElement TopColumn()
        {
            foreach (var d in FindObjectsByType<UIDocument>())
            {
                var column = d.rootVisualElement?.Q<VisualElement>("top-column");
                if (column != null) return column;
            }
            return null;
        }

        /// <summary>
        /// A click in the world, not on the HUD and not while building: a quest board, the Ironhill runestone, or the
        /// mangrove island (the monkey chief's).
        /// </summary>
        private static Transform runestone;

        private static Clicked UnderMouse()
        {
            if (HudPointer.IsOverHud() || UIUtility.IsPointerOverUIElement() || DraggableWindow.IsPointerOverWindow) return Clicked.Nothing;
            if (VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive) return Clicked.Nothing;
            var cam = Camera.main;
            if (!cam) return Clicked.Nothing;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var count = Physics.RaycastNonAlloc(ray, hits, 1000f, ~0, QueryTriggerInteraction.Ignore);
            var nearest = -1;
            for (var i = 0; i < count; ++i)
            {
                if (nearest < 0 || hits[i].distance < hits[nearest].distance) nearest = i;
            }

            // the forge-master has no collider: a click on or just round them counts, unless something nearer is in the way
            var smith = StoryLog.Smith;
            if (smith && StoryLog.Latest != null)
            {
                var middle = smith.transform.position + Vector3.up * 0.8f;
                var along = Vector3.Dot(middle - ray.origin, ray.direction);
                var off = Vector3.Cross(ray.direction, middle - ray.origin).magnitude;
                if (along > 0f && along <= 250f && off < 0.9f && (nearest < 0 || along <= hits[nearest].distance + 1f)) return Clicked.Smith;
            }

            if (nearest < 0) return Clicked.Nothing;

            // the runestone has only a trigger round it: a click on the stone (the cave's rock) near it counts
            if (!runestone) runestone = GameObject.Find("Ironhill Island/" + RunestoneName)?.transform;
            if (runestone && Vector3.Distance(hits[nearest].point, runestone.position) < 5f) return Clicked.Runestone;

            for (var t = hits[nearest].collider.transform; t; t = t.parent)
            {
                if (t.name.StartsWith(BoardPrefix, System.StringComparison.Ordinal)) return hits[nearest].distance <= 250f ? Clicked.Board : Clicked.Nothing;
                if (t.name == RunestoneName) return Clicked.Runestone;
                if (t.name == MangroveName && t.parent && t.parent.name == "Islands") return Clicked.Mangrove;
            }
            return Clicked.Nothing;
        }

        private static float Progress(StreamQuestData q) => q.Amount > 0 ? Mathf.Clamp01((float)q.Progress / q.Amount) : 0f;

        private static Label Text(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            if (element == null) return;
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (element.style.display != display) element.style.display = display;
        }
    }
}
