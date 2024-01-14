using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TMPro;

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using System;

public class ThemeEditor : OdinEditorWindow
{
    private Dictionary<string, List<Graphic>> colors = new Dictionary<string, List<Graphic>>();
    private Dictionary<string, List<TextMeshProUGUI>> fonts = new Dictionary<string, List<TextMeshProUGUI>>();

    private Graphic[] colorGraphics;
    private TextMeshProUGUI[] fontGraphics;

    private List<ColorGroup> colorGroups;
    private List<FontGroup> fontGroups;

    [Header("Color Theme")]
    public Color[] Colors;
    
    [Header("Font Theme")]
    public TMP_FontAsset[] Fonts;

    [MenuItem("Ravenfall/Theme Editor")]
    private static void OpenWindow()
    {
        var window = GetWindow<ThemeEditor>();
        window.Init();
        window.Show();
    }

    private void Init()
    {
        this.colorGraphics = FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        this.fontGraphics = FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        colors.Clear();

        // get the color count usage for both background and foreground so we can categorize them by usage count.
        foreach (var ui in colorGraphics)
        {
            var color = ui.color;
            var key = Stringify(color);
            colors.TryGetValue(key, out var items);
            if (items == null) items = new List<Graphic>();
            items.Add(ui);
            colors[key] = items;
        }

        foreach (var text in fontGraphics)
        {
            var font = text.font;
            var key = "missing-font";
            if (font)
            {
                key = font.name;
            }

            fonts.TryGetValue(key, out var items);
            if (items == null) items = new List<TextMeshProUGUI>();
            items.Add(text);
            fonts[key] = items;
        }

        Colors = colors
            .OrderByDescending(x => x.Value.Count)
            .Select(x => Colorify(x.Key))
            .ToArray();

        colorGroups = new List<ColorGroup>();
        for (int i = 0; i < Colors.Length; i++)
        {
            var color = Colors[i];
            var key = Stringify(color);
            var items = colors[key];
            colorGroups.Add(new ColorGroup
            {
                Color = color,
                Graphics = items
            });
        }

        Fonts = fonts
            .OrderByDescending(x => x.Value.Count)
            .Select(x => x.Value[0].font)
            .ToArray();

        fontGroups = new List<FontGroup>();
        for (int i = 0; i < Fonts.Length; i++)
        {
            var font = Fonts[i];
            var key = !font ? "missing-font" : font.name;
            var items = fonts[key];
            fontGroups.Add(new FontGroup
            {
                Font = font,
                Texts = items
            });
        }
    }
    private Color Colorify(string color)
    {
        if (!color.StartsWith("#")) color = "#" + color;
        ColorUtility.TryParseHtmlString(color, out var c);
        return c;
    }
    private string Stringify(Color color)
    {
        var rgba = ColorUtility.ToHtmlStringRGBA(color);
        return rgba;
    }

    [HorizontalGroup(GroupID = "Color")]
    [Button("Apply Colors")]
    private void ApplyThemeColorChanges()
    {
        for (var i = 0; i < Colors.Length; ++i)
        {
            var color = Colors[i];
            var group = colorGroups[i];
            group.SetColor(color);
        }

        Init();
    }

    [HorizontalGroup(GroupID = "Color")]
    [Button("Revert Colors")]
    private void RevertThemeColorChanges()
    {
        foreach (var group in colorGroups)
        {
            group.Reset();
        }
    }

    [HorizontalGroup(GroupID = "Font")]
    [Button("Apply Fonts")]
    private void ApplyThemeFontChanges()
    {
        for (var i = 0; i < Fonts.Length; ++i)
        {
            var font = Fonts[i];
            var group = fontGroups[i];
            group.SetFont(font);
        }

        Init();
    }

    [HorizontalGroup(GroupID = "Font")]
    [Button("Revert Fonts")]
    private void RevertThemeFontChanges()
    {
        foreach (var group in fontGroups)
        {
            group.Reset();
        }
    }

}


public class FontGroup
{
    public TMP_FontAsset Font;
    public List<TextMeshProUGUI> Texts;
    internal void Reset()
    {
        foreach (var g in Texts)
        {
            g.font = Font;
        }
    }

    internal void SetFont(TMP_FontAsset font)
    {
        foreach (var g in Texts)
        {
            g.font = font;
        }
    }
    public override string ToString()
    {
        return Font.name;
    }
}

public class ColorGroup
{
    public Color Color;
    public List<Graphic> Graphics;

    internal void Reset()
    {
        foreach (var g in Graphics)
        {
            g.color = Color;
        }
    }

    internal void SetColor(Color color)
    {
        foreach (var g in Graphics)
        {
            g.color = color;
        }
    }

    public override string ToString()
    {
        return Color.ToString();
    }
}
