using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The settings window, opened from the game menu.
    /// Moved by its header and resized from the corner; both are remembered. Tabs down the left; each tab's rows are built when it is shown, from the current values in
    /// <see cref="SettingsApplier"/>, which applies every change straight away. The settings file is
    /// written when the window closes.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SettingsWindow : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Vector2 minimumSize = new Vector2(860f, 520f);

        public static SettingsWindow Instance { get; private set; }

        /// <summary>
        /// True while the window is up, and for the rest of the frame it closed in. Esc closes the
        /// window, and GameManager must not treat that same key press as a request for the menu.
        /// </summary>
        public static bool IsOpen => Instance && (Instance.open || Instance.closedFrame == Time.frameCount);

        private struct Tab
        {
            public string Title;
            public string Glyph;
            public Action<VisualElement> Build;
            public Func<bool> Visible;
        }

        private readonly List<Tab> tabs = new List<Tab>();
        private readonly List<Button> tabButtons = new List<Button>();

        private VisualElement modal;
        private VisualElement tabColumn;
        private ScrollView content;
        private Button closeButton;
        private Button doneButton;

        private bool bound;
        private bool open;
        private int activeTab;
        private int closedFrame = -1;
        private Action onClosed;
        private DraggableWindow drag;

        private void Awake()
        {
            Instance = this;
            if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();

            tabs.Add(new Tab { Title = Localization.UI_SETTINGS_TAB_GAME, Glyph = "", Build = BuildGame });
            tabs.Add(new Tab { Title = Localization.UI_SETTINGS_TAB_GRAPHICS, Glyph = "", Build = BuildGraphics });
            tabs.Add(new Tab { Title = Localization.UI_SETTINGS_TAB_SOUND, Glyph = "", Build = BuildSound });
            tabs.Add(new Tab { Title = Localization.UI_SETTINGS_TAB_INTERFACE, Glyph = "", Build = BuildInterface });
            tabs.Add(new Tab { Title = Localization.UI_SETTINGS_TAB_BOT, Glyph = "", Build = BuildBotMessages });
            tabs.Add(new Tab { Title = Localization.UI_SETTINGS_TAB_ADMIN, Glyph = "", Build = BuildAdmin, Visible = IsAdministrator });
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => TryBind();

        private void OnDisable()
        {
            if (open) SettingsApplier.Save();
            bound = false;
            tabButtons.Clear();
            drag?.Detach();
            drag = null;
        }

        private void OnApplicationQuit()
        {
            if (open) SettingsApplier.Save();
        }

        private void Update()
        {
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (open && Input.GetKeyUp(KeyCode.Escape))
            {
                Close();
            }
        }

        /// <param name="closed">Called when the window closes, for the menu to come back.</param>
        public void Open(Action closed = null)
        {
            onClosed = closed;
            open = true;
            if (bound) ShowNow();
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            closedFrame = Time.frameCount;
            SettingsApplier.Save();
            if (modal != null) modal.style.display = DisplayStyle.None;

            var closed = onClosed;
            onClosed = null;
            closed?.Invoke();
        }

        private void TryBind()
        {
            if (bound) return;

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) return;

            modal = root.Q<VisualElement>("settings-modal");
            if (modal == null) return;

            tabColumn = root.Q<VisualElement>("settings-tabs");
            content = root.Q<ScrollView>("settings-content");
            closeButton = root.Q<Button>("settings-close");
            doneButton = root.Q<Button>("settings-done");

            root.Q<Label>("settings-title").text = Localization.UI_SETTINGS_TITLE;
            root.Q<Label>("settings-footer").text = Localization.UI_SETTINGS_FOOTER;
            doneButton.text = Localization.UI_SETTINGS_DONE;
            closeButton.clicked += Close;
            doneButton.clicked += Close;
            content.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            var window = root.Q<VisualElement>("settings-window");
            drag = DraggableWindow.Attach(window, root.Q<VisualElement>(className: "rf-settings__header"), "settings");
            drag?.EnableResize(root.Q<VisualElement>("settings-grip"), minimumSize);

            tabButtons.Clear();
            tabColumn.Clear();
            for (var i = 0; i < tabs.Count; ++i)
            {
                var index = i;
                var button = new Button(() => SelectTab(index));
                button.AddToClassList("rf-settings__tab");
                var icon = new Label(tabs[i].Glyph) { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("rf-icon");
                icon.AddToClassList("rf-settings__tab-icon");
                var text = new Label(tabs[i].Title) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("rf-settings__tab-text");
                button.Add(icon);
                button.Add(text);
                tabColumn.Add(button);
                tabButtons.Add(button);
            }

            bound = true;
            if (open) ShowNow();
            else modal.style.display = DisplayStyle.None;
        }

        private void ShowNow()
        {
            for (var i = 0; i < tabs.Count; ++i)
            {
                tabButtons[i].style.display = tabs[i].Visible == null || tabs[i].Visible() ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (tabs[activeTab].Visible != null && !tabs[activeTab].Visible()) activeTab = 0;
            SelectTab(activeTab);
            modal.style.display = DisplayStyle.Flex;
        }

        private void SelectTab(int index)
        {
            activeTab = index;
            for (var i = 0; i < tabButtons.Count; ++i)
            {
                tabButtons[i].EnableInClassList("rf-settings__tab--active", i == index);
            }

            content.Clear();
            tabs[index].Build(content.contentContainer);
            content.scrollOffset = Vector2.zero;
        }

        private bool IsAdministrator() => gameManager && gameManager.SessionSettings != null && gameManager.SessionSettings.IsAdministrator;

        // ---- Tabs -----------------------------------------------------------------------------------

        private void BuildGame(VisualElement c)
        {
            var g = gameManager;
            Section(c, Localization.UI_SETTINGS_SECTION_CAMERA);
            SliderRow(c, Localization.UI_SETTINGS_CAMERA_ROTATION, Localization.UI_SETTINGS_CAMERA_ROTATION_HINT,
                -20f, 20f, SettingsApplier.CameraRotation, RotationText, SettingsApplier.SetCameraRotation, whole: true);

            Section(c, Localization.UI_SETTINGS_SECTION_PLAYERS);
            Choice(c, Localization.UI_SETTINGS_BOOST, Localization.UI_SETTINGS_BOOST_HINT, Localization.UI_SETTINGS_BOOST_OPTIONS,
                Mathf.Clamp(SettingsApplier.BoostRequirement(g), 0, 1), v => SettingsApplier.SetBoostRequirement(g, v));
            Choice(c, Localization.UI_SETTINGS_EXPIRY, Localization.UI_SETTINGS_EXPIRY_HINT, Localization.UI_SETTINGS_EXPIRY_OPTIONS,
                SettingsApplier.PlayerStateExpiry, SettingsApplier.SetPlayerStateExpiry);
            Switch(c, Localization.UI_SETTINGS_EXPIRY_ALERT, null, SettingsApplier.AlertExpiredState(g), v => SettingsApplier.SetAlertExpiredState(g, v));

            Section(c, Localization.UI_SETTINGS_SECTION_CHAT);
            Label example = null;
            var drops = Choice(c, Localization.UI_SETTINGS_DROPS, Localization.UI_SETTINGS_DROPS_HINT, Localization.UI_SETTINGS_DROPS_OPTIONS,
                SettingsApplier.ItemDropMessages(g), v =>
                {
                    SettingsApplier.SetItemDropMessages(g, v);
                    example.text = DropExample(v);
                });
            example = new Label(DropExample(SettingsApplier.ItemDropMessages(g))) { pickingMode = PickingMode.Ignore };
            example.AddToClassList("rf-settings-row__example");
            drops.Q(className: "rf-settings-row__text").Add(example);

            Section(c, Localization.UI_SETTINGS_SECTION_WORLD);
            Choice(c, Localization.UI_SETTINGS_PATHFINDING, Localization.UI_SETTINGS_PATHFINDING_HINT, Localization.UI_SETTINGS_PATHFINDING_OPTIONS,
                SettingsApplier.PathfindingQuality, v => SettingsApplier.SetPathfindingQuality(g, v));
            Switch(c, Localization.UI_SETTINGS_HOUSES, Localization.UI_SETTINGS_HOUSES_HINT, SettingsApplier.AutoAssignHouses, SettingsApplier.SetAutoAssignHouses);
            Switch(c, Localization.UI_SETTINGS_LABELS, Localization.UI_SETTINGS_LABELS_HINT, SettingsApplier.StreamLabels, SettingsApplier.SetStreamLabels);
            Switch(c, Localization.UI_SETTINGS_NO_RAIDS, null, SettingsApplier.DisableRaids, SettingsApplier.SetDisableRaids);
            Switch(c, Localization.UI_SETTINGS_NO_DUNGEONS, null, SettingsApplier.DisableDungeons, SettingsApplier.SetDisableDungeons);
        }

        private void BuildGraphics(VisualElement c)
        {
            var g = gameManager;
            Section(c, Localization.UI_SETTINGS_SECTION_QUALITY);
            Choice(c, Localization.UI_SETTINGS_QUALITY, null, Localization.UI_SETTINGS_QUALITY_OPTIONS,
                Mathf.Clamp(SettingsApplier.QualityLevel, 0, 2), SettingsApplier.SetQualityLevel);
            Switch(c, Localization.UI_SETTINGS_POTATO, Localization.UI_SETTINGS_POTATO_HINT, SettingsApplier.PotatoMode(g), v => SettingsApplier.SetPotatoMode(g, v));
            Switch(c, Localization.UI_SETTINGS_AUTO_POTATO, Localization.UI_SETTINGS_AUTO_POTATO_HINT, SettingsApplier.AutoPotatoMode(g), v => SettingsApplier.SetAutoPotatoMode(g, v));
            Switch(c, Localization.UI_SETTINGS_POST, Localization.UI_SETTINGS_POST_HINT, SettingsApplier.PostProcessing(g), v => SettingsApplier.SetPostProcessing(g, v));

            Section(c, Localization.UI_SETTINGS_SECTION_DAYNIGHT);
            VisualElement timeRow = null;
            Switch(c, Localization.UI_SETTINGS_DAYNIGHT, null, SettingsApplier.DayNightCycle, v =>
            {
                SettingsApplier.SetDayNightCycle(g, v);
                timeRow.SetEnabled(!v);
            });
            Switch(c, Localization.UI_SETTINGS_REALTIME, Localization.UI_SETTINGS_REALTIME_HINT, SettingsApplier.RealTimeDayNight(g), v => SettingsApplier.SetRealTimeDayNight(g, v));
            timeRow = SliderRow(c, Localization.UI_SETTINGS_TIME, Localization.UI_SETTINGS_TIME_HINT, 0f, 1f, SettingsApplier.TimeOfDay,
                v => v < 0.5f ? Localization.UI_SETTINGS_DAY : Localization.UI_SETTINGS_NIGHT, v => SettingsApplier.SetTimeOfDay(g, v));
            timeRow.SetEnabled(!SettingsApplier.DayNightCycle);

            Section(c, Localization.UI_SETTINGS_SECTION_PERFORMANCE);
            SliderRow(c, Localization.UI_SETTINGS_VIEW_DISTANCE, null, 0f, 1f, SettingsApplier.ViewDistance, Percent, v => SettingsApplier.SetViewDistance(g, v));
            if (SettingsApplier.ResolutionScaleSupported)
            {
                SliderRow(c, Localization.UI_SETTINGS_RESOLUTION, Localization.UI_SETTINGS_RESOLUTION_HINT, 0.25f, 1f, SettingsApplier.ResolutionScale, Percent, SettingsApplier.SetResolutionScale);
            }

            ButtonRow(c, Localization.UI_SETTINGS_CLAN_CACHE, Localization.UI_SETTINGS_CLAN_CACHE_HINT, Localization.UI_SETTINGS_CLAN_CACHE_BUTTON,
                () => { if (g.PlayerLogo) g.PlayerLogo.ClearCache(); });
        }

        private void BuildSound(VisualElement c)
        {
            var g = gameManager;
            SliderRow(c, Localization.UI_SETTINGS_MUSIC, null, 0f, 1f, SettingsApplier.MusicVolume(g),
                v => v <= 0.001f ? Localization.UI_SETTINGS_SILENCE : Percent(v), v => SettingsApplier.SetMusicVolume(g, v));
            SliderRow(c, Localization.UI_SETTINGS_EVENTS, Localization.UI_SETTINGS_EVENTS_HINT, 0f, 1f, SettingsApplier.EventVolume(g),
                v => v >= 0.999f ? Localization.UI_SETTINGS_BWAAAH : v <= 0.001f ? Localization.UI_SETTINGS_OFF : Percent(v), v => SettingsApplier.SetEventVolume(g, v));
        }

        private void BuildInterface(VisualElement c)
        {
            var g = gameManager;
            SliderRow(c, Localization.UI_SETTINGS_UI_SCALE, Localization.UI_SETTINGS_UI_SCALE_HINT, UiScale.Min, UiScale.Max, UiScale.Value, Percent,
                v =>
                {
                    // the file is written when the window closes, not on every step of the drag
                    UiScale.Set(v, save: false);
                    PlayerSettings.Instance.UIScale = UiScale.Value;
                }, step: UiScale.Step);
            Switch(c, Localization.UI_SETTINGS_NAMES, null, SettingsApplier.PlayerNames(g), v => SettingsApplier.SetPlayerNames(g, v));
            Switch(c, Localization.UI_SETTINGS_PLAYER_LIST, null, SettingsApplier.PlayerList(g), v => SettingsApplier.SetPlayerList(g, v));
            Switch(c, Localization.UI_SETTINGS_PLAYER_DETAILS, Localization.UI_SETTINGS_PLAYER_DETAILS_HINT, SettingsApplier.PlayerDetails(g), v => SettingsApplier.SetPlayerDetails(g, v));
            Switch(c, Localization.UI_SETTINGS_SHORTCUTS, Localization.UI_SETTINGS_SHORTCUTS_HINT, SettingsApplier.ShortcutBar, SettingsApplier.SetShortcutBar);
            ButtonRow(c, Localization.UI_SETTINGS_RESET_WINDOWS, Localization.UI_SETTINGS_RESET_WINDOWS_HINT, Localization.UI_SETTINGS_RESET_WINDOWS_BUTTON, ResetWindowPositions);
        }

        private void BuildBotMessages(VisualElement c)
        {
            var g = gameManager;
            Intro(c, Localization.UI_SETTINGS_BOT_HINT);
            BotSwitch(c, "ExpMultiplierExpire", Localization.UI_SETTINGS_BOT_EXP, null);
            BotSwitch(c, "PlayerWelcome", Localization.UI_SETTINGS_BOT_WELCOME, null);
            BotSwitch(c, "Disembark", Localization.UI_SETTINGS_BOT_DISEMBARK, null);
            BotSwitch(c, "TrainWarning", Localization.UI_SETTINGS_BOT_TRAIN, Localization.UI_SETTINGS_BOT_TRAIN_HINT);
            BotSwitch(c, "DungeonResult", Localization.UI_SETTINGS_BOT_DUNGEON, null);
            BotSwitch(c, "RaidResult", Localization.UI_SETTINGS_BOT_RAID, null);
            BotSwitch(c, "ItemDrop", Localization.UI_SETTINGS_BOT_ITEMS, Localization.UI_SETTINGS_BOT_ITEMS_HINT);

            void BotSwitch(VisualElement parent, string key, string title, string hint)
            {
                Switch(parent, title, hint, SettingsApplier.BotMessageEnabled(key), v => SettingsApplier.SetBotMessageEnabled(g, key, v));
            }
        }

        private void BuildAdmin(VisualElement c)
        {
            var g = gameManager;
            Switch(c, Localization.UI_SETTINGS_DEV_TOOLS, Localization.UI_SETTINGS_DEV_TOOLS_HINT, SettingsApplier.DevTools(g), v => SettingsApplier.SetDevTools(g, v));
            ButtonRow(c, Localization.UI_SETTINGS_DISCONNECT, Localization.UI_SETTINGS_DISCONNECT_HINT, Localization.UI_SETTINGS_DISCONNECT_BUTTON,
                () => { if (g.RavenNest != null) g.RavenNest.Tcp.Disconnect(); });
        }

        private static void ResetWindowPositions()
        {
            var menu = FindAnyObjectByType<GameMenuHandler>(FindObjectsInactive.Include);
            if (menu) menu.ResetUIPositions();
            else DraggableWindow.ResetAllPositions();
        }

        // ---- Value texts ----------------------------------------------------------------------------

        private static string Percent(float v) => Mathf.RoundToInt(v * 100f).ToString(CultureInfo.InvariantCulture) + "%";

        /// <summary>Arrows show which way the camera turns; full speed either way earns a cheer.</summary>
        private static string RotationText(float v)
        {
            var n = Mathf.RoundToInt(v);
            var abs = Mathf.Abs(n).ToString(CultureInfo.InvariantCulture);
            if (n == 0) return Localization.UI_SETTINGS_CAMERA_STILL;
            if (n <= -20) return "<< " + Localization.UI_SETTINGS_CAMERA_WHEE;
            if (n >= 20) return Localization.UI_SETTINGS_CAMERA_WHEE + " >>";
            if (n < -10) return "<< " + abs;
            if (n < 0) return "< " + abs;
            if (n > 10) return abs + " >>";
            return abs + " >";
        }

        /// <summary>The real message builder run on a couple of made up drops, so the example always matches chat.</summary>
        private static string DropExample(int setting)
        {
            var drops = new Dictionary<string, List<string>>
            {
                ["Rune Platebody"] = new List<string> { "Zerratar", "SirClucksworth" },
                ["Golden Egg"] = new List<string> { "SirClucksworth" },
            };
            var text = new PlayerItemDropText(drops, (PlayerItemDropMessageSettings)setting);
            return text.Messages.Count == 0 ? Localization.UI_SETTINGS_DROPS_NONE : string.Join("\n", text.Messages);
        }

        // ---- Row builders ---------------------------------------------------------------------------

        private static void Section(VisualElement parent, string title)
        {
            var label = new Label(title) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-settings__section");
            parent.Add(label);
        }

        private static void Intro(VisualElement parent, string text)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-settings__intro");
            parent.Add(label);
        }

        /// <returns>The row, and through <paramref name="control"/> the slot on its right for the control.</returns>
        private static VisualElement Row(VisualElement parent, string title, string hint, out VisualElement control)
        {
            var row = new VisualElement();
            row.AddToClassList("rf-settings-row");

            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-settings-row__text");
            var label = new Label(title) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-settings-row__label");
            text.Add(label);
            if (!string.IsNullOrEmpty(hint))
            {
                var hintLabel = new Label(hint) { pickingMode = PickingMode.Ignore };
                hintLabel.AddToClassList("rf-settings-row__hint");
                text.Add(hintLabel);
            }

            control = new VisualElement();
            control.AddToClassList("rf-settings-row__control");
            row.Add(text);
            row.Add(control);
            parent.Add(row);
            return row;
        }

        /// <summary>An on/off switch. The whole row is the click target, not just the little switch.</summary>
        private static VisualElement Switch(VisualElement parent, string title, string hint, bool value, Action<bool> changed)
        {
            var row = Row(parent, title, hint, out var control);
            row.AddToClassList("rf-settings-row--clickable");

            var state = new Label { pickingMode = PickingMode.Ignore };
            state.AddToClassList("rf-switch__state");
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("rf-switch");
            var knob = new VisualElement { pickingMode = PickingMode.Ignore };
            knob.AddToClassList("rf-switch__knob");
            track.Add(knob);
            control.Add(state);
            control.Add(track);
            control.pickingMode = PickingMode.Ignore;

            void Show(bool on)
            {
                track.EnableInClassList("rf-switch--on", on);
                state.EnableInClassList("rf-switch--on-state", on);
                state.text = on ? Localization.UI_SETTINGS_ON : Localization.UI_SETTINGS_OFF;
            }

            Show(value);
            row.AddManipulator(new Clickable(() =>
            {
                if (!row.enabledInHierarchy) return;
                value = !value;
                Show(value);
                changed(value);
            }));
            return row;
        }

        /// <param name="step">Snaps the value to this step; <paramref name="whole"/> is a step of 1.</param>
        private static VisualElement SliderRow(VisualElement parent, string title, string hint, float low, float high, float value,
            Func<float, string> format, Action<float> changed, bool whole = false, float step = 0f)
        {
            var row = Row(parent, title, hint, out var control);
            if (whole) step = 1f;

            var slider = new Slider(low, high);
            slider.AddToClassList("rf-slider");
            var readout = new Label { pickingMode = PickingMode.Ignore };
            readout.AddToClassList("rf-settings-row__value");
            control.Add(slider);
            control.Add(readout);

            float Snap(float v) => step > 0f ? Mathf.Round(v / step) * step : v;

            value = Snap(Mathf.Clamp(value, low, high));
            slider.SetValueWithoutNotify(value);
            readout.text = format(value);
            slider.RegisterValueChangedCallback(e =>
            {
                var v = Snap(e.newValue);
                if (!Mathf.Approximately(v, e.newValue)) slider.SetValueWithoutNotify(v);
                readout.text = format(v);
                changed(v);
            });
            return row;
        }

        /// <summary>A row of buttons for up to four choices, arrows either side of the value for more.</summary>
        private static VisualElement Choice(VisualElement parent, string title, string hint, string[] options, int value, Action<int> changed)
        {
            var row = Row(parent, title, hint, out var control);
            value = Mathf.Clamp(value, 0, options.Length - 1);

            if (options.Length <= 4)
            {
                var group = new VisualElement();
                group.AddToClassList("rf-segmented");
                var items = new Button[options.Length];
                for (var i = 0; i < options.Length; ++i)
                {
                    var index = i;
                    items[i] = new Button(() =>
                    {
                        if (index == value) return;
                        value = index;
                        Mark();
                        changed(index);
                    }) { text = options[i] };
                    items[i].AddToClassList("rf-segmented__item");
                    group.Add(items[i]);
                }

                void Mark()
                {
                    for (var i = 0; i < items.Length; ++i) items[i].EnableInClassList("rf-segmented__item--active", i == value);
                }

                Mark();
                control.Add(group);
                return row;
            }

            var stepper = new VisualElement();
            stepper.AddToClassList("rf-stepper");
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-stepper__value");
            Button prev = null, next = null;
            prev = new Button(() => Move(-1)) { text = "" };
            next = new Button(() => Move(1)) { text = "" };
            foreach (var arrow in new[] { prev, next })
            {
                arrow.AddToClassList("rf-icon");
                arrow.AddToClassList("rf-stepper__arrow");
            }

            void ShowValue()
            {
                label.text = options[value];
                prev.SetEnabled(value > 0);
                next.SetEnabled(value < options.Length - 1);
            }

            void Move(int by)
            {
                var v = Mathf.Clamp(value + by, 0, options.Length - 1);
                if (v == value) return;
                value = v;
                ShowValue();
                changed(v);
            }

            ShowValue();
            stepper.Add(prev);
            stepper.Add(label);
            stepper.Add(next);
            control.Add(stepper);
            return row;
        }

        private static VisualElement ButtonRow(VisualElement parent, string title, string hint, string buttonText, Action clicked)
        {
            var row = Row(parent, title, hint, out var control);
            var button = new Button(clicked) { text = buttonText };
            button.AddToClassList("rf-button");
            button.AddToClassList("rf-settings-row__button");
            control.Add(button);
            return row;
        }
    }
}
