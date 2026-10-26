using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ravenfall > Image Generator: the OpenAI key (saved on this computer), and pictures by hand: a prompt, optional input
/// images to edit or to follow, where to write the result; or an item icon in the game's style, new or as a variant.
/// The same calls are what tools and evals use (<see cref="ImageGen"/>, <see cref="IconForge"/>).
/// </summary>
public sealed class ImageGenWindow : EditorWindow
{
    private string keyField = "";
    private ImageGen.Model model = ImageGen.Model.Flare;
    private string quality = "high";
    private string size = "1024x1024";
    private bool transparent = true;
    private string prompt = "";
    private string outPath = "Temp/ImageGen/picture.png";
    private readonly List<Texture2D> inputs = new List<Texture2D>();
    private string iconItem = "";
    private string iconDescription = "";
    private string variantFrom = "";
    private string variantChange = "";
    private Vector2 scroll;
    private Texture2D preview;
    private string previewPath;

    private static readonly string[] Qualities = { "low", "medium", "high", "xhigh", "max", "auto" };
    private static readonly string[] Sizes = { "1024x1024", "1536x1024", "1024x1536", "auto" };

    [MenuItem("Ravenfall/Image Generator")]
    public static void Open() => GetWindow<ImageGenWindow>("Image Generator");

    private void OnEnable() => EditorApplication.update += Repaint;
    private void OnDisable() => EditorApplication.update -= Repaint;

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("OpenAI key", EditorStyles.boldLabel);
        if (ImageGen.KeyFromEnvironment)
        {
            EditorGUILayout.HelpBox("Using the OPENAI_API_KEY environment variable.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(ImageGen.HasKey
                ? "A key is saved on this computer (EditorPrefs). It is never written to the project or logged. Every picture costs money on its account."
                : "Paste your OpenAI API key and save it. It stays on this computer (EditorPrefs), never in the project.", ImageGen.HasKey ? MessageType.None : MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                keyField = EditorGUILayout.PasswordField(keyField);
                if (GUILayout.Button("Save", GUILayout.Width(60))) { ImageGen.SaveKey(keyField); keyField = ""; GUI.FocusControl(null); }
                if (ImageGen.HasKey && GUILayout.Button("Forget", GUILayout.Width(60))) ImageGen.SaveKey("");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        model = (ImageGen.Model)EditorGUILayout.EnumPopup(new GUIContent("Model", "Flare: quick, for most pictures. Sunburst: keeps closest to the input images in edits."), model);
        quality = Qualities[Mathf.Max(0, EditorGUILayout.Popup("Quality", System.Array.IndexOf(Qualities, quality), Qualities))];
        size = Sizes[Mathf.Max(0, EditorGUILayout.Popup("Size", System.Array.IndexOf(Sizes, size), Sizes))];
        transparent = EditorGUILayout.Toggle("Transparent background", transparent);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("A picture", EditorStyles.boldLabel);
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.MinHeight(60));
        EditorGUILayout.LabelField("Input images (to edit, or to follow the style of)");
        for (var i = 0; i < inputs.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                inputs[i] = (Texture2D)EditorGUILayout.ObjectField(inputs[i], typeof(Texture2D), false);
                if (GUILayout.Button("x", GUILayout.Width(22))) { inputs.RemoveAt(i); break; }
            }
        }
        if (GUILayout.Button("Add input image")) inputs.Add(null);
        outPath = EditorGUILayout.TextField("Write to", outPath);
        using (new EditorGUI.DisabledScope(!ImageGen.HasKey || string.IsNullOrWhiteSpace(prompt)))
        {
            var files = inputs.Where(t => t).Select(AssetDatabase.GetAssetPath).Where(p => p.EndsWith(".png")).ToArray();
            if (GUILayout.Button(files.Length > 0 ? "Edit (" + files.Length + " input" + (files.Length > 1 ? "s" : "") + ")" : "Generate"))
            {
                var options = new ImageGen.Options { Model = model, Quality = quality, Size = size, Transparent = transparent };
                if (files.Length > 0) ImageGen.Edit(prompt, files, outPath, options, null, ShowResult);
                else ImageGen.Generate(prompt, outPath, options, ShowResult);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Item icons", EditorStyles.boldLabel);
        iconItem = EditorGUILayout.TextField("Item name", iconItem);
        iconDescription = EditorGUILayout.TextField("What it looks like", iconDescription);
        using (new EditorGUI.DisabledScope(!ImageGen.HasKey || string.IsNullOrWhiteSpace(iconItem)))
        {
            if (GUILayout.Button("New icon in the game's style")) IconForge.NewIcon(iconItem, iconDescription, null, model, ShowResult);
        }
        variantFrom = EditorGUILayout.TextField(new GUIContent("Variant of (icon)", "An existing icon's file name, e.g. raw-tuna"), variantFrom);
        variantChange = EditorGUILayout.TextField(new GUIContent("Change", "e.g. cooked: grilled golden brown with char marks"), variantChange);
        using (new EditorGUI.DisabledScope(!ImageGen.HasKey || string.IsNullOrWhiteSpace(iconItem) || string.IsNullOrWhiteSpace(variantFrom) || string.IsNullOrWhiteSpace(variantChange)))
        {
            if (GUILayout.Button("Variant icon (item name above)")) IconForge.Variant(variantFrom, iconItem, variantChange, ImageGen.Model.Sunburst, ShowResult);
        }
        if (GUILayout.Button("List items without an icon"))
        {
            var missing = IconForge.MissingIcons();
            Debug.Log("Image generator: " + missing.Count + " items have no icon" + (missing.Count > 0 ? ": " + string.Join(", ", missing) : "."));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Recent", EditorStyles.boldLabel);
        foreach (var job in ImageGen.Jobs.Reverse().Take(12))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(job.ToString(), EditorStyles.wordWrappedMiniLabel);
                if (job.State == ImageGen.JobState.Done && GUILayout.Button("Show", GUILayout.Width(50))) ShowResult(job);
            }
        }
        if (preview)
        {
            EditorGUILayout.LabelField(previewPath);
            var rect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(false));
            EditorGUI.DrawTextureTransparent(rect, preview, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.EndScrollView();
    }

    private void ShowResult(ImageGen.Job job)
    {
        if (job.State != ImageGen.JobState.Done || !File.Exists(job.OutPath)) return;
        if (preview) DestroyImmediate(preview);
        preview = new Texture2D(2, 2);
        preview.LoadImage(File.ReadAllBytes(job.OutPath));
        previewPath = job.OutPath;
        Repaint();
    }
}
