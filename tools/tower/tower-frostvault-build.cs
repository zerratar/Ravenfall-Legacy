// Builds Assets/Scenes/Tower/Tower_FrostVault.unity: the ice vault of the Frozen Vault band (floors 13, 14, 17, 18,
// 20), made from the crypt's builder: the same rooms and walls, frozen over, cold blue light against warm braziers,
// icicles hanging off walls and pillars, snow drifted in the corners, ice crystals glowing blue. Was: the crypt under the Overgrown Halls, a real dungeon (Zerratar
// 2026-11-07: some floors should look like an actual dungeon, but not like the dungeon events). Rooms of old stone
// tiles, low broken walls (the camera looks down from behind and above, so walls stay low and there is no ceiling),
// big round pillars, roots and moss grown through everything, giant mushrooms, bones, coffins, braziers and their warm
// light against a cold green dark. An entry chamber with the stairs down, a long pillared hall, two side crypts and a
// great throne hall for the boss. At (0, 0, -6000) like every floor (one floor is loaded at a time).
// Its own navmesh from invisible colliders. Writes %TEMP%/tower-crypt-build.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var origin = new UnityEngine.Vector3(0f, 0f, -6000f);
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/Tower_FrostVault";
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_FrostVault");

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);

var root = new UnityEngine.GameObject("Tower Floor - The Frost Vault");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var floor = root.AddComponent<TowerFloor>();
floor.Radius = 110f;
floor.SkyAmbient = new UnityEngine.Color(0.42f, 0.52f, 0.68f);
floor.EquatorAmbient = new UnityEngine.Color(0.28f, 0.35f, 0.48f);
floor.GroundAmbient = new UnityEngine.Color(0.12f, 0.15f, 0.20f);
floor.FogColor = new UnityEngine.Color(0.30f, 0.40f, 0.54f);
floor.FogDensity = 0.011f;
floor.SunColor = new UnityEngine.Color(0.75f, 0.86f, 1f);
floor.SunIntensity = 0.75f;
floor.SunAngles = new UnityEngine.Vector3(70f, -20f, 0f);

var e = "Assets/PolygonDungeon/Prefabs/Environments/";
var pr = "Assets/PolygonDungeon/Prefabs/Props/";
var fv = "Assets/Synty/PolygonFantasyVillage/Prefabs/Environment/";
var pnb = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/";
var ve = "Assets/Synty/PolygonVikingRealm/Prefabs/Environment/";
var rng = new System.Random(23);
float R() => (float)rng.NextDouble();
var placed = 0;

// a prefab with its bounds' middle (on the ground plane) at a local point, turned by yaw
UnityEngine.GameObject Put(string path, UnityEngine.Vector3 local, float yaw, UnityEngine.Vector3 scale, UnityEngine.Transform parent, bool centre = true)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { log.AppendLine("missing " + path); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    go.transform.SetParent(parent, false);
    go.transform.localScale = scale;
    go.transform.localRotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
    go.transform.localPosition = local;
    if (centre)
    {
        var rs = go.GetComponentsInChildren<UnityEngine.Renderer>();
        if (rs.Length > 0)
        {
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var want = root.transform.TransformPoint(local);
            var off = want - b.center; off.y = 0f;
            go.transform.position += off;
        }
    }
    placed++;
    return go;
}
UnityEngine.Vector3 V(float x, float z) => new UnityEngine.Vector3(x, 0f, z);
UnityEngine.Vector3 One(float s) => UnityEngine.Vector3.one * s;

var geo = new UnityEngine.GameObject("Rooms").transform; geo.SetParent(root.transform, false);
var walls = new UnityEngine.GameObject("Walls").transform; walls.SetParent(root.transform, false);
var props = new UnityEngine.GameObject("Props").transform; props.SetParent(root.transform, false);
var growth = new UnityEngine.GameObject("Overgrowth").transform; growth.SetParent(root.transform, false);
var lights = new UnityEngine.GameObject("Lights").transform; lights.SetParent(root.transform, false);
var nav = new UnityEngine.GameObject("Walkable").transform; nav.SetParent(root.transform, false);

