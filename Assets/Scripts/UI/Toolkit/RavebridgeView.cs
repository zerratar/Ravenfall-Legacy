using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Ravebridge on screen (docs/ravebridge-design.md): while the event runs, a panel in the bottom left corner (the
    /// players list has the top left; the hero's orbs sit by the skill bar) with who is played, how long the event
    /// has been open and who is there, and the streamer's big buttons: Play (who plays) or, while playing, Leave
    /// Ravebridge, and End Ravebridge; Stay for a forced event that would close by itself and New look for the
    /// Ravebridge character. The streamer is in Ravebridge as the hero or not there at all (the user, 2026-11-03). It
    /// shows in full when the event opens and when play starts, then folds into a slim strip (the time and Play or
    /// Leave) once the character walks off or the mouse has been away from it a few seconds, so it never sits over the
    /// game (playtest 13); a click on the strip opens it again. The hero's keys show bottom centre, over the skill bar,
    /// for a while after play starts and while the keyboard button is on. Play opens the "Who plays?" window, in the
    /// quest window's look: the streamer's own characters in the stream (where each is, with Play) and the Ravebridge
    /// character (Play, Export, a new one, Import), and the step that makes one, with Back. Esc closes the window.
    /// </summary>
    public class RavebridgeView : MonoBehaviour
    {
        private VisualElement wrap;
        private VisualElement panel;
        private VisualElement body;
        private Label title, info, playingLabel, meta;
        private Button stripAction, primary, end, stay, look, help, chevron;
        private VisualElement extras;
        private VisualElement keys;
        private bool collapsed;
        private bool hovered;
        private float hideAt;
        private const float HideAfter = 6f;

        // the keys show for a while after play starts (or hero mode does), and while the keyboard button is on
        private const float KeysFor = 20f;
        private float keysUntil;
        private bool keysPinned;
        private string keysShown;

        // the character walking off folds the panel
        private PlayerController watched;
        private Vector3 watchedFrom;
        private bool wasPlaying, wasHero;

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

        private bool wasActive;
        private float askAt; // when to ask who plays, once the opening announcement is gone

        private void Changed()
        {
            // Ravebridge just opened: who plays is asked first, nobody is sent along unasked; but after the opening
            // announcement (RavebridgeManager.BannerSeconds), which the window covered when both came at once
            var rb = RavebridgeManager.Instance;
            var active = rb && rb.Active;
            if (active && !wasActive && !RavebridgePlay.Playing && !WindowOpen) askAt = Time.unscaledTime + RavebridgeManager.BannerSeconds;
            if (!active) askAt = 0f;
            wasActive = active;
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
            wrap.AddToClassList("rf-rbp-wrap");
            panel = new VisualElement { name = "ravebridge-panel" };
            panel.AddToClassList("rf-rbp");

            // the head: always there; folded, the strip (the time, Hero mode or Play, the chevron)
            var head = new VisualElement();
            head.AddToClassList("rf-rbp__head");
            var icon = new Label("\uf6d9") { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-rbp__icon");
            title = new Label(Localization.UI_RAVEBRIDGE_TITLE) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("rf-rbp__title");
            info = new Label { pickingMode = PickingMode.Ignore };
            info.AddToClassList("rf-rbp__strip-info");
            stripAction = Big(string.Empty, "\uf71d", "rf-rbp__strip-action", PrimaryPressed);
            help = IconButton("\uf11c", () => { keysPinned = !keysPinned; if (!keysPinned) keysUntil = 0f; Redraw(); });
            help.tooltip = Localization.UI_RAVEBRIDGE_KEYS_TIP;
            chevron = IconButton("\uf077", () => SetCollapsed(!collapsed));
            head.Add(icon);
            head.Add(title);
            head.Add(info);
            head.Add(stripAction);
            head.Add(help);
            head.Add(chevron);
            // a click on the folded strip (not on its buttons) opens it
            head.RegisterCallback<UnityEngine.UIElements.ClickEvent>(e => { if (collapsed && !(e.target is Button)) SetCollapsed(false); });

            // the rest: who is played, the event, the big buttons
            body = new VisualElement();
            body.AddToClassList("rf-rbp__body");
            playingLabel = new Label { pickingMode = PickingMode.Ignore };
            playingLabel.AddToClassList("rf-rbp__playing");
            meta = new Label { pickingMode = PickingMode.Ignore };
            meta.AddToClassList("rf-rbp__meta");
            primary = Big(string.Empty, "\uf71d", "rf-rbp__big--primary", PrimaryPressed);
            end = Big(Localization.UI_RAVEBRIDGE_END_EVENT, "\uf52b", "rf-rbp__big--danger", () => { CloseWindow(); var rb = RavebridgeManager.Instance; if (rb) rb.End(); });
            extras = new VisualElement();
            extras.AddToClassList("rf-rbp__extras");
            stay = Small(Localization.UI_RAVEBRIDGE_STAY, () => { var rb = RavebridgeManager.Instance; if (rb) rb.Attended = true; Redraw(); });
            look = Small(Localization.UI_RAVEBRIDGE_NEW_LOOK, () =>
            {
                var p = RavebridgePlay.Playing;
                if (p && p.IsLocal) p.Appearance.SetAppearance(RavenNest.SDK.BotPlayerGenerator.GenerateRandomSyntyAppearance(), () => RavebridgeCharacter.Save(), false);
            });
            extras.Add(stay);
            extras.Add(look);
            body.Add(playingLabel);
            body.Add(meta);
            body.Add(primary);
            body.Add(end);
            body.Add(extras);

            panel.Add(head);
            panel.Add(body);
            panel.RegisterCallback<PointerEnterEvent>(_ => hovered = true);
            panel.RegisterCallback<PointerLeaveEvent>(_ => { hovered = false; hideAt = Time.unscaledTime + HideAfter; });

            keys = new VisualElement { pickingMode = PickingMode.Ignore };
            keys.AddToClassList("rf-rbp-keys");
            keysShown = null;

            wrap.Add(panel);
            layer.Add(wrap);
            layer.Add(keys);
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

        /// <summary>Folded into the strip, or open in full.</summary>
        private void SetCollapsed(bool value)
        {
            collapsed = value;
            if (!collapsed) hideAt = Time.unscaledTime + HideAfter;
            Redraw();
        }

        /// <summary>The big button: Play while nobody is played, else Leave Ravebridge.</summary>
        private void PrimaryPressed()
        {
            if (!RavebridgePlay.Playing) { PlayPressed(); return; }
            LeaveRavebridge();
            Redraw();
        }

        private static Button Big(string label, string glyph, string className, System.Action clicked)
        {
            var button = new Button(clicked);
            button.AddToClassList("rf-rbp__big");
            button.AddToClassList(className);
            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-rbp__big-icon");
            var text = new Label(label) { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-rbp__big-text");
            button.Add(icon);
            button.Add(text);
            return button;
        }

        private static void SetBig(Button button, string label, string glyph)
        {
            var labels = button.Query<Label>().ToList();
            if (labels.Count < 2) return;
            labels[0].text = glyph;
            labels[1].text = label;
        }

        private static Button Small(string label, System.Action clicked)
        {
            var button = new Button(clicked) { text = label };
            button.AddToClassList("rf-rbp__small");
            return button;
        }

        private static Button IconButton(string glyph, System.Action clicked)
        {
            var button = new Button(clicked) { text = glyph };
            button.AddToClassList("rf-icon");
            button.AddToClassList("rf-rbp__icon-button");
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

        /// <summary>Play: who plays (the window), or Leave while playing.</summary>
        private void PlayPressed()
        {
            if (RavebridgePlay.Playing) { LeaveRavebridge(); Redraw(); return; }
            if (WindowOpen) { CloseWindow(); return; }
            OpenWindow();
        }

        private void OpenWindow()
        {
            askAt = 0f;
            creating = false;
            window.style.display = DisplayStyle.Flex;
            // the opening announcement gives way: it sat over the window for its last seconds
            var banner = FindAnyObjectByType<NotificationScreenView>();
            if (banner) banner.Hide();
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
            if (askAt > 0f && Time.unscaledTime >= askAt)
            {
                askAt = 0f;
                var rb = RavebridgeManager.Instance;
                if (rb && rb.Active && !RavebridgePlay.Playing && !WindowOpen) OpenWindow();
            }
            // Esc closes the window (it no longer leaves hero mode or stops playing: the game menu opens)
            if (Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping && WindowOpen) { EscapeKey.Claim(); CloseWindow(); }
            if (Time.unscaledTime >= nextSecond)
            {
                nextSecond = Time.unscaledTime + 1f;
                // someone is being played already (picked here, by chat or by hero mode): "Who plays?" has been answered
                if (WindowOpen && !creating && RavebridgePlay.Playing) CloseWindow();
                Redraw();
            }
            Watch();
            // the panel folds by itself once the mouse has left it a while (never with the window open)
            if (!collapsed && !hovered && !WindowOpen && Time.unscaledTime >= hideAt && wrap.style.display == DisplayStyle.Flex) SetCollapsed(true);
        }

        /// <summary>
        /// Leaves Ravebridge (Esc used to leave hero mode; it opens the game menu now, the user found it ended play by
        /// surprise). During a wave run it asks first: leaving gives no rewards, so the first press warns and a second
        /// within <see cref="WaveRun.LeaveAskSeconds"/> leaves; between waves it is a retreat that keeps the chest and
        /// the record, mid wave the run is abandoned.
        /// </summary>
        private void LeaveRavebridge()
        {
            var run = WaveRun.Current;
            if (run && !run.Finished && !run.LeaveAsked)
            {
                run.AskToLeave();
                return;
            }
            if (run && !run.Finished) run.Leave();
            RavebridgePlay.Stop();
        }

        /// <summary>
        /// Play starting (or hero mode) opens the panel and shows the keys a while; the character walking a few steps
        /// from where that happened folds it again, they are on their way.
        /// </summary>
        private void Watch()
        {
            var playing = RavebridgePlay.Playing;
            var hero = HeroController.Active;
            if ((playing && !wasPlaying) || (hero != wasHero && playing))
            {
                keysUntil = Time.unscaledTime + KeysFor;
                watched = playing;
                watchedFrom = playing.Position;
                if (collapsed) SetCollapsed(false);
                else hideAt = Time.unscaledTime + HideAfter;
                Redraw();
            }
            wasPlaying = playing;
            wasHero = hero;
            if (watched && playing == watched && !collapsed && !hovered && !WindowOpen && (watched.Position - watchedFrom).sqrMagnitude > 4f * 4f)
            {
                watched = null;
                SetCollapsed(true);
            }
            if (keysUntil > 0f && Time.unscaledTime >= keysUntil) { keysUntil = 0f; Redraw(); }
        }

        /// <summary>The key chips, from "WASD=walk|E=talk": the key and what it does.</summary>
        private void DrawKeys(string list)
        {
            if (list == keysShown) return;
            keysShown = list;
            keys.Clear();
            if (string.IsNullOrEmpty(list)) return;
            foreach (var pair in list.Split('|'))
            {
                var at = pair.IndexOf('=');
                if (at <= 0) continue;
                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("rf-rbp-key");
                var key = new Label(pair.Substring(0, at)) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("rf-rbp-key__key");
                var what = new Label(pair.Substring(at + 1)) { pickingMode = PickingMode.Ignore };
                what.AddToClassList("rf-rbp-key__text");
                chip.Add(key);
                chip.Add(what);
                keys.Add(chip);
            }
        }

        private void Redraw()
        {
            if (!bound) return;
            var rb = RavebridgeManager.Instance;
            var shown = rb && rb.Active && !(VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive);
            wrap.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown) { CloseWindow(); keys.style.display = DisplayStyle.None; return; }

            var playing = RavebridgePlay.Playing;
            var hero = HeroController.Active;
            var clock = Clock(Time.time - rb.StartedAt);
            var left = rb.SecondsLeft;
            if (!string.IsNullOrEmpty(notice) && Time.unscaledTime > noticeUntil) notice = null;

            panel.EnableInClassList("rf-rbp--collapsed", collapsed);
            chevron.text = collapsed ? "\uf078" : "\uf077";
            body.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;

            // folded: the time (or a quest's notice) and the one button that matters
            info.text = collapsed && !string.IsNullOrEmpty(notice) ? notice : clock;
            info.style.display = collapsed ? DisplayStyle.Flex : DisplayStyle.None;
            var action = !playing ? Localization.UI_RAVEBRIDGE_PLAY : Localization.UI_RAVEBRIDGE_LEAVE;
            var glyph = !playing ? "\uf04b" : "\uf2f5";
            SetBig(stripAction, action, glyph);
            stripAction.style.display = collapsed ? DisplayStyle.Flex : DisplayStyle.None;

            playingLabel.text = playing ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_PLAYING_NAME, playing.Name) : Localization.UI_RAVEBRIDGE_NOBODY;
            var line = string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_INFO, clock, rb.PlayerCount);
            if (left >= 0f) line += "\n" + string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_BAR_FORCED, Clock(left));
            else if (rb.Forced && !string.IsNullOrEmpty(rb.ForcedReason)) line += "\n" + rb.ForcedReason;
            if (!string.IsNullOrEmpty(notice)) line += "\n" + notice;
            meta.text = line;

            SetBig(primary, !playing ? Localization.UI_RAVEBRIDGE_PLAY_WHO : Localization.UI_RAVEBRIDGE_LEAVE, glyph);
            stay.style.display = left >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            look.style.display = playing && playing.IsLocal ? DisplayStyle.Flex : DisplayStyle.None;
            extras.style.display = left >= 0f || (playing && playing.IsLocal) ? DisplayStyle.Flex : DisplayStyle.None;

            // the keys while playing: for a while after it starts, or while the keyboard button is on
            var showKeys = playing && (keysPinned || keysUntil > 0f);
            help.style.display = playing ? DisplayStyle.Flex : DisplayStyle.None;
            help.EnableInClassList("rf-rbp__icon-button--on", showKeys);
            DrawKeys(showKeys ? Localization.UI_HERO_KEYS : null);
            keys.style.display = showKeys ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static string Clock(float seconds)
        {
            var s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return s >= 3600 ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", s / 3600, s / 60 % 60, s % 60) : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", s / 60, s % 60);
        }
    }
}
