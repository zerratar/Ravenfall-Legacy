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

var kitMats = new System.Collections.Generic.Dictionary<string, string> {
    { "PolygonFantasyKingdom_Mat_01_A", "Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_01_A.mat" },
    { "PolygonFantasyKingdom_Mat_Castle_Wall", "Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_Castle_Wall.mat" },
    { "PolygonFantasyKingdom_Mat_Castle_Wall Darker", "Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_Castle_Wall Darker.mat" },
    { "PolygonFantasyKingdom_Mat_Castle_Roof", "Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_Castle_Roof.mat" },
    { "Village_StainedGlass", glassPath } };
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