// rooms: x0, z0, x1, z1 (local metres, multiples of 5)
var rooms = new System.Collections.Generic.List<(string name, float x0, float z0, float x1, float z1)>
{
    ("Entry", -10, -75, 10, -50),
    ("Hall", -15, -50, 15, 10),
    ("West crypt", -45, -40, -15, -10),
    ("East crypt", 15, -40, 45, -10),
    ("Throne hall", -30, 10, 30, 60),
};
bool InRoom(float x, float z, float m = 0f) => rooms.Any(r => x > r.x0 + m && x < r.x1 - m && z > r.z0 + m && z < r.z1 - m);

// floors: 5 m tiles, worn ones mixed in; runes and ornate tiles in the throne hall's middle
var plain = new[] { "SM_Env_Tiles_01", "SM_Env_Tiles_02", "SM_Env_Tiles_03", "SM_Env_Tiles_04", "SM_Env_Tiles_05", "SM_Env_Tiles_06", "SM_Env_Tiles_07", "SM_Env_Tiles_08", "SM_Env_Tiles_09", "SM_Env_Tiles_010" };
foreach (var r in rooms)
{
    for (var x = r.x0; x < r.x1; x += 5)
        for (var z = r.z0; z < r.z1; z += 5)
        {
            var cx = x + 2.5f; var cz = z + 2.5f;
            string tile;
            var throneMid = r.name == "Throne hall" && UnityEngine.Mathf.Abs(cx) < 12 && cz > 22 && cz < 48;
            if (throneMid) tile = (UnityEngine.Mathf.Abs(cx) < 4 && UnityEngine.Mathf.Abs(cz - 35) < 4) ? "SM_Env_Tiles_Rune_0" + (1 + rng.Next(5)) : "SM_Env_Tiles_Ornate_0" + (1 + rng.Next(2));
            else if (r.name == "Hall" && UnityEngine.Mathf.Abs(cx) < 4 && rng.Next(3) == 0) tile = "SM_Env_Tiles_Rune_0" + (1 + rng.Next(5));
            else tile = plain[rng.Next(plain.Length)];
            Put(e + "Floors/" + tile + ".prefab", V(cx, cz), 90f * rng.Next(4), One(1f), geo);
        }
    // what players walk on: one invisible slab per room
    var slab = new UnityEngine.GameObject("Floor " + r.name); slab.transform.SetParent(nav, false);
    slab.transform.localPosition = new UnityEngine.Vector3((r.x0 + r.x1) / 2f, -0.25f, (r.z0 + r.z1) / 2f);
    var bc = slab.AddComponent<UnityEngine.BoxCollider>(); bc.size = new UnityEngine.Vector3(r.x1 - r.x0, 0.5f, r.z1 - r.z0);
}
// the doorways between rooms are open; small slabs bridge them
void Door(float x, float z, float w, float d) { var s = new UnityEngine.GameObject("Doorway"); s.transform.SetParent(nav, false); s.transform.localPosition = new UnityEngine.Vector3(x, -0.25f, z); var b = s.AddComponent<UnityEngine.BoxCollider>(); b.size = new UnityEngine.Vector3(w, 0.5f, d); }
Door(0, -50, 10, 2); Door(-15, -25, 2, 10); Door(15, -25, 2, 10); Door(0, 10, 20, 2);

