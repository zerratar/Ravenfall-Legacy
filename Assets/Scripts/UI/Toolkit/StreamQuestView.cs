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
    /// runestone, the mangrove island) opens it on that story.
    /// </summary>
    /// <remarks>Shares the connection warnings' document; ConnectionStatusView adds it. Built in code.</remarks>
    [RequireComponent(typeof(UIDocument))]
    public class StreamQuestView : MonoBehaviour
    {
        private const string BoardPrefix = "SM_Prop_Quest_Board";
        private const string RunestoneName = "Runestone Event";
        private const string MangroveName = "Mangrove";

        private enum Clicked { Nothing, Board, Runestone, Mangrove }
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
        private int tab;
        private Label hint;
        private Label error;

        private bool bound;
        private bool windowOpen;
        private bool busy;
        private float nextSecond;

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

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            var column = TopColumn();
            if (layer == null || column == null) return;

            // again after a document was rebuilt: the old pieces go first
            bar?.RemoveFromHierarchy();
            window?.RemoveFromHierarchy();

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
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rf-squest__scroll");
            content = scroll.contentContainer;
            content.AddToClassList("rf-squest__content");
            error = new Label();
            error.AddToClassList("rf-squest__error");
            hint = new Label();
            hint.AddToClassList("rf-squest__hint");
            window.Add(header);
            window.Add(tabs);
            window.Add(scroll);
            window.Add(error);
            window.Add(hint);
            layer.Add(window);

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
            }

            if (Input.GetMouseButtonDown(0))
            {
                switch (UnderMouse())
                {
                    case Clicked.Board: storyPanel.Focus = null; Open(); break;
                    case Clicked.Runestone: OpenStory("runestone"); break;
                    case Clicked.Mangrove: OpenStory("bananas"); break;
                }
            }
            if (windowOpen && Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping) Close();
        }

        // ---- The bar ---------------------------------------------------------------------------------------

        private void UpdateBar()
        {
            var game = GameManager.Instance;
            var online = game && game.RavenNest != null && game.RavenNest.SessionStarted && StreamQuests.Latest != null;
            var hidden = !online || (VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive);
            SetDisplay(bar, !hidden);
            if (hidden) return;

            var q = StreamQuests.Active;
            var goal = q == null ? StreamerQuests.RunningGoal : null;
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
            hint.text = tab == 2 ? Localization.UI_STORY_HINT : tab == 1 ? Localization.UI_STREAMER_QUEST_HINT : Localization.UI_STREAM_QUEST_HINT;
            content.Clear();
            activeTime = null;

            tabs.Clear();
            tabs.Add(Tab(Localization.UI_STREAM_QUEST_TAB, 0));
            tabs.Add(Tab(Localization.UI_STREAMER_QUEST_TAB, 1));
            tabs.Add(Tab(Localization.UI_STORY_TAB, 2));
            if (tab == 1)
            {
                streamerPanel.Build(content);
                return;
            }
            if (tab == 2)
            {
                storyPanel.Build(content);
                return;
            }

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

            content.Add(Text(Localization.UI_STREAM_QUEST_OFFERS, "rf-squest__section"));
            if (state.Offers == null || state.Offers.Length == 0)
            {
                content.Add(Text(Localization.UI_QUEST_NO_OFFERS, "rf-squest__empty"));
                return;
            }

            foreach (var offer in state.Offers) content.Add(OfferCard(offer));
        }

        private VisualElement OfferCard(StreamQuestData offer)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-squest-card");
            var body = new VisualElement();
            body.AddToClassList("rf-squest-card__body");
            body.Add(Text(StreamQuests.Text(offer), "rf-squest-card__title"));
            body.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_MINUTES, offer.Minutes), "rf-squest-card__meta"));
            body.Add(Text(StreamQuests.Reward(offer), "rf-squest-card__reward"));
            card.Add(body);

            var slot = offer.Slot;
            var start = new Button(() => StartQuest(slot)) { text = Localization.UI_STREAM_QUEST_START };
            start.AddToClassList("rf-button");
            start.AddToClassList("rf-button--primary");
            start.AddToClassList("rf-squest-card__button");
            start.SetEnabled(!busy);
            card.Add(start);
            return card;
        }

        private void DrawActive(StreamQuestData q)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-squest-card");
            card.AddToClassList("rf-squest-card--active");
            var body = new VisualElement();
            body.AddToClassList("rf-squest-card__body");
            body.Add(Text(StreamQuests.Text(q), "rf-squest-card__title"));

            var track = new VisualElement();
            track.AddToClassList("rf-squest-track");
            track.AddToClassList("rf-squest-track--large");
            var fill = new VisualElement();
            fill.AddToClassList("rf-squest-track__fill");
            fill.style.width = new Length(Progress(q) * 100f, LengthUnit.Percent);
            track.Add(fill);
            body.Add(track);

            activeTime = Text(string.Empty, "rf-squest-card__meta");
            body.Add(activeTime);
            UpdateActiveTimes();
            body.Add(Text(StreamQuests.Reward(q), "rf-squest-card__reward"));
            body.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_TOP,
                StreamQuests.Helpers(q.TopHelpers, TopHelpersShown)), "rf-squest-card__helpers"));
            card.Add(body);

            var stop = new Button(StopQuest) { text = Localization.UI_STREAM_QUEST_STOP };
            stop.AddToClassList("rf-button");
            stop.AddToClassList("rf-squest-card__button");
            stop.SetEnabled(!busy);
            card.Add(stop);
            content.Add(card);
        }

        private void UpdateActiveTimes()
        {
            var q = StreamQuests.Active;
            if (activeTime == null || q == null) return;
            activeTime.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_STREAM_QUEST_ACTIVE_META,
                StreamQuests.Number(q.Progress), StreamQuests.Number(q.Amount), StreamQuests.TimeLeft(q), q.Helpers);
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
        private static Clicked UnderMouse()
        {
            if (HudPointer.IsOverHud() || UIUtility.IsPointerOverUIElement() || DraggableWindow.IsPointerOverWindow) return Clicked.Nothing;
            if (VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive) return Clicked.Nothing;
            var cam = Camera.main;
            if (!cam) return Clicked.Nothing;

            var count = Physics.RaycastNonAlloc(cam.ScreenPointToRay(Input.mousePosition), hits, 1000f, ~0, QueryTriggerInteraction.Ignore);
            var nearest = -1;
            for (var i = 0; i < count; ++i)
            {
                if (nearest < 0 || hits[i].distance < hits[nearest].distance) nearest = i;
            }

            if (nearest < 0) return Clicked.Nothing;
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
