using System.Globalization;
using System.Linq;
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
    /// for a while after play starts and while the keyboard button is on. The "Who plays?" window opens with the event
    /// (the user, 2026-11-03: one window instead of a banner and then the window): at the top, for chat, how it opened
    /// and how to join; under it a card for each character the streamer can play, with its portrait (their own
    /// characters in the stream, and the Ravebridge character with Export, a new one and Import), and the step that
    /// makes one, with Back. Play opens it again later. Esc closes it.
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
        private VisualElement viewers;
        private Label viewersLine, viewersJoin;
        private VisualElement content;
        private Label hint;
        // the cards' portraits, rendered each time the window is drawn and let go when it is drawn again
        private readonly System.Collections.Generic.List<Texture2D> portraits = new System.Collections.Generic.List<Texture2D>();
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
            // Ravebridge just opened: who plays is asked at once, in the window that also tells chat how to join (no
            // banner first); not when the streamer is being played anyway or is away (HeroStart, Ask false)
            var rb = RavebridgeManager.Instance;
            var active = rb && rb.Active;
            if (active && !wasActive && !RavebridgePlay.Playing && !WindowOpen && rb.Ask) askAt = Time.unscaledTime + 0.2f;
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

            // the "Who plays?" window: the opening for chat at the top, the streamer's cards under it
            window = new VisualElement { name = "ravebridge-window" };
            window.AddToClassList("rf-squest");
            window.AddToClassList("rf-rb-window");
            var header = new VisualElement();
            header.AddToClassList("rf-rbw__header");
            var castle = new Label("\uf6d9") { pickingMode = PickingMode.Ignore };
            castle.AddToClassList("rf-icon");
            castle.AddToClassList("rf-rbw__castle");
            windowTitle = new Label();
            windowTitle.AddToClassList("rf-rbw__title");
            windowTitle.AddToClassList("rf-font-eb");
            var close = new Button(CloseWindow) { text = "\uf00d" };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-rbw__close");
            header.Add(castle);
            header.Add(windowTitle);
            header.Add(close);
            viewers = new VisualElement();
            viewers.AddToClassList("rf-rbw__viewers");
            viewersLine = new Label { enableRichText = true };
            viewersLine.AddToClassList("rf-rbw__viewers-line");
            viewersJoin = new Label { enableRichText = true };
            viewersJoin.AddToClassList("rf-rbw__viewers-join");
            viewersJoin.AddToClassList("rf-font-b");
            viewers.Add(viewersLine);
            viewers.Add(viewersJoin);
            content = new VisualElement();
            content.AddToClassList("rf-rb-window__content");
            var foot = new VisualElement();
            foot.AddToClassList("rf-rbw__foot");
            var footIcon = new Label("\uf05a") { pickingMode = PickingMode.Ignore };
            footIcon.AddToClassList("rf-icon");
            footIcon.AddToClassList("rf-rbw__foot-icon");
            hint = new Label();
            hint.AddToClassList("rf-rbw__hint");
            foot.Add(footIcon);
            foot.Add(hint);
            window.Add(header);
            window.Add(viewers);
            window.Add(content);
            window.Add(foot);
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
            ReleasePortraits();
        }

        private void ReleasePortraits()
        {
            foreach (var t in portraits) if (t) Destroy(t);
            portraits.Clear();
        }

        private void DrawWindow()
        {
            if (!bound) return;
            content.Clear();
            ReleasePortraits();
            var rb = RavebridgeManager.Instance;
            var game = GameManager.Instance;

            // for chat: how it opened and how to join (the command lit up), and who is there already
            windowTitle.text = Localization.UI_RAVEBRIDGE_OPEN;
            var opening = rb ? rb.OpeningLine : null;
            var says = !string.IsNullOrEmpty(opening) && opening.Contains("{command}");
            viewersLine.text = string.IsNullOrEmpty(opening) ? string.Empty : NotificationScreenView.WithCommand(opening, "!ravebridge");
            viewersLine.style.display = string.IsNullOrEmpty(opening) ? DisplayStyle.None : DisplayStyle.Flex;
            var join = says ? string.Empty : NotificationScreenView.WithCommand(Localization.UI_RAVEBRIDGE_JOIN, "!ravebridge");
            if (rb && rb.PlayerCount > 0) join = (join.Length > 0 ? join + "   " : string.Empty) + string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_THERE_NOW, rb.PlayerCount);
            viewersJoin.text = join;
            viewersJoin.style.display = join.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            if (creating) { DrawCreator(); return; }
            hint.text = Localization.UI_RAVEBRIDGE_WHO_HINT;
            content.Add(Text(Localization.UI_RAVEBRIDGE_WHO, "rf-rbw__section"));
            var cards = new VisualElement();
            cards.AddToClassList("rf-rbw__cards");
            content.Add(cards);

            var any = false;
            if (game && game.Players != null)
            {
                foreach (var player in game.Players.GetAllPlayers())
                {
                    if (!player || player.IsBot || !RavebridgeManager.IsStreamers(player)) continue;
                    any = true;
                    var p = player;
                    var where = rb && rb.Contains(p) ? Localization.UI_RAVEBRIDGE_ON_ISLAND : string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_AT, p.Island ? p.Island.Identifier : Localization.UI_RAVEBRIDGE_AT_SEA);
                    var buttons = new VisualElement();
                    buttons.Add(CardButton(Localization.UI_RAVEBRIDGE_PLAY_HERO, true, () => { CloseWindow(); RavebridgePlay.Begin(p); Redraw(); }));
                    cards.Add(PlayCard(Portrait(p), false, p.Name, Localization.UI_RAVEBRIDGE_YOUR_CHARACTER,
                        string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_CARD_LEVEL, p.Stats.CombatLevel),
                        string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_CARD_DOING, p.GetTask(), where), buttons));
                }
            }

            var data = RavebridgeCharacter.Current;
            var row = new VisualElement();
            if (data != null)
            {
                var level = new Skills(data.Skills).CombatLevel;
                row.Add(CardButton(Localization.UI_RAVEBRIDGE_PLAY_HERO, true, () => { CloseWindow(); var p = RavebridgeCharacter.Spawn(); if (p) RavebridgePlay.Begin(p); Redraw(); }));
                var small = new VisualElement();
                small.AddToClassList("rf-rbw-card__small");
                small.Add(CardButton(Localization.UI_RAVEBRIDGE_NEW_CHARACTER_AGAIN, false, () => { creating = true; DrawWindow(); }));
                small.Add(CardButton(Localization.UI_RAVEBRIDGE_EXPORT, false, () =>
                {
                    var path = RavebridgeCharacter.Export();
                    ShowNotice(path != null ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_EXPORTED, System.IO.Path.GetFileName(path)) : null);
                }));
                small.Add(CardButton(Localization.UI_RAVEBRIDGE_IMPORT, false, Import));
                row.Add(small);
                var spawned = RavebridgeCharacter.Spawned && game ? game.Players.GetAllPlayers().FirstOrDefault(x => x && x.IsLocal) : null;
                cards.Add(PlayCard(spawned ? Portrait(spawned) : null, false, data.Name, Localization.UI_RAVEBRIDGE_LOCAL_SECTION,
                    string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_CARD_LEVEL, level),
                    string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_CARD_QUESTS, data.Quest?.Points ?? 0, RavebridgeQuests.QuestCount), row));
            }
            else
            {
                row.Add(CardButton(Localization.UI_RAVEBRIDGE_CREATE_ONE, true, () => { creating = true; DrawWindow(); }));
                row.Add(CardButton(Localization.UI_RAVEBRIDGE_IMPORT, false, Import));
                cards.Add(PlayCard(null, true, Localization.UI_RAVEBRIDGE_LOCAL_SECTION, null, Localization.UI_RAVEBRIDGE_LOCAL_SHORT, null, row));
            }
            if (!any) cards.Insert(0, PlayCard(null, false, Localization.UI_RAVEBRIDGE_YOUR_CHARACTER, null, Localization.UI_RAVEBRIDGE_NO_OWN, null, new VisualElement()));
        }

        private Texture2D Portrait(PlayerController p)
        {
            var face = NpcPortrait.Of(p);
            if (face) portraits.Add(face);
            return face;
        }

        /// <summary>
        /// A character to play: the portrait (or a silhouette, with a plus for one still to make), the name, what it is,
        /// two short lines and its buttons.
        /// </summary>
        private static VisualElement PlayCard(Texture2D portrait, bool toMake, string name, string kind, string line1, string line2, VisualElement buttons)
        {
            var card = new VisualElement();
            card.AddToClassList("rf-rbw-card");
            var picture = new VisualElement { pickingMode = PickingMode.Ignore };
            picture.AddToClassList("rf-rbw-card__portrait");
            if (portrait) picture.style.backgroundImage = new StyleBackground(portrait);
            else
            {
                var shape = new Label("\uf007") { pickingMode = PickingMode.Ignore };
                shape.AddToClassList("rf-icon");
                shape.AddToClassList("rf-rbw-card__silhouette");
                picture.Add(shape);
                if (toMake)
                {
                    var plus = new Label("\uf067") { pickingMode = PickingMode.Ignore };
                    plus.AddToClassList("rf-icon");
                    plus.AddToClassList("rf-rbw-card__plus");
                    picture.Add(plus);
                }
            }
            card.Add(picture);
            var body = new VisualElement();
            body.AddToClassList("rf-rbw-card__body");
            var title = Text(name, "rf-rbw-card__name");
            title.AddToClassList("rf-font-eb");
            body.Add(title);
            if (!string.IsNullOrEmpty(kind)) body.Add(Text(kind, "rf-rbw-card__kind"));
            var rule = new VisualElement { pickingMode = PickingMode.Ignore };
            rule.AddToClassList("rf-rbw-card__rule");
            body.Add(rule);
            if (!string.IsNullOrEmpty(line1)) body.Add(Text(line1, "rf-rbw-card__line"));
            if (!string.IsNullOrEmpty(line2)) body.Add(Text(line2, "rf-rbw-card__line"));
            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.style.flexGrow = 1;
            body.Add(spacer);
            buttons.AddToClassList("rf-rbw-card__buttons");
            body.Add(buttons);
            card.Add(body);
            return card;
        }

        private static Button CardButton(string label, bool primary, System.Action clicked)
        {
            var button = new Button(clicked) { text = label };
            button.AddToClassList("rf-rbw-btn");
            button.AddToClassList("rf-font-b");
            button.EnableInClassList("rf-rbw-btn--primary", primary);
            return button;
        }

        private void Import()
        {
            ShowNotice(RavebridgeCharacter.Import() ? Localization.UI_RAVEBRIDGE_IMPORTED : Localization.UI_RAVEBRIDGE_IMPORT_NONE);
            DrawWindow();
        }

        /// <summary>A name for the new character; its look is rolled, and New look rolls again while it is played.</summary>
        private void DrawCreator()
        {
            content.Add(Text(Localization.UI_RAVEBRIDGE_NEW_CHARACTER, "rf-rbw__section"));
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
