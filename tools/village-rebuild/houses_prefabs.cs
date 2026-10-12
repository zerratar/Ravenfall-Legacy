// Makes a house prefab for each building type from the models built by blender/house_*.py, and points the
// building definitions (Assets/Prefabs/Buildings/*.asset) at them. Run models_import.cs first.
// Each prefab: TownHouseController and a footprint BoxCollider on the root (plots measure their footprint
// from it, and it is the click target), and the model as a child. Houses face -z (south), towards the village
// camera and the build mode card camera (BuildingPreview), like the town halls.
// The old prefabs in Assets/Prefabs/Buildings/Objects are left alone; pointing a definition back at one undoes it.
var sb = new System.Text.StringBuilder("HOUSES|");
const string DIR = "Assets/Village/Prefabs/";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Village/Prefabs")) UnityEditor.AssetDatabase.CreateFolder("Assets/Village", "Prefabs");
var models = new System.Collections.Generic.Dictionary<int, string> {
    { 1, "Lumbermill" }, { 2, "Magic" }, { 3, "Archery" }, { 4, "Barracks" }, { 5, "Cooking" }, { 6, "Crafting" },
    { 7, "Farming" }, { 8, "Mining" }, { 9, "Sailing" }, { 10, "Slayer" }, { 11, "Fishing" }, { 12, "Healing" },
    { 13, "Gathering" }, { 14, "Alchemy" } };
var ci = System.Globalization.CultureInfo.InvariantCulture;
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:TownHouse", new[] { "Assets/Prefabs/Buildings" }))
{
    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<TownHouse>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
    string name;
    if (!models.TryGetValue((int)def.Type, out name)) { sb.Append("no model for ").Append(def.Name).Append(" || "); continue; }
    var fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Village/Models/Village_House_" + name + ".fbx");
    if (!fbx) { sb.Append("missing model ").Append(name).Append(" || "); continue; }
    var root = new UnityEngine.GameObject("House_" + name);
    root.AddComponent<TownHouseController>();
    var model = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fbx, root.transform);
    model.transform.localPosition = UnityEngine.Vector3.zero;
    model.transform.localRotation = UnityEngine.Quaternion.identity;
    var rs = model.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    var box = root.AddComponent<UnityEngine.BoxCollider>();
    var size = b.size; size.x = UnityEngine.Mathf.Min(size.x, 9.4f); size.z = UnityEngine.Mathf.Min(size.z, 9.4f);
    var min = UnityEngine.Mathf.Max(b.min.y, 0f);
    box.center = new UnityEngine.Vector3(b.center.x, (min + b.max.y) / 2, b.center.z);
    box.size = new UnityEngine.Vector3(size.x, b.max.y - min, size.z);
    var boxText = box.size.x.ToString("F1", ci) + "x" + box.size.y.ToString("F1", ci) + "x" + box.size.z.ToString("F1", ci);
    var path = DIR + "House_" + name + ".prefab";
    var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEngine.Object.DestroyImmediate(root);
    var old = def.Prefab ? def.Prefab.name : "none";
    def.Prefab = prefab;
    UnityEditor.EditorUtility.SetDirty(def);
    sb.Append(def.Name).Append(": ").Append(old).Append(" -> ").Append(prefab.name).Append(" box ").Append(boxText).Append(" || ");
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEngine.Debug.Log(sb.ToString());
return null;
