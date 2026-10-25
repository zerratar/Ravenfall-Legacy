using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The Ravebridge bar (phase 6b of docs/ravebridge-design.md): while the event runs, a bar of its own a little down
    /// the screen (clear of the top column and the event banners), who is there and how long it has been open, with
    /// the streamer's buttons: Play (by hand, 6c), Hero mode,
    /// Stay (a forced event otherwise closes by itself) and End. Opening it is in the quest window's Village tab and
    /// !ravebridge start.
    /// </summary>
    public class RavebridgeView : MonoBehaviour
    {
        private VisualElement wrap;
        private VisualElement bar;
        private Label text;
        private Label meta;
        private Button stay;
        private Button play;
        private Button end;
        private Button look;
        private Button hero;
        private VisualElement choose;
        private bool bound;
        private float nextSecond;

        /// <summary>More characters to play than the streamer's own in the stream (the Ravebridge character, 6d).</summary>
        public static System.Func<System.Collections.Generic.IEnumerable<(string Label, System.Action Pick)>> MoreChoices;

        private void OnEnable()
        {
            bound = false;
            // the Ravebridge character: play it, make one, export or import it (6d)
            MoreChoices = CharacterChoices;
            RavebridgeQuests.Notice += ShowNotice;
            RavebridgeManager.Changed += Redraw;
            Languages.Changed += Rebuild;
        }

        private void OnDisable()
        {
            RavebridgeManager.Changed -= Redraw;
            RavebridgeQuests.Notice -= ShowNotice;
            Languages.Changed -= Rebuild;
            bound = false;
        }

        private void Rebuild() => bound = false;

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            if (layer == null) return;

            wrap?.RemoveFromHierarchy();
            wrap = new VisualElement { name = "ravebridge", pickingMode = PickingMode.Ignore };
            wrap.AddToClassList("rf-rb");
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
            stay = Chip(Localization.UI_RAVEBRIDGE_STAY, true, () => { var rb = RavebridgeManager.Instance; if (rb) rb.Attended = true; Redraw(); });
            play = Chip(Localization.UI_RAVEBRIDGE_PLAY, true, PlayPressed);
            end = Chip(Localization.UI_RAVEBRIDGE_END, false, () => { var rb = RavebridgeManager.Instance; if (rb) rb.End(); });
            look = Chip(Localization.UI_RAVEBRIDGE_NEW_LOOK, false, () =>
            {
                var p = RavebridgePlay.Playing;
                if (p && p.IsLocal) p.Appearance.SetAppearance(RavenNest.SDK.BotPlayerGenerator.GenerateRandomSyntyAppearance(), () => RavebridgeCharacter.Save(), false);
            });
            hero = Chip(Localization.UI_HERO_ENTER, true, () =>
            {
                if (HeroController.Active) HeroController.End();
                else if (RavebridgePlay.Playing) HeroController.Begin(RavebridgePlay.Playing);
                Redraw();
            });
            buttons.Add(stay);
            buttons.Add(hero);
            buttons.Add(look);
            buttons.Add(play);
            buttons.Add(end);
            bar.Add(icon);
            bar.Add(body);
            bar.Add(buttons);
            wrap.Add(bar);

            choose = new VisualElement { name = "ravebridge-choose" };
            choose.AddToClassList("rf-rb-panel");
            choose.style.display = DisplayStyle.None;
            wrap.Add(choose);
            layer.Add(wrap);
            bound = true;
            Redraw();
        }

        private System.Collections.Generic.IEnumerable<(string Label, System.Action Pick)> CharacterChoices()
        {
            var c = RavebridgeCharacter.Current;
            if (c != null)
            {
                yield return (string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_LOCAL_CHARACTER, c.Name), () => { var p = RavebridgeCharacter.Spawn(); if (p) RavebridgePlay.Begin(p); });
                yield return (Localization.UI_RAVEBRIDGE_EXPORT, () => { var path = RavebridgeCharacter.Export(); notice = path != null ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_EXPORTED, System.IO.Path.GetFileName(path)) : null; });
            }
            yield return (c != null ? Localization.UI_RAVEBRIDGE_NEW_CHARACTER_AGAIN : Localization.UI_RAVEBRIDGE_NEW_CHARACTER, OpenCreator);
            yield return (Localization.UI_RAVEBRIDGE_IMPORT, () => { notice = RavebridgeCharacter.Import() ? Localization.UI_RAVEBRIDGE_IMPORTED : Localization.UI_RAVEBRIDGE_IMPORT_NONE; });
        }

        private string notice;
        private float noticeUntil;

        /// <summary>Something found or a quest done, on the bar for a few seconds.</summary>
        private void ShowNotice(string text)
        {
            notice = text;
            noticeUntil = Time.unscaledTime + 6f;
            Redraw();
        }
        private VisualElement creator;
        private TextField creatorName;

        /// <summary>A name for the new character; the look is random and can be rolled again while playing it.</summary>
        private void OpenCreator()
        {
            creator?.RemoveFromHierarchy();
            creator = new VisualElement();
            creator.AddToClassList("rf-rb-panel");
            creatorName = new TextField(Localization.UI_RAVEBRIDGE_NAME) { maxLength = 20 };
            creatorName.AddToClassList("rf-rb-name");
            creator.Add(creatorName);
            creator.Add(Chip(Localization.UI_RAVEBRIDGE_CREATE, true, () =>
            {
                var name = creatorName.value;
                creator.RemoveFromHierarchy();
                creator = null;
                RavebridgeCharacter.Create(name, RavenNest.SDK.BotPlayerGenerator.GenerateRandomSyntyAppearance());
                var p = RavebridgeCharacter.Spawn();
                if (p) RavebridgePlay.Begin(p);
                Redraw();
            }));
            wrap.Add(creator);
        }

        /// <summary>Play: who to play (the streamer's own characters here, the Ravebridge character), or Stop.</summary>
        private void PlayPressed()
        {
            if (RavebridgePlay.Playing) { RavebridgePlay.Stop(); Redraw(); return; }
            var choices = new System.Collections.Generic.List<(string Label, System.Action Pick)>();
            var game = GameManager.Instance;
            if (game && game.Players != null)
            {
                foreach (var player in game.Players.GetAllPlayers())
                {
                    var p = player;
                    if (p && !p.IsBot && RavebridgeManager.IsStreamers(p)) choices.Add((p.Name, () => RavebridgePlay.Begin(p)));
                }
            }
            if (MoreChoices != null) choices.AddRange(MoreChoices());
            if (choices.Count == 1) { choices[0].Pick(); Redraw(); return; }

            choose.Clear();
            if (choices.Count == 0)
            {
                var none = new Label(Localization.UI_RAVEBRIDGE_NO_CHARACTER);
                none.AddToClassList("rf-rb-panel__text");
                choose.Add(none);
            }
            foreach (var choice in choices)
            {
                var c = choice;
                choose.Add(Chip(c.Label, false, () => { choose.style.display = DisplayStyle.None; c.Pick(); Redraw(); }));
            }
            choose.style.display = choose.style.display == DisplayStyle.Flex ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static Button Chip(string label, bool primary, System.Action clicked)
        {
            var button = new Button(clicked) { text = label };
            button.AddToClassList("rf-rb-button");
            button.EnableInClassList("rf-rb-button--primary", primary);
            return button;
        }

        private void Update()
        {
            if (bound && wrap.panel == null) bound = false;
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }
            // Esc leaves hero mode, and again stops playing by hand
            if (RavebridgePlay.Playing && Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping)
            {
                EscapeKey.Claim();
                if (HeroController.Active) HeroController.End();
                else RavebridgePlay.Stop();
                Redraw();
            }
            if (Time.unscaledTime >= nextSecond)
            {
                nextSecond = Time.unscaledTime + 1f;
                Redraw();
            }
        }

        private void Redraw()
        {
            if (!bound) return;
            var rb = RavebridgeManager.Instance;
            var shown = rb && rb.Active && !(VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive);
            wrap.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown) { choose.style.display = DisplayStyle.None; return; }

            text.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_BAR, rb.PlayerCount, Clock(Time.time - rb.StartedAt));
            var left = rb.SecondsLeft;
            meta.text = RavebridgePlay.Playing ? Localization.UI_RAVEBRIDGE_CONTROLS
                : left >= 0f ? string.Format(CultureInfo.InvariantCulture, Localization.UI_RAVEBRIDGE_BAR_FORCED, Clock(left))
                : (rb.Forced && !string.IsNullOrEmpty(rb.ForcedReason) ? rb.ForcedReason : string.Empty);
            meta.style.display = string.IsNullOrEmpty(meta.text) ? DisplayStyle.None : DisplayStyle.Flex;
            stay.style.display = left >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            play.text = RavebridgePlay.Playing ? Localization.UI_RAVEBRIDGE_STOP : Localization.UI_RAVEBRIDGE_PLAY;
            look.style.display = RavebridgePlay.Playing && RavebridgePlay.Playing.IsLocal && !HeroController.Active ? DisplayStyle.Flex : DisplayStyle.None;
            hero.style.display = RavebridgePlay.Playing ? DisplayStyle.Flex : DisplayStyle.None;
            hero.text = HeroController.Active ? Localization.UI_HERO_EXIT : Localization.UI_HERO_ENTER;
            if (HeroController.Active && string.IsNullOrEmpty(notice)) meta.text = Localization.UI_HERO_CONTROLS;
            if (!string.IsNullOrEmpty(notice) && noticeUntil > 0f && Time.unscaledTime > noticeUntil) notice = null;
            if (!string.IsNullOrEmpty(notice)) { meta.text = notice; meta.style.display = DisplayStyle.Flex; }
        }

        private static string Clock(float seconds)
        {
            var s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return s >= 3600 ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", s / 3600, s / 60 % 60, s % 60) : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", s / 60, s % 60);
        }
    }
}