// walls: low and broken (0.55 of their height), 5 m pieces along every room edge except the doorways
var wallKinds = new[] { "SM_Env_Wall_01", "SM_Env_Wall_02", "SM_Env_Wall_03", "SM_Env_Wall_04", "SM_Env_Wall_05" };
bool IsDoor(float x, float z)
{
    if (UnityEngine.Mathf.Abs(z - -50) < 0.1f && UnityEngine.Mathf.Abs(x) < 5) return true;
    if (UnityEngine.Mathf.Abs(z - 10) < 0.1f && UnityEngine.Mathf.Abs(x) < 10) return true;
    if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.Abs(x) - 15) < 0.1f && z > -30 && z < -20) return true;
    return false;
}
var wallCount = 0;
foreach (var r in rooms)
{
    void Edge(float ax, float az, float bx, float bz, float yaw)
    {
        var len = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(bx - ax), UnityEngine.Mathf.Abs(bz - az));
        for (var t = 0f; t < len - 0.1f; t += 5f)
        {
            var mx = ax + (bx - ax) * (t + 2.5f) / len; var mz = az + (bz - az) * (t + 2.5f) / len;
            if (IsDoor(mx, mz)) continue;
            // inside another room (a shared edge that is open)
            var nx = (bz - az) / len; var nz = -(bx - ax) / len;
            if (InRoom(mx + nx * 1.5f, mz + nz * 1.5f) && InRoom(mx - nx * 1.5f, mz - nz * 1.5f)) continue;
            var broken = rng.Next(5) == 0;
            var h = broken ? 0.3f + R() * 0.15f : 0.5f + R() * 0.12f;
            Put(e + "Walls/" + (broken ? "SM_Env_Wall_Broken_Edge_0" + (1 + rng.Next(2)) : wallKinds[rng.Next(wallKinds.Length)]) + ".prefab", V(mx, mz), yaw, new UnityEngine.Vector3(1f, h, 1f), walls);
            var col = new UnityEngine.GameObject("Wall"); col.transform.SetParent(nav, false);
            col.transform.localPosition = new UnityEngine.Vector3(mx, 1.5f, mz);
            col.transform.localRotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
            var bc = col.AddComponent<UnityEngine.BoxCollider>(); bc.size = new UnityEngine.Vector3(5f, 3f, 0.8f);
            wallCount++;
        }
    }
    Edge(r.x0, r.z0, r.x1, r.z0, 0f);
    Edge(r.x0, r.z1, r.x1, r.z1, 180f);
    Edge(r.x0, r.z0, r.x0, r.z1, 90f);
    Edge(r.x1, r.z0, r.x1, r.z1, 270f);
}
log.AppendLine("walls " + wallCount);

// the void around the rooms: a dark floor far below, so the camera sees darkness, not sky
var voidPlane = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane);
voidPlane.name = "Void"; UnityEngine.Object.DestroyImmediate(voidPlane.GetComponent<UnityEngine.Collider>());
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(voidPlane, scene);
voidPlane.transform.SetParent(root.transform, false);
voidPlane.transform.localPosition = new UnityEngine.Vector3(0f, -6f, 0f);
voidPlane.transform.localScale = new UnityEngine.Vector3(300f, 1f, 300f);
var voidMat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Unlit"));
voidMat.color = new UnityEngine.Color(0.05f, 0.08f, 0.13f);
UnityEditor.AssetDatabase.DeleteAsset(dataFolder + "/Void.mat");
UnityEditor.AssetDatabase.CreateAsset(voidMat, dataFolder + "/Void.mat");
voidPlane.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = voidMat;
voidPlane.GetComponent<UnityEngine.MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

// pillars: two rows down the hall, a ring in the throne hall, a few in the crypts
void Pillar(float x, float z, bool broken)
{
    Put(e + "Pillars/" + (broken ? "SM_Env_Pillar_Broken_0" + (1 + rng.Next(2)) : "SM_Env_Wall_Pillar_Large_Round_01") + ".prefab", V(x, z), R() * 360f, One(broken ? 1.3f : 1.4f), props);
    var c = new UnityEngine.GameObject("Pillar"); c.transform.SetParent(nav, false); c.transform.localPosition = new UnityEngine.Vector3(x, 1.5f, z);
    var cc = c.AddComponent<UnityEngine.CapsuleCollider>(); cc.radius = 1.0f; cc.height = 3f;
    if (!broken && rng.Next(3) != 0) Put(ve + "SM_Env_Icicle_Small_Cluster_0" + (1 + rng.Next(2)) + ".prefab", V(x + 0.9f, z), R() * 360f, One(1.4f), growth, false).transform.localPosition = new UnityEngine.Vector3(x + 0.8f, 3.6f + R() * 1.5f, z);
}
for (var z = -45f; z <= 5f; z += 10f) { Pillar(-9f, z, rng.Next(5) == 0); Pillar(9f, z, rng.Next(5) == 0); }
for (var i = 0; i < 10; i++)
{
    var a = (i + 0.5f) / 10f * UnityEngine.Mathf.PI * 2f;
    var x = UnityEngine.Mathf.Cos(a) * 20f; var z = 35f + UnityEngine.Mathf.Sin(a) * 18f;
    if (z < 20f && UnityEngine.Mathf.Abs(x) < 12f) continue; // the way in stays open
    if (z > 48f && UnityEngine.Mathf.Abs(x) < 8f) continue; // nothing in front of the throne
    Pillar(x, z, i % 4 == 1);
}
Pillar(-30f, -25f, true); Pillar(30f, -25f, false);

