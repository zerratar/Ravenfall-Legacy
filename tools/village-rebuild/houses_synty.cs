// Puts the Synty props into a village house and bakes it: reads the SYNTY_ anchors that blender/house_*.py leaves
// in Assets/Village/Models/Village_House_<Name>.fbx (see anchor() in house_kit.py), places the Synty prefab for each
// key there, then combines the house and every prop into one mesh with one part per material and writes it to
// Assets/Village/Models/Baked/Village_House_<Name>_Baked.fbx, materials mapped to the real ones. houses_prefabs.cs
// uses the baked model when there is one.
//
// One house per run, to stay inside the editor's time limit: the name is read from
// Temp/village-shots/houses_synty_house.txt (for example "Barracks"). Run models_import.cs first.
//
// KEYS: key | prefab under Assets/ (several joined with + are placed together, sized by the first) | size in metres | which size (h height, x length, z depth, w widest) |
// turn in degrees to make the prefab face Unity -z | how it stands (pivot: as modelled, sit: bottom on the anchor,
// centre: bottom on the anchor and centred over it, for prefabs whose pivot is at a corner).
var KEYS = new string[] {
    "barrel|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Barrel_01|0.85|h|0|pivot",
    "crate|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Crate_Wood_01|0.62|h|0|pivot",
    "crate_stack|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Crate_Stack_01|1.5|h|0|pivot",
    "tree|PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Tree_Round_01|4.4|h|0|pivot",
    "pine|PolygonVikings/Prefabs/Environments/SM_Env_Tree_Pine_01|4.5|h|0|pivot",                 // the Polygon Nature pine uses a tree shader that draws pink here
    "bush|PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Bush_Small_01|0.85|h|0|pivot",
    "hay_bale|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Hay_Bale_01|0.6|h|0|sit",
    "hay_pile|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Hay_Pile_01|1.8|h|0|pivot",
    "sack|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Sack_01|0.65|x|0|pivot",
    "sack_stack|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Sack_Stack_01|0.9|h|0|pivot",
    "wood_pile|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Wood_Pile_01|2.4|x|0|pivot",
    "stump|PolygonFantasyKingdom/Prefabs/Generic/SM_Generic_TreeStump_01|0.5|h|0|pivot",
    "basket|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Basket_01|0.36|h|0|pivot",
    "mushroom|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Mushroom_01|0.25|h|0|pivot",
    "torch|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Torch_01|0.9|h|0|pivot",
    "lamp|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Street_Lamp_01|2.6|h|0|pivot",
    "dummy|PolygonFantasyKingdom/Prefabs/Props/BattleGround/SM_Prop_Dummy_Armour_01|1.7|h|0|pivot",
    "dummy_down|PolygonFantasyKingdom/Prefabs/Props/BattleGround/SM_Prop_Dummy_Armour_01|1.7|h|0|sit",
    "target|PolygonFantasyKingdom/Prefabs/Props/BattleGround/SM_Prop_Target_01|1.3|h|0|pivot",
    "fence|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Fence_01|2.47|x|0|pivot",
    "bench|PolygonFantasyKingdom/Prefabs/Props/Furniture/SM_Prop_Bench_Seat_01|1.7|x|0|pivot",
    "weapon_rack|PolygonFantasyKingdom/Prefabs/Props/BattleGround/SM_Prop_Weapon_Rack_01|1.8|x|0|pivot",
    "camp_rack|PolygonFantasyKingdom/Prefabs/Props/BattleGround/SM_Prop_Camp_Weapon_Rack_01|1.9|h|0|pivot",
    "spear|PolygonFantasyKingdom/Prefabs/Weapons/SM_Wep_Spear_01|2.0|h|0|pivot",
    "sword|PolygonFantasyKingdom/Prefabs/Weapons/SM_Wep_Sword_01|1.1|h|90|pivot",
    "shield|PolygonFantasyKingdom/Prefabs/Weapons/SM_Wep_Shield_01|0.75|h|180|pivot",
    "axe|PolygonFantasyKingdom/Prefabs/Weapons/SM_Wep_Axe_01|0.9|h|0|pivot",
    "bow|PolygonFantasyKingdom/Prefabs/Weapons/SM_Prop_Bow_01|1.3|h|0|pivot",
    "banner|PolygonFantasyKingdom/Prefabs/Props/Banners/SM_Prop_Battle_Banner_01|2.6|h|0|pivot",
    "well|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Well_01|2.7|h|0|pivot",
    "rowboat|PolygonKnights/Prefabs/Props/SM_Prop_Rowboat_01|4.0|z|0|sit",
    "fish_crate|PolygonPirates/Prefabs/Props/SM_Prop_Fishing_Crate_01|0.8|x|0|pivot",
    "net|PolygonVikings/Prefabs/Props/SM_Prop_Net_01|2.0|z|0|pivot",
    "fish_bucket|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Bucket_Fish_01|0.5|h|0|pivot",
    "fish|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Meat_Fish_01|0.45|z|0|pivot",
    "fish_rack|PolygonVikings/Prefabs/Props/SM_Prop_Fish_Rack_01|2.6|x|0|pivot",
    "bread|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Bread_01|0.32|z|0|pivot",
    "pie|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Pie_01|0.3|x|0|pivot",
    "bread_crate|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Bread_Crate_01|0.75|x|0|pivot",
    "bottle|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Bottle_01|0.32|h|0|pivot",
    "bottle2|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Bottle_02|0.34|h|0|pivot",
    "cauldron|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Cauldron_01|0.75|h|0|pivot",
    "anvil|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Anvil_01|0.75|z|0|pivot",
    "workbench|PolygonFantasyKingdom/Prefabs/Props/Furniture/SM_Prop_Workbench_01|2.0|x|0|pivot",
    "table|PolygonFantasyKingdom/Prefabs/Props/Furniture/SM_Prop_Table_Wood_01|2.0|z|0|pivot",
    "wheelbarrow|PolygonFantasyKingdom/Prefabs/Vehicles/SM_Veh_Wheelbarrow_01|1.8|z|0|pivot",
    "cart|PolygonFantasyKingdom/Prefabs/Vehicles/SM_Veh_Cart_01|3.0|z|0|pivot",
    "firepit|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_FirePit_01|1.0|x|0|pivot",
    "rock|PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Rock_01|1.0|w|0|sit",
    "minecart|PolygonDungeon/Prefabs/Props/SM_Prop_Minecart_01|1.6|w|0|sit",
    "ore_gold|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Ore_Gold_01|0.45|w|0|sit",
    "ore_iron|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Ore_Iron_01|0.5|w|0|sit",
    "ore_gem|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Ore_Gem_01|0.4|w|0|sit",
    "cheese|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Cheese_Wheel_01|0.34|w|0|sit",
    "sausages|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Meat_Sausages_01|0.85|h|0|pivot",
    "bellows|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Bellows_01|0.9|w|0|pivot",
    "forge|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Forge_01|1.6|h|0|pivot",
    "grindstone|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Grinding_Wheel_01|1.1|h|0|pivot",
    "hammer|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Hammer_01|0.45|h|0|pivot",
    "pickaxe|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Pickaxe_01|0.85|h|0|pivot",
    "saw|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Saw_01|0.7|h|0|pivot",
    "shovel|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Shovel_01|1.2|h|0|pivot",
    "cage_cart|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Cage_Cart_01|2|h|0|pivot",
    "lectern|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Church_Lectern_01|1.25|h|0|pivot",
    "book|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Book_01|0.3|w|0|pivot",
    "books|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Books_01|0.45|w|0|pivot",
    "crystal|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Crystal_01|1|h|0|pivot",
    "broom|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Broom_01|1.4|h|0|pivot",
    "herbs|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Herb_Bunch_01|0.62|h|0|pivot",
    "planter|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_PlanterBox_Preset_01|1.3|x|0|centre",
    "cabbage|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Cabbage_01|0.32|w|0|pivot",
    "oar|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Oar_01|2.2|w|0|pivot",
    "log|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Log_01|2.4|w|0|pivot",
    "plank|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Plank_01|2.4|w|0|pivot",
    "planks|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Ground_Planks_01|2.4|w|0|pivot",
    "bucket|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Bucket_01|0.4|h|0|pivot",
    "tub|PolygonFantasyKingdom/Prefabs/Items/SM_Item_Wash_Bucket_01|0.9|w|0|pivot",
    "horseshoe|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Forge_Horseshoe_01|0.3|w|0|pivot",
    "candles|PolygonFantasyKingdom/Prefabs/Props/Preset/SM_Prop_Candle_Melted_Group_01|0.3|h|0|pivot",
    "garlic|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Garlic_Rope_01|0.8|h|0|pivot",
    "anvil2|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Anvil_02|0.75|w|0|pivot",
    "chopping_board|PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Chopping_Board_01|0.5|w|0|pivot",
    "ship_wheel|PolygonPirates/Prefabs/Props/SM_Prop_ShipWheel_01|1.1|h|0|pivot",
    "ship_anchor|PolygonPirates/Prefabs/Props/SM_Prop_Anchor_01|1.4|h|0|pivot",
    "lantern|PolygonPirates/Prefabs/Props/SM_Prop_Lantern_01|0.45|h|0|pivot",
    "fishing_rod|PolygonPirates/Prefabs/Props/SM_Prop_FishingRod_01|2.4|h|0|pivot",
    "chest|PolygonPirates/Prefabs/Props/SM_Prop_Chest_01|0.7|w|0|pivot",
    "rope|PolygonVikings/Prefabs/Props/SM_Prop_Rope_01|0.7|w|0|pivot",
    "fish_pile|PolygonVikings/Prefabs/Props/SM_Prop_Fish_Pile_01|0.9|w|0|pivot",
    "fish_hanging|PolygonVikings/Prefabs/Props/SM_Prop_Fish_Hanging_01|0.8|h|0|pivot",
    "skull|PolygonVikings/Prefabs/Props/SM_Prop_Skull_01|0.35|w|0|sit",
    "skull_pole|PolygonVikings/Prefabs/Props/SM_Prop_Skull_Pole_01|2.2|h|0|pivot",
    "spike|PolygonVikings/Prefabs/Props/SM_Prop_Wood_Spike_01|1.8|h|0|pivot",
    "cow_skull|PolygonVikings/Prefabs/Props/SM_Prop_Cow_Skull_01|0.6|w|0|pivot",
    "quiver|PolygonElvenRealm/Prefabs/Characters/Attachments/SM_Prop_Quiver_01+PolygonElvenRealm/Prefabs/Characters/Attachments/SM_Prop_Quiver_01_Arrows_01|1.0|h|0|sit",
    "flowers|PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Bush_Flowers_01|0.7|h|0|pivot",
    "potion1|PolygonDungeon/Prefabs/Items/SM_Item_Potion_01|0.26|h|0|sit",
    "potion2|PolygonDungeon/Prefabs/Items/SM_Item_Potion_02|0.26|h|0|sit",
    "potion3|PolygonDungeon/Prefabs/Items/SM_Item_Potion_03|0.26|h|0|sit",
    "potion4|PolygonDungeon/Prefabs/Items/SM_Item_Potion_04|0.26|h|0|sit",
    "potion5|PolygonDungeon/Prefabs/Items/SM_Item_Potion_05|0.26|h|0|sit",
    "potion6|PolygonDungeon/Prefabs/Items/SM_Item_Potion_06|0.26|h|0|sit",
    "flower1|PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Flowers_01|0.4|h|0|pivot",
    "flower2|PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Flowers_02|0.4|h|0|pivot",
    "flower3|PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Flowers_03|0.4|h|0|pivot",
    "flower4|PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Flowers_04|0.4|h|0|pivot",
    "sunflower|PolygonFantasyKingdom/Prefabs/Environments/SM_Env_Sunflower_01|1.6|h|0|pivot",
};

