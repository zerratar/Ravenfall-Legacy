using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The inspector of a <see cref="GrassField"/>: what is grown now, the settings to try out, and reading the
/// terrain layers' colours into the profile. Ravenfall &gt; Grass &gt; Add Grass To Terrain sets a terrain up with a
/// profile and a blade material in Assets/World/Grass.
/// </summary>
[CustomEditor(typeof(GrassField))]
public class GrassFieldEditor : Editor
{
    private const string Folder = "Assets/World/Grass";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var field = (GrassField)target;

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