// the throne: on a dais of steps at the north wall, roots everywhere around it
Put(e + "Misc/SM_Env_Stone_Throne_01.prefab", V(0f, 56f), 180f, One(2.6f), props);
Put(pr + "SM_Prop_Skeleton_Knight_Throne_01.prefab", V(-8f, 56.5f), 180f, One(1.2f), props);
Put(e + "Misc/SM_Env_Statue_0" + (1 + rng.Next(4)) + ".prefab", V(-14f, 57f), 160f, One(1.6f), props);
Put(e + "Misc/SM_Env_Statue_0" + (1 + rng.Next(4)) + ".prefab", V(14f, 57f), 200f, One(1.6f), props);

// coffins and bones in the side crypts, the hall's alcoves
foreach (var side in new[] { -1f, 1f })
{
    for (var i = 0; i < 4; i++) Put(pr + "SM_Prop_Coffin_01.prefab", V(side * (22f + i * 6f), -37f), side > 0 ? 0f : 180f, One(1.2f), props);
    for (var i = 0; i < 6; i++) { var bp = V(side * (18f + R() * 24f), -36f + R() * 24f); Put(ve + "SM_Env_Trim_Snow_0" + (1 + rng.Next(7)) + ".prefab", bp, R() * 360f, One(0.45f + R() * 0.25f), props); }
    Put(pr + "SM_Prop_Skeleton_Slave_Shackles_01.prefab", V(side * 44f, -20f), side > 0 ? 270f : 90f, One(1f), props);
}
for (var i = 0; i < 14; i++)
{
    var x = (R() - 0.5f) * 26f; var z = -48f + R() * 56f;
    Put(ve + "SM_Env_Trim_Snow_0" + (1 + rng.Next(7)) + ".prefab", V(x, z), R() * 360f, One(0.7f), props);
}
for (var i = 0; i < 10; i++) Put(pr + "SM_Prop_Bricks_0" + (1 + rng.Next(4)) + ".prefab", V((R() - 0.5f) * 50f, 14f + R() * 44f), R() * 360f, One(1f), props);
// what is left of something very big that died here long ago (a hint of floor 10)
Put(e + "Bones/SM_Env_Bone_Skull_01.prefab", V(-22f, 50f), 135f, One(0.7f), props);
// the stairs down from the halls above, behind the arrival point
Put(e + "Floors/SM_Env_Stairs_Large_01.prefab", V(0f, -79.5f), 0f, One(1f), props);

// the overgrowth: earth and grass breaking up through the floor, creeping vines over the low walls, grass at
// the wall feet, dead trees grown through the stones in corners
// grass breaking up between the flagstones in patches
for (var i = 0; i < 26; i++)
{
    var rr = rooms[rng.Next(rooms.Count)];
    var x = rr.x0 + 3f + R() * (rr.x1 - rr.x0 - 6f); var z = rr.z0 + 3f + R() * (rr.z1 - rr.z0 - 6f);
    if (rr.name == "Throne hall" && new UnityEngine.Vector2(x, z - 38f).magnitude < 12f) continue;
    for (var k = 0; k < 2; k++) Put(ve + "SM_Env_Trim_Snow_0" + (1 + rng.Next(7)) + ".prefab", V(x + (R() - 0.5f) * 3f, z + (R() - 0.5f) * 3f), R() * 360f, One(0.35f + R() * 0.25f), growth);
}
foreach (UnityEngine.Transform w in walls)
{
    if (rng.Next(3) != 0) continue;
    var v = Put(ve + (rng.Next(2) == 0 ? "SM_Env_Icicle_Small_Sloped_0" + (1 + rng.Next(2)) : "SM_Env_Icicle_Small_0" + (1 + rng.Next(3))) + ".prefab", root.transform.InverseTransformPoint(w.position), w.localEulerAngles.y, One(1.5f), growth, false);
    if (v) v.transform.position = w.position + UnityEngine.Vector3.up * 2.2f;
}
for (var i = 0; i < 90; i++)
{
    var rr = rooms[rng.Next(rooms.Count)];
    var side = rng.Next(4);
    var x = side < 2 ? rr.x0 + R() * (rr.x1 - rr.x0) : (side == 2 ? rr.x0 + 0.8f : rr.x1 - 0.8f);
    var z = side >= 2 ? rr.z0 + R() * (rr.z1 - rr.z0) : (side == 0 ? rr.z0 + 0.8f : rr.z1 - 0.8f);
    if (rng.Next(2) == 0) Put(ve + "SM_Env_Snow_Edge_0" + (1 + rng.Next(6)) + ".prefab", V(x, z), R() * 360f, One(0.6f + R() * 0.4f), growth);
}
foreach (var c in new[] { V(-41, -36), V(41, -14), V(-26, 56), V(26, 14), V(-7, -71) })
    Put(ve + "SM_Env_Snow_Edge_0" + (1 + rng.Next(6)) + ".prefab", c, R() * 360f, One(0.5f + R() * 0.3f), growth);
