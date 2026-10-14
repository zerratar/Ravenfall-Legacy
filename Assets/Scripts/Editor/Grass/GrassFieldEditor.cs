using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The inspector of a <see cref="GrassField"/>: what is grown now, the settings to try out, reading the terrain
/// layers' colours into the profile, and the brush that paints grass in or out:
/// <list type="bullet">
/// <item>More: click or drag to fill in grass, up to full grass whatever the ground.</item>
/// <item>Less: thin it out, down to none (under a market stall, a dance floor, a beach).</item>
/// <item>Reset: back to the terrain's own grass.</item>
/// </list>
/// [ and ] change the brush size, Escape leaves the mode, one undo per stroke. Roads and village plots keep grass
/// off by themselves and need no painting. Ravenfall &gt; Grass &gt; Add Grass To Terrain sets a terrain up with a
/// profile and a blade material in Assets/World/Grass.
/// </summary>
[CustomEditor(typeof(GrassField))]
public class GrassFieldEditor : Editor
{
    private enum Mode { Off, More, Less, Reset }

    private const string Folder = "Assets/World/Grass";
    private const string PrefsPrefix = "Ravenfall.GrassBrush.";
    private static readonly string[] ModeNames = { "Off", "More", "Less", "Reset" };

    private static Mode mode;
    private int strokeGroup = -1;
    private Vector3 lastDab;
    private Vector2 strokeMin, strokeMax;

    private static float Radius
    {
        get => EditorPrefs.GetFloat(PrefsPrefix + "Radius", 4f);
        set => EditorPrefs.SetFloat(PrefsPrefix + "Radius", Mathf.Clamp(value, 0.5f, 40f));
    }

    private static float Strength
    {
        get => EditorPrefs.GetFloat(PrefsPrefix + "Strength", 0.5f);
        set => EditorPrefs.SetFloat(PrefsPrefix + "Strength", Mathf.Clamp(value, 0.05f, 1f));
    }

    private void OnEnable()
    {
        mode = Mode.Off;
        Undo.undoRedoPerformed += Undone;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= Undone;
        EndStroke();
        Tools.hidden = false;
    }

