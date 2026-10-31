using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The keyboard shortcuts as a small menu of icon buttons in the bottom right corner, in the manner
    /// of an MMO micro menu: each button does the same as its key.
    /// Hovering one names it in a tooltip over the button, with its key as a key cap and an arrow
    /// pointing down at it, since runtime UI Toolkit shows no tooltips of its own. The bar dims while
    /// the mouse is elsewhere so it stays out of the stream's way.
    /// </summary>
    /// <remarks>
    /// Lives in the TopStatus document (markup "shortcut-bar" in TopStatus.uxml); TownBarView adds this
    /// component to the same GameObject. Hidden before login, during village build mode (which uses the
    /// bottom of the screen) and when F1 has hidden the interface. Turned off in the UI settings
    /// (PlayerSettings.ShortcutBarVisible), only the menu button stays, so the menu can always be
    /// reached with the mouse, as it can with the settings cog in the top right corner (uGUI
    /// Game UI/SettingsButton, which the user wants kept). The raid and dungeon buttons only show for
    /// administrators, as their keys only work for them.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class ShortcutBarView : MonoBehaviour
    {
        public static ShortcutBarView Instance { get; private set; }

        private enum Group { Camera, Players, Interface, Admin, Menu }

        private sealed class Shortcut
        {
            public Group Group;
            /// <summary>The rebindable action, or none: the menu (always Esc) and the quest window (no key).</summary>
            public Keybinds.Action? Action;
            /// <summary>The name and key of a button without an action.</summary>
            public System.Func<string> FixedName;
            public string FixedKey;
            public string Icon;
            public Action Run;
            public bool AdminOnly;
            public Button Button;
            public string Name;
            public string Key;
        }

        private readonly List<Shortcut> shortcuts = new List<Shortcut>();
        /// <summary>Every group and divider, with whether it is the admin's and whether it is the menu's.</summary>
        private readonly List<(VisualElement Element, bool Admin, bool Menu)> parts = new List<(VisualElement, bool, bool)>();
        private VisualElement bar;
        private VisualElement tip;
        private Label tipName;
        private Label tipKey;
        private VisualElement tipArrow;
        private Shortcut tipFor;
        private bool bound;
        private bool shown;
        private bool shownAdmin;
        private bool shownMenuOnly;

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

        /// <summary>The tooltips follow the keys as they are now, after any rebinding.</summary>
        private void RefreshKeys()
        {
            foreach (var s in shortcuts)
            {
                if (s.Action.HasValue)
                {
                    s.Name = Keybinds.Name(s.Action.Value);
                    s.Key = Keybinds.Label(s.Action.Value);
                }
                else
                {
                    s.Name = s.FixedName?.Invoke() ?? string.Empty;
                    s.Key = s.FixedKey;
                }
            }
            if (tipFor != null) ShowTip(tipFor);
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
            // the tooltip and empty space must not catch clicks meant for the world; the row does the picking
            bar.pickingMode = PickingMode.Ignore;
            parts.Clear();

            // the tooltip floats over the hovered button: placed from its bounds once it has a size
            tip = new VisualElement { pickingMode = PickingMode.Ignore };
            tip.AddToClassList("rf-shortcuts__tip");
            tipName = new Label { pickingMode = PickingMode.Ignore };
            tipName.AddToClassList("rf-shortcuts__tip-name");
            tipKey = new Label { pickingMode = PickingMode.Ignore };
            tipKey.AddToClassList("rf-shortcuts__tip-key");
            tipArrow = new VisualElement { pickingMode = PickingMode.Ignore };
            tipArrow.AddToClassList("rf-shortcuts__tip-arrow");
            tip.Add(tipName);
            tip.Add(tipKey);
            tip.Add(tipArrow);
            tip.RegisterCallback<GeometryChangedEvent>(_ => PlaceTip());
            bar.Add(tip);

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
                        parts.Add((divider, s.AdminOnly, false));
                    }

                    group = new VisualElement();
                    group.AddToClassList("rf-shortcuts__group");
                    row.Add(group);
                    parts.Add((group, s.AdminOnly, s.Group == Group.Menu));
                    current = s.Group;
                }

                var button = new Button { text = string.Empty };
                button.AddToClassList("rf-shortcut");
                if (s.Group == Group.Menu) button.AddToClassList("rf-shortcut--menu");
                var icon = new Label(s.Icon) { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("rf-icon");
                icon.AddToClassList("rf-shortcut__icon");
                button.Add(icon);

                var shortcut = s;
                button.RegisterCallback<PointerEnterEvent>(e => ShowTip(shortcut));
                button.RegisterCallback<PointerLeaveEvent>(e => { if (tipFor == shortcut) ShowTip(null); });

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
            ShowTip(null);
            shownAdmin = true;
            shownMenuOnly = false;
            bound = true;
        }

        private void ShowTip(Shortcut shortcut)
        {
            tipFor = shortcut;
            if (tip == null) return;
            tip.EnableInClassList("rf-shortcuts__tip--visible", shortcut != null);
            if (shortcut == null) return;
            tipName.text = shortcut.Name ?? string.Empty;
            tipKey.text = shortcut.Key ?? string.Empty;
            tipKey.style.display = string.IsNullOrEmpty(shortcut.Key) ? DisplayStyle.None : DisplayStyle.Flex;
            PlaceTip();
        }

        /// <summary>
        /// Over the hovered button, centred on it but kept inside the bar's width (the menu button sits
        /// at the screen's right edge), the arrow under the button wherever the tooltip had to go.
        /// </summary>
        private void PlaceTip()
        {
            if (tip == null || tipFor?.Button == null || bar == null) return;
            var button = tipFor.Button.worldBound;
            var area = bar.worldBound;
            var width = tip.resolvedStyle.width;
            if (float.IsNaN(width)) width = 0f;
            var centre = button.center.x - area.x;
            var left = Mathf.Clamp(centre - width * 0.5f, 0f, Mathf.Max(0f, area.width - width));
            tip.style.left = left;
            tip.style.bottom = area.yMax - button.yMin + 10f;
            tipArrow.style.left = centre - left - 7f;
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

            // the quest window, always within reach now that its bar at the top only shows when there is something on
            Add(Group.Menu, null, "\uf70e", () => { if (StreamQuestView.Instance) StreamQuestView.Instance.Toggle(); }, name: () => Localization.UI_SHORTCUT_QUESTS);
            Add(Group.Menu, null, "\uf0c9", () => GameManager.Instance.ShowMenu(), name: () => Localization.UI_SHORTCUT_MENU, key: "Esc");
        }

        private void Add(Group group, Keybinds.Action? action, string icon, Action run, bool adminOnly = false, System.Func<string> name = null, string key = null)
        {
            shortcuts.Add(new Shortcut { Group = group, Action = action, Icon = icon, Run = run, AdminOnly = adminOnly, FixedName = name, FixedKey = key });
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
            var menuOnly = !PlayerSettings.Instance.ShortcutBarVisible.GetValueOrDefault(true);
            SetShown(loggedIn && !building && !hudHidden);

            var admin = gm && gm.SessionSettings != null && gm.SessionSettings.IsAdministrator;
            if (shown && (admin != shownAdmin || menuOnly != shownMenuOnly))
            {
                shownAdmin = admin;
                shownMenuOnly = menuOnly;
                foreach (var (element, adminOnly, menu) in parts)
                {
                    var on = (menu || !menuOnly) && (!adminOnly || admin);
                    element.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void SetShown(bool value)
        {
            if (value == shown) return;
            shown = value;
            bar.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (!value) ShowTip(null);
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