var mushroomSpots = new[] { V(-42, -37), V(42, -13), V(-27, 13), V(27, 57), V(-27, 57), V(-7, -72), V(12, -46), V(-12, 6), V(40, -37), V(-40, -13) };
foreach (var m in mushroomSpots)
{
    for (var k = 0; k < 4; k++)
    {
        var crystal = Put(ve + "SM_Env_Icicle_0" + (1 + rng.Next(4)) + ".prefab", m + V((R() - 0.5f) * 2.5f, (R() - 0.5f) * 2.5f), R() * 360f, One(1.6f + R() * 1.4f), growth, false);
        if (crystal) { crystal.transform.localRotation = UnityEngine.Quaternion.Euler(180f + (R() - 0.5f) * 30f, R() * 360f, (R() - 0.5f) * 30f); crystal.transform.localPosition += UnityEngine.Vector3.up * 0.2f; }
    }
}
// big roots breaking in over the walls of the throne hall
for (var i = 0; i < 12; i++)
{
    var a = R() * UnityEngine.Mathf.PI * 2f;
    var x = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.Cos(a) * 32f, -29f, 29f); var z = UnityEngine.Mathf.Clamp(35f + UnityEngine.Mathf.Sin(a) * 28f, 11f, 59f);
    var ic = Put(ve + "SM_Env_Icicle_0" + (1 + rng.Next(4)) + ".prefab", V(x, z), R() * 360f, One(2.2f + R()), growth, false);
    if (ic) ic.transform.localPosition = new UnityEngine.Vector3(x, 3.4f, z);
}

// rubble and cobwebs, statues watching down the hall
var dr = "Assets/PolygonDungeonRealms/Prefabs/Environments/";
for (var i = 0; i < 30; i++)
{
    var rr = rooms[rng.Next(rooms.Count)];
    var x = rr.x0 + 2f + R() * (rr.x1 - rr.x0 - 4f); var z = rr.z0 + 2f + R() * (rr.z1 - rr.z0 - 4f);
    if (rr.name == "Throne hall" && new UnityEngine.Vector2(x, z - 38f).magnitude < 12f) continue;
    Put(ve + "SM_Env_Stone_0" + (1 + rng.Next(4)) + ".prefab", V(x, z), R() * 360f, One(0.4f + R() * 0.35f), props);
}
foreach (var rr in rooms)
    foreach (var c in new[] { V(rr.x0 + 1f, rr.z0 + 1f), V(rr.x1 - 1f, rr.z1 - 1f), V(rr.x0 + 1f, rr.z1 - 1f), V(rr.x1 - 1f, rr.z0 + 1f) })
        if (rng.Next(9) == 0) Put(dr + "SM_Env_SpiderWeb_0" + (1 + rng.Next(6)) + ".prefab", c, R() * 360f, One(1.5f), growth, false)?.transform.SetLocalPositionAndRotation(c + UnityEngine.Vector3.up * 1.5f, UnityEngine.Quaternion.Euler(0f, R() * 360f, 0f));
for (var z = -40f; z <= 0f; z += 20f) { Put(e + "Misc/SM_Env_Statue_0" + (1 + rng.Next(4)) + ".prefab", V(-13.5f, z), 90f, One(1.3f), props); Put(e + "Misc/SM_Env_Statue_0" + (1 + rng.Next(4)) + ".prefab", V(13.5f, z + 10f), 270f, One(1.3f), props); }

