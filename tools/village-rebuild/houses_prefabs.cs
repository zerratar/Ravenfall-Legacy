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
    // the baked model (house and Synty props in one mesh, houses_synty.cs) when there is one, else the house alone
    var fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Village/Models/Baked/Village_House_" + name + "_Baked.fbx")
        ?? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Village/Models/Village_House_" + name + ".fbx");
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
    // the yards: the house model (not the baked one) carries YARD markers, see yard() in blender/house_kit.py
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Village/Models/Village_House_" + name + ".fbx");
    var yards = 0;
    if (source)
    {
        foreach (var t in source.GetComponentsInChildren<UnityEngine.Transform>(true))
        {
            if (!t.name.StartsWith("YARD__")) continue;
            float hx = 4.6f, hz = 4.4f, jag = 0.12f, seed = 1f, yaw = 0f; var kind = "dirt";
            foreach (var part in t.name.Split(new[] { "__" }, System.StringSplitOptions.None))
            {
                if (part.Length < 2) continue;
                float v; var num = float.TryParse(part.Substring(1), System.Globalization.NumberStyles.Float, ci, out v);
                switch (part[0]) { case 'x': if (num) hx = v; break; case 'z': if (num) hz = v; break; case 'j': if (num) jag = v; break; case 'e': if (num) seed = v; break; case 'y': if (num) yaw = v; break; case 'k': kind = part.Substring(1); break; }
            }
            var yardGo = new UnityEngine.GameObject("Yard");
            yardGo.transform.SetParent(root.transform, false);
            // the marker's position comes through the model's axes; its turn is given in Blender's, as for the props
            yardGo.transform.localPosition = source.transform.InverseTransformPoint(t.position);
            yardGo.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f - yaw, 0f);
            yardGo.AddComponent<HouseYard>().Configure(new UnityEngine.Vector2(hx, hz), jag, seed, kind == "gravel" ? WorldRoad.Surface.Gravel : WorldRoad.Surface.Dirt);
            yards++;
        }
    }

    var path = DIR + "House_" + name + ".prefab";
    var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEngine.Object.DestroyImmediate(root);
    var old = def.Prefab ? def.Prefab.name : "none";
    def.Prefab = prefab;
    UnityEditor.EditorUtility.SetDirty(def);
    sb.Append(def.Name).Append(": ").Append(old).Append(" -> ").Append(prefab.name).Append(" box ").Append(boxText).Append(" yards ").Append(yards).Append(" || ");
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEngine.Debug.Log(sb.ToString());
return null;