var log = new System.Text.StringBuilder("SYNTY|");
var shots = "G:/Ravenfall/Projects/Ravenfall Legacy/Temp/village-shots/";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var houseName = System.IO.File.ReadAllText(shots + "houses_synty_house.txt").Trim();
System.Action<string> done = text => System.IO.File.WriteAllText(shots + "houses_synty_" + houseName + ".txt", log.ToString() + text);

var modelPath = "Assets/Village/Models/Village_House_" + houseName + ".fbx";
var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(modelPath);
if (!model) { done("no model " + modelPath); return null; }

// the key table, with each prefab's size measured once from its first level of detail
var specs = new System.Collections.Generic.Dictionary<string, string[]>();
foreach (var line in KEYS) { var p = line.Split('|'); specs[p[0]] = p; }

System.Func<UnityEngine.GameObject, System.Collections.Generic.List<UnityEngine.MeshRenderer>> drawn = go =>
{
    var list = new System.Collections.Generic.List<UnityEngine.MeshRenderer>();
    var lod = go.GetComponentInChildren<UnityEngine.LODGroup>();
    if (lod && lod.GetLODs().Length > 0)
    {
        foreach (var r in lod.GetLODs()[0].renderers) if (r is UnityEngine.MeshRenderer mr) list.Add(mr);
    }
    else list.AddRange(go.GetComponentsInChildren<UnityEngine.MeshRenderer>());
    list.RemoveAll(r => !r || !r.enabled || !r.GetComponent<UnityEngine.MeshFilter>() || !r.GetComponent<UnityEngine.MeshFilter>().sharedMesh);
    return list;
};
System.Func<UnityEngine.GameObject, UnityEngine.Bounds> boundsOf = go =>
{
    var b = new UnityEngine.Bounds(); var first = true;
    foreach (var r in drawn(go)) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
    return b;
};

