// builds the dwarf hold on Ironhill's shelf from a list; rerunnable (clears its group); pieces stand on the terrain.
// 2026-10-27: the forge corner moved into the open strip right of the paved way: the big crag east of the yard had
// swallowed it (the crane, a house and a cart that stood there were dropped), and the worker became the forge-master,
// who gives the runestone's story (IslandNpc, StoryLog.SmithKey)
var island = UnityEngine.GameObject.Find("============ WORLD/Islands/Ironhill Island").transform;
var group = island.Find("Dwarf Hold");
if (group) UnityEngine.Object.DestroyImmediate(group.gameObject);
group = new UnityEngine.GameObject("Dwarf Hold").transform; group.SetParent(island, false);
UnityEngine.Terrain t = null; foreach (var x in UnityEngine.Terrain.activeTerrains) if (x.name == "Terrain") t = x;
string D(string n) => "Assets/PolygonDungeonRealms/Prefabs/" + n + ".prefab";
// prefab, x, z, yaw, scale, lift (metres above the ground)
var list = new System.Collections.Generic.List<(string p, float x, float z, float yaw, float s, float lift)>
{
    (D("Environments/SM_Env_Dwarf_Gate_01"), 16, 483.5f, 180, 0.42f, -0.4f),
    (D("Environments/SM_Env_Dwarf_Obelisk_01"), 6, 477, 180, 1.1f, 0),
    (D("Environments/SM_Env_Dwarf_Obelisk_01"), 26, 477, 180, 1.1f, 0),
    (D("Props/SM_Prop_Dwarf_Brazier_01"), 9, 472, 0, 1, 0),
    (D("Props/SM_Prop_Dwarf_Brazier_01"), 23, 472, 0, 1, 0),
    (D("Environments/SM_Env_Dwarf_Forge_Smelter_01"), -6, 474, 150, 0.55f, 0),
    (D("Environments/SM_Env_Dwarf_Lava_Wheel_01"), 29.5f, 469, 90, 1.2f, 2.5f),
    (D("Props/SM_Prop_Dwarf_Fire_Pit_01"), 25.5f, 462, 0, 1.2f, 0),
    (D("Environments/SM_Env_Dwarf_Forge_Smelter_02"), 0, 457, 200, 1, 0),
    (D("Props/SM_Prop_Dwarf_Smelting_Pot_01"), 26.5f, 450.5f, 0, 0.8f, 0),
    (D("Props/SM_Prop_Dwarf_Cog_Pile_01"), 25, 465, 30, 1.4f, 0),
    (D("Props/SM_Prop_Dwarf_Totem_01"), 8, 436, 160, 1.2f, 0),
    (D("Buildings/SM_Bld_Dwarf_Building_03"), 4, 444, 90, 1, 0),
    ("Assets/PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Anvil_01.prefab", 25.3f, 457.4f, 120, 1.3f, 0),
    (D("Props/SM_Prop_Dwarf_Cart_02"), 1, 466, 20, 1, 1.5f),
    (D("Props/SM_Prop_Dwarf_Cart_03"), 9, 455, 300, 1, 1.5f),
    (D("Props/SM_Prop_Dwarf_Weapon_Barrel_01"), -2, 462, 0, 1, 0),
    (D("Props/SM_Prop_Dwarf_Weapon_Barrel_02"), 26.8f, 447, 40, 1, 0),
    (D("Props/SM_Prop_Dwarf_Lever_01"), 21, 479, 180, 1.2f, 0),
    (D("Props/SM_Prop_Dwarf_Brazier_01"), 12, 452, 0, 1, 0),
    (D("Props/SM_Prop_Dwarf_Brazier_01"), 20, 452, 0, 1, 0),
    (D("Props/SM_Prop_Dwarf_Brazier_01"), 12, 440, 0, 1, 0),
    (D("Props/SM_Prop_Dwarf_Brazier_01"), 20, 440, 0, 1, 0),
};
var placed = new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach (var e in list)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(e.p);
    if (!prefab) return "missing " + e.p;
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, group);
    var y = t.SampleHeight(new UnityEngine.Vector3(e.x, 0, e.z)) + t.transform.position.y;
    if (e.p.Contains("Gate_01")) y = t.SampleHeight(new UnityEngine.Vector3(e.x, 0, e.z - 6)) + t.transform.position.y; // the yard floor in front
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(e.x, y + e.lift, e.z), UnityEngine.Quaternion.Euler(0, e.yaw, 0));
    go.transform.localScale = UnityEngine.Vector3.one * e.s;
    go.isStatic = true;
    placed.Add(go);
}

var floors = new[] { "01", "02", "03", "04", "05", "06" };
var prng = new System.Random(3);
void Tile(float px, float pz)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(D("Environments/SM_Env_Dwarf_Floor_" + floors[prng.Next(floors.Length)]));
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, group);
    var y = t.SampleHeight(new UnityEngine.Vector3(px - 2.5f, 0, pz + 2.5f)) + t.transform.position.y;
    go.transform.position = new UnityEngine.Vector3(px, y + 0.06f, pz);
    go.isStatic = true;
}
for (var px = 11f; px <= 26f; px += 5f) for (var pz = 469f; pz <= 474f; pz += 5f) Tile(px, pz);
for (var pz = 434f; pz < 469f; pz += 5f) Tile(18.5f, pz);

// the dwarves: guards at the gate, a miner by the gate, the forge-master at the anvil; the island NPCs' idle (humanoid)
var idle = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>("Assets/Animations/Barkeep.controller");
foreach (var (n, x, z, yaw) in new[] { ("Chr_BR_Dwarf_Soldier_Male_01", 11.5f, 478f, 180f), ("Chr_BR_Dwarf_Soldier_Female_01", 20.5f, 478f, 180f), ("Chr_BR_Dwarf_Miner_01", 26.5f, 475f, 110f), ("Chr_BR_Dwarf_Worker_01", 23.8f, 456f, 210f) })
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(D("Characters/" + n));
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, group);
    var y = t.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + t.transform.position.y;
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(x, y, z), UnityEngine.Quaternion.Euler(0, yaw, 0));
    foreach (var a in go.GetComponentsInChildren<UnityEngine.Animator>(true)) { a.runtimeAnimatorController = idle; a.applyRootMotion = false; }
    if (n == "Chr_BR_Dwarf_Worker_01")
    {
        go.name = "Forge-master";
        var npc = go.AddComponent<IslandNpc>();
        npc.Key = StoryLog.SmithKey; npc.HeadHeight = 1.85f; npc.TalkDistance = 3f;
    }
}
// clear the crags that cut into the gate's opening and the yard in front of it
var gate = placed[0];
var gb = new UnityEngine.Bounds(gate.transform.position + new UnityEngine.Vector3(0, 10, -6), new UnityEngine.Vector3(20, 24, 22));
var yard = new UnityEngine.Bounds(new UnityEngine.Vector3(14, 6, 463), new UnityEngine.Vector3(46, 12, 26));
var cleared = 0;
var mountain = island.Find("Mountain");
if (mountain)
    for (var i = mountain.childCount - 1; i >= 0; i--)
    {
        var c = mountain.GetChild(i).gameObject;
        if (gb.Contains(c.transform.position) || yard.Contains(c.transform.position)) { UnityEngine.Object.DestroyImmediate(c); cleared++; }
    }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
return "placed " + placed.Count + ", cleared " + cleared + " crags; gate at " + gate.transform.position.ToString("F1");