// the floor a shade darker than the pack's, so the brazier light pools read
var tileRenderers = geo.GetComponentsInChildren<UnityEngine.MeshRenderer>();
var darkened = new System.Collections.Generic.Dictionary<UnityEngine.Material, UnityEngine.Material>();
foreach (var tr in tileRenderers)
{
    var mats = tr.sharedMaterials;
    for (var k = 0; k < mats.Length; k++)
    {
        var m0 = mats[k]; if (!m0) continue;
        if (!darkened.TryGetValue(m0, out var dm))
        {
            dm = new UnityEngine.Material(m0) { name = m0.name + " Vault" };
            var col = dm.HasProperty("_BaseColor") ? dm.GetColor("_BaseColor") : dm.color;
            col = new UnityEngine.Color(col.r * 0.78f, col.g * 0.88f, col.b * 1.08f, col.a);
            if (dm.HasProperty("_Smoothness")) dm.SetFloat("_Smoothness", 0.45f);
            if (dm.HasProperty("_BaseColor")) dm.SetColor("_BaseColor", col); else dm.color = col;
            var mp = dataFolder + "/" + dm.name.Replace(" ", "_") + ".mat";
            UnityEditor.AssetDatabase.DeleteAsset(mp); UnityEditor.AssetDatabase.CreateAsset(dm, mp);
            darkened[m0] = dm;
        }
        mats[k] = dm;
    }
    tr.sharedMaterials = mats;
}
log.AppendLine("darkened " + darkened.Count + " floor materials");

// light: braziers with warm light, candles on the dais, a cold glow from the mushrooms
var lightCount = 0;
void Brazier(float x, float z, float range, float intensity)
{
    Put(pr + "SM_Prop_Brazier_01.prefab", V(x, z), R() * 360f, One(2.2f), props);
    Put("Assets/Synty/PolygonDarkFortress/Prefabs/FX/FX_Fire_01.prefab", V(x, z), 0f, One(1.4f), props, false)?.transform.SetLocalPositionAndRotation(new UnityEngine.Vector3(x, 1.4f, z), UnityEngine.Quaternion.identity);
    var l = new UnityEngine.GameObject("Brazier Light"); l.transform.SetParent(lights, false); l.transform.localPosition = new UnityEngine.Vector3(x, 2.4f, z);
    var light = l.AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Point; light.color = new UnityEngine.Color(1f, 0.58f, 0.26f); light.range = range * 1.2f; light.intensity = intensity * 1.8f; light.shadows = UnityEngine.LightShadows.None;
    lightCount++;
}
Brazier(-6f, -64f, 14f, 2.2f); Brazier(6f, -64f, 14f, 2.2f);
for (var z = -40f; z <= 0f; z += 20f) { Brazier(-12f, z, 15f, 2f); Brazier(12f, z + 10f, 15f, 2f); }
Brazier(-30f, -32f, 16f, 2f); Brazier(30f, -18f, 16f, 2f);
Brazier(-10f, 52f, 16f, 2.6f); Brazier(10f, 52f, 16f, 2.6f); Brazier(-24f, 20f, 16f, 2f); Brazier(24f, 20f, 16f, 2f);
foreach (var m in mushroomSpots.Take(6))
{
    var l = new UnityEngine.GameObject("Crystal Glow"); l.transform.SetParent(lights, false); l.transform.localPosition = m + UnityEngine.Vector3.up * 2.5f;
    var light = l.AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Point; light.color = new UnityEngine.Color(0.55f, 0.8f, 1f); light.range = 10f; light.intensity = 1.6f; light.shadows = UnityEngine.LightShadows.None;
    lightCount++;
}
for (var i = 0; i < 6; i++) Put(pr + "SM_Prop_Candles_0" + (1 + rng.Next(4)) + ".prefab", V(-5f + i * 2f, 53f), R() * 360f, One(1.2f), props);
log.AppendLine("lights " + lightCount + ", placed " + placed);