var root = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(model);
root.transform.position = UnityEngine.Vector3.zero;
root.transform.rotation = UnityEngine.Quaternion.identity;
root.transform.localScale = UnityEngine.Vector3.one;
var anchors = new System.Collections.Generic.List<UnityEngine.Transform>();
foreach (var t in root.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name.StartsWith("SYNTY_")) anchors.Add(t);

var props = new System.Collections.Generic.List<UnityEngine.GameObject>();
var counts = new System.Collections.Generic.Dictionary<string, int>();
foreach (var a in anchors)
{
    // SYNTY_<key>__<n>__y<yaw>__t<tilt>__r<roll>__s<scale>[__l<length>][__h<height>]; Blender may add ".001" on a clash
    var name = a.name; var dot = name.IndexOf('.'); if (dot > 0 && !char.IsDigit(name[dot - 1])) name = name.Substring(0, dot);
    var parts = name.Substring(6).Split(new[] { "__" }, System.StringSplitOptions.None);
    var key = parts[0];
    float yaw = 0f, tilt = 0f, roll = 0f, scale = 1f, length = -1f, height = -1f;
    foreach (var part in parts)
    {
        if (part.Length < 2) continue;
        float v;
        if (!float.TryParse(part.Substring(1), System.Globalization.NumberStyles.Float, inv, out v)) continue;
        switch (part[0]) { case 'y': yaw = v; break; case 't': tilt = v; break; case 'r': roll = v; break; case 's': scale = v; break; case 'l': length = v; break; case 'h': height = v; break; }
    }
    string[] spec;
    if (!specs.TryGetValue(key, out spec)) { log.Append("unknown key " + key + "; "); continue; }
    // one prefab, or several placed together as one (a quiver and its arrows)
    var paths = spec[1].Split('+');
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/" + paths[0] + ".prefab");
    if (!prefab) { log.Append("missing prefab " + paths[0] + "; "); continue; }

    var prop = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(prefab);
    prop.transform.position = UnityEngine.Vector3.zero; prop.transform.rotation = UnityEngine.Quaternion.identity; prop.transform.localScale = UnityEngine.Vector3.one;
    for (var pi = 1; pi < paths.Length; ++pi)
    {
        var extra = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/" + paths[pi] + ".prefab");
        if (extra) ((UnityEngine.GameObject)UnityEngine.Object.Instantiate(extra, prop.transform, false)).transform.localPosition = UnityEngine.Vector3.zero;
        else log.Append("missing prefab " + paths[pi] + "; ");
    }
    var natural = boundsOf(prop);
    var size = float.Parse(spec[2], inv);
    var axis = spec[3];
    var have = axis == "x" ? natural.size.x : axis == "z" ? natural.size.z : axis == "w" ? UnityEngine.Mathf.Max(natural.size.x, natural.size.z) : natural.size.y;
    var k = size * scale / UnityEngine.Mathf.Max(0.01f, have);
    var s3 = new UnityEngine.Vector3(k, k, k);
    // stretched to a span (fence sections): the length along x, the height as asked
    if (length > 0f) { s3.x = length / UnityEngine.Mathf.Max(0.01f, natural.size.x); }
    if (height > 0f) { s3.y = s3.z = height / 0.9f; }
    prop.transform.localScale = s3;
    // Blender turns counterclockwise from +y (Unity -z); Unity turns clockwise from +z
    // the prop's own turn first, so tilt and roll work about the way it faces once it faces -z
    var own = UnityEngine.Quaternion.Euler(0f, float.Parse(spec[4], inv), 0f);
    prop.transform.rotation = UnityEngine.Quaternion.Euler(0f, 180f - yaw, 0f) * UnityEngine.Quaternion.Euler(tilt, 0f, 0f) * UnityEngine.Quaternion.Euler(0f, 0f, roll) * own;
    prop.transform.position = a.position;
    if (spec[5] == "sit" || spec[5] == "centre")
    {
        UnityEngine.Physics.SyncTransforms();
        var placed = boundsOf(prop);
        var shift = new UnityEngine.Vector3(0f, a.position.y - placed.min.y, 0f);
        if (spec[5] == "centre") { shift.x = a.position.x - placed.center.x; shift.z = a.position.z - placed.center.z; }
        prop.transform.position += shift;
    }
    props.Add(prop);
    counts[key] = (counts.ContainsKey(key) ? counts[key] : 0) + 1;
}

