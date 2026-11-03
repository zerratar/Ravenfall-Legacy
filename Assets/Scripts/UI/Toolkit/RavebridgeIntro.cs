using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Ravebridge's introduction (the user, 2026-11-03): the first time the streamer plays on Ravebridge, a window in the
    /// manner of a modern game's tutorial, over a dimmed screen: page by page a picture from the game (drifting slowly,
    /// as a looped clip would move), a title, a few lines and the keys, how hero play works (moving and fighting, the
    /// skills, the people and their quests, the map and the gear, chat fighting along). Back and Next, the dots, the
    /// arrow keys, Enter and Esc. Once per streamer on this PC; the Ravebridge panel's ? opens it any time, and the
    /// settings (Interface, "Introductions") show it, and the rising from the sea, again.
    /// While it is open the hero does not move or fight (HeroController asks <see cref="IsOpen"/>).
    /// </summary>
    public static class RavebridgeIntro
    {
        private const float KenBurns = 12f;     // seconds for one slow drift across a picture
        private const float PageFade = 0.25f;

        private sealed class Page
        {
            public string Picture;
            public Func<string> Title;
            public Func<string> Text;
            public (string Key, Func<string> What)[] Keys;
        }

        private static readonly Page[] Pages =
        {
            new Page { Picture = "intro-1", Title = () => Localization.UI_RB_INTRO_1_TITLE, Text = () => Localization.UI_RB_INTRO_1_TEXT, Keys = new (string, Func<string>)[0] },
            new Page { Picture = "intro-2", Title = () => Localization.UI_RB_INTRO_2_TITLE, Text = () => Localization.UI_RB_INTRO_2_TEXT,
                Keys = new (string, Func<string>)[] { ("WASD", () => Localization.UI_RB_INTRO_KEY_WALK), ("Click", () => Localization.UI_RB_INTRO_KEY_ATTACK), ("Space", () => Localization.UI_RB_INTRO_KEY_DODGE), ("V", () => Localization.UI_RB_INTRO_KEY_CAMERA) } },
            new Page { Picture = "intro-3", Title = () => Localization.UI_RB_INTRO_3_TITLE, Text = () => Localization.UI_RB_INTRO_3_TEXT,
                Keys = new (string, Func<string>)[] { ("1-4", () => Localization.UI_RB_INTRO_KEY_SKILLS), ("Q", () => Localization.UI_RB_INTRO_KEY_POTION), ("Tab", () => Localization.UI_RB_INTRO_KEY_STYLE) } },
            new Page { Picture = "intro-4", Title = () => Localization.UI_RB_INTRO_4_TITLE, Text = () => Localization.UI_RB_INTRO_4_TEXT,
                Keys = new (string, Func<string>)[] { ("E", () => Localization.UI_RB_INTRO_KEY_TALK) } },
            new Page { Picture = "intro-5", Title = () => Localization.UI_RB_INTRO_5_TITLE, Text = () => Localization.UI_RB_INTRO_5_TEXT,
                Keys = new (string, Func<string>)[] { ("M", () => Localization.UI_RB_INTRO_KEY_MAP), ("I", () => Localization.UI_RB_INTRO_KEY_BAG), ("C", () => Localization.UI_RB_INTRO_KEY_CHARACTER) } },
            new Page { Picture = "intro-6", Title = () => Localization.UI_RB_INTRO_6_TITLE, Text = () => Localization.UI_RB_INTRO_6_TEXT, Keys = new (string, Func<string>)[0] },
        };

        public static bool IsOpen => root != null && root.panel != null;

        private static VisualElement root;
        private static VisualElement picture, body, dots, keys;
        private static Label counter, title, text;
        private static Button back, next;
        private static int page;
        private static float pageShownAt;
        private static IVisualElementScheduledItem ticking;
        private static Texture2D fade;

        /// <summary>The streamer has seen it (kept on this PC, per streamer).</summary>
        private static string SeenKey
        {
            get
            {
                var game = GameManager.Instance;
                return "ravebridge.intro." + (game && game.RavenNest != null ? game.RavenNest.UserId.ToString() : "local");
            }
        }

        /// <summary>
        /// Just started playing (RavebridgePlay): the first time, it opens a moment later, once the hero is in control;
        /// not in the middle of a fight (a storm starts its waves at once), then it waits for another time.
        /// </summary>
        public static void ShowFirstTime()
        {
            if (PlayerPrefs.GetInt(SeenKey, 0) == 1 || IsOpen) return;
            var host = Host();
            host?.schedule.Execute(() =>
            {
                if (!HeroController.Active || WaveRun.Current || IsOpen || PlayerPrefs.GetInt(SeenKey, 0) == 1) return;
                Open();
            }).StartingIn(1200);
        }

        /// <summary>Shows the introduction and the rising from the sea again next time (the settings).</summary>
        public static void ForgetSeen()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
        }

        public static void Open()
        {
            if (IsOpen) return;
            var host = Host();
            if (host == null) return;
            Build();
            host.Add(root);
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            Show(0);
            ticking = root.schedule.Execute(Tick).Every(0);
        }

        public static void Close()
        {
            ticking?.Pause();
            ticking = null;
            root?.RemoveFromHierarchy();
            root = null;
        }

        /// <summary>The HUD's document (the one the hero HUD lives in), on top of everything in it.</summary>
        private static VisualElement Host()
        {
            foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var r = doc.rootVisualElement;
                if (r != null && r.Q<VisualElement>("conn-layer") != null) return r;
            }
            return null;
        }

        private static void Build()
        {
            root = new VisualElement { name = "ravebridge-intro" };
            root.AddToClassList("rf-intro");
            // the dimmed screen takes the clicks: nothing behind it is walked to or hit
            root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            var panel = new VisualElement();
            panel.AddToClassList("rf-intro__panel");

            var media = new VisualElement();
            media.AddToClassList("rf-intro__media");
            picture = new VisualElement { pickingMode = PickingMode.Ignore };
            picture.AddToClassList("rf-intro__picture");
            var shade = new VisualElement { pickingMode = PickingMode.Ignore };
            shade.AddToClassList("rf-intro__shade");
            shade.style.backgroundImage = new StyleBackground(Fade());
            counter = new Label { pickingMode = PickingMode.Ignore };
            counter.AddToClassList("rf-intro__counter");
            counter.AddToClassList("rf-font-b");
            var close = new Button(Close) { text = "" };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-intro__close");
            media.Add(picture);
            media.Add(shade);
            media.Add(counter);
            media.Add(close);

            body = new VisualElement();
            body.AddToClassList("rf-intro__body");
            var eyebrow = new Label(Localization.UI_RB_INTRO_EYEBROW.ToUpperInvariant()) { pickingMode = PickingMode.Ignore };
            eyebrow.AddToClassList("rf-intro__eyebrow");
            eyebrow.AddToClassList("rf-font-b");
            title = new Label { pickingMode = PickingMode.Ignore };
            title.AddToClassList("rf-intro__title");
            title.AddToClassList("rf-font-eb");
            text = new Label { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-intro__text");
            keys = new VisualElement { pickingMode = PickingMode.Ignore };
            keys.AddToClassList("rf-intro__keys");
            body.Add(eyebrow);
            body.Add(title);
            body.Add(text);
            body.Add(keys);

            var foot = new VisualElement();
            foot.AddToClassList("rf-intro__foot");
            dots = new VisualElement();
            dots.AddToClassList("rf-intro__dots");
            for (var i = 0; i < Pages.Length; i++)
            {
                var at = i;
                var dot = new VisualElement();
                dot.AddToClassList("rf-intro__dot");
                dot.RegisterCallback<PointerDownEvent>(e => { Show(at); e.StopPropagation(); });
                dots.Add(dot);
            }
            var buttons = new VisualElement();
            buttons.AddToClassList("rf-intro__buttons");
            back = new Button(() => Show(page - 1)) { text = Localization.UI_RB_INTRO_BACK };
            back.AddToClassList("rf-intro__button");
            back.AddToClassList("rf-font-b");
            next = new Button(Next);
            next.AddToClassList("rf-intro__button");
            next.AddToClassList("rf-intro__button--primary");
            next.AddToClassList("rf-font-eb");
            buttons.Add(back);
            buttons.Add(next);
            foot.Add(dots);
            foot.Add(buttons);

            panel.Add(media);
            panel.Add(body);
            panel.Add(foot);
            root.Add(panel);
        }

        private static void Next()
        {
            if (page >= Pages.Length - 1) Close();
            else Show(page + 1);
        }

        private static void Show(int index)
        {
            if (root == null) return;
            page = Mathf.Clamp(index, 0, Pages.Length - 1);
            pageShownAt = Time.unscaledTime;
            var p = Pages[page];
            var art = Resources.Load<Texture2D>("HeroUI/" + p.Picture);
            picture.style.backgroundImage = art ? new StyleBackground(art) : new StyleBackground();
            counter.text = (page + 1) + " / " + Pages.Length;
            title.text = p.Title();
            // chat's commands in gold, so they stand out
            text.text = System.Text.RegularExpressions.Regex.Replace(p.Text(), @"![a-z]+", m => "<color=#ECBE34>" + m.Value + "</color>");
            keys.Clear();
            foreach (var (key, what) in p.Keys)
            {
                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("rf-intro__key");
                var cap = new Label(key) { pickingMode = PickingMode.Ignore };
                cap.AddToClassList("rf-intro__keycap");
                cap.AddToClassList("rf-font-b");
                var label = new Label(what()) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("rf-intro__keytext");
                chip.Add(cap);
                chip.Add(label);
                keys.Add(chip);
            }
            keys.style.display = p.Keys.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            for (var i = 0; i < dots.childCount; i++) dots[i].EnableInClassList("rf-intro__dot--on", i == page);
            back.style.visibility = page > 0 ? Visibility.Visible : Visibility.Hidden;
            next.text = page >= Pages.Length - 1 ? Localization.UI_RB_INTRO_DONE : Localization.UI_RB_INTRO_NEXT;
            Tick();
        }

        private static void Tick()
        {
            if (root == null) return;
            var t = Time.unscaledTime - pageShownAt;
            // the picture drifts and slowly comes closer, back and forth, as a looped clip would move
            var u = Mathf.PingPong(t / KenBurns, 1f);
            var ease = u * u * (3f - 2f * u);
            picture.style.scale = new Scale(Vector3.one * Mathf.Lerp(1.02f, 1.1f, ease));
            picture.style.translate = new Translate(Length.Percent(Mathf.Lerp(-1.8f, 1.8f, ease)), Length.Percent(Mathf.Lerp(0.8f, -0.8f, ease)));
            var appear = Mathf.Clamp01(t / PageFade);
            picture.style.opacity = appear;
            body.style.opacity = appear;
            body.style.translate = new Translate(0, (1f - appear) * 12f);

            if (Keybinds.IsTyping) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { EscapeKey.Claim(); Close(); }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Next();
            else if (Input.GetKeyDown(KeyCode.LeftArrow) && page > 0) Show(page - 1);
        }

        /// <summary>The picture's bottom edge fades into the panel: clear at the top, the panel's colour at the bottom.</summary>
        private static Texture2D Fade()
        {
            if (fade) return fade;
            const int Height = 64;
            fade = new Texture2D(1, Height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Intro Fade" };
            for (var y = 0; y < Height; y++)
            {
                var a = Mathf.Pow(1f - y / (Height - 1f), 2f);
                fade.SetPixel(0, y, new Color(20f / 255f, 18f / 255f, 15f / 255f, a));
            }
            fade.Apply();
            return fade;
        }
    }
}
