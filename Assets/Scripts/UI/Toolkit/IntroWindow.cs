using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// A window in the manner of a modern game's tutorial, over a dimmed screen (first made for Ravebridge's introduction,
    /// the user, 2026-11-03): page by page a picture from the game drifting slowly, as a looped clip would move, a title,
    /// a few lines (chat's commands in gold) and the keys. Back and Next, the dots, the arrow keys, Enter and Esc.
    /// RavebridgeIntro and WhatsNew open one with their own pages.
    /// </summary>
    public sealed class IntroWindow
    {
        private const float KenBurns = 12f;     // seconds for one slow drift across a picture
        private const float PageFade = 0.25f;

        public sealed class Page
        {
            public string Picture;
            public Func<string> Title;
            public Func<string> Text;
            public (string Key, Func<string> What)[] Keys = new (string, Func<string>)[0];
        }

        /// <summary>The window that is open, or null.</summary>
        public static IntroWindow Current { get; private set; }

        private readonly string name;
        private readonly string eyebrow;
        private readonly string pictures;
        private readonly Page[] Pages;
        private Action closed;

        public bool IsOpen => root != null && root.panel != null;

        private VisualElement root;
        private VisualElement picture, body, dots, keys;
        private Label counter, title, text;
        private Button back, next;
        private int page;
        private float pageShownAt;
        private IVisualElementScheduledItem ticking;
        private static Texture2D fade;

        /// <summary>
        /// Opens a window of <paramref name="pages"/> (pictures from Resources/<paramref name="pictures"/>), with
        /// <paramref name="eyebrow"/> over every title. One at a time: null when another is open or there is no HUD.
        /// <paramref name="closed"/> runs when it closes.
        /// </summary>
        public static IntroWindow Open(string name, string eyebrow, string pictures, Page[] pages, Action closed = null)
        {
            if (Current != null && Current.IsOpen) return null;
            var host = Host();
            if (host == null || pages == null || pages.Length == 0) return null;
            var window = new IntroWindow(name, eyebrow, pictures, pages, closed);
            window.Build();
            host.Add(window.root);
            window.Show(0);
            window.ticking = window.root.schedule.Execute(window.Tick).Every(0);
            Current = window;
            return window;
        }

        private IntroWindow(string name, string eyebrow, string pictures, Page[] pages, Action closed)
        {
            this.name = name;
            this.eyebrow = eyebrow;
            this.pictures = pictures;
            Pages = pages;
            this.closed = closed;
        }

        public void Close()
        {
            if (root == null) return;
            ticking?.Pause();
            ticking = null;
            root.RemoveFromHierarchy();
            root = null;
            if (Current == this) Current = null;
            var callback = closed;
            closed = null;
            callback?.Invoke();
        }

        /// <summary>The HUD's document (the one the hero HUD lives in), on top of everything in it.</summary>
        public static VisualElement Host()
        {
            foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>())
            {
                var r = doc.rootVisualElement;
                if (r != null && r.Q<VisualElement>("conn-layer") != null) return r;
            }
            return null;
        }

        private void Build()
        {
            root = new VisualElement { name = name };
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
            var over = new Label((eyebrow ?? "").ToUpperInvariant()) { pickingMode = PickingMode.Ignore };
            over.AddToClassList("rf-intro__eyebrow");
            over.AddToClassList("rf-font-b");
            title = new Label { pickingMode = PickingMode.Ignore };
            title.AddToClassList("rf-intro__title");
            title.AddToClassList("rf-font-eb");
            text = new Label { pickingMode = PickingMode.Ignore };
            text.AddToClassList("rf-intro__text");
            keys = new VisualElement { pickingMode = PickingMode.Ignore };
            keys.AddToClassList("rf-intro__keys");
            body.Add(over);
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

        private void Next()
        {
            if (page >= Pages.Length - 1) Close();
            else Show(page + 1);
        }

        private void Show(int index)
        {
            if (root == null) return;
            page = Mathf.Clamp(index, 0, Pages.Length - 1);
            pageShownAt = Time.unscaledTime;
            var p = Pages[page];
            var art = Resources.Load<Texture2D>(pictures + p.Picture);
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

        private void Tick()
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
