using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Ravebridge on screen (docs/ravebridge-design.md): while the event runs, a drawer at the top of the screen with who
    /// is there and how long it has been open, and the streamer's buttons: Play (or Stop while playing), Hero mode while
    /// playing, Stay for a forced event that would close by itself, and End. A few seconds after the mouse leaves it the
    /// drawer slides up out of the way, leaving a small tab at the top edge (who is there, the time, a quest's notice);
    /// the tab brings it down again. Play opens
    /// the "Who plays?" window, in the quest window's look: the streamer's own characters in the stream (where each is,
    /// with Play and Send home or Bring along) and the Ravebridge character (Play, Export, a new one, Import), and the
    /// step that makes one, with Back. Esc closes the window, leaves hero mode, then stops playing.
    /// </summary>
    public class RavebridgeView : MonoBehaviour
    {
        private VisualElement wrap;
        private VisualElement bar;
        private Label text;
        private Label meta;
        private Button stay, play, end, look, hero;
        private VisualElement drawer;
        private Label tabText, tabChevron;
        private bool collapsed;
        private bool hovered;
        private float hideAt;
        private const float HideAfter = 5f;

        private VisualElement window;
        private Label windowTitle;
        private VisualElement content;
        private Label hint;
        private bool creating;
        private TextField creatorName;

        private bool bound;
        private float nextSecond;
        private string notice;
        private float noticeUntil;

        private void OnEnable()
        {
            bound = false;
            RavebridgeQuests.Notice += ShowNotice;
            RavebridgeManager.Changed += Changed;
            RavebridgePlay.Changed += Changed;
            Languages.Changed += Rebuild;
        }

        private void OnDisable()
        {
            RavebridgeManager.Changed -= Changed;
            RavebridgePlay.Changed -= Changed;
            RavebridgeQuests.Notice -= ShowNotice;
            Languages.Changed -= Rebuild;
            bound = false;
        }

        private void Rebuild() => bound = false;

        private void Changed()
        {
            Redraw();
            if (WindowOpen && !creating) DrawWindow();
        }

        // ---- Building ---------------------------------------------------------------------------------------

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            if (layer == null) return;

            wrap?.RemoveFromHierarchy();
            window?.RemoveFromHierarchy();

            wrap = new VisualElement { name = "ravebridge", pickingMode = PickingMode.Ignore };
            wrap.AddToClassList("rf-rb");
            drawer = new VisualElement { pickingMode = PickingMode.Ignore };
            drawer.AddToClassList("rf-rb-drawer");
            bar = new VisualElement { name = "ravebridge-bar" };
            bar.AddToClassList("rf-rb-bar");
            var icon = new Label("") { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-rb-bar__icon");
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("rf-rb-bar__body");
            text = new Label { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-rb-bar__text");
            meta = new Label { pickingMode = PickingMode.Ignore };
            meta.AddToClassList("rf-rb-bar__meta");
            body.Add(text);
            body.Add(meta);
            var buttons = new VisualElement();
            buttons.AddToClassList("rf-rb-buttons");
            stay = BarButton(Localization.UI_RAVEBRIDGE_STAY, true, () => { var rb = RavebridgeManager.Instance; if (rb) rb.Attended = true; Redraw(); });
            hero = BarButton(Localization.UI_HERO_ENTER, true, () =>
            {
                if (HeroController.Active) HeroController.End();
                else if (RavebridgePlay.Playing) HeroController.Begin(RavebridgePlay.Playing);
                Redraw();
            });
            look = BarButton(Localization.UI_RAVEBRIDGE_NEW_LOOK, false, () =>
            {
                var p = RavebridgePlay.Playing;
                if (p && p.IsLocal) p.Appearance.SetAppearance(RavenNest.SDK.BotPlayerGenerator.GenerateRandomSyntyAppearance(), () => RavebridgeCharacter.Save(), false);
            });
            play = BarButton(Localization.UI_RAVEBRIDGE_PLAY, true, PlayPressed);
            end = BarButton(Localization.UI_RAVEBRIDGE_END, false, () => { CloseWindow(); var rb = RavebridgeManager.Instance; if (rb) rb.End(); });
            buttons.Add(stay);
            buttons.Add(hero);
            buttons.Add(look);
            buttons.Add(play);
            buttons.Add(end);
            bar.Add(icon);
            bar.Add(body);
            bar.Add(buttons);

            // the tab under the bar: the bar slides up behind the top edge and leaves it showing
            var tab = new VisualElement { name = "ravebridge-tab", pickingMode = PickingMode.Position };
            tab.AddToClassList("rf-rb-tab");
            var tabIcon = new Label("\uf6d9") { pickingMode = PickingMode.Ignore };
            tabIcon.AddToClassList("rf-icon");
            tabIcon.AddToClassList("rf-rb-tab__icon");
            tabText = new Label { pickingMode = PickingMode.Ignore };
            tabText.AddToClassList("rf-rb-tab__text");
            tabChevron = new Label { pickingMode = PickingMode.Ignore };
            tabChevron.AddToClassList("rf-icon");
            tabChevron.AddToClassList("rf-rb-tab__chevron");
            tab.Add(tabIcon);
            tab.Add(tabText);
            tab.Add(tabChevron);
            tab.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => SetCollapsed(!collapsed));

            drawer.Add(bar);
            drawer.Add(tab);
            drawer.RegisterCallback<PointerEnterEvent>(_ => hovered = true);
            drawer.RegisterCallback<PointerLeaveEvent>(_ => { hovered = false; hideAt = Time.unscaledTime + HideAfter; });
            bar.RegisterCallback<GeometryChangedEvent>(_ => Slide());
            wrap.Add(drawer);
            layer.Add(wrap);
            collapsed = false;
            hideAt = Time.unscaledTime + HideAfter + 3f;

            // the "Who plays?" window, in the quest window's look
            window = new VisualElement { name = "ravebridge-window" };
            window.AddToClassList("rf-squest");
            window.AddToClassList("rf-rb-window");
            var header = new VisualElement();
            header.AddToClassList("rf-squest__header");
            windowTitle = new Label();
            windowTitle.AddToClassList("rf-squest__title");
            var close = new Button(CloseWindow) { text = "" };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-squest__close");
            header.Add(windowTitle);
            header.Add(close);
            content = new VisualElement();
            content.AddToClassList("rf-rb-window__content");
            hint = new Label();
            hint.AddToClassList("rf-squest__hint");
            window.Add(header);
            window.Add(content);
            window.Add(hint);
            window.style.display = DisplayStyle.None;
            layer.Add(window);

            bound = true;
            Redraw();
        }

        /// <summary>The drawer up (only the tab showing) or down.</summary>
        private void SetCollapsed(bool value)
        {
            collapsed = value;
            if (!collapsed) hideAt = Time.unscaledTime + HideAfter;
            Slide();
            Redraw();
        }

        private void Slide()
        {
            if (drawer == null || bar == null) return;
            var height = bar.resolvedStyle.height;
            if (float.IsNaN(height)) height = 0f;
            drawer.style.translate = new Translate(0, collapsed ? -(height + 8f) : 0f);
            if (tabChevron != null) tabChevron.text = collapsed ? "\uf078" : "\uf077";
        }

        private static Button BarButton(string label, bool primary, System.Action clicked)
        {
            var button = new Button(clicked) { text = label };
            button.AddToClassList("rf-rb-button");
            button.EnableInClassList("rf-rb-button--primary", primary);
            return button;
        }

        private static Button Chip(string label, bool primary, System.Action clicked)
        {
            var button = new Button(clicked) { text = label };
            button.AddToClassList("rf-squest-chip");
            button.EnableInClassList("rf-squest-chip--active", primary);
            return button;
        }

        private static Label Text(string value, string className)
        {
            var label = new Label(value);
            label.AddToClassList(className);
            return label;
        }

        // ---- The window -------------------------------------------------------------------------------------

        private bool WindowOpen => window != null && window.style.display == DisplayStyle.Flex;

        /// <summary>Play: who plays (the window), or Stop while playing.</summary>
        private void PlayPressed()
        {
            if (RavebridgePlay.Playing) { RavebridgePlay.Stop(); Redraw(); return; }
            if (WindowOpen) { CloseWindow(); return; }
            creating = false;
            window.style.display = DisplayStyle.Flex;
            DrawWindow();
        }

        private void CloseWindow()
        {
            if (window != null) window.style.display = DisplayStyle.None;
            creating = false;
        }

        private void DrawWindow()
        {
            if (!bound) return;
            content.Clear();
            if (creating) { DrawCreator(); return; }

            windowTitle.text = Localization.UI_RAVEBRIDGE_WHO;
            hint.text = Localization.UI_RAVEBRIDGE_WHO_HINT;
            var rb = RavebridgeManager.Instance;
            var game = GameManager.Instance;

            content.Add(Text(Localization.UI_RAVEBRIDGE_YOURS, "rf-squest__section"));
            var any = false;
            if (game && game.Players != null)
            {
                foreach (var player in game.Players.GetAllPlayers())
                {
                    if (!player || player.IsBot || !RavebridgeManager.IsStreamers(player)) continue;
                    any = true;
                    var p = player;
                    var there = rb && rb.Contains(p);
                    var where = there ? Localization.UI_RAVEBRIDGE_ON_ISLAND : string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_AT, p.Island ? p.Island.Identifier : Localization.UI_RAVEBRIDGE_AT_SEA);
                    var buttons = new VisualElement();
                    buttons.AddToClassList("rf-squest-chips");
                    buttons.Add(Chip(Localization.UI_RAVEBRIDGE_PLAY, true, () => { CloseWindow(); RavebridgePlay.Begin(p); Redraw(); }));
                    if (there) buttons.Add(Chip(Localization.UI_RAVEBRIDGE_SEND_HOME, false, () => { rb.Leave(p); DrawWindow(); }));
                    else buttons.Add(Chip(Localization.UI_RAVEBRIDGE_BRING, false, () => { rb.Join(p); DrawWindow(); }));
                    content.Add(Card(p.Name, string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_CHARACTER_LINE, p.Stats.CombatLevel, p.GetTask(), where), buttons, there));
                }
            }
            if (!any) content.Add(Text(Localization.UI_RAVEBRIDGE_NO_OWN, "rf-squest__empty"));

            content.Add(Text(Localization.UI_RAVEBRIDGE_LOCAL_SECTION, "rf-squest__section"));
            var data = RavebridgeCharacter.Current;
            var row = new VisualElement();
            row.AddToClassList("rf-squest-chips");
            if (data != null)
            {
                var level = new Skills(data.Skills).CombatLevel;
                row.Add(Chip(Localization.UI_RAVEBRIDGE_PLAY, true, () => { CloseWindow(); var p = RavebridgeCharacter.Spawn(); if (p) RavebridgePlay.Begin(p); Redraw(); }));
                row.Add(Chip(Localization.UI_RAVEBRIDGE_EXPORT, false, () =>
                {
                    var path = RavebridgeCharacter.Export();
                    ShowNotice(path != null ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_EXPORTED, System.IO.Path.GetFileName(path)) : null);
                }));
                row.Add(Chip(Localization.UI_RAVEBRIDGE_NEW_CHARACTER_AGAIN, false, () => { creating = true; DrawWindow(); }));
                row.Add(Chip(Localization.UI_RAVEBRIDGE_IMPORT, false, Import));
                content.Add(Card(data.Name, string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_LOCAL_LINE, level, data.Quest?.Points ?? 0, RavebridgeQuests.QuestCount), row, RavebridgeCharacter.Spawned));
            }
            else
            {
                row.Add(Chip(Localization.UI_RAVEBRIDGE_CREATE_ONE, true, () => { creating = true; DrawWindow(); }));
                row.Add(Chip(Localization.UI_RAVEBRIDGE_IMPORT, false, Import));
                content.Add(Card(Localization.UI_RAVEBRIDGE_NEW_CHARACTER, Localization.UI_RAVEBRIDGE_LOCAL_ABOUT, row, false));
            }
        }

        private void Import()
        {
            ShowNotice(RavebridgeCharacter.Import() ? Localization.UI_RAVEBRIDGE_IMPORTED : Localization.UI_RAVEBRIDGE_IMPORT_NONE);
            DrawWindow();
        }

        private static VisualElement Card(string title, string line, VisualElement buttons, bool active)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-squest-card");
            card.EnableInClassList("rf-squest-card--active", active);
            var body = new VisualElement();
            body.AddToClassList("rf-squest-card__body");
            body.Add(Text(title, "rf-squest-card__title"));
            body.Add(Text(line, "rf-squest-card__meta"));
            body.Add(buttons);
            card.Add(body);
            return card;
        }

        /// <summary>A name for the new character; its look is rolled, and New look rolls again while it is played.</summary>
        private void DrawCreator()
        {
            windowTitle.text = Localization.UI_RAVEBRIDGE_NEW_CHARACTER;
            hint.text = Localization.UI_RAVEBRIDGE_LOCAL_ABOUT;
            if (RavebridgeCharacter.Current != null) content.Add(Text(string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_REPLACES, RavebridgeCharacter.Current.Name), "rf-squest__error"));
            creatorName = new TextField(Localization.UI_RAVEBRIDGE_NAME) { maxLength = 20 };
            creatorName.AddToClassList("rf-squest-field");
            content.Add(creatorName);
            var row = new VisualElement();
            row.AddToClassList("rf-squest-chips");
            row.Add(Chip(Localization.UI_RAVEBRIDGE_BACK, false, () => { creating = false; DrawWindow(); }));
            row.Add(Chip(Localization.UI_RAVEBRIDGE_CREATE, true, () =>
            {
                var name = creatorName.value;
                CloseWindow();
                RavebridgeCharacter.Create(name, RavenNest.SDK.BotPlayerGenerator.GenerateRandomSyntyAppearance());
                var p = RavebridgeCharacter.Spawn();
                if (p) RavebridgePlay.Begin(p);
                Redraw();
            }));
            content.Add(row);
            creatorName.schedule.Execute(() => creatorName.Focus());
        }

        // ---- Every frame ------------------------------------------------------------------------------------

        /// <summary>Something found or a quest done, on the bar for a few seconds.</summary>
        private void ShowNotice(string value)
        {
            notice = value;
            noticeUntil = Time.unscaledTime + 6f;
            Redraw();
        }

        private void Update()
        {
            if (bound && (wrap.panel == null || window.panel == null)) bound = false;
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }
            // Esc closes the window, leaves hero mode, then stops playing by hand
            if (Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping)
            {
                if (WindowOpen) { EscapeKey.Claim(); CloseWindow(); }
                else if (RavebridgePlay.Playing)
                {
                    EscapeKey.Claim();
                    if (HeroController.Active) HeroController.End();
                    else RavebridgePlay.Stop();
                    Redraw();
                }
            }
            if (Time.unscaledTime >= nextSecond)
            {
                nextSecond = Time.unscaledTime + 1f;
                Redraw();
            }
            // the drawer goes up by itself once the mouse has left it a while (never with the window open)
            if (!collapsed && !hovered && !WindowOpen && Time.unscaledTime >= hideAt && wrap.style.display == DisplayStyle.Flex) SetCollapsed(true);
        }

        private void Redraw()
        {
            if (!bound) return;
            var rb = RavebridgeManager.Instance;
            var shown = rb && rb.Active && !(VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive);
            wrap.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown) { CloseWindow(); return; }

            var playing = RavebridgePlay.Playing;
            text.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_BAR, rb.PlayerCount, Clock(Time.time - rb.StartedAt));
            var left = rb.SecondsLeft;
            meta.text = playing ? (HeroController.Active ? Localization.UI_HERO_CONTROLS : string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_PLAYING, playing.Name) + " " + Localization.UI_RAVEBRIDGE_CONTROLS)
                : left >= 0f ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_BAR_FORCED, Clock(left))
                : (rb.Forced && !string.IsNullOrEmpty(rb.ForcedReason) ? rb.ForcedReason : string.Empty);
            if (!string.IsNullOrEmpty(notice) && Time.unscaledTime > noticeUntil) notice = null;
            if (!string.IsNullOrEmpty(notice)) meta.text = notice;
            meta.style.display = string.IsNullOrEmpty(meta.text) ? DisplayStyle.None : DisplayStyle.Flex;
            tabText.text = !string.IsNullOrEmpty(notice) && collapsed ? notice : text.text;
            tabText.style.display = collapsed ? DisplayStyle.Flex : DisplayStyle.None;

            stay.style.display = left >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            hero.style.display = playing ? DisplayStyle.Flex : DisplayStyle.None;
            hero.text = HeroController.Active ? Localization.UI_HERO_EXIT : Localization.UI_HERO_ENTER;
            look.style.display = playing && playing.IsLocal && !HeroController.Active ? DisplayStyle.Flex : DisplayStyle.None;
            play.text = playing ? Localization.UI_RAVEBRIDGE_STOP : Localization.UI_RAVEBRIDGE_PLAY;
            play.EnableInClassList("rf-rb-button--primary", !playing);
        }

        private static string Clock(float seconds)
        {
            var s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return s >= 3600 ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", s / 3600, s / 60 % 60, s % 60) : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", s / 60, s % 60);
        }
    }
}
