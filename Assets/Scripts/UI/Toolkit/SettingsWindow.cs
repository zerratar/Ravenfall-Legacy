using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The settings window, opened from the game menu. Moved by its header and resized from the
    /// corner; both are remembered. Tabs down the left; each tab's rows are built when it is shown,
    /// from the current values in <see cref="SettingsApplier"/>, which applies every change straight
    /// away. The settings file is written when the window closes.
    /// Every row knows its default: a reset button shows beside a setting that has been changed, and
    /// the footer resets the whole page.
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
            /// <summary>Read when drawn, so it follows the language.</summary>
            public Func<string> Title;
            public string Glyph;
            public Action<VisualElement> Build;
            public Func<bool> Visible;
            /// <summary>Anything the page reset does beyond resetting its rows.</summary>
            public Action ExtraReset;
        }

        /// <summary>One setting on the current page: whether it is at its default, and how to put it back.</summary>
        private struct RowState
        {
            public Func<bool> IsDefault;
            public Action Reset;
        }

        private readonly List<Tab> tabs = new List<Tab>();
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<RowState> pageRows = new List<RowState>();

        private VisualElement modal;
        private VisualElement tabColumn;
        private ScrollView content;
        private Button closeButton;
        private Button doneButton;
        private Button resetPageButton;
        private Label titleLabel;
        private Label footerLabel;

        private bool bound;
        private bool open;
        private int activeTab;
        private int closedFrame = -1;
        private Action onClosed;
        private DraggableWindow drag;

        // waiting for a key to bind
        private Keybinds.Action? capturing;
        private bool releaseBeforeKeys;
        private string keyNote;
        private static KeyCode[] bindableKeys;

        private void Awake()
        {
            Instance = this;
            if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();

            tabs.Add(new Tab { Title = () => Localization.UI_SETTINGS_TAB_GAME, Glyph = "\uf11b", Build = BuildGame });
            tabs.Add(new Tab { Title = () => Localization.UI_SETTINGS_TAB_GRAPHICS, Glyph = "\uf108", Build = BuildGraphics });
            tabs.Add(new Tab { Title = () => Localization.UI_SETTINGS_TAB_SOUND, Glyph = "\uf028", Build = BuildSound });
            tabs.Add(new Tab { Title = () => Localization.UI_SETTINGS_TAB_INTERFACE, Glyph = "\uf2d0", Build = BuildInterface, ExtraReset = ResetWindowPositions });
            tabs.Add(new Tab { Title = () => Localization.UI_SETTINGS_TAB_BOT, Glyph = "\uf544", Build = BuildBotMessages });
            tabs.Add(new Tab { Title = () => Localization.UI_SETTINGS_TAB_KEYS, Glyph = "\uf11c", Build = BuildKeys, ExtraReset = Keybinds.ResetAll });
            tabs.Add(new Tab { Title = () => Localization.UI_SETTINGS_TAB_ADMIN, Glyph = "\uf505", Build = BuildAdmin, Visible = IsAdministrator });
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            Languages.Changed += OnLanguageChanged;
            TryBind();
        }

        /// <summary>Every text in the window is drawn again in the new language.</summary>
        private void OnLanguageChanged()
        {
            if (!bound) return;
            ApplyTexts();
            BuildTabButtons();
            if (open) ShowNow();
        }

        private void OnDisable()
        {
            Languages.Changed -= OnLanguageChanged;
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
            // A live reload of the markup or styles replaces the tree; bind again to the new one.
            if (bound && modal.panel == null)
            {
                bound = false;
                drag?.Detach();
                drag = null;
            }

            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            // Keys stay off until everything is let go after binding one, so the new key does not
            // also do its job (or the old one's) on the way up.
            if (releaseBeforeKeys && !Input.anyKey)
            {
                releaseBeforeKeys = false;
                Keybinds.Suspended = false;
            }

            if (capturing.HasValue)
            {
                PollCapture();
                return;
            }

            if (open && Input.GetKeyUp(KeyCode.Escape))
            {
                Close();
            }
        }

        /// <summary>The Bot tab, with the bot messages and the bot connection.</summary>
        public const int BotTab = 4;

        /// <summary>Opens on a given tab, such as <see cref="BotTab"/>.</summary>
        public void OpenTab(int tab, Action closed = null)
        {
            activeTab = Mathf.Clamp(tab, 0, tabs.Count - 1);
            Open(closed);
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
            if (capturing.HasValue) EndCapture();
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
            resetPageButton = root.Q<Button>("settings-reset-page");

            titleLabel = root.Q<Label>("settings-title");
            footerLabel = root.Q<Label>("settings-footer");
            closeButton.clicked += Close;
            doneButton.clicked += Close;
            if (resetPageButton != null) resetPageButton.clicked += ResetPage;
            ApplyTexts();

            content.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            var window = root.Q<VisualElement>("settings-window");
            drag = DraggableWindow.Attach(window, root.Q<VisualElement>(className: "rf-settings__header"), "settings");
            drag?.EnableResize(root.Q<VisualElement>("settings-grip"), minimumSize);

            BuildTabButtons();

            bound = true;
            if (open) ShowNow();
            else modal.style.display = DisplayStyle.None;
        }

        /// <summary>The window's own labels, apart from the pages, which are built when shown.</summary>
        private void ApplyTexts()
        {
            if (titleLabel != null) titleLabel.text = Localization.UI_SETTINGS_TITLE;
            if (footerLabel != null) footerLabel.text = Localization.UI_SETTINGS_FOOTER;
            doneButton.text = Localization.UI_SETTINGS_DONE;
            if (resetPageButton != null)
            {
                resetPageButton.text = Localization.UI_SETTINGS_RESET_PAGE;
                resetPageButton.tooltip = Localization.UI_SETTINGS_RESET_PAGE_HINT;
            }
        }

        private void BuildTabButtons()
        {
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
                var text = new Label(tabs[i].Title()) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("rf-settings__tab-text");
                button.Add(icon);
                button.Add(text);
                tabColumn.Add(button);
                tabButtons.Add(button);
            }
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

            pageRows.Clear();
            content.Clear();
            tabs[index].Build(content.contentContainer);
            content.scrollOffset = Vector2.zero;
            RefreshResetPage();
        }

        private void ResetPage()
        {
            foreach (var row in pageRows.ToArray())
            {
                if (!row.IsDefault()) row.Reset();
            }

            tabs[activeTab].ExtraReset?.Invoke();
            // rebuilt so every row shows its value again
            SelectTab(activeTab);
        }

        /// <summary>The page reset is only offered when something on the page has been changed.</summary>
        private void RefreshResetPage()
        {
            if (resetPageButton == null) return;
            var changed = tabs[activeTab].ExtraReset != null;
            for (var i = 0; i < pageRows.Count && !changed; ++i) changed = !pageRows[i].IsDefault();
            resetPageButton.SetEnabled(changed);
        }

        private bool IsAdministrator() => gameManager && gameManager.SessionSettings != null && gameManager.SessionSettings.IsAdministrator;

        // ---- Tabs -----------------------------------------------------------------------------------

        private void BuildGame(VisualElement c)
        {
            var g = gameManager;
            Section(c, Localization.UI_SETTINGS_SECTION_OBSERVER);
            SliderRow(c, Localization.UI_SETTINGS_CAMERA_ROTATION, Localization.UI_SETTINGS_CAMERA_ROTATION_HINT,
                -20f, 20f, SettingsApplier.CameraRotation, SettingsApplier.Defaults.CameraRotation, RotationText, SettingsApplier.SetCameraRotation, step: 1f);
            SliderRow(c, Localization.UI_SETTINGS_OBSERVE, Localization.UI_SETTINGS_OBSERVE_HINT, 5f, 120f,
                SettingsApplier.ObserveSeconds, SettingsApplier.Defaults.ObserveSeconds, Seconds, SettingsApplier.SetObserveSeconds, step: 1f);
            SliderRow(c, Localization.UI_SETTINGS_OBSERVE_SUB, Localization.UI_SETTINGS_OBSERVE_SUB_HINT, 5f, 120f,
                SettingsApplier.ObserveSubscriberSeconds, SettingsApplier.Defaults.ObserveSecondsSpecial, Seconds, SettingsApplier.SetObserveSubscriberSeconds, step: 1f);
            SliderRow(c, Localization.UI_SETTINGS_OBSERVE_VIP, Localization.UI_SETTINGS_OBSERVE_VIP_HINT, 5f, 120f,
                SettingsApplier.ObserveVipSeconds, SettingsApplier.Defaults.ObserveSecondsSpecial, Seconds, SettingsApplier.SetObserveVipSeconds, step: 1f);
            SliderRow(c, Localization.UI_SETTINGS_OBSERVE_MOD, Localization.UI_SETTINGS_OBSERVE_MOD_HINT, 5f, 120f,
                SettingsApplier.ObserveModeratorSeconds, SettingsApplier.Defaults.ObserveSecondsSpecial, Seconds, SettingsApplier.SetObserveModeratorSeconds, step: 1f);
            SliderRow(c, Localization.UI_SETTINGS_OBSERVE_EVENT, Localization.UI_SETTINGS_OBSERVE_EVENT_HINT, 5f, 120f,
                SettingsApplier.ObserveEventSeconds, SettingsApplier.Defaults.ObserveSecondsSpecial, Seconds, SettingsApplier.SetObserveEventSeconds, step: 1f);
            SliderRow(c, Localization.UI_SETTINGS_OBSERVE_ISLAND, Localization.UI_SETTINGS_OBSERVE_ISLAND_HINT, 5f, 60f,
                SettingsApplier.IslandObserveSeconds, SettingsApplier.Defaults.IslandObserveSeconds, Seconds, SettingsApplier.SetIslandObserveSeconds, step: 1f);
            Switch(c, Localization.UI_SETTINGS_OBSERVE_EMPTY, Localization.UI_SETTINGS_OBSERVE_EMPTY_HINT,
                SettingsApplier.ObserveEmptyIslands, SettingsApplier.Defaults.ObserveEmptyIslands, SettingsApplier.SetObserveEmptyIslands);

            Section(c, Localization.UI_SETTINGS_SECTION_FREE_CAMERA);
            SliderRow(c, Localization.UI_SETTINGS_FREE_SPEED, Localization.UI_SETTINGS_FREE_SPEED_HINT, 10f, 200f,
                SettingsApplier.FreeCameraSpeed, SettingsApplier.Defaults.FreeCameraSpeed, v => Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture),
                SettingsApplier.SetFreeCameraSpeed, step: 5f);
            SliderRow(c, Localization.UI_SETTINGS_LOOK, Localization.UI_SETTINGS_LOOK_HINT, 0.25f, 3f,
                SettingsApplier.LookSensitivity, SettingsApplier.Defaults.Sensitivity, Times, SettingsApplier.SetLookSensitivity, step: 0.05f);
            Switch(c, Localization.UI_SETTINGS_INVERT_LOOK, Localization.UI_SETTINGS_INVERT_LOOK_HINT, SettingsApplier.InvertLook, SettingsApplier.Defaults.InvertLook, SettingsApplier.SetInvertLook);
            SliderRow(c, Localization.UI_SETTINGS_ZOOM, Localization.UI_SETTINGS_ZOOM_HINT, 0.25f, 3f,
                SettingsApplier.ZoomSensitivity, SettingsApplier.Defaults.Sensitivity, Times, SettingsApplier.SetZoomSensitivity, step: 0.05f);
            Switch(c, Localization.UI_SETTINGS_INVERT_ZOOM, Localization.UI_SETTINGS_INVERT_ZOOM_HINT, SettingsApplier.InvertZoom, SettingsApplier.Defaults.InvertZoom, SettingsApplier.SetInvertZoom);

            Section(c, Localization.UI_SETTINGS_SECTION_PLAYERS);
            Choice(c, Localization.UI_SETTINGS_BOOST, Localization.UI_SETTINGS_BOOST_HINT, Localization.UI_SETTINGS_BOOST_OPTIONS,
                Mathf.Clamp(SettingsApplier.BoostRequirement(g), 0, 1), SettingsApplier.Defaults.BoostRequirement, v => SettingsApplier.SetBoostRequirement(g, v));
            Choice(c, Localization.UI_SETTINGS_AFK, Localization.UI_SETTINGS_AFK_HINT, Localization.UI_SETTINGS_AFK_OPTIONS,
                SettingsApplier.AfkKick, SettingsApplier.Defaults.AfkKick, SettingsApplier.SetAfkKick);
            Choice(c, Localization.UI_SETTINGS_EXPIRY, Localization.UI_SETTINGS_EXPIRY_HINT, Localization.UI_SETTINGS_EXPIRY_OPTIONS,
                SettingsApplier.PlayerStateExpiry, SettingsApplier.Defaults.PlayerStateExpiry, SettingsApplier.SetPlayerStateExpiry);
            Switch(c, Localization.UI_SETTINGS_EXPIRY_ALERT, Localization.UI_SETTINGS_EXPIRY_ALERT_HINT, SettingsApplier.AlertExpiredState(g), SettingsApplier.Defaults.AlertExpiredState,
                v => SettingsApplier.SetAlertExpiredState(g, v));

            Section(c, Localization.UI_SETTINGS_SECTION_WORLD);
            Choice(c, Localization.UI_SETTINGS_PATHFINDING, Localization.UI_SETTINGS_PATHFINDING_HINT, Localization.UI_SETTINGS_PATHFINDING_OPTIONS,
                SettingsApplier.PathfindingQuality, SettingsApplier.Defaults.PathfindingQuality, v => SettingsApplier.SetPathfindingQuality(g, v));
            Switch(c, Localization.UI_SETTINGS_HOUSES, Localization.UI_SETTINGS_HOUSES_HINT, SettingsApplier.AutoAssignHouses,
                SettingsApplier.Defaults.AutoAssignHouses, SettingsApplier.SetAutoAssignHouses);
            Switch(c, Localization.UI_SETTINGS_NO_RAIDS, Localization.UI_SETTINGS_NO_RAIDS_HINT, SettingsApplier.DisableRaids, SettingsApplier.Defaults.DisableRaids, SettingsApplier.SetDisableRaids);
            Switch(c, Localization.UI_SETTINGS_NO_DUNGEONS, Localization.UI_SETTINGS_NO_DUNGEONS_HINT, SettingsApplier.DisableDungeons, SettingsApplier.Defaults.DisableDungeons, SettingsApplier.SetDisableDungeons);
            Switch(c, Localization.UI_SETTINGS_JOIN_CODE, Localization.UI_SETTINGS_JOIN_CODE_HINT, SettingsApplier.RequireJoinCode, SettingsApplier.Defaults.RequireJoinCode, SettingsApplier.SetRequireJoinCode);
            Switch(c, Localization.UI_SETTINGS_PHYSICS, WithKey(Localization.UI_SETTINGS_PHYSICS_HINT, Keybinds.Action.Physics), SettingsApplier.Physics, SettingsApplier.Defaults.Physics, SettingsApplier.SetPhysics);

            Section(c, Localization.UI_SETTINGS_SECTION_OVERLAYS);
            VisualElement textRow = null, jsonRow = null;
            Switch(c, Localization.UI_SETTINGS_LABELS, Localization.UI_SETTINGS_LABELS_HINT, SettingsApplier.StreamLabels, SettingsApplier.Defaults.StreamLabels, v =>
            {
                SettingsApplier.SetStreamLabels(v);
                textRow.SetEnabled(v);
                jsonRow.SetEnabled(v);
            });
            textRow = Switch(c, Localization.UI_SETTINGS_LABELS_TEXT, Localization.UI_SETTINGS_LABELS_TEXT_HINT, SettingsApplier.StreamLabelTextFiles, SettingsApplier.Defaults.StreamLabelFiles, SettingsApplier.SetStreamLabelTextFiles);
            jsonRow = Switch(c, Localization.UI_SETTINGS_LABELS_JSON, Localization.UI_SETTINGS_LABELS_JSON_HINT, SettingsApplier.StreamLabelJsonFiles, SettingsApplier.Defaults.StreamLabelFiles, SettingsApplier.SetStreamLabelJsonFiles);
            textRow.SetEnabled(SettingsApplier.StreamLabels);
            jsonRow.SetEnabled(SettingsApplier.StreamLabels);

            VisualElement addressRow = null;
            Switch(c, Localization.UI_SETTINGS_QUERY, Localization.UI_SETTINGS_QUERY_HINT, SettingsApplier.QueryEngine, SettingsApplier.Defaults.QueryEngine, v =>
            {
                SettingsApplier.SetQueryEngine(v);
                addressRow.SetEnabled(v);
            });
            addressRow = TextRow(c, Localization.UI_SETTINGS_QUERY_ADDRESS, Localization.UI_SETTINGS_QUERY_ADDRESS_HINT, SettingsApplier.QueryEngineAddress, SettingsApplier.Defaults.QueryEngineAddress,
                SettingsApplier.SetQueryEngineAddress);
            addressRow.SetEnabled(SettingsApplier.QueryEngine);
            ReloadRow(c);
        }

        private void BuildGraphics(VisualElement c)
        {
            var g = gameManager;
            Section(c, Localization.UI_SETTINGS_SECTION_DISPLAY);
            Choice(c, Localization.UI_SETTINGS_WINDOW_MODE, Localization.UI_SETTINGS_WINDOW_MODE_HINT, Localization.UI_SETTINGS_WINDOW_MODE_OPTIONS,
                SettingsApplier.WindowMode, SettingsApplier.Defaults.WindowMode, SettingsApplier.SetWindowMode);
            var sizes = SettingsApplier.Resolutions;
            var sizeNames = new string[sizes.Length];
            for (var i = 0; i < sizes.Length; ++i)
            {
                sizeNames[i] = string.Format(CultureInfo.InvariantCulture, i == 0 ? Localization.UI_SETTINGS_SCREEN_NATIVE : Localization.UI_SETTINGS_SCREEN_SIZE_VALUE, sizes[i].x, sizes[i].y);
            }

            Choice(c, Localization.UI_SETTINGS_SCREEN_SIZE, Localization.UI_SETTINGS_SCREEN_SIZE_HINT, sizeNames, SettingsApplier.ResolutionIndex, SettingsApplier.Defaults.Resolution, SettingsApplier.SetResolutionIndex, forceStepper: true);
            VisualElement capRow = null;
            Switch(c, Localization.UI_SETTINGS_VSYNC, Localization.UI_SETTINGS_VSYNC_HINT, SettingsApplier.VSync, SettingsApplier.Defaults.VSync, v =>
            {
                SettingsApplier.SetVSync(v);
                capRow.SetEnabled(!v);
            });
            capRow = Choice(c, Localization.UI_SETTINGS_FPS, Localization.UI_SETTINGS_FPS_HINT, Localization.UI_SETTINGS_FPS_OPTIONS,
                SettingsApplier.FrameRateCap, SettingsApplier.Defaults.FrameRateCap, SettingsApplier.SetFrameRateCap);
            capRow.SetEnabled(!SettingsApplier.VSync);

            Section(c, Localization.UI_SETTINGS_SECTION_QUALITY);
            Choice(c, Localization.UI_SETTINGS_QUALITY, Localization.UI_SETTINGS_QUALITY_HINT, Localization.UI_SETTINGS_QUALITY_OPTIONS,
                Mathf.Clamp(SettingsApplier.QualityLevel, 0, 2), SettingsApplier.Defaults.QualityLevel, SettingsApplier.SetQualityLevel);
            Switch(c, Localization.UI_SETTINGS_DRAW_WORLD, WithKey(Localization.UI_SETTINGS_DRAW_WORLD_HINT, Keybinds.Action.Graphics), !SettingsApplier.GraphicsOff, !SettingsApplier.Defaults.GraphicsOff,
                v => SettingsApplier.SetGraphicsOff(g, !v));
            Switch(c, Localization.UI_SETTINGS_POTATO, Localization.UI_SETTINGS_POTATO_HINT, SettingsApplier.PotatoMode(g), SettingsApplier.Defaults.PotatoMode,
                v => SettingsApplier.SetPotatoMode(g, v));
            Switch(c, Localization.UI_SETTINGS_AUTO_POTATO, Localization.UI_SETTINGS_AUTO_POTATO_HINT, SettingsApplier.AutoPotatoMode(g), SettingsApplier.Defaults.AutoPotatoMode,
                v => SettingsApplier.SetAutoPotatoMode(g, v));
            Switch(c, Localization.UI_SETTINGS_POST, Localization.UI_SETTINGS_POST_HINT, SettingsApplier.PostProcessing(g), SettingsApplier.Defaults.PostProcessing,
                v => SettingsApplier.SetPostProcessing(g, v));
            Switch(c, Localization.UI_SETTINGS_BLOOM, Localization.UI_SETTINGS_BLOOM_HINT, SettingsApplier.Bloom, SettingsApplier.Defaults.Bloom,
                SettingsApplier.SetBloom);
            VisualElement grassDistanceRow = null, grassDensityRow = null;
            Switch(c, Localization.UI_SETTINGS_GRASS, Localization.UI_SETTINGS_GRASS_HINT, SettingsApplier.Grass, SettingsApplier.Defaults.Grass, v =>
            {
                SettingsApplier.SetGrass(v);
                grassDistanceRow.SetEnabled(v);
                grassDensityRow.SetEnabled(v);
            });
            grassDistanceRow = SliderRow(c, Localization.UI_SETTINGS_GRASS_DISTANCE, Localization.UI_SETTINGS_GRASS_DISTANCE_HINT, 0f, 1f, SettingsApplier.GrassDistance,
                SettingsApplier.Defaults.GrassDistance, v => string.Format(CultureInfo.InvariantCulture, Localization.UI_SETTINGS_GRASS_METRES, Mathf.RoundToInt(SettingsApplier.GrassMetres(v))),
                SettingsApplier.SetGrassDistance);
            grassDensityRow = SliderRow(c, Localization.UI_SETTINGS_GRASS_DENSITY, Localization.UI_SETTINGS_GRASS_DENSITY_HINT, 0.25f, 1f, SettingsApplier.GrassDensity,
                SettingsApplier.Defaults.GrassDensity, Percent, SettingsApplier.SetGrassDensity);
            grassDistanceRow.SetEnabled(SettingsApplier.Grass);
            grassDensityRow.SetEnabled(SettingsApplier.Grass);
            VisualElement tiltStrengthRow = null, tiltBandRow = null, tiltSoftnessRow = null;
            Switch(c, Localization.UI_SETTINGS_TILT_SHIFT, Localization.UI_SETTINGS_TILT_SHIFT_HINT, SettingsApplier.TiltShiftEnabled, SettingsApplier.Defaults.TiltShift, v =>
            {
                SettingsApplier.SetTiltShift(v);
                tiltStrengthRow.SetEnabled(v);
                tiltBandRow.SetEnabled(v);
                tiltSoftnessRow.SetEnabled(v);
            });
            tiltStrengthRow = SliderRow(c, Localization.UI_SETTINGS_TILT_SHIFT_STRENGTH, Localization.UI_SETTINGS_TILT_SHIFT_STRENGTH_HINT, 0f, 1f, SettingsApplier.TiltShiftStrength,
                SettingsApplier.Defaults.TiltShiftStrength, Percent, SettingsApplier.SetTiltShiftStrength);
            tiltBandRow = SliderRow(c, Localization.UI_SETTINGS_TILT_SHIFT_BAND, Localization.UI_SETTINGS_TILT_SHIFT_BAND_HINT, 0f, 1f, SettingsApplier.TiltShiftBand,
                SettingsApplier.Defaults.TiltShiftBand, Percent, SettingsApplier.SetTiltShiftBand);
            tiltSoftnessRow = SliderRow(c, Localization.UI_SETTINGS_TILT_SHIFT_SOFTNESS, Localization.UI_SETTINGS_TILT_SHIFT_SOFTNESS_HINT, 0f, 1f, SettingsApplier.TiltShiftSoftness,
                SettingsApplier.Defaults.TiltShiftSoftness, Percent, SettingsApplier.SetTiltShiftSoftness);
            tiltStrengthRow.SetEnabled(SettingsApplier.TiltShiftEnabled);
            tiltBandRow.SetEnabled(SettingsApplier.TiltShiftEnabled);
            tiltSoftnessRow.SetEnabled(SettingsApplier.TiltShiftEnabled);
            Switch(c, Localization.UI_SETTINGS_CLOUD_SHADOWS, Localization.UI_SETTINGS_CLOUD_SHADOWS_HINT, SettingsApplier.CloudShadowsEnabled, SettingsApplier.Defaults.CloudShadows,
                SettingsApplier.SetCloudShadows);
            Switch(c, Localization.UI_SETTINGS_DETAILED_WATER, Localization.UI_SETTINGS_DETAILED_WATER_HINT, SettingsApplier.DetailedWater, SettingsApplier.Defaults.DetailedWater,
                SettingsApplier.SetDetailedWater);
            Switch(c, Localization.UI_SETTINGS_VOLUMETRIC_CLOUDS, Localization.UI_SETTINGS_VOLUMETRIC_CLOUDS_HINT, SettingsApplier.VolumetricClouds, SettingsApplier.Defaults.VolumetricClouds,
                SettingsApplier.SetVolumetricClouds);
            Switch(c, Localization.UI_SETTINGS_WEATHER, Localization.UI_SETTINGS_WEATHER_HINT, SettingsApplier.ChangingWeather, SettingsApplier.Defaults.ChangingWeather,
                SettingsApplier.SetChangingWeather);
            Switch(c, Localization.UI_SETTINGS_LIGHTNING, Localization.UI_SETTINGS_LIGHTNING_HINT, SettingsApplier.LightningFlashes, SettingsApplier.Defaults.LightningFlashes,
                SettingsApplier.SetLightningFlashes);
            VisualElement aoStrengthRow = null;
            Switch(c, Localization.UI_SETTINGS_AMBIENT_OCCLUSION, Localization.UI_SETTINGS_AMBIENT_OCCLUSION_HINT, SettingsApplier.AmbientOcclusionEnabled, SettingsApplier.Defaults.AmbientOcclusion, v =>
            {
                SettingsApplier.SetAmbientOcclusion(v);
                aoStrengthRow.SetEnabled(v);
            });
            aoStrengthRow = SliderRow(c, Localization.UI_SETTINGS_AO_STRENGTH, Localization.UI_SETTINGS_AO_STRENGTH_HINT, 0f, 1f, SettingsApplier.AmbientOcclusionStrength,
                SettingsApplier.Defaults.AmbientOcclusionStrength, Percent, SettingsApplier.SetAmbientOcclusionStrength);
            aoStrengthRow.SetEnabled(SettingsApplier.AmbientOcclusionEnabled);
            Switch(c, Localization.UI_SETTINGS_AMBIENT_EFFECTS, Localization.UI_SETTINGS_AMBIENT_EFFECTS_HINT, SettingsApplier.AmbientEffects, SettingsApplier.Defaults.AmbientEffects,
                SettingsApplier.SetAmbientEffects);
            Switch(c, Localization.UI_SETTINGS_TREE_WIND, Localization.UI_SETTINGS_TREE_WIND_HINT, SettingsApplier.TreeWind, SettingsApplier.Defaults.TreeWind,
                SettingsApplier.SetTreeWind);
            Switch(c, Localization.UI_SETTINGS_DUNGEON_LIGHTING, Localization.UI_SETTINGS_DUNGEON_LIGHTING_HINT, SettingsApplier.DungeonLighting, SettingsApplier.Defaults.DungeonLighting,
                SettingsApplier.SetDungeonLighting);
            Switch(c, Localization.UI_SETTINGS_HAZE, Localization.UI_SETTINGS_HAZE_HINT, SettingsApplier.AtmosphericHaze, SettingsApplier.Defaults.AtmosphericHaze,
                SettingsApplier.SetAtmosphericHaze);

            Section(c, Localization.UI_SETTINGS_SECTION_DAYNIGHT);
            VisualElement timeRow = null;
            Switch(c, Localization.UI_SETTINGS_DAYNIGHT, Localization.UI_SETTINGS_DAYNIGHT_HINT, SettingsApplier.DayNightCycle, SettingsApplier.Defaults.DayNightCycle, v =>
            {
                SettingsApplier.SetDayNightCycle(g, v);
                timeRow.SetEnabled(!v);
            });
            Switch(c, Localization.UI_SETTINGS_REALTIME, Localization.UI_SETTINGS_REALTIME_HINT, SettingsApplier.RealTimeDayNight(g), SettingsApplier.Defaults.RealTimeDayNight,
                v => SettingsApplier.SetRealTimeDayNight(g, v));
            timeRow = SliderRow(c, Localization.UI_SETTINGS_TIME, Localization.UI_SETTINGS_TIME_HINT, 0f, 1f, SettingsApplier.TimeOfDay, SettingsApplier.Defaults.TimeOfDay,
                v => v < 0.5f ? Localization.UI_SETTINGS_DAY : Localization.UI_SETTINGS_NIGHT, v => SettingsApplier.SetTimeOfDay(g, v));
            timeRow.SetEnabled(!SettingsApplier.DayNightCycle);

            Section(c, Localization.UI_SETTINGS_SECTION_PERFORMANCE);
            SliderRow(c, Localization.UI_SETTINGS_VIEW_DISTANCE, Localization.UI_SETTINGS_VIEW_DISTANCE_HINT, 0f, 1f, SettingsApplier.ViewDistance, SettingsApplier.Defaults.ViewDistance,
                Percent, v => SettingsApplier.SetViewDistance(g, v));
            if (SettingsApplier.ResolutionScaleSupported)
            {
                SliderRow(c, Localization.UI_SETTINGS_RESOLUTION, Localization.UI_SETTINGS_RESOLUTION_HINT, 0.25f, 1f, SettingsApplier.ResolutionScale,
                    SettingsApplier.Defaults.ResolutionScale, Percent, SettingsApplier.SetResolutionScale);
            }

            ButtonRow(c, Localization.UI_SETTINGS_CLAN_CACHE, Localization.UI_SETTINGS_CLAN_CACHE_HINT, Localization.UI_SETTINGS_CLAN_CACHE_BUTTON,
                () => { if (g.PlayerLogo) g.PlayerLogo.ClearCache(); });
        }

        private void BuildSound(VisualElement c)
        {
            var g = gameManager;
            SliderRow(c, Localization.UI_SETTINGS_MASTER, Localization.UI_SETTINGS_MASTER_HINT, 0f, 1f, SettingsApplier.MasterVolume, SettingsApplier.Defaults.MasterVolume,
                v => v <= 0.001f ? Localization.UI_SETTINGS_SILENCE : Percent(v), SettingsApplier.SetMasterVolume);
            SliderRow(c, Localization.UI_SETTINGS_MUSIC, Localization.UI_SETTINGS_MUSIC_HINT, 0f, 1f, SettingsApplier.MusicVolume(g), SettingsApplier.Defaults.MusicVolume,
                v => v <= 0.001f ? Localization.UI_SETTINGS_SILENCE : Percent(v), v => SettingsApplier.SetMusicVolume(g, v));
            SliderRow(c, Localization.UI_SETTINGS_EVENTS, Localization.UI_SETTINGS_EVENTS_HINT, 0f, 1f, SettingsApplier.EventVolume(g), SettingsApplier.Defaults.EventVolume,
                v => v >= 0.999f ? Localization.UI_SETTINGS_BWAAAH : v <= 0.001f ? Localization.UI_SETTINGS_OFF : Percent(v), v => SettingsApplier.SetEventVolume(g, v));
            SliderRow(c, Localization.UI_SETTINGS_WEATHER_SOUNDS, Localization.UI_SETTINGS_WEATHER_SOUNDS_HINT, 0f, 1f, SettingsApplier.WeatherVolume, SettingsApplier.Defaults.WeatherVolume,
                v => v <= 0.001f ? Localization.UI_SETTINGS_OFF : Percent(v), SettingsApplier.SetWeatherVolume);
        }

        private void BuildInterface(VisualElement c)
        {
            var g = gameManager;
            var languages = Languages.Available;
            var names = new string[languages.Count];
            var current = 0;
            for (var i = 0; i < languages.Count; ++i)
            {
                names[i] = languages[i].Name;
                if (languages[i].Code == Languages.Current) current = i;
            }

            Choice(c, Localization.UI_SETTINGS_LANGUAGE, Localization.UI_SETTINGS_LANGUAGE_HINT, names, current, 0, i =>
            {
                // the window redraws itself in the new language when it is applied
                Languages.Set(languages[i].Code);
            }, forceStepper: true);

            // Applied when the slider is let go: the window scales with the setting, so applying while
            // dragging moved the slider under the pointer and made it jump back and forth.
            SliderRow(c, Localization.UI_SETTINGS_UI_SCALE, string.Format(CultureInfo.InvariantCulture, Localization.UI_SETTINGS_UI_SCALE_HINT, Keybinds.Label(Keybinds.Action.UiBigger), Keybinds.Label(Keybinds.Action.UiSmaller)), UiScale.Min, UiScale.Max, UiScale.Value,
                SettingsApplier.Defaults.UiScale, Percent, v =>
                {
                    // the file is written when the window closes, not on every change
                    UiScale.Set(v, save: false);
                    PlayerSettings.Instance.UIScale = UiScale.Value;
                }, step: UiScale.Step, applyOnRelease: true);
            Switch(c, Localization.UI_SETTINGS_NAMES, Localization.UI_SETTINGS_NAMES_HINT, SettingsApplier.PlayerNames(g), SettingsApplier.Defaults.PlayerNames, v => SettingsApplier.SetPlayerNames(g, v));
            Switch(c, Localization.UI_SETTINGS_PLAYER_LIST, WithKey(Localization.UI_SETTINGS_PLAYER_LIST_HINT, Keybinds.Action.ToggleHud), SettingsApplier.PlayerList(g), SettingsApplier.Defaults.PlayerList, v => SettingsApplier.SetPlayerList(g, v));
            Choice(c, Localization.UI_SETTINGS_EXP_DISPLAY, WithKey(Localization.UI_SETTINGS_EXP_DISPLAY_HINT, Keybinds.Action.ExpRate), Localization.UI_SETTINGS_EXP_DISPLAY_OPTIONS,
                SettingsApplier.ExpDisplay, SettingsApplier.Defaults.ExpDisplay, v => SettingsApplier.SetExpDisplay(g, v));
            Switch(c, Localization.UI_SETTINGS_PLAYER_DETAILS, WithKey(Localization.UI_SETTINGS_PLAYER_DETAILS_HINT, Keybinds.Action.PlayerDetails), SettingsApplier.PlayerDetails(g), SettingsApplier.Defaults.PlayerDetails,
                v => SettingsApplier.SetPlayerDetails(g, v));
            Switch(c, Localization.UI_SETTINGS_HIDE_LOGOS, WithKey(Localization.UI_SETTINGS_HIDE_LOGOS_HINT, Keybinds.Action.Logos), SettingsApplier.HideLogos, SettingsApplier.Defaults.HideLogos,
                v => SettingsApplier.SetHideLogos(g, v));
            Switch(c, Localization.UI_SETTINGS_SHORTCUTS, Localization.UI_SETTINGS_SHORTCUTS_HINT, SettingsApplier.ShortcutBar, SettingsApplier.Defaults.ShortcutBar,
                SettingsApplier.SetShortcutBar);
            ButtonRow(c, Localization.UI_SETTINGS_RESET_WINDOWS, Localization.UI_SETTINGS_RESET_WINDOWS_HINT, Localization.UI_SETTINGS_RESET_WINDOWS_BUTTON, ResetWindowPositions);
        }

        private void BuildBotMessages(VisualElement c)
        {
            var g = gameManager;
            Intro(c, Localization.UI_SETTINGS_BOT_HINT);

            Section(c, Localization.UI_SETTINGS_SECTION_MESSAGES);
            VisualElement expTimes = null;
            BotSwitch(c, "ExpMultiplierExpire", Localization.UI_SETTINGS_BOT_EXP, Localization.UI_SETTINGS_BOT_EXP_HINT, on => expTimes.SetEnabled(on));
            expTimes = Chips(c, Localization.UI_SETTINGS_EXP_TIMES, Localization.UI_SETTINGS_EXP_TIMES_HINT, Localization.UI_SETTINGS_EXP_TIME_OPTIONS,
                SettingsApplier.ExpAnnouncement, SettingsApplier.SetExpAnnouncement, SettingsApplier.Defaults.ExpAnnouncements);
            expTimes.SetEnabled(SettingsApplier.BotMessageEnabled("ExpMultiplierExpire"));
            BotSwitch(c, "PlayerWelcome", Localization.UI_SETTINGS_BOT_WELCOME, Localization.UI_SETTINGS_BOT_WELCOME_HINT);
            BotSwitch(c, "Disembark", Localization.UI_SETTINGS_BOT_DISEMBARK, Localization.UI_SETTINGS_BOT_DISEMBARK_HINT);
            BotSwitch(c, "TrainWarning", Localization.UI_SETTINGS_BOT_TRAIN, Localization.UI_SETTINGS_BOT_TRAIN_HINT);
            VisualElement minutesRow = null;
            Switch(c, Localization.UI_SETTINGS_NO_EXP, Localization.UI_SETTINGS_NO_EXP_HINT, SettingsApplier.AnnounceNoExp, SettingsApplier.Defaults.AnnounceNoExp, v =>
            {
                SettingsApplier.SetAnnounceNoExp(v);
                minutesRow.SetEnabled(v);
            });
            minutesRow = Choice(c, Localization.UI_SETTINGS_NO_EXP_TIME, Localization.UI_SETTINGS_NO_EXP_TIME_HINT, Localization.UI_SETTINGS_NO_EXP_OPTIONS,
                SettingsApplier.NoExpMinutes, SettingsApplier.Defaults.NoExpMinutes, SettingsApplier.SetNoExpMinutes);
            minutesRow.SetEnabled(SettingsApplier.AnnounceNoExp);
            BotSwitch(c, "DungeonResult", Localization.UI_SETTINGS_BOT_DUNGEON, Localization.UI_SETTINGS_BOT_DUNGEON_HINT);
            BotSwitch(c, "RaidResult", Localization.UI_SETTINGS_BOT_RAID, Localization.UI_SETTINGS_BOT_RAID_HINT);

            Section(c, Localization.UI_SETTINGS_SECTION_LOOT);
            VisualElement dropsRow = null;
            BotSwitch(c, "ItemDrop", Localization.UI_SETTINGS_BOT_ITEMS, Localization.UI_SETTINGS_BOT_ITEMS_HINT, on => dropsRow.SetEnabled(on));
            Label example = null;
            dropsRow = Choice(c, Localization.UI_SETTINGS_DROPS, Localization.UI_SETTINGS_DROPS_HINT, Localization.UI_SETTINGS_DROPS_OPTIONS,
                SettingsApplier.ItemDropMessages(g), SettingsApplier.Defaults.ItemDropMessages, v =>
                {
                    SettingsApplier.SetItemDropMessages(g, v);
                    example.text = DropExample(v);
                });
            example = new Label(DropExample(SettingsApplier.ItemDropMessages(g))) { pickingMode = PickingMode.Ignore };
            example.AddToClassList("rf-settings-row__example");
            dropsRow.Q(className: "rf-settings-row__text").Add(example);
            dropsRow.SetEnabled(SettingsApplier.BotMessageEnabled("ItemDrop"));
            Switch(c, Localization.UI_SETTINGS_LOOT_ORIGIN, Localization.UI_SETTINGS_LOOT_ORIGIN_HINT, SettingsApplier.LootOrigin, SettingsApplier.Defaults.LootOrigin,
                SettingsApplier.SetLootOrigin);

            Section(c, Localization.UI_SETTINGS_SECTION_CONNECTION);
            TextRow(c, Localization.UI_SETTINGS_BOT_SERVER, Localization.UI_SETTINGS_BOT_SERVER_HINT, SettingsApplier.BotServer, SettingsApplier.Defaults.BotServer,
                SettingsApplier.SetBotServer);
            VisualElement portRow = null;
            Switch(c, Localization.UI_SETTINGS_LOCAL_BOT, Localization.UI_SETTINGS_LOCAL_BOT_HINT, SettingsApplier.LocalBot, SettingsApplier.Defaults.LocalBot, v =>
            {
                SettingsApplier.SetLocalBot(v);
                portRow.SetEnabled(v);
            });
            portRow = TextRow(c, Localization.UI_SETTINGS_LOCAL_PORT, Localization.UI_SETTINGS_LOCAL_PORT_HINT, SettingsApplier.LocalBotPort.ToString(CultureInfo.InvariantCulture),
                SettingsApplier.Defaults.LocalBotPort.ToString(CultureInfo.InvariantCulture), v =>
                {
                    if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)) SettingsApplier.SetLocalBotPort(port);
                }, numeric: true);
            portRow.SetEnabled(SettingsApplier.LocalBot);
            ReloadRow(c);

            void BotSwitch(VisualElement parent, string key, string title, string hint, Action<bool> also = null)
            {
                Switch(parent, title, hint, SettingsApplier.BotMessageEnabled(key), SettingsApplier.Defaults.BotMessage, v =>
                {
                    SettingsApplier.SetBotMessageEnabled(g, key, v);
                    also?.Invoke(v);
                });
            }
        }

        private void BuildAdmin(VisualElement c)
        {
            var g = gameManager;
            Switch(c, Localization.UI_SETTINGS_DEV_TOOLS, Localization.UI_SETTINGS_DEV_TOOLS_HINT, SettingsApplier.DevTools(g), SettingsApplier.Defaults.DevTools,
                v => SettingsApplier.SetDevTools(g, v));
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

        /// <summary>A hint naming the shortcut for the same thing, as it is bound now.</summary>
        private static string WithKey(string format, Keybinds.Action action) => string.Format(CultureInfo.InvariantCulture, format, Keybinds.Label(action));

        private static string Times(float v) => string.Format(CultureInfo.InvariantCulture, Localization.UI_SETTINGS_TIMES, v.ToString("0.00", CultureInfo.InvariantCulture));

        private static string Seconds(float v) => string.Format(CultureInfo.InvariantCulture, Localization.UI_SETTINGS_SECONDS, Mathf.RoundToInt(v));

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
            var text = new PlayerItemDropText(drops, (PlayerItemDropMessageSettings)setting, Localization.ItemDropWording());
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

        /// <summary>
        /// The small reset button at the start of a control. It keeps its space when hidden, so the
        /// controls stay lined up whether or not a setting has been changed.
        /// </summary>
        private Button ResetButton(VisualElement control, Action reset)
        {
            var button = new Button(reset) { text = "\uf2ea", tooltip = Localization.UI_SETTINGS_RESET_ONE };
            button.AddToClassList("rf-icon");
            button.AddToClassList("rf-settings-row__reset");
            // a switch row is clickable all over; the reset must not also flip the switch
            button.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            control.Add(button);
            return button;
        }

        private static void ShowReset(Button reset, bool changed) => reset.style.visibility = changed ? Visibility.Visible : Visibility.Hidden;

        /// <summary>An on/off switch. The whole row is the click target, not just the little switch.</summary>
        private VisualElement Switch(VisualElement parent, string title, string hint, bool value, bool defaultValue, Action<bool> changed)
        {
            var row = Row(parent, title, hint, out var control);
            row.AddToClassList("rf-settings-row--clickable");

            Button reset = null;
            var state = new Label { pickingMode = PickingMode.Ignore };
            state.AddToClassList("rf-switch__state");
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("rf-switch");
            var knob = new VisualElement { pickingMode = PickingMode.Ignore };
            knob.AddToClassList("rf-switch__knob");
            track.Add(knob);

            void Show()
            {
                track.EnableInClassList("rf-switch--on", value);
                state.EnableInClassList("rf-switch--on-state", value);
                state.text = value ? Localization.UI_SETTINGS_ON : Localization.UI_SETTINGS_OFF;
                ShowReset(reset, value != defaultValue);
            }

            void Set(bool v)
            {
                value = v;
                Show();
                changed(v);
                RefreshResetPage();
            }

            reset = ResetButton(control, () => Set(defaultValue));
            control.Add(state);
            control.Add(track);
            Show();

            row.AddManipulator(new Clickable(() =>
            {
                if (!row.enabledInHierarchy) return;
                Set(!value);
            }));
            pageRows.Add(new RowState { IsDefault = () => value == defaultValue, Reset = () => Set(defaultValue) });
            return row;
        }

        /// <param name="step">Snaps the value to this step; 0 for none.</param>
        /// <param name="applyOnRelease">
        /// Only call <paramref name="changed"/> once the slider is let go, for settings that move the
        /// slider itself, such as the interface size. Clicks on the track and keys apply at once.
        /// </param>
        private VisualElement SliderRow(VisualElement parent, string title, string hint, float low, float high, float value, float defaultValue,
            Func<float, string> format, Action<float> changed, float step = 0f, bool applyOnRelease = false)
        {
            var row = Row(parent, title, hint, out var control);

            float Snap(float v) => step > 0f ? Mathf.Round(v / step) * step : v;
            bool IsDefault(float v) => Mathf.Abs(v - Snap(Mathf.Clamp(defaultValue, low, high))) < Mathf.Max(step * 0.5f, 0.001f);

            Button reset = null;
            var slider = new Slider(low, high);
            slider.AddToClassList("rf-slider");
            var readout = new Label { pickingMode = PickingMode.Ignore };
            readout.AddToClassList("rf-settings-row__value");

            var applied = Snap(Mathf.Clamp(value, low, high));
            var dragging = false;

            void Apply(float v)
            {
                applied = v;
                changed(v);
                ShowReset(reset, !IsDefault(v));
                RefreshResetPage();
            }

            reset = ResetButton(control, () =>
            {
                var v = Snap(Mathf.Clamp(defaultValue, low, high));
                slider.SetValueWithoutNotify(v);
                readout.text = format(v);
                Apply(v);
            });
            control.Add(slider);
            control.Add(readout);

            slider.SetValueWithoutNotify(applied);
            readout.text = format(applied);
            ShowReset(reset, !IsDefault(applied));

            slider.RegisterValueChangedCallback(e =>
            {
                var v = Snap(e.newValue);
                if (!Mathf.Approximately(v, e.newValue)) slider.SetValueWithoutNotify(v);
                readout.text = format(v);
                if (!applyOnRelease || !dragging) Apply(v);
            });

            if (applyOnRelease)
            {
                // Trickled down so the slider sees them even though its dragger captures the pointer.
                slider.RegisterCallback<PointerDownEvent>(e => dragging = true, TrickleDown.TrickleDown);
                slider.RegisterCallback<PointerUpEvent>(e =>
                {
                    if (!dragging) return;
                    dragging = false;
                    var v = Snap(slider.value);
                    if (!Mathf.Approximately(v, applied)) Apply(v);
                }, TrickleDown.TrickleDown);
            }

            pageRows.Add(new RowState
            {
                IsDefault = () => IsDefault(applied),
                Reset = () =>
                {
                    var v = Snap(Mathf.Clamp(defaultValue, low, high));
                    slider.SetValueWithoutNotify(v);
                    readout.text = format(v);
                    Apply(v);
                }
            });
            return row;
        }

        /// <summary>A row of buttons for up to four choices, arrows either side of the value for more.</summary>
        private VisualElement Choice(VisualElement parent, string title, string hint, string[] options, int value, int defaultValue, Action<int> changed, bool forceStepper = false)
        {
            var row = Row(parent, title, hint, out var control);
            value = Mathf.Clamp(value, 0, options.Length - 1);
            defaultValue = Mathf.Clamp(defaultValue, 0, options.Length - 1);

            Action show = null;
            void Set(int v)
            {
                value = v;
                show();
                changed(v);
                RefreshResetPage();
            }

            var reset = ResetButton(control, () => Set(defaultValue));

            if (options.Length <= 4 && !forceStepper)
            {
                var group = new VisualElement();
                group.AddToClassList("rf-segmented");
                var items = new Button[options.Length];
                for (var i = 0; i < options.Length; ++i)
                {
                    var index = i;
                    items[i] = new Button(() => { if (index != value) Set(index); }) { text = options[i] };
                    items[i].AddToClassList("rf-segmented__item");
                    group.Add(items[i]);
                }

                show = () =>
                {
                    for (var i = 0; i < items.Length; ++i) items[i].EnableInClassList("rf-segmented__item--active", i == value);
                    ShowReset(reset, value != defaultValue);
                };
                control.Add(group);
            }
            else
            {
                var stepper = new VisualElement();
                stepper.AddToClassList("rf-stepper");
                var label = new Label { pickingMode = PickingMode.Ignore };
                label.AddToClassList("rf-stepper__value");
                var prev = new Button(() => { if (value > 0) Set(value - 1); }) { text = "\uf053" };
                var next = new Button(() => { if (value < options.Length - 1) Set(value + 1); }) { text = "\uf054" };
                foreach (var arrow in new[] { prev, next })
                {
                    arrow.AddToClassList("rf-icon");
                    arrow.AddToClassList("rf-stepper__arrow");
                }

                show = () =>
                {
                    label.text = options[value];
                    prev.SetEnabled(value > 0);
                    next.SetEnabled(value < options.Length - 1);
                    ShowReset(reset, value != defaultValue);
                };
                stepper.Add(prev);
                stepper.Add(label);
                stepper.Add(next);
                control.Add(stepper);
            }

            show();
            pageRows.Add(new RowState { IsDefault = () => value == defaultValue, Reset = () => Set(defaultValue) });
            return row;
        }

        /// <summary>The connection and query engine are read when the game starts; a reload applies them.</summary>
        private void ReloadRow(VisualElement parent)
        {
            ButtonRow(parent, Localization.UI_SETTINGS_RELOAD_NOTE, null, Localization.UI_SETTINGS_RELOAD_BUTTON, () =>
            {
                Close();
                if (gameManager) gameManager.SaveStateAndLoadScene();
            });
        }

        /// <summary>A text box, committed when Enter is pressed or it loses the keyboard.</summary>
        private VisualElement TextRow(VisualElement parent, string title, string hint, string value, string defaultValue, Action<string> changed, bool numeric = false)
        {
            var row = Row(parent, title, hint, out var control);
            Button reset = null;
            var field = new TextField { value = value ?? string.Empty, isDelayed = true };
            field.AddToClassList("rf-settings-field");
            if (numeric) field.AddToClassList("rf-settings-field--short");

            void Commit(string v)
            {
                value = v;
                changed(v);
                ShowReset(reset, value != defaultValue);
                RefreshResetPage();
            }

            reset = ResetButton(control, () =>
            {
                field.SetValueWithoutNotify(defaultValue);
                Commit(defaultValue);
            });
            control.Add(field);
            ShowReset(reset, value != defaultValue);

            // isDelayed: the value only changes on Enter or when the field loses focus
            field.RegisterValueChangedCallback(e => Commit(e.newValue));

            pageRows.Add(new RowState
            {
                IsDefault = () => value == defaultValue,
                Reset = () =>
                {
                    field.SetValueWithoutNotify(defaultValue);
                    Commit(defaultValue);
                }
            });
            return row;
        }

        /// <summary>Several choices that can each be on or off, as a row of chips.</summary>
        private VisualElement Chips(VisualElement parent, string title, string hint, string[] options, Func<int, bool> isOn, Action<int, bool> set, bool[] defaults)
        {
            var row = Row(parent, title, hint, out var control);
            Button reset = null;
            var chips = new Button[options.Length];

            bool AllDefault()
            {
                for (var i = 0; i < options.Length; ++i) if (isOn(i) != defaults[i]) return false;
                return true;
            }

            void Show()
            {
                for (var i = 0; i < chips.Length; ++i) chips[i].EnableInClassList("rf-chip--on", isOn(i));
                ShowReset(reset, !AllDefault());
            }

            void ResetAll()
            {
                for (var i = 0; i < options.Length; ++i) if (isOn(i) != defaults[i]) set(i, defaults[i]);
                Show();
                RefreshResetPage();
            }

            reset = ResetButton(control, ResetAll);
            var group = new VisualElement();
            group.AddToClassList("rf-chips");
            for (var i = 0; i < options.Length; ++i)
            {
                var index = i;
                chips[i] = new Button(() =>
                {
                    set(index, !isOn(index));
                    Show();
                    RefreshResetPage();
                }) { text = options[i] };
                chips[i].AddToClassList("rf-chip");
                group.Add(chips[i]);
            }

            control.Add(group);
            Show();
            pageRows.Add(new RowState { IsDefault = AllDefault, Reset = ResetAll });
            return row;
        }

        // ---- Keys -----------------------------------------------------------------------------------

        private void BuildKeys(VisualElement c)
        {
            Intro(c, string.IsNullOrEmpty(keyNote) ? Localization.UI_SETTINGS_KEYS_HINT : keyNote);
            keyNote = null;

            Section(c, Localization.UI_SETTINGS_SECTION_KEYS_CAMERA);
            KeyRow(c, Keybinds.Action.NextPlayer);
            KeyRow(c, Keybinds.Action.FreeCamera);
            KeyRow(c, Keybinds.Action.NextIsland);
            KeyRow(c, Keybinds.Action.PlayerDetails);
            KeyRow(c, Keybinds.Action.ControlPlayer);

            Section(c, Localization.UI_SETTINGS_SECTION_KEYS_INTERFACE);
            KeyRow(c, Keybinds.Action.ToggleHud);
            KeyRow(c, Keybinds.Action.Search);
            KeyRow(c, Keybinds.Action.ExpRate);
            KeyRow(c, Keybinds.Action.Logos);
            KeyRow(c, Keybinds.Action.UiBigger);
            KeyRow(c, Keybinds.Action.UiSmaller);

            Section(c, Localization.UI_SETTINGS_SECTION_KEYS_GAME);
            KeyRow(c, Keybinds.Action.Graphics);
            KeyRow(c, Keybinds.Action.Physics);
            KeyRow(c, Keybinds.Action.SaveState);
            KeyRow(c, Keybinds.Action.Reload);

            if (IsAdministrator())
            {
                Section(c, Localization.UI_SETTINGS_SECTION_KEYS_ADMIN);
                KeyRow(c, Keybinds.Action.StartRaid);
                KeyRow(c, Keybinds.Action.StartDungeon);
            }

            Intro(c, Localization.UI_SETTINGS_KEYS_FIXED);
        }

        private void KeyRow(VisualElement parent, Keybinds.Action action)
        {
            var row = Row(parent, Keybinds.Name(action), null, out var control);
            var reset = ResetButton(control, () =>
            {
                Keybinds.Reset(action);
                SelectTab(activeTab);
            });
            var cap = new Button(() => BeginCapture(action)) { text = Keybinds.Label(action) };
            cap.AddToClassList("rf-keycap");
            if (capturing == action)
            {
                cap.text = Localization.UI_SETTINGS_KEYS_PRESS;
                cap.AddToClassList("rf-keycap--waiting");
            }

            control.Add(cap);
            ShowReset(reset, !Keybinds.IsDefault(action));
            pageRows.Add(new RowState { IsDefault = () => Keybinds.IsDefault(action), Reset = () => Keybinds.Reset(action) });
        }

        private void BeginCapture(Keybinds.Action action)
        {
            capturing = action;
            Keybinds.Suspended = true;
            SelectTab(activeTab);
        }

        private void EndCapture()
        {
            capturing = null;
            // stay suspended until every key is up, see Update
            releaseBeforeKeys = true;
        }

        private void PollCapture()
        {
            if (!Input.anyKeyDown) return;

            var action = capturing.Value;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EndCapture();
                SelectTab(activeTab);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Keybinds.Set(action, Keybinds.Binding.None);
                EndCapture();
                SelectTab(activeTab);
                return;
            }

            if (bindableKeys == null)
            {
                var all = (KeyCode[])Enum.GetValues(typeof(KeyCode));
                bindableKeys = Array.FindAll(all, Keybinds.IsBindable);
            }

            foreach (var key in bindableKeys)
            {
                if (!Input.GetKeyDown(key)) continue;
                var swapped = Keybinds.Set(action, new Keybinds.Binding(key, Keybinds.HeldMods()));
                if (swapped.HasValue) keyNote = string.Format(CultureInfo.InvariantCulture, Localization.UI_SETTINGS_KEYS_SWAPPED, Keybinds.Name(swapped.Value));
                EndCapture();
                SelectTab(activeTab);
                return;
            }
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
