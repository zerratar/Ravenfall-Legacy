using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The keyboard shortcuts as a small menu of icon buttons in the bottom right corner, in the manner
    /// of an MMO micro menu: each button does the same as its key, which sits as a badge on the button.
    /// Hovering one names it in a caption above the cluster, since runtime UI Toolkit shows no tooltips.
    /// The cluster dims while the mouse is elsewhere so it stays out of the stream's way.
    /// </summary>
    /// <remarks>
    /// Lives in the TopStatus document (markup "shortcut-bar" in TopStatus.uxml); TownBarView adds this
    /// component to the same GameObject. Hidden before login, during village build mode (which uses the
    /// bottom of the screen), when F1 has hidden the interface, and when turned off in the UI settings
    /// (PlayerSettings.ShortcutBarVisible). The raid and dungeon buttons only show for administrators,
    /// as their keys only work for them.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class ShortcutBarView : MonoBehaviour
    {
        public static ShortcutBarView Instance { get; private set; }

        private enum Group { Camera, Players, Interface, Admin, Menu }

        private sealed class Shortcut
        {
            public Group Group;
            /// <summary>The rebindable action, or none for the menu, which is always Esc.</summary>
            public Keybinds.Action? Action;
            public string Icon;
            public Action Run;
            public bool AdminOnly;
            public Button Button;
            public Label KeyLabel;
            public string Caption;
        }

        private readonly List<Shortcut> shortcuts = new List<Shortcut>();
        private readonly List<VisualElement> adminElements = new List<VisualElement>();
        private VisualElement bar;
        private Label caption;
        private bool bound;
        private bool shown;
        private bool shownAdmin;

        private void OnEnable()
        {
            Instance = this;
            Keybinds.Changed += RefreshKeys;
            Languages.Changed += RefreshKeys;
            TryBind();
        }

        private void OnDisable()
        {
            Keybinds.Changed -= RefreshKeys;
            Languages.Changed -= RefreshKeys;
            if (Instance == this) Instance = null;
        }

        /// <summary>Badges and captions follow the keys as they are now, after any rebinding.</summary>
        private void RefreshKeys()
        {
            foreach (var s in shortcuts)
            {
                if (s.KeyLabel == null) continue;
                if (s.Action.HasValue)
                {
                    var label = Keybinds.Label(s.Action.Value);
                    s.KeyLabel.text = Badge(label);
                    s.Caption = Keybinds.Name(s.Action.Value) + " (" + label + ")";
                }
                else
                {
                    s.KeyLabel.text = "Esc";
                    s.Caption = Localization.UI_SHORTCUT_MENU;
                }
            }
        }

        /// <summary>Shift as an arrow keeps the badge short enough for its button.</summary>
        private static string Badge(string label) => label.Replace("Shift+", "\u2191");

        private void TryBind()
        {
            if (bound) return;
            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            bar = root?.Q<VisualElement>("shortcut-bar");
            if (bar == null) return;

            Define();
            bar.Clear();
            // the caption's empty space must not catch clicks meant for the world; the row does the picking
            bar.pickingMode = PickingMode.Ignore;
            adminElements.Clear();

            caption = new Label { pickingMode = PickingMode.Ignore };
            caption.AddToClassList("rf-shortcuts__caption");
            bar.Add(caption);

            var row = new VisualElement();
            row.AddToClassList("rf-shortcuts__row");
            bar.Add(row);

            VisualElement group = null;
            Group? current = null;
            foreach (var s in shortcuts)
            {
                if (current != s.Group)
                {
                    if (current != null)
                    {
                        var divider = new VisualElement { pickingMode = PickingMode.Ignore };
                        divider.AddToClassList("rf-shortcuts__divider");
                        row.Add(divider);
                        if (s.AdminOnly) adminElements.Add(divider);
                    }

                    group = new VisualElement();
                    group.AddToClassList("rf-shortcuts__group");
                    row.Add(group);
                    if (s.AdminOnly) adminElements.Add(group);
                    current = s.Group;
                }

                var button = new Button { text = string.Empty };
                button.AddToClassList("rf-shortcut");
                if (s.Group == Group.Menu) button.AddToClassList("rf-shortcut--menu");
                var icon = new Label(s.Icon) { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("rf-icon");
                icon.AddToClassList("rf-shortcut__icon");
                var key = new Label { pickingMode = PickingMode.Ignore };
                key.AddToClassList("rf-shortcut__key");
                button.Add(icon);
                button.Add(key);
                s.KeyLabel = key;

                var shortcut = s;
                button.RegisterCallback<PointerEnterEvent>(e => ShowCaption(shortcut.Caption));
                button.RegisterCallback<PointerLeaveEvent>(e => ShowCaption(null));

                var run = s.Run;
                button.clicked += () =>
                {
                    try { run(); }
                    catch (Exception exc) { Shinobytes.Debug.LogError("Shortcut button failed: " + exc.Message); }
                };
                s.Button = button;
                group.Add(button);
            }

            RefreshKeys();
            ShowCaption(null);
            shownAdmin = true;
            bound = true;
        }

        private void ShowCaption(string text)
        {
            if (caption == null) return;
            caption.text = text ?? string.Empty;
            caption.EnableInClassList("rf-shortcuts__caption--visible", !string.IsNullOrEmpty(text));
        }

        /// <summary>The shortcuts, in groups and in the order they appear. Each runs exactly what its key runs.</summary>
        private void Define()
        {
            shortcuts.Clear();
            Add(Group.Camera, Keybinds.Action.PlayerDetails, "\uf2c2", () => SettingsApplier.TogglePlayerDetails(GameManager.Instance));
            Add(Group.Camera, Keybinds.Action.NextPlayer, "\uf061", () => GameManager.Instance.Camera.ObserveNextPlayer());
            Add(Group.Camera, Keybinds.Action.FreeCamera, "\uf03d", ToggleFreeCamera);

            Add(Group.Players, Keybinds.Action.Search, "\uf002", () => GameManager.Instance.ShowPlayerSearch());
            Add(Group.Players, Keybinds.Action.ExpRate, "\uf201", () => SettingsApplier.CycleExpDisplay(GameManager.Instance));

            Add(Group.Interface, Keybinds.Action.ToggleHud, "\uf06e", () => { if (HUDController.Instance) HUDController.Instance.CycleState(); });
            Add(Group.Interface, Keybinds.Action.Logos, "\uf070", () => SettingsApplier.ToggleHideLogos(GameManager.Instance));
            Add(Group.Interface, Keybinds.Action.UiBigger, "\uf00e", () => UiScale.Increase());
            Add(Group.Interface, Keybinds.Action.UiSmaller, "\uf010", () => UiScale.Decrease());
            Add(Group.Interface, Keybinds.Action.Graphics, "\uf108", () => SettingsApplier.ToggleGraphics(GameManager.Instance));

            Add(Group.Admin, Keybinds.Action.StartRaid, "\uf6d5", () => GameManager.Instance.Raid.ForceStartRaid(), adminOnly: true);
            Add(Group.Admin, Keybinds.Action.StartDungeon, "\uf6d9", () => GameManager.Instance.Dungeons.ForceActivateDungeon(), adminOnly: true);

            Add(Group.Menu, null, "\uf0c9", () => GameManager.Instance.ShowMenu());
        }

        private void Add(Group group, Keybinds.Action? action, string icon, Action run, bool adminOnly = false)
        {
            shortcuts.Add(new Shortcut { Group = group, Action = action, Icon = icon, Run = run, AdminOnly = adminOnly });
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
            // A live reload of the markup or styles replaces the tree; bind again to the new one.
            if (bound && bar.panel == null)
            {
                bound = false;
                shown = false;
            }

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
                foreach (var e in adminElements)
                {
                    e.style.display = admin ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void SetShown(bool value)
        {
            if (value == shown) return;
            shown = value;
            bar.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (!value) ShowCaption(null);
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
