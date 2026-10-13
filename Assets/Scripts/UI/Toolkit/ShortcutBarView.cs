using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// A strip along the bottom of the screen listing the keyboard shortcuts, each a button that does the
    /// same as its key. Streamers kept asking which key does what; this answers it on screen.
    /// </summary>
    /// <remarks>
    /// Lives in the TopStatus document (markup "shortcut-bar" in TopStatus.uxml); TownBarView adds this
    /// component to the same GameObject. Hidden before login, during village build mode (which uses the
    /// bottom of the screen), when F1 has hidden the interface, and when turned off in the UI settings
    /// (PlayerSettings.ShortcutBarVisible).
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class ShortcutBarView : MonoBehaviour
    {
        public static ShortcutBarView Instance { get; private set; }

        private sealed class Shortcut
        {
            public string Key;
            public string Icon;
            public string Tooltip;
            public Action Run;
            public bool AdminOnly;
            public Button Button;
        }

        private readonly List<Shortcut> shortcuts = new List<Shortcut>();
        private VisualElement bar;
        private bool bound;
        private bool shown;
        private bool shownAdmin;

        private void OnEnable()
        {
            Instance = this;
            TryBind();
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        private void TryBind()
        {
            if (bound) return;
            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            bar = root?.Q<VisualElement>("shortcut-bar");
            if (bar == null) return;

            Define();
            bar.Clear();
            foreach (var s in shortcuts)
            {
                var button = new Button { text = string.Empty, tooltip = s.Tooltip };
                button.AddToClassList("rf-shortcut");
                var icon = new Label(s.Icon) { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("rf-icon");
                icon.AddToClassList("rf-shortcut__icon");
                var key = new Label(s.Key) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("rf-shortcut__key");
                button.Add(icon);
                button.Add(key);
                var run = s.Run;
                button.clicked += () =>
                {
                    try { run(); }
                    catch (Exception exc) { Shinobytes.Debug.LogError("Shortcut button failed: " + exc.Message); }
                };
                s.Button = button;
                bar.Add(button);
            }

            shownAdmin = true;
            bound = true;
        }

        /// <summary>The shortcuts, in the order they appear. Each runs exactly what its key runs.</summary>
        private void Define()
        {
            shortcuts.Clear();
            Add("F1", "", Localization.UI_SHORTCUT_HUD, () => { if (HUDController.Instance) HUDController.Instance.CycleState(); });
            Add("F2", "", Localization.UI_SHORTCUT_DETAILS, () => GameManager.Instance.Camera.Observer.ToggleVisibility());
            Add("Tab", "", Localization.UI_SHORTCUT_NEXT_PLAYER, () => GameManager.Instance.Camera.ObserveNextPlayer());
            Add("Shift+Tab", "", Localization.UI_SHORTCUT_FREE_CAMERA, ToggleFreeCamera);
            Add("Ctrl+F", "", Localization.UI_SHORTCUT_SEARCH, () => GameManager.Instance.ShowPlayerSearch());
            Add("I", "", Localization.UI_SHORTCUT_EXP, () => GameManager.Instance.PlayerList.ToggleExpRate());
            Add("Q", "", Localization.UI_SHORTCUT_LOGOS, () => GameManager.Instance.LogoCensor = !GameManager.Instance.LogoCensor);
            Add("Ctrl+", "", Localization.UI_SHORTCUT_UI_BIGGER, () => UiScale.Increase());
            Add("Ctrl-", "", Localization.UI_SHORTCUT_UI_SMALLER, () => UiScale.Decrease());
            Add("F9", "", Localization.UI_SHORTCUT_GRAPHICS, () => GameManager.Instance.Graphics.ToggleAllGraphics());
            Add("Esc", "", Localization.UI_SHORTCUT_MENU, () => GameManager.Instance.ShowMenu());
            Add("Ctrl+P", "", Localization.UI_SHORTCUT_RAID, () => GameManager.Instance.Raid.ForceStartRaid(), adminOnly: true);
            Add("Ctrl+O", "", Localization.UI_SHORTCUT_DUNGEON, () => GameManager.Instance.Dungeons.ForceActivateDungeon(), adminOnly: true);
        }

        private void Add(string key, string icon, string tooltip, Action run, bool adminOnly = false)
        {
            shortcuts.Add(new Shortcut { Key = key, Icon = icon, Tooltip = tooltip, Run = run, AdminOnly = adminOnly });
        }

        /// <summary>The key is held to fly; a click switches the free camera on or off instead.</summary>
        private static void ToggleFreeCamera()
        {
            var camera = GameManager.Instance.Camera;
            if (camera.ForcedFreeCamera) camera.ReleaseFreeCamera();
            else camera.ForceFreeCamera(false);
        }

        private void Update()
        {
            if (!bound)
            {
                TryBind();
                return;
            }

            var gm = GameManager.Instance;
            var loggedIn = gm && gm.IsLoaded && gm.RavenNest != null && gm.RavenNest.Authenticated;
            var building = VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive;
            var hudHidden = HUDController.Instance && HUDController.Instance.State == HUDController.UIState.Nothing;
            var wanted = PlayerSettings.Instance.ShortcutBarVisible.GetValueOrDefault(true);
            SetShown(loggedIn && wanted && !building && !hudHidden);

            var admin = gm && gm.SessionSettings != null && gm.SessionSettings.IsAdministrator;
            if (shown && admin != shownAdmin)
            {
                shownAdmin = admin;
                foreach (var s in shortcuts)
                {
                    if (s.AdminOnly) s.Button.style.display = admin ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void SetShown(bool value)
        {
            if (value == shown) return;
            shown = value;
            bar.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Turns the bar on or off and remembers it, for the UI settings.</summary>
        public static void SetEnabled(bool visible)
        {
            if (PlayerSettings.Instance.ShortcutBarVisible == visible) return;
            PlayerSettings.Instance.ShortcutBarVisible = visible;
            PlayerSettings.Save();
        }
    }
}
