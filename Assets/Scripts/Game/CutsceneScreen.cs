using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The screen while a cutscene plays (RavebridgeReveal, TowerCutscene): everything on it steps aside (the UI Toolkit
/// panels and the old canvases), a Skip button sits in the corner, and a title (a big name and a line under it) fades in
/// when the cutscene says. <see cref="Restore"/> puts it all back.
/// </summary>
public sealed class CutsceneScreen
{
    private readonly List<(VisualElement Element, StyleEnum<DisplayStyle> Display)> hiddenUi = new List<(VisualElement, StyleEnum<DisplayStyle>)>();
    private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
    private VisualElement title;
    private Button skip;

    /// <summary>Gold, as Ravebridge's name.</summary>
    public static readonly Color Gold = new Color(1f, 0.84f, 0.36f);
    public static readonly Color GoldOutline = new Color(0.25f, 0.15f, 0.02f);

    /// <summary>Hides the screen and builds the (invisible) title and the Skip button, which calls <paramref name="onSkip"/>.</summary>
    /// <param name="size">The name's size: a boss's long name is smaller.</param>
    /// <param name="top">The title in the upper part of the screen, over the sky, not over what the shot is of (a boss).</param>
    public static CutsceneScreen Show(string name, string line, Color color, Color outline, Action onSkip, int size = 120, bool top = false)
    {
        var screen = new CutsceneScreen();
        screen.Hide(name, line, color, outline, onSkip, size, top);
        return screen;
    }

    private void Hide(string name, string line, Color color, Color outline, Action onSkip, int size, bool top)
    {
        UIDocument host = null;
        foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>())
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;
            if (!host && root.Q<VisualElement>("conn-layer") != null) host = doc;
            foreach (var child in root.Children())
            {
                hiddenUi.Add((child, child.style.display));
                child.style.display = DisplayStyle.None;
            }
        }
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
        {
            if (!canvas.enabled || !canvas.isRootCanvas) continue;
            canvas.enabled = false;
            hiddenCanvases.Add(canvas);
        }

        if (!host) return;
        title = new VisualElement { pickingMode = PickingMode.Ignore };
        title.style.position = Position.Absolute;
        title.style.left = 0; title.style.right = 0; title.style.top = 0; title.style.bottom = 0;
        title.style.alignItems = Align.Center;
        title.style.justifyContent = top ? Justify.FlexStart : Justify.Center;
        if (top) title.style.paddingTop = Length.Percent(6);
        title.style.opacity = 0f;
        // a soft dark band across the screen behind the words: a light name over snow or a bright sky was hard to read
        var card = new VisualElement { name = "cutscene-title-band", pickingMode = PickingMode.Ignore };
        card.style.alignSelf = Align.Stretch;
        card.style.alignItems = Align.Center;
        card.style.paddingTop = 46;
        card.style.paddingBottom = 52;
        card.style.backgroundImage = Band();
        card.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
        title.Add(card);
        var shadow = new TextShadow { offset = new Vector2(0f, 4f), blurRadius = 14f, color = new Color(0f, 0f, 0f, 0.85f) };
        var big = new Label(name) { pickingMode = PickingMode.Ignore };
        big.AddToClassList("rf-font-eb");
        big.style.fontSize = size;
        big.style.color = color;
        big.style.textShadow = shadow;
        big.style.unityTextOutlineWidth = 2f;
        big.style.unityTextOutlineColor = outline;
        card.Add(big);
        if (!string.IsNullOrEmpty(line))
        {
            var small = new Label(line) { pickingMode = PickingMode.Ignore };
            small.AddToClassList("rf-font-b");
            small.style.fontSize = 38;
            small.style.marginTop = -6;
            small.style.color = new Color(0.97f, 0.94f, 0.86f);
            small.style.textShadow = shadow;
            // a hard dark edge: a boss's line over a bright ice wall or snow washed out (2026-11-26)
            small.style.unityTextOutlineWidth = 1.5f;
            small.style.unityTextOutlineColor = new Color(0.08f, 0.06f, 0.05f);
            // a line of two rows (a floor boss: whose boss it is, then what it says) is centred like one row
            small.style.unityTextAlign = TextAnchor.UpperCenter;
            card.Add(small);
        }
        host.rootVisualElement.Add(title);

        // the only thing on screen besides the scene: a Skip button in the corner (the user, 2026-11-03), Esc too
        var gold = new Color(0.84f, 0.67f, 0.16f);
        skip = new Button(() => onSkip?.Invoke()) { name = "cutscene-skip" };
        skip.style.position = Position.Absolute;
        skip.style.right = 36; skip.style.bottom = 32;
        skip.style.flexDirection = FlexDirection.Row;
        skip.style.alignItems = Align.Center;
        skip.style.paddingLeft = 18; skip.style.paddingRight = 10; skip.style.paddingTop = 8; skip.style.paddingBottom = 8;
        skip.style.backgroundColor = new Color(0.08f, 0.07f, 0.06f, 0.92f);
        skip.style.borderLeftWidth = skip.style.borderRightWidth = skip.style.borderTopWidth = skip.style.borderBottomWidth = 2;
        skip.style.borderLeftColor = skip.style.borderRightColor = skip.style.borderTopColor = skip.style.borderBottomColor = gold;
        skip.style.borderTopLeftRadius = skip.style.borderTopRightRadius = skip.style.borderBottomLeftRadius = skip.style.borderBottomRightRadius = 10;
        var word = new Label(Localization.UI_CUTSCENE_SKIP) { pickingMode = PickingMode.Ignore };
        word.AddToClassList("rf-font-eb");
        word.style.fontSize = 20;
        word.style.color = new Color(0.97f, 0.94f, 0.86f);
        word.style.marginRight = 12;
        var key = new Label("Esc") { pickingMode = PickingMode.Ignore };
        key.AddToClassList("rf-font-b");
        key.style.fontSize = 15;
        key.style.color = new Color(0.97f, 0.94f, 0.86f);
        key.style.paddingLeft = 8; key.style.paddingRight = 8; key.style.paddingTop = 2; key.style.paddingBottom = 3;
        key.style.backgroundColor = new Color(0.16f, 0.14f, 0.11f);
        key.style.borderLeftWidth = key.style.borderRightWidth = key.style.borderTopWidth = key.style.borderBottomWidth = 1;
        key.style.borderLeftColor = key.style.borderRightColor = key.style.borderTopColor = key.style.borderBottomColor = gold;
        key.style.borderTopLeftRadius = key.style.borderTopRightRadius = key.style.borderBottomLeftRadius = key.style.borderBottomRightRadius = 5;
        skip.Add(word);
        skip.Add(key);
        var button = skip;
        skip.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = new Color(0.2f, 0.16f, 0.06f, 0.95f));
        skip.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = new Color(0.08f, 0.07f, 0.06f, 0.92f));
        host.rootVisualElement.Add(skip);
    }

    private static Texture2D band;

    /// <summary>Dark in the middle, clear at the top and the bottom and toward the sides.</summary>
    private static Texture2D Band()
    {
        if (band) return band;
        const int W = 64, H = 32;
        band = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "Cutscene Title Band", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[W * H];
        for (var y = 0; y < H; y++)
        {
            var v = Mathf.Sin(Mathf.PI * (y + 0.5f) / H);
            for (var x = 0; x < W; x++)
            {
                var u = Mathf.Sin(Mathf.PI * (x + 0.5f) / W);
                var a = 0.62f * v * v * Mathf.Lerp(0.35f, 1f, Mathf.Sqrt(u));
                pixels[y * W + x] = new Color32(0, 0, 0, (byte)(255f * a));
            }
        }
        band.SetPixels32(pixels);
        band.Apply();
        return band;
    }

    /// <summary>The title's fade (0 to 1) and its slow growth (0.94 to 1 over <paramref name="grow"/>, 0 to 1).</summary>
    public void SetTitle(float alpha, float grow)
    {
        if (title == null) return;
        title.style.opacity = alpha;
        title.style.scale = new Scale(Vector3.one * Mathf.Lerp(0.94f, 1f, Mathf.Clamp01(grow)));
    }

    public void Restore()
    {
        foreach (var (element, display) in hiddenUi) element.style.display = display;
        hiddenUi.Clear();
        foreach (var canvas in hiddenCanvases) if (canvas) canvas.enabled = true;
        hiddenCanvases.Clear();
        title?.RemoveFromHierarchy();
        title = null;
        skip?.RemoveFromHierarchy();
        skip = null;
    }
}