// bake: every drawn mesh, house and props, grouped by material, into one mesh with a part per material
var groups = new System.Collections.Generic.Dictionary<UnityEngine.Material, System.Collections.Generic.List<UnityEngine.CombineInstance>>();
var order = new System.Collections.Generic.List<UnityEngine.Material>();
var all = new System.Collections.Generic.List<UnityEngine.MeshRenderer>(drawn(root));
foreach (var prop in props) all.AddRange(drawn(prop));
var vertexCount = 0;
foreach (var r in all)
{
    var mesh = r.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
    var mats = r.sharedMaterials;
    for (var sub = 0; sub < mesh.subMeshCount; ++sub)
    {
        var mat = mats.Length > 0 ? mats[UnityEngine.Mathf.Min(sub, mats.Length - 1)] : null;
        if (!mat) continue;
        if (!groups.ContainsKey(mat)) { groups[mat] = new System.Collections.Generic.List<UnityEngine.CombineInstance>(); order.Add(mat); }
        groups[mat].Add(new UnityEngine.CombineInstance { mesh = mesh, subMeshIndex = sub, transform = r.transform.localToWorldMatrix });
        vertexCount += mesh.vertexCount;
    }
}
var parts32 = new System.Collections.Generic.List<UnityEngine.CombineInstance>();
foreach (var mat in order)
{
    var part = new UnityEngine.Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
    part.CombineMeshes(groups[mat].ToArray(), true, true);
    parts32.Add(new UnityEngine.CombineInstance { mesh = part, transform = UnityEngine.Matrix4x4.identity });
}
var baked = new UnityEngine.Mesh { name = "Village_House_" + houseName + "_Baked", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
baked.CombineMeshes(parts32.ToArray(), false, false);
baked.RecalculateBounds();

var bakedGo = new UnityEngine.GameObject("Village_House_" + houseName + "_Baked");
bakedGo.AddComponent<UnityEngine.MeshFilter>().sharedMesh = baked;
bakedGo.AddComponent<UnityEngine.MeshRenderer>().sharedMaterials = order.ToArray();

if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Village/Models/Baked")) UnityEditor.AssetDatabase.CreateFolder("Assets/Village/Models", "Baked");
var outPath = "Assets/Village/Models/Baked/Village_House_" + houseName + "_Baked.fbx";
UnityEditor.Formats.Fbx.Exporter.ModelExporter.ExportObject(outPath, bakedGo);

UnityEngine.Object.DestroyImmediate(bakedGo);
UnityEngine.Object.DestroyImmediate(root);
foreach (var prop in props) UnityEngine.Object.DestroyImmediate(prop);
foreach (var p in parts32) UnityEngine.Object.DestroyImmediate(p.mesh);
UnityEngine.Object.DestroyImmediate(baked);

// the exported materials are stand-ins: map every slot back to the material it came from, by name
UnityEditor.AssetDatabase.ImportAsset(outPath, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(outPath);
importer.materialImportMode = UnityEditor.ModelImporterMaterialImportMode.ImportStandard;
importer.materialLocation = UnityEditor.ModelImporterMaterialLocation.InPrefab;
foreach (var mat in order) importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material), mat.name), mat);
importer.SaveAndReimport();

var summary = new System.Text.StringBuilder();
foreach (var kv in counts) summary.Append(kv.Key + " x" + kv.Value + " ");
done(houseName + ": " + anchors.Count + " anchors, props " + summary + "| materials " + order.Count + " | vertices " + vertexCount + " -> " + outPath);
return null;