// ---- arrival, boss and monster spots ----
UnityEngine.Vector3 W(float x, float z) => origin + new UnityEngine.Vector3(x, 0f, z);
var spawn = new UnityEngine.GameObject("Spawn Point"); spawn.transform.SetParent(root.transform, false);
spawn.transform.position = W(0f, -66f);
floor.SpawnPoint = spawn.transform;
var bossPoint = new UnityEngine.GameObject("Boss Point"); bossPoint.transform.SetParent(root.transform, false);
bossPoint.transform.position = W(0f, 38f);
bossPoint.transform.rotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
floor.BossPoint = bossPoint.transform;
var spawns = new UnityEngine.GameObject("Spawns").transform; spawns.SetParent(root.transform, false);
floor.Spawns = spawns;
var spots = new System.Collections.Generic.List<UnityEngine.Vector3>();
var areas = new[] { ("Hall", 10), ("West crypt", 5), ("East crypt", 5), ("Throne hall", 4) };
foreach (var (name, count) in areas)
{
    var r = rooms.First(x => x.name == name);
    var tries = 0; var made = 0;
    while (made < count && tries++ < 500)
    {
        var x = r.x0 + 4f + R() * (r.x1 - r.x0 - 8f); var z = r.z0 + 4f + R() * (r.z1 - r.z0 - 8f);
        if (name == "Hall" && UnityEngine.Mathf.Abs(UnityEngine.Mathf.Abs(x) - 9f) < 2.2f) continue; // pillars
        if (name == "Throne hall" && new UnityEngine.Vector2(x, z - 38f).magnitude < 14f) continue; // the boss's floor
        if (spots.Any(s => (s - V(x, z)).magnitude < 6f)) continue;
        spots.Add(V(x, z)); made++;
    }
}
foreach (var s in spots)
{
    var go = new UnityEngine.GameObject("Spot"); go.transform.SetParent(spawns, false);
    go.transform.position = W(s.x, s.z);
    go.transform.rotation = UnityEngine.Quaternion.Euler(0f, R() * 360f, 0f);
}
log.AppendLine("spots " + spots.Count);

// ---- monster templates, hidden (the same looks as the courtyard) ----
var templates = new UnityEngine.GameObject("Templates").transform; templates.SetParent(root.transform, false);
templates.gameObject.SetActive(false);
floor.Templates = templates;
var models = new[] { "iceheim-viking", "home-bunny", "ravebridge-skeleton", "ravebridge-tormented-soul", "ironhill-lord-quackers", "iceheim-viking-chief", "ironhill-dragon-white", "kyojin-samurai", "home-cow" };
var all = UnityEngine.Object.FindObjectsByType<EnemyController>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
foreach (var m in models)
{
    var source = all.FirstOrDefault(x => x.gameObject.scene == main && x.Definition && x.Definition.Definition != null && x.Definition.Definition.Id == m && !x.IsRaidBoss && !x.IsDungeonBoss);
    if (!source) { log.AppendLine("no enemy for " + m); continue; }
    var copy = UnityEngine.Object.Instantiate(source.gameObject);
    copy.name = m;
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, scene);
    copy.transform.SetParent(templates, false);
    copy.transform.localPosition = UnityEngine.Vector3.zero;
    copy.transform.localRotation = UnityEngine.Quaternion.identity;
    copy.SetActive(true);
    copy.GetComponent<EnemyController>().Island = null;
}

// ---- navmesh: the floor's own, from the invisible slabs, walls and pillars ----
var surface = root.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.agentTypeID = 0;
surface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
surface.layerMask = 25556799;
surface.defaultArea = 0;
templates.gameObject.SetActive(false);
surface.BuildNavMesh();
var navPath = dataFolder + "/NavMesh.asset";
if (surface.navMeshData)
{
    UnityEditor.AssetDatabase.DeleteAsset(navPath);
    UnityEditor.AssetDatabase.CreateAsset(surface.navMeshData, navPath);
    log.AppendLine("navmesh bounds " + surface.navMeshData.sourceBounds);
}
else log.AppendLine("navmesh build gave nothing");
// a quick check: a path from the arrival to the boss, and to each crypt
foreach (var target in new[] { W(0f, 38f), W(-30f, -25f), W(30f, -25f) })
{
    var path = new UnityEngine.AI.NavMeshPath();
    UnityEngine.AI.NavMesh.SamplePosition(W(0f, -66f), out var a, 3f, UnityEngine.AI.NavMesh.AllAreas);
    UnityEngine.AI.NavMesh.SamplePosition(target, out var b, 3f, UnityEngine.AI.NavMesh.AllAreas);
    UnityEngine.AI.NavMesh.CalculatePath(a.position, b.position, UnityEngine.AI.NavMesh.AllAreas, path);
    log.AppendLine("path to " + (target - origin) + ": " + path.status);
}

var scenePath = folder + "/Tower_FrostVault.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception ex) { log.AppendLine("ERROR " + ex); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-frostvault-build.txt"), log.ToString());
return null;
