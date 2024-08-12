using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TMPro;

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;
using Unity.Hierarchy;
using UnityEditor.SceneManagement;

public class ThemeEditor : OdinEditorWindow
{
    private Dictionary<string, List<Graphic>> colors = new Dictionary<string, List<Graphic>>();
    private Dictionary<string, List<TMP_Text>> fonts = new Dictionary<string, List<TMP_Text>>();

    private Graphic[] colorGraphics;
    private TMP_Text[] fontGraphics;

    private List<ColorGroup> colorGroups;
    private List<FontGroup> fontGroups;


    [TabGroup("Colors")]
    [Header("Color Theme")]
    public Color[] Colors;

    [TabGroup("Fonts")]
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
        this.fontGraphics = FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

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
            if (items == null) items = new List<TMP_Text>();
            items.Add(text);
            fonts[key] = items;
        }

        Colors = colors
            // .OrderByDescending(x => x.Value.Count) // order by how many items that uses this color
            .Select(x => Colorify(x.Key))
            .OrderByDescending(x => x.r + x.g + x.b) // order by color
            .ThenByDescending(x => x.a) // order by alpha
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

    [TabGroup("Colors")]
    [Header("Search and select object with color")]
    public Color ColorSearch;

    [TabGroup("Colors")]
    [Button("Select Object with Color")]
    private void SelectObjWithColor()
    {
        var objToSelect = new List<Object>();

        var key = Stringify(ColorSearch);
        if (!colors.TryGetValue(key, out var items))
        {
            return;
        }

        objToSelect.AddRange(items);

        if (objToSelect.Count > 0)
        {
            Selection.objects = objToSelect.ToArray();
            foreach (var obj in objToSelect)
            {
                ExpandToParent(((Component)obj).transform);
            }
        }
    }

    [TabGroup("Colors")]
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

    [TabGroup("Colors")]
    [Button("Revert Colors")]
    private void RevertThemeColorChanges()
    {
        foreach (var group in colorGroups)
        {
            group.Reset();
        }
    }



    [TabGroup("Fonts")]
    [Header("Search and select object with font")]
    public TMP_FontAsset FontSearch;

    [TabGroup("Fonts")]
    [Button("Select Object with Font")]
    private void SelectObjWithFont()
    {
        var objToSelect = new List<Object>();

        var key = !FontSearch ? "missing-font" : FontSearch.name;
        if (!fonts.TryGetValue(key, out var items))
        {
            return; // no missing fonts.
        }

        objToSelect.AddRange(items);

        if (objToSelect.Count > 0)
        {
            Selection.objects = objToSelect.ToArray();
            var obj = objToSelect.FirstOrDefault();

            //foreach (var obj in objToSelect)
            //{
            ExpandToParent(((Component)obj).transform);
            //}
        }
    }


    [TabGroup("Fonts")]
    [Button("Select Object with Missing Font")]
    private void SelectObjWithMissingFont()
    {
        var objToSelect = new List<Object>();
        foreach (var gfx in fontGraphics)
        {
            if (gfx.font == null || !gfx.font)
            {
                objToSelect.Add(gfx);
            }
        }

        //if (!fonts.TryGetValue("missing-font", out var items))
        //{
        //    return; // no missing fonts.
        //}

        if (objToSelect.Count > 0)
        {
            Selection.objects = objToSelect.ToArray();
            foreach (var obj in objToSelect)
            {
                ExpandToParent(((Component)obj).transform);
            }
        }
    }


    [TabGroup("Fonts")]
    public string Text;

    [TabGroup("Fonts")]
    [Button("Find object by text")]
    private void SelectOBjWithText()
    {
        var objToSelect = new List<Object>();
        foreach (var gfx in fontGraphics)
        {
            if ((string.IsNullOrEmpty(gfx.text) && string.IsNullOrEmpty(Text)) ||
                (!string.IsNullOrEmpty(gfx.text) && !string.IsNullOrEmpty(Text) && gfx.text.Contains(Text, System.StringComparison.OrdinalIgnoreCase)))
            {
                objToSelect.Add(gfx);
            }
        }

        if (objToSelect.Count > 0)
        {
            Selection.objects = objToSelect.ToArray();
            foreach (var obj in objToSelect)
            {
                ExpandToParent(((Component)obj).transform);
            }
        }
    }

    [TabGroup("Fonts")]
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

    [TabGroup("Fonts")]
    [Button("Revert Fonts")]
    private void RevertThemeFontChanges()
    {
        foreach (var group in fontGroups)
        {
            group.Reset();
        }
    }



    static void ExpandToParent(Transform transform)
    {
        // Recursively go up to the root and expand each parent
        if (transform.parent != null)
        {
            ExpandToParent(transform.parent);
        }

        // Get the path in the hierarchy and expand it
        string hierarchyPath = GetHierarchyPath(transform);
        ExpandHierarchyToPath(hierarchyPath);
    }

    static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    static void ExpandHierarchyToPath(string hierarchyPath)
    {
        // Open the hierarchy window and focus on the object
        EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
        //EditorApplication.ExecuteMenuItem("Window/Layouts/Default"); // Optional: reset layout to ensure Hierarchy is visible

        // Expand all GameObjects in the path
        var gameObject = GameObject.Find(hierarchyPath);
        if (gameObject != null)
        {
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
        }
    }

}


public class FontGroup
{
    public TMP_FontAsset Font;
    public List<TMP_Text> Texts;
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
