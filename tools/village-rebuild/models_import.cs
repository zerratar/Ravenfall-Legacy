// Sets up the village models built by blender/*.py: maps their material slots onto the kit materials by
// name, and makes the stained glass material (the kit palette, glowing softly). Run after rebuilding a model.
var sb = new System.Text.StringBuilder("MODELS|");
const string DIR = "Assets/Village/Models/";
const string MATDIR = "Assets/Village/Materials/";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Village/Materials")) UnityEditor.AssetDatabase.CreateFolder("Assets/Village", "Materials");
var glassPath = MATDIR + "Village_StainedGlass.mat";
var glass = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(glassPath);
if (!glass)
{
    var src = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_01_A_Emmisive.mat");
    glass = new UnityEngine.Material(src);
    UnityEditor.AssetDatabase.CreateAsset(glass, glassPath);
}
var palette = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/PolygonFantasyKingdom/Textures/PolygonFantasyKingdom_Texture_01_A.png");
glass.SetTexture("_EmissionMap", palette);
glass.SetColor("_EmissionColor", new UnityEngine.Color(0.85f, 0.85f, 0.85f));
glass.EnableKeyword("_EMISSION");
glass.globalIlluminationFlags = UnityEngine.MaterialGlobalIlluminationFlags.RealtimeEmissive;
UnityEditor.EditorUtility.SetDirty(glass);

// Materials of our own: a kit material with another kit texture (the same list as DERIVED in blender/common.py),
// and the normal map to go with it.
var derived = new[] {
    new[] { "Village_Wall_Stucco", "PolygonFantasyKingdom_Mat_Castle_Wall", "PolygonFantasyKingdon_Texture_Wall_Stucco_01", "PolygonFantasyKingdon_Texture_Wall_Stucco_Normal" },
    new[] { "Village_Roof_Tile_Brown", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Tile_02", "keep" },
    new[] { "Village_Roof_Tile_Red", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Tile_03", "keep" },
    new[] { "Village_Roof_Tile_Teal", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Tile_04", "keep" },
    new[] { "Village_Roof_Slat_Blue", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Slat_01", "" },
    new[] { "Village_Roof_Slat_Brown", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Slat_02", "" },
    new[] { "Village_Roof_Slat_Red", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Slat_03", "" },
    new[] { "Village_Roof_Slat_Teal", "PolygonFantasyKingdom_Mat_Castle_Roof", "PolygonFantasyKingdon_Texture_Roof_Slat_04", "" },
};
var derivedPaths = new System.Collections.Generic.Dictionary<string, string>();
foreach (var d in derived)
{
    var path = MATDIR + d[0] + ".mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (!m)
    {
        m = new UnityEngine.Material(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyKingdom/Materials/" + d[1] + ".mat"));
        UnityEditor.AssetDatabase.CreateAsset(m, path);
    }
    var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/PolygonFantasyKingdom/Textures/Castle/" + d[2] + ".png");
    foreach (var prop in new[] { "_BaseMap", "_MainTex" }) if (m.HasProperty(prop)) m.SetTexture(prop, tex);
    // the fourth entry: a normal map of its own, "keep" for the copied one, or empty for none (the slats have none)
    if (d[3] != "keep" && m.HasProperty("_BumpMap"))
        m.SetTexture("_BumpMap", d[3] == "" ? null : UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/PolygonFantasyKingdom/Textures/Castle/" + d[3] + ".png"));
    UnityEditor.EditorUtility.SetDirty(m);
    derivedPaths[d[0]] = path;
}

var kitMats = new System.Collections.Generic.Dictionary<string, string> {
    { "PolygonFantasyKingdom_Mat_01_A", "Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_01_A.mat" },
    { "PolygonFantasyKingdom_Mat_Castle_Wall", "Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_Castle_Wall.mat" },
    { "PolygonFantasyKingdom_Mat_Castle_Wall Darker", "Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_Castle_Wall Darker.mat" },
    { "PolygonFantasyKingdom_Mat_Castle_Roof", "Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_Castle_Roof.mat" },
    { "Village_StainedGlass", glassPath } };
foreach (var kv in derivedPaths) kitMats[kv.Key] = kv.Value;
UnityEditor.AssetDatabase.Refresh();
foreach (var path in System.IO.Directory.GetFiles(DIR, "*.fbx"))
{
    var p = path.Replace('\\', '/');
    var imp = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(p);
    imp.materialImportMode = UnityEditor.ModelImporterMaterialImportMode.ImportStandard;
    imp.materialLocation = UnityEditor.ModelImporterMaterialLocation.InPrefab;
    imp.importAnimation = false; imp.importCameras = false; imp.importLights = false;
    imp.animationType = UnityEditor.ModelImporterAnimationType.None;
    imp.isReadable = false;
    imp.addCollider = false;
    foreach (var kv in kitMats)
        imp.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material), kv.Key), UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(kv.Value));
    imp.SaveAndReimport();
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p);
    var r = go.GetComponentInChildren<UnityEngine.MeshRenderer>();
    var mesh = go.GetComponentInChildren<UnityEngine.MeshFilter>().sharedMesh;
    sb.Append(System.IO.Path.GetFileNameWithoutExtension(p)).Append(" bounds c").Append(mesh.bounds.center.ToString("F2")).Append(" s").Append(mesh.bounds.size.ToString("F2"))
      .Append(" rootScale ").Append(go.transform.localScale.ToString("F2")).Append(" rootRot ").Append(go.transform.localEulerAngles.ToString("F0"))
      .Append(" child ").Append(r.transform == go.transform ? "self" : r.transform.localEulerAngles.ToString("F0") + "/" + r.transform.localScale.ToString("F2"))
      .Append(" mats ");
    foreach (var m in r.sharedMaterials) sb.Append(m ? m.name : "null").Append(",");
    sb.Append(" tris ").Append(mesh.triangles.Length / 3).Append(" || ");
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEngine.Debug.Log(sb.ToString());
return null;