    private void Undone()
    {
        if (target) GrassRenderer.GroundChanged((GrassField)target);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var field = (GrassField)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
        if (!field.Mask)
        {
            if (GUILayout.Button("Create Mask To Paint")) field.Mask = CreateMask();
        }
        else
        {
            var chosen = (Mode)GUILayout.Toolbar((int)mode, ModeNames);
            if (chosen != mode)
            {
                mode = chosen;
                SceneView.RepaintAll();
            }
            if (mode != Mode.Off)
            {
                Radius = EditorGUILayout.Slider("Size", Radius, 0.5f, 40f);
                Strength = EditorGUILayout.Slider("Strength", Strength, 0.05f, 1f);
                EditorGUILayout.HelpBox("Click or drag on the ground. [ and ] change the size, Escape stops. Roads and village plots keep grass off by themselves.", MessageType.None);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grown now", GrassRenderer.CellCount + " cells, " + GrassRenderer.InstanceCount.ToString("N0") + " instances");
        var last = GrassRenderer.LastDrawn;
        EditorGUILayout.LabelField("Drawn last", last.near.ToString("N0") + " near, " + last.far.ToString("N0") + " far, " + last.triangles.ToString("N0") + " triangles");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Settings (for this session)", EditorStyles.boldLabel);
        GrassRenderer.Enabled = EditorGUILayout.Toggle("Grass", GrassRenderer.Enabled);
        GrassRenderer.Distance = EditorGUILayout.Slider("Distance", GrassRenderer.Distance, 10f, 250f);
        GrassRenderer.Density = EditorGUILayout.Slider("Density", GrassRenderer.Density, 0.1f, 1f);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Read Layer Colours") && field.Profile && field.Terrain)
            {
                ReadLayerColours(field.Profile, field.Terrain.terrainData);
                GrassRenderer.GroundChanged(field);
            }
            if (GUILayout.Button("Grow Again"))
            {
                GrassRenderer.GroundChanged(field);
            }
        }
        if (GUI.changed) SceneView.RepaintAll();
    }

    private void OnSceneGUI()
    {
        var field = (GrassField)target;
        Tools.hidden = mode != Mode.Off;
        if (mode == Mode.Off || !field.Mask || !field.Terrain) return;

        var e = Event.current;
        var control = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(control);
        if (e.type == EventType.MouseMove) HandleUtility.Repaint();

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            mode = Mode.Off;
            Tools.hidden = false;
            e.Use();
            Repaint();
            return;
        }
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.LeftBracket || e.keyCode == KeyCode.RightBracket))
        {
            Radius *= e.keyCode == KeyCode.RightBracket ? 1.15f : 1f / 1.15f;
            e.Use();
            Repaint();
        }

        if (!ResourceGround.UnderMouse(e.mousePosition, out var ground))
        {
            if (e.type == EventType.MouseUp && GUIUtility.hotControl == control) EndStroke();
            return;
        }

        Handles.color = mode == Mode.More ? new Color(0.45f, 0.95f, 0.5f) : mode == Mode.Less ? new Color(1f, 0.55f, 0.35f) : new Color(0.8f, 0.8f, 0.8f);
        Handles.DrawWireDisc(ground.point, ground.normal, Radius, 2f);
        Handles.DrawWireDisc(ground.point, ground.normal, Radius * 0.5f, 1f);

        if (e.alt || e.button != 0) return;
        if (e.type == EventType.MouseDown && HandleUtility.nearestControl == control)
        {
            GUIUtility.hotControl = control;
            Undo.IncrementCurrentGroup();
            strokeGroup = Undo.GetCurrentGroup();
            strokeMin = new Vector2(float.MaxValue, float.MaxValue);
            strokeMax = new Vector2(float.MinValue, float.MinValue);
            Dab(field, ground.point);
            lastDab = ground.point;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == control)
        {
            if (Vector3.Distance(ground.point, lastDab) >= Radius * 0.3f)
            {
                Dab(field, ground.point);
                lastDab = ground.point;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseUp && GUIUtility.hotControl == control)
        {
            EndStroke();
            e.Use();
        }
    }

    /// <summary>
    /// Paints the mask round a point, strongest in the middle and fading to the edge of the brush, then grows the
    /// grass there again.
    /// </summary>
    private void Dab(GrassField field, Vector3 centre)
    {
        var mask = field.Mask;
        Undo.RecordObject(mask, "Paint grass");
        var target = mode == Mode.More ? 255f : mode == Mode.Less ? 0f : GrassMask.Neutral;
        var origin = field.Terrain.transform.position;
        var metres = mask.MetresPerTexel;
        var radius = Radius;
        var from = Vector2Int.FloorToInt(new Vector2(centre.x - radius - origin.x, centre.z - radius - origin.z) / metres);
        var to = Vector2Int.CeilToInt(new Vector2(centre.x + radius - origin.x, centre.z + radius - origin.z) / metres);
        for (var y = Mathf.Max(0, from.y); y <= to.y; ++y)
        {
            for (var x = Mathf.Max(0, from.x); x <= to.x; ++x)
            {
                var at = new Vector2(origin.x + (x + 0.5f) * metres, origin.z + (y + 0.5f) * metres);
                var d = Vector2.Distance(at, new Vector2(centre.x, centre.z)) / radius;
                if (d >= 1f) continue;
                var weight = Strength * (1f - d * d);
                var value = Mathf.Lerp(mask.Get(x, y), target, weight);
                mask.Set(x, y, (byte)Mathf.Clamp(Mathf.RoundToInt(value), 0, 255));
            }
        }
        EditorUtility.SetDirty(mask);

        var min = new Vector2(centre.x - radius, centre.z - radius);
        var max = new Vector2(centre.x + radius, centre.z + radius);
        strokeMin = Vector2.Min(strokeMin, min);
        strokeMax = Vector2.Max(strokeMax, max);
        GrassRenderer.MaskChanged(field, min, max);
        SceneView.RepaintAll();
    }

    private void EndStroke()
    {
        if (strokeGroup >= 0)
        {
            Undo.CollapseUndoOperations(strokeGroup);
            var field = target as GrassField;
            if (field && field.Mask) field.Mask.Tidy();
        }
        strokeGroup = -1;
        if (GUIUtility.hotControl != 0) GUIUtility.hotControl = 0;
    }

    private static GrassMask CreateMask()
    {
        EnsureFolder(Folder);
        var mask = CreateInstance<GrassMask>();
        AssetDatabase.CreateAsset(mask, AssetDatabase.GenerateUniqueAssetPath(Folder + "/Grass Mask.asset"));
        AssetDatabase.SaveAssets();
        return mask;
    }

    [MenuItem("Ravenfall/Grass/Add Grass To Terrain")]
    private static void AddToTerrain()
    {
        var terrain = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Terrain>() : null;
        if (!terrain) terrain = Terrain.activeTerrain;
        if (!terrain)
        {
            EditorUtility.DisplayDialog("Add grass", "Select a terrain first.", "OK");
            return;
        }

        var field = terrain.GetComponent<GrassField>();
        if (!field) field = Undo.AddComponent<GrassField>(terrain.gameObject);
        if (!field.Profile) field.Profile = CreateProfile(terrain.terrainData);
        EditorUtility.SetDirty(field);
        Selection.activeGameObject = terrain.gameObject;
    }

    /// <summary>
    /// A profile for the terrain: every layer with a guess at how grassy it is from its name (grass, meadow dirt
    /// and snow grass grow some; paths, mud and rock none), its colour from its texture, and one kind of blades.
    /// </summary>
    private static GrassProfile CreateProfile(TerrainData data)
    {
        EnsureFolder(Folder);
        var profile = CreateInstance<GrassProfile>();
        profile.layers = data.terrainLayers.Select(l => new GrassProfile.Layer { terrainLayer = l, density = GuessDensity(l) }).ToArray();

        var material = new Material(Shader.Find("Ravenfall/Grass")) { name = "Grass Blades" };
        AssetDatabase.CreateAsset(material, Folder + "/Grass Blades.mat");
        profile.kinds = new[]
        {
            new GrassProfile.Kind { name = "Blades", blades = 7, bladeWidth = 0.05f, material = material, perSquareMetre = 5f, size = new Vector2(0.3f, 0.55f) },
        };

        AssetDatabase.CreateAsset(profile, Folder + "/Grass Profile.asset");
        ReadLayerColours(profile, data);
        return profile;
    }

    private static float GuessDensity(TerrainLayer layer)
    {
        var name = layer ? layer.name.ToLowerInvariant() : "";
        if (name.Contains("snow")) return 0.5f;
        if (name.Contains("grass")) return 1f;
        if (name.Contains("meadow_dirt")) return 0.7f;
        return 0f;
    }

    /// <summary>
    /// Each layer's colour: its texture's average, read on the GPU (the textures are not readable), times the
    /// layer's tint. New terrain layers are added with no grass.
    /// </summary>
    public static void ReadLayerColours(GrassProfile profile, TerrainData data)
    {
        var layers = profile.layers.ToList();
        foreach (var terrainLayer in data.terrainLayers)
        {
            if (!terrainLayer || profile.LayerFor(terrainLayer) != null) continue;
            layers.Add(new GrassProfile.Layer { terrainLayer = terrainLayer, density = 0f });
        }
        profile.layers = layers.ToArray();

        foreach (var layer in profile.layers)
        {
            if (!layer.terrainLayer || !layer.terrainLayer.diffuseTexture) continue;
            var average = Average(layer.terrainLayer.diffuseTexture);
            var tint = layer.terrainLayer.diffuseRemapMax;
            layer.colour = new Color(average.r * tint.x, average.g * tint.y, average.b * tint.z, 1f);
        }
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }

    private static Color Average(Texture texture)
    {
        const int Size = 64;
        var rt = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(texture, rt);
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var read = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        read.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        var sum = Vector3.zero;
        foreach (var c in read.GetPixels()) sum += new Vector3(c.r, c.g, c.b);
        DestroyImmediate(read);
        sum /= Size * Size;
        return new Color(sum.x, sum.y, sum.z, 1f);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
