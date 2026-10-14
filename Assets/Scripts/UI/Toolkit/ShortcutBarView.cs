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
            public string Key;
            public string Icon;
            public string Caption;
            public Action Run;
            public bool AdminOnly;
            public Button Button;
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
                var key = new Label(s.Key) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("rf-shortcut__key");
                button.Add(icon);
                button.Add(key);

                var text = s.Caption;
                button.RegisterCallback<PointerEnterEvent>(e => ShowCaption(text));
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
            Add(Group.Camera, "F2", "\uf2c2", Localization.UI_SHORTCUT_DETAILS, () => GameManager.Instance.Camera.Observer.ToggleVisibility());
            Add(Group.Camera, "Tab", "\uf061", Localization.UI_SHORTCUT_NEXT_PLAYER, () => GameManager.Instance.Camera.ObserveNextPlayer());
            Add(Group.Camera, "\u2191Tab", "\uf03d", Localization.UI_SHORTCUT_FREE_CAMERA, ToggleFreeCamera);

            Add(Group.Players, "Ctrl+F", "\uf002", Localization.UI_SHORTCUT_SEARCH, () => GameManager.Instance.ShowPlayerSearch());
            Add(Group.Players, "I", "\uf201", Localization.UI_SHORTCUT_EXP, () => GameManager.Instance.PlayerList.ToggleExpRate());

            Add(Group.Interface, "F1", "\uf06e", Localization.UI_SHORTCUT_HUD, () => { if (HUDController.Instance) HUDController.Instance.CycleState(); });
            Add(Group.Interface, "Q", "\uf070", Localization.UI_SHORTCUT_LOGOS, () => GameManager.Instance.LogoCensor = !GameManager.Instance.LogoCensor);
            Add(Group.Interface, "Ctrl+", "\uf00e", Localization.UI_SHORTCUT_UI_BIGGER, () => UiScale.Increase());
            Add(Group.Interface, "Ctrl-", "\uf010", Localization.UI_SHORTCUT_UI_SMALLER, () => UiScale.Decrease());
            Add(Group.Interface, "F9", "\uf108", Localization.UI_SHORTCUT_GRAPHICS, () => GameManager.Instance.Graphics.ToggleAllGraphics());

            Add(Group.Admin, "Ctrl+P", "\uf6d5", Localization.UI_SHORTCUT_RAID, () => GameManager.Instance.Raid.ForceStartRaid(), adminOnly: true);
            Add(Group.Admin, "Ctrl+O", "\uf6d9", Localization.UI_SHORTCUT_DUNGEON, () => GameManager.Instance.Dungeons.ForceActivateDungeon(), adminOnly: true);

            Add(Group.Menu, "Esc", "\uf0c9", Localization.UI_SHORTCUT_MENU, () => GameManager.Instance.ShowMenu());
        }

        private void Add(Group group, string key, string icon, string caption, Action run, bool adminOnly = false)
        {
            shortcuts.Add(new Shortcut { Group = group, Key = key, Icon = icon, Caption = caption, Run = run, AdminOnly = adminOnly });
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
