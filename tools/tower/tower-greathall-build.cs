// Builds Assets/Scenes/Tower/Tower_GreatHall.unity: the band 4 (the Dark Fortress, v0.9.8) great hall, the first floor of
// rooms (docs/tower-design.md section 22, concept docs/mockups/tower/band4/floor-b-great-hall.jpg). A route of five
// rooms the group fights through one after another, each sealed with ghostfire until the one before is cleared:
//   0 the Gatehall (where players arrive, the skull gate they came in by behind them),
//   1 the Hall of Arms (a gallery of knights in armor),
//   2 the Chapel (pews, an altar, a pool of soul water),
//   3 the Great Hall (the hold: feast tables, the violet rune circle, moonbeams through the windows; it refills),
//   4 the Throne Room (the boss's, opened when the boss is called).
// Dark Fortress walls 6 m high, cut down to stubs where they stand between the camera and the fight (TowerCutaway); no
// roof. Dark stone, crimson banners, green ghostfire, cold moonlight, the rune circle's violet glow.
// Shapes the props do not have (the wall stubs, the rune circle) come from tools/tower/blender/tower_great_hall.py.
// At (0, 0, -6000) like every floor. Not in the climb yet: the debug menu's "Tower: Great Hall" loads it for any floor.
// Writes %TEMP%/tower-greathall-build.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var origin = new UnityEngine.Vector3(0f, 0f, -6000f);
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/Tower_GreatHall";
if (!UnityEditor.AssetDatabase.IsValidFolder(dataFolder)) UnityEditor.AssetDatabase.CreateFolder(folder, "Tower_GreatHall");

var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);

var root = new UnityEngine.GameObject("Tower Floor - The Great Hall");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.position = origin;
var floor = root.AddComponent<TowerFloor>();
floor.Radius = 110f;
// night in the fortress: cold moonlight from high up, a violet-blue dark, the fog the colour of the shadows
floor.SkyAmbient = new UnityEngine.Color(0.48f, 0.47f, 0.72f);
floor.EquatorAmbient = new UnityEngine.Color(0.32f, 0.29f, 0.46f);
floor.GroundAmbient = new UnityEngine.Color(0.08f, 0.07f, 0.10f);
floor.FogColor = new UnityEngine.Color(0.08f, 0.07f, 0.12f);
floor.FogDensity = 0.008f;
// a few pale ghost lights drifting about (Ambience's fireflies)
floor.Fireflies = 2.5f;
floor.SunColor = new UnityEngine.Color(0.62f, 0.68f, 0.95f);
floor.SunIntensity = 1.1f;
floor.SunAngles = new UnityEngine.Vector3(58f, 35f, 0f);

var DF = "Assets/Synty/PolygonDarkFortress/Prefabs/";
var HM = "Assets/Synty/PolygonHorrorMansion/Prefabs/Props/";
var AE = "Assets/PolygonAncientEmpire/Prefabs/Props/Banners/";
var PD = "Assets/PolygonDungeon/Prefabs/Environments/";
var placed = 0;

UnityEngine.GameObject Put(string path, UnityEngine.Vector3 local, float yaw, UnityEngine.Vector3 scale, UnityEngine.Transform parent, bool centre = true)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { log.AppendLine("missing " + path); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    go.transform.SetParent(parent, true);
    // the prefab's own root turn and size kept under ours (a Synty or Blender root may be turned to stand up)
    go.transform.localScale = UnityEngine.Vector3.Scale(prefab.transform.localScale, scale);
    go.transform.rotation = root.transform.rotation * UnityEngine.Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;
    go.transform.position = root.transform.TransformPoint(local);
    if (centre)
    {
        var rs = go.GetComponentsInChildren<UnityEngine.Renderer>().Where(r => !(r is UnityEngine.ParticleSystemRenderer)).ToArray();
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
UnityEngine.Vector3 V3(float x, float y, float z) => new UnityEngine.Vector3(x, y, z);
UnityEngine.Vector3 One(float s) => UnityEngine.Vector3.one * s;
UnityEngine.Transform Group(string name, UnityEngine.Transform parent) { var g = new UnityEngine.GameObject(name).transform; g.SetParent(parent, false); return g; }

var geo = Group("Floors", root.transform);
var walls = Group("Walls", root.transform);
var props = Group("Props", root.transform);
var lights = Group("Lights", root.transform);
var fxRoot = Group("Fx", root.transform); fxRoot.gameObject.AddComponent<TowerFx>();
var nav = Group("Walkable", root.transform);
var zonesRoot = Group("Zones", root.transform);

// ---- materials: the pack's stone darkened and cooled toward the concept's slate ----
var darkened = new System.Collections.Generic.Dictionary<UnityEngine.Material, UnityEngine.Material>();
UnityEngine.Material Dark(UnityEngine.Material m0, UnityEngine.Color tint)
{
    if (!m0) return m0;
    if (darkened.TryGetValue(m0, out var dm)) return dm;
    var path = dataFolder + "/" + m0.name.Replace(" ", "_") + "_Hall.mat";
    dm = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (!dm) { dm = new UnityEngine.Material(m0); UnityEditor.AssetDatabase.CreateAsset(dm, path); }
    else dm.CopyPropertiesFromMaterial(m0);
    dm.name = m0.name + "_Hall";
    var col = dm.HasProperty("_BaseColor") ? dm.GetColor("_BaseColor") : dm.color;
    col = new UnityEngine.Color(col.r * tint.r, col.g * tint.g, col.b * tint.b, col.a);
    if (dm.HasProperty("_BaseColor")) dm.SetColor("_BaseColor", col);
    if (dm.HasProperty("_Color")) dm.SetColor("_Color", col);
    UnityEditor.EditorUtility.SetDirty(dm);
    darkened[m0] = dm;
    return dm;
}
// lighter than the first pass: the concept's floor is a pale slate under the moon, the walls a darker one
var stoneTint = new UnityEngine.Color(0.56f, 0.56f, 0.66f);
var floorTint = new UnityEngine.Color(0.66f, 0.64f, 0.78f);
void DarkenAll(UnityEngine.GameObject go, UnityEngine.Color tint)
{
    if (!go) return;
    foreach (var r in go.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
    {
        var ms = r.sharedMaterials;
        for (var i = 0; i < ms.Length; i++) ms[i] = Dark(ms[i], tint);
        r.sharedMaterials = ms;
    }
}
UnityEngine.Material OwnMat(string name, UnityEngine.Color color, UnityEngine.Color glow, float smooth = 0.1f)
{
    var path = dataFolder + "/" + name + ".mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (!m) { m = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit")); UnityEditor.AssetDatabase.CreateAsset(m, path); }
    m.SetColor("_BaseColor", color);
    m.SetFloat("_Smoothness", smooth);
    if (glow.maxColorComponent > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", glow); m.globalIlluminationFlags = UnityEngine.MaterialGlobalIlluminationFlags.None; }
    else m.DisableKeyword("_EMISSION");
    UnityEditor.EditorUtility.SetDirty(m);
    return m;
}
var capMat = OwnMat("WallCap", new UnityEngine.Color(0.13f, 0.12f, 0.15f), UnityEngine.Color.black);
var runeMat = OwnMat("RuneGlow", new UnityEngine.Color(0.45f, 0.16f, 0.75f), new UnityEngine.Color(3.2f, 0.9f, 6.4f));
// green ghostfire: the pack's own fire with its green flames (no prefab of the pack uses them)
var greenFlames = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Synty/PolygonDarkFortress/Materials/FX/Fire/FireFrames_Green_01.mat");
var emberSrc = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Synty/PolygonDarkFortress/Materials/FX/Fire/Ember_01.mat");
UnityEngine.Material greenEmbers = null;
if (emberSrc)
{
    var ep = dataFolder + "/Ember_Green.mat";
    greenEmbers = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(ep);
    if (!greenEmbers) { greenEmbers = new UnityEngine.Material(emberSrc); UnityEditor.AssetDatabase.CreateAsset(greenEmbers, ep); }
    else greenEmbers.CopyPropertiesFromMaterial(emberSrc);
    foreach (var prop in new[] { "_BaseColor", "_Color" }) if (greenEmbers.HasProperty(prop)) greenEmbers.SetColor(prop, new UnityEngine.Color(0.5f, 3.2f, 1.2f, 1f));
    if (greenEmbers.HasProperty("_EmissionColor")) greenEmbers.SetColor("_EmissionColor", new UnityEngine.Color(0.1f, 1.2f, 0.4f));
    UnityEditor.EditorUtility.SetDirty(greenEmbers);
}
var ghostGreen = new UnityEngine.Color(0.35f, 1f, 0.55f);

// ---- the rooms: x on a 2.5 + 5k grid, z on 5k, so every wall is whole 5 m pieces and doors sit on one ----
var rooms = new System.Collections.Generic.List<(string name, float x0, float z0, float x1, float z1)>
{
    ("Gatehall", -12.5f, -95f, 12.5f, -75f),
    ("Hall of Arms", -7.5f, -75f, 7.5f, -55f),
    ("Chapel", 7.5f, -75f, 27.5f, -55f),
    ("Great Hall", -17.5f, -55f, 27.5f, -15f),
    ("Throne Room", -12.5f, -15f, 22.5f, 15f),
};
bool InRoom(float x, float z, float m = 0f) => rooms.Any(r => x > r.x0 + m && x < r.x1 - m && z > r.z0 + m && z < r.z1 - m);
// the doors, by the middle of their wall piece: room i's exit into room i + 1
var doors = new[] { V(0f, -75f), V(7.5f, -62.5f), V(20f, -55f), V(5f, -15f) };
string Key(float x, float z) => UnityEngine.Mathf.RoundToInt(x * 2f) + "," + UnityEngine.Mathf.RoundToInt(z * 2f);
int DoorAt(float x, float z) { for (var i = 0; i < doors.Length; i++) if (UnityEngine.Mathf.Abs(doors[i].x - x) < 0.1f && UnityEngine.Mathf.Abs(doors[i].z - z) < 0.1f) return i; return -1; }

// ---- floors: the dungeon pack's 5 m stone tiles, darkened; a doorway's threshold tile too ----
var tileRng = new System.Random(41);
var plain = new[] { "SM_Env_Tiles_01", "SM_Env_Tiles_02", "SM_Env_Tiles_03", "SM_Env_Tiles_04", "SM_Env_Tiles_05", "SM_Env_Tiles_06", "SM_Env_Tiles_07", "SM_Env_Tiles_08" };
foreach (var r in rooms)
{
    for (var x = r.x0; x < r.x1 - 0.1f; x += 5)
        for (var z = r.z0; z < r.z1 - 0.1f; z += 5)
        {
            var cx = x + 2.5f; var cz = z + 2.5f;
            var tile = r.name == "Throne Room" && UnityEngine.Mathf.Abs(cx - 5f) < 6f ? "SM_Env_Tiles_Ornate_0" + (1 + tileRng.Next(2)) : plain[tileRng.Next(plain.Length)];
            var tg = Put(PD + "Floors/" + tile + ".prefab", V(cx, cz), 90f * tileRng.Next(4), One(1f), geo);
            DarkenAll(tg, floorTint);
            // the tiles' tops 2 cm over the walkable floor (the pack's stand 18 cm proud: feet sank into them)
            if (tg) { var trs = tg.GetComponentsInChildren<UnityEngine.Renderer>(); var tb = trs[0].bounds; foreach (var q in trs) tb.Encapsulate(q.bounds); tg.transform.position += UnityEngine.Vector3.up * (origin.y + 0.02f - tb.max.y); }
        }
    var slab = new UnityEngine.GameObject("Floor " + r.name); slab.transform.SetParent(nav, false);
    slab.transform.localPosition = new UnityEngine.Vector3((r.x0 + r.x1) / 2f, -0.25f, (r.z0 + r.z1) / 2f);
    var bc = slab.AddComponent<UnityEngine.BoxCollider>(); bc.size = new UnityEngine.Vector3(r.x1 - r.x0, 0.5f, r.z1 - r.z0);
}
foreach (var d in doors)
{
    var s = new UnityEngine.GameObject("Doorway"); s.transform.SetParent(nav, false); s.transform.localPosition = new UnityEngine.Vector3(d.x, -0.25f, d.z);
    var b = s.AddComponent<UnityEngine.BoxCollider>(); b.size = new UnityEngine.Vector3(4f, 0.5f, 4f);
}

// ---- walls: every 5 m piece once, a cut-away piece each (TowerWallPiece: the full wall and its stub) ----
var stubA = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Tower/GreatHall/Models/GreatHallWallStub.fbx");
var stubB = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Tower/GreatHall/Models/GreatHallWallStubB.fbx");
var stubArch = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Tower/GreatHall/Models/GreatHallArchStub.fbx");
if (!stubA || !stubArch) log.AppendLine("missing stubs (run tools/tower/blender/tower_great_hall.py)");

// a stub in the place of a full piece: turned and sized until its footprint matches the piece's, the pack's materials
// by slot (the Blender cut kept the slot order), the cut faces dark stone
void Stub(UnityEngine.GameObject model, UnityEngine.GameObject fullGo, UnityEngine.Transform parent, UnityEngine.Vector3 local, float yaw)
{
    if (!model || !fullGo) return;
    var fr = fullGo.GetComponentsInChildren<UnityEngine.MeshRenderer>();
    var fb = fr[0].bounds; foreach (var r in fr) fb.Encapsulate(r.bounds);
    UnityEngine.GameObject best = null; var bestErr = float.MaxValue;
    foreach (var turn in new[] { 0f, 90f, 180f, 270f })
    {
        var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model, scene);
        go.transform.SetParent(parent, true);
        go.transform.rotation = root.transform.rotation * UnityEngine.Quaternion.Euler(0f, yaw + turn, 0f) * model.transform.localRotation;
        go.transform.position = root.transform.TransformPoint(local);
        var rs = go.GetComponentsInChildren<UnityEngine.MeshRenderer>();
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        // sized by its length (an FBX in centimetres comes in a hundred times too big)
        var len = UnityEngine.Mathf.Max(b.size.x, b.size.z); var want = UnityEngine.Mathf.Max(fb.size.x, fb.size.z);
        if (len > 0.01f && UnityEngine.Mathf.Abs(len / want - 1f) > 0.05f) go.transform.localScale *= want / len;
        b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.position += new UnityEngine.Vector3(fb.center.x - b.center.x, fb.min.y - b.min.y, fb.center.z - b.center.z);
        b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        var err = UnityEngine.Mathf.Abs(b.size.x - fb.size.x) + UnityEngine.Mathf.Abs(b.size.z - fb.size.z);
        if (err < bestErr) { if (best) UnityEngine.Object.DestroyImmediate(best); best = go; bestErr = err; }
        else UnityEngine.Object.DestroyImmediate(go);
    }
    best.name = "Low";
    {
        // a stub must stand a metre or so high: anything else is turned wrong (one came out lying beside every door)
        var rs = best.GetComponentsInChildren<UnityEngine.MeshRenderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        if (b.size.y < 0.5f || b.size.y > 2f) log.AppendLine("STUB TURNED WRONG " + model.name + " at " + local + " size " + b.size);
    }
    var fullMats = fr[0].sharedMaterials;
    foreach (var r in best.GetComponentsInChildren<UnityEngine.MeshRenderer>())
    {
        var ms = r.sharedMaterials;
        for (var i = 0; i < ms.Length; i++)
        {
            var n = ms[i] ? ms[i].name.ToLowerInvariant() : "";
            // by the FBX's own names (the arch's stub is a plain wall's, so its slots are not the arch's)
            UnityEngine.Material pick = null;
            if (n.StartsWith("cap")) pick = capMat;
            else if (n.Contains("brick_large")) pick = fullMats.FirstOrDefault(m => m && m.name.StartsWith("Brick_Large"));
            else if (n.Contains("mat_01a")) pick = fullMats.FirstOrDefault(m => m && m.name.StartsWith("PolygonDarkFortress_Mat_01_A"));
            ms[i] = pick ? pick : fullMats.FirstOrDefault(m => m && m.name.StartsWith("Brick")) ?? fullMats[UnityEngine.Mathf.Min(i, fullMats.Length - 1)];
        }
        r.sharedMaterials = ms;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // hidden until the camera looks over its wall (TowerWallPiece shows it), so the editor shows the rooms as built
        r.enabled = false;
    }
}

var wallRng = new System.Random(7);
var done = new System.Collections.Generic.HashSet<string>();
var pieces = new System.Collections.Generic.List<(UnityEngine.Transform piece, UnityEngine.Vector3 mid, UnityEngine.Vector3 inward, bool outer, int door)>();
var gates = new TowerGate[doors.Length];
var plainWalls = new[] { "SM_Bld_Wall_L_01", "SM_Bld_Wall_L_01", "SM_Bld_Wall_L_02", "SM_Bld_Wall_L_03" };
var windowWalls = new[] { "SM_Bld_Wall_L_Window_01", "SM_Bld_Wall_L_Window_02" };
foreach (var r in rooms)
{
    void Edge(float ax, float az, float bx, float bz)
    {
        var len = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(bx - ax), UnityEngine.Mathf.Abs(bz - az));
        var alongX = UnityEngine.Mathf.Abs(bx - ax) > 0.1f;
        for (var t = 0f; t < len - 0.1f; t += 5f)
        {
            var mx = ax + (bx - ax) * (t + 2.5f) / len; var mz = az + (bz - az) * (t + 2.5f) / len;
            if (!done.Add(Key(mx, mz))) continue;
            // the side the room is on (into this room) and whether another room is behind it
            var n = alongX ? new UnityEngine.Vector3(0f, 0f, 1f) : new UnityEngine.Vector3(1f, 0f, 0f);
            if (!InRoom(mx + n.x * 1.5f, mz + n.z * 1.5f)) n = -n;
            var outer = !InRoom(mx - n.x * 1.5f, mz - n.z * 1.5f);
            var door = DoorAt(mx, mz);
            var yaw = alongX ? 0f : 90f;
            var piece = new UnityEngine.GameObject((door >= 0 ? "Door " : "Wall ") + mx + "," + mz).transform;
            piece.SetParent(walls, false);
            piece.localPosition = V(mx, mz);
            piece.localRotation = UnityEngine.Quaternion.LookRotation(n);
            var full = Group("Full", piece);
            string kind;
            if (door >= 0) kind = "SM_Bld_Wall_L_Arch_01";
            else if (outer && wallRng.Next(3) == 0) kind = windowWalls[wallRng.Next(windowWalls.Length)];
            else kind = plainWalls[wallRng.Next(plainWalls.Length)];
            var w = Put(DF + "Buildings/" + kind + ".prefab", V(mx, mz), yaw, One(1f), full);
            DarkenAll(w, stoneTint);
            var low = Group("LowRoot", piece);
            Stub(door >= 0 ? stubArch : (wallRng.Next(2) == 0 ? stubA : (stubB ? stubB : stubA)), w, low, V(mx, mz), yaw);
            var wp = piece.gameObject.AddComponent<TowerWallPiece>();
            wp.Full = full.gameObject; wp.Low = low.gameObject; wp.HalfLength = 2.5f;
            if (door < 0)
            {
                var col = new UnityEngine.GameObject("Wall"); col.transform.SetParent(nav, false);
                col.transform.localPosition = new UnityEngine.Vector3(mx, 1.5f, mz);
                col.transform.localRotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
                var bc = col.AddComponent<UnityEngine.BoxCollider>(); bc.size = new UnityEngine.Vector3(5.2f, 3f, 0.9f);
            }
            else
            {
                // the doorway's sides (the arch's piers) still block
                foreach (var side in new[] { -2.1f, 2.1f })
                {
                    var col = new UnityEngine.GameObject("Door Pier"); col.transform.SetParent(nav, false);
                    var along = alongX ? new UnityEngine.Vector3(1f, 0f, 0f) : new UnityEngine.Vector3(0f, 0f, 1f);
                    col.transform.localPosition = new UnityEngine.Vector3(mx, 1.5f, mz) + along * side * 0.88f;
                    col.transform.localRotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
                    var bc = col.AddComponent<UnityEngine.BoxCollider>(); bc.size = new UnityEngine.Vector3(0.9f, 3f, 0.9f);
                }
            }
            pieces.Add((piece, V(mx, mz), n, outer, door));
        }
    }
    Edge(r.x0, r.z0, r.x1, r.z0);
    Edge(r.x0, r.z1, r.x1, r.z1);
    Edge(r.x0, r.z0, r.x0, r.z1);
    Edge(r.x1, r.z0, r.x1, r.z1);
}
log.AppendLine("wall pieces " + pieces.Count);

// pillars at every joint of the walls, each its own cut-away piece (their stub the pillar's foot)
var joints = new System.Collections.Generic.Dictionary<string, (UnityEngine.Vector3 at, UnityEngine.Vector3 n)>();
foreach (var p in pieces)
{
    var along = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, p.inward).normalized;
    foreach (var e in new[] { -2.5f, 2.5f })
    {
        var at = p.mid + along * e;
        var k = Key(at.x, at.z);
        if (!joints.ContainsKey(k)) joints[k] = (at, p.inward);
    }
}
foreach (var j in joints.Values)
{
    var piece = new UnityEngine.GameObject("Pillar " + j.at.x + "," + j.at.z).transform;
    piece.SetParent(walls, false);
    piece.localPosition = j.at;
    piece.localRotation = UnityEngine.Quaternion.LookRotation(j.n);
    var full = Group("Full", piece);
    var pg = Put(DF + "Buildings/SM_Bld_L_Pillar_01.prefab", j.at, UnityEngine.Quaternion.LookRotation(j.n).eulerAngles.y, One(1.05f), full);
    DarkenAll(pg, stoneTint);
    var low = Group("LowRoot", piece);
    var foot = Put(DF + "Buildings/SM_Bld_L_Pillar_01.prefab", j.at, UnityEngine.Quaternion.LookRotation(j.n).eulerAngles.y, new UnityEngine.Vector3(1.05f, 0.2f, 1.05f), low);
    DarkenAll(foot, stoneTint);
    if (foot) foreach (var r in foot.GetComponentsInChildren<UnityEngine.Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.enabled = false; }
    var wp = piece.gameObject.AddComponent<TowerWallPiece>();
    wp.Full = full.gameObject; wp.Low = low.gameObject; wp.HalfLength = 0.6f;
}
log.AppendLine("pillars " + joints.Count);

// ---- the gates: the arch's opening shut by two door leaves and sealed with ghostfire ----
var fireRng = new System.Random(5);
UnityEngine.GameObject GreenFire(UnityEngine.Vector3 local, float scale, UnityEngine.Transform parent)
{
    var f = Put(DF + "FX/FX_Fire_01.prefab", local, 0f, One(scale), parent, false);
    if (!f) return null;
    foreach (var pr in f.GetComponentsInChildren<UnityEngine.ParticleSystemRenderer>(true))
    {
        var ms = pr.sharedMaterials;
        for (var i = 0; i < ms.Length; i++)
        {
            if (!ms[i]) continue;
            if (ms[i].name.StartsWith("FireFrames") && greenFlames) ms[i] = greenFlames;
            else if (ms[i].name.StartsWith("Ember") && greenEmbers) ms[i] = greenEmbers;
        }
        pr.sharedMaterials = ms;
    }
    return f;
}
for (var i = 0; i < doors.Length; i++)
{
    var d = doors[i];
    var piece = pieces.First(p => p.door == i);
    // the next room's side: room i + 1 is where the leaves swing to
    var next = rooms[i + 1];
    var toNext = piece.inward;
    if (!(d.x + toNext.x * 2f > next.x0 && d.x + toNext.x * 2f < next.x1 && d.z + toNext.z * 2f > next.z0 && d.z + toNext.z * 2f < next.z1)) toNext = -toNext;
    var gateGo = new UnityEngine.GameObject("Gate " + i + " (" + rooms[i].name + " to " + next.name + ")");
    gateGo.transform.SetParent(props, false);
    gateGo.transform.localPosition = d;
    gateGo.transform.localRotation = UnityEngine.Quaternion.LookRotation(toNext);
    var gate = gateGo.AddComponent<TowerGate>();
    // leaves hinged at the opening's sides, swinging into the next room
    var leaves = new System.Collections.Generic.List<UnityEngine.Transform>();
    var yaws = new System.Collections.Generic.List<float>();
    foreach (var side in new[] { -1f, 1f })
    {
        var hinge = new UnityEngine.GameObject(side < 0 ? "Left Leaf" : "Right Leaf").transform;
        hinge.SetParent(gateGo.transform, false);
        // the arch's opening is about 2.8 m at the floor: two leaves of 1.4 m
        hinge.localPosition = new UnityEngine.Vector3(side * 1.4f, 0f, 0f);
        var leaf = Put(DF + "Buildings/SM_Bld_Door_Large_01.prefab", UnityEngine.Vector3.zero, 0f, new UnityEngine.Vector3(0.74f, 0.82f, 1f), hinge, false);
        if (leaf)
        {
            leaf.transform.SetParent(hinge, false);
            // the leaf's own pivot is its right edge, the door running to -x: the right leaf as it is, the left turned
            var leafRoot = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(DF + "Buildings/SM_Bld_Door_Large_01.prefab").transform.localRotation;
            leaf.transform.localRotation = UnityEngine.Quaternion.Euler(0f, side > 0 ? 0f : 180f, 0f) * leafRoot;
            leaf.transform.localPosition = UnityEngine.Vector3.zero;
            leaf.transform.localScale = new UnityEngine.Vector3(0.74f, 0.82f, 1f);
        }
        leaves.Add(hinge);
        // turning about +y moves -x toward +z (the next room) for the right leaf; the left the other way
        yaws.Add(side > 0 ? 100f : -100f);
    }
    gate.Leaves = leaves.ToArray();
    gate.OpenYaw = yaws.ToArray();
    var seal = new UnityEngine.GameObject("Seal"); seal.transform.SetParent(gateGo.transform, false);
    for (var k = -1; k <= 1; k++) GreenFire(root.transform.InverseTransformPoint(gateGo.transform.TransformPoint(new UnityEngine.Vector3(k * 1.1f, 0.1f, -0.6f))), 0.9f, seal.transform);
    var sl = new UnityEngine.GameObject("Seal Light"); sl.transform.SetParent(gateGo.transform, false); sl.transform.localPosition = new UnityEngine.Vector3(0f, 2f, -1.2f);
    var light = sl.AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Point; light.color = ghostGreen; light.range = 9f; light.intensity = 3f; light.shadows = UnityEngine.LightShadows.None;
    gate.Seal = seal;
    gate.SealLight = light;
    var obstacle = gateGo.AddComponent<UnityEngine.AI.NavMeshObstacle>();
    obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
    obstacle.size = new UnityEngine.Vector3(3.6f, 3f, 1.2f);
    obstacle.center = new UnityEngine.Vector3(0f, 1.5f, 0f);
    obstacle.carving = true;
    obstacle.carveOnlyStationary = false;
    gates[i] = gate;
}

// ---- the rooms' dressing ----
var propRng = new System.Random(13);
float PR() => (float)propRng.NextDouble();
var blockers = new System.Collections.Generic.List<(UnityEngine.Vector3 at, float r)>();
void Block(UnityEngine.Vector3 at, UnityEngine.Vector3 size, float yaw)
{
    var c = new UnityEngine.GameObject("Prop Blocker"); c.transform.SetParent(nav, false);
    c.transform.localPosition = at + UnityEngine.Vector3.up * size.y / 2f;
    c.transform.localRotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
    var bc = c.AddComponent<UnityEngine.BoxCollider>(); bc.size = size;
    blockers.Add((at, UnityEngine.Mathf.Max(size.x, size.z) * 0.6f + 1f));
}
var lightCount = 0;
UnityEngine.Light PointLight(UnityEngine.Vector3 local, UnityEngine.Color color, float range, float intensity, UnityEngine.Transform parent)
{
    var l = new UnityEngine.GameObject("Light"); l.transform.SetParent(parent, false); l.transform.localPosition = local;
    var light = l.AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Point; light.color = color; light.range = range; light.intensity = intensity; light.shadows = UnityEngine.LightShadows.None;
    lightCount++;
    return light;
}
// a brazier of ghostfire: the bowl, its green flames (Fx: off in potato mode) and its light (always)
void Ghostfire(float x, float z, bool big)
{
    Put(DF + "Props/" + (big ? "SM_Prop_Brazier_03" : "SM_Prop_Brazier_05") + ".prefab", V(x, z), PR() * 360f, One(big ? 1.6f : 1.5f), props);
    GreenFire(V3(x, big ? 1.9f : 1.65f, z), big ? 1.2f : 0.8f, fxRoot);
    PointLight(V3(x, 2.6f, z), ghostGreen, big ? 13f : 10f, big ? 3.2f : 2.4f, lights);
    Block(V(x, z), new UnityEngine.Vector3(1.4f, 1.5f, 1.4f), 0f);
}
// a banner hung on the inner face of a wall piece (it goes with the wall when the wall is cut)
var bannerRng = new System.Random(19);
void Banner(UnityEngine.Transform piece, UnityEngine.Vector3 mid, UnityEngine.Vector3 inward, bool crimson)
{
    var full = piece.Find("Full");
    var path = crimson ? AE + (bannerRng.Next(2) == 0 ? "SM_Prop_Banner_01_A" : "SM_Prop_Banner_02_A") + ".prefab" : DF + "Props/SM_Prop_Banner_04.prefab";
    var at = mid + inward * 0.45f;
    var yaw = UnityEngine.Quaternion.LookRotation(-inward).eulerAngles.y + 180f;
    var b = Put(path, at, yaw, One(crimson ? 1.9f : 1.3f), full, true);
    if (!b) return;
    // hung from 5.5 m
    var rs = b.GetComponentsInChildren<UnityEngine.Renderer>();
    var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
    b.transform.position += UnityEngine.Vector3.up * (origin.y + 5.6f - bb.max.y);
}
foreach (var p in pieces)
{
    if (p.door >= 0) continue;
    var roll = bannerRng.Next(10);
    if (roll < 4) Banner(p.piece, p.mid, p.inward, true);
    else if (roll < 5) Banner(p.piece, p.mid, p.inward, false);
    // the shared walls get the other side's banner now and then too
    if (!p.outer && bannerRng.Next(4) == 0) Banner(p.piece, p.mid, -p.inward, true);
}

// moonbeams through the windows of the outer walls (TowerLightShaft, off in potato mode), with the wall
// only the windows the moon shines in by (all beams parallel, along the moonlight), down to the floor
var shafts = 0;
var moon = UnityEngine.Quaternion.Euler(floor.SunAngles) * UnityEngine.Vector3.forward;
var moonFlat = new UnityEngine.Vector3(moon.x, 0f, moon.z).normalized;
foreach (var p in pieces.Where(p => p.outer && p.door < 0))
{
    var full = p.piece.Find("Full");
    var name = full.childCount > 0 ? full.GetChild(0).name : "";
    if (!name.Contains("Window") || UnityEngine.Vector3.Dot(p.inward, moonFlat) < 0.35f) continue;
    var s = new UnityEngine.GameObject("Moonbeam"); s.transform.SetParent(full, false);
    s.transform.position = root.transform.TransformPoint(p.mid + p.inward * 0.35f + UnityEngine.Vector3.up * 3.4f);
    s.transform.rotation = UnityEngine.Quaternion.LookRotation(moon, UnityEngine.Vector3.up);
    var shaft = s.AddComponent<TowerLightShaft>();
    shaft.Width = 1.3f; shaft.Height = 2.2f; shaft.Length = 3.4f / UnityEngine.Mathf.Max(0.3f, -moon.y) + 1.5f; shaft.Spread = 1.35f;
    shaft.Tint = new UnityEngine.Color(0.66f, 0.75f, 1f, 0.10f);
    shafts++;
}
log.AppendLine("moonbeams " + shafts);

// 0, the Gatehall: the skull gate the players came in by (in the place of the wall piece behind the arrival, so it is
// cut away like the rest), braziers either side, rubble and the fallen
{
    var behind = pieces.First(p => UnityEngine.Mathf.Abs(p.mid.x) < 0.1f && UnityEngine.Mathf.Abs(p.mid.z + 95f) < 0.1f);
    var full = behind.piece.Find("Full");
    for (var k = full.childCount - 1; k >= 0; k--) UnityEngine.Object.DestroyImmediate(full.GetChild(k).gameObject);
    DarkenAll(Put(DF + "Buildings/SM_Bld_Wall_Door_Large_01.prefab", V(0f, -95f), 0f, One(1f), full), stoneTint);
}
Ghostfire(-4.5f, -91.5f, true); Ghostfire(4.5f, -91.5f, true);
Ghostfire(-10f, -77.5f, false); Ghostfire(10f, -77.5f, false);
foreach (var (x, z) in new[] { (-9.5f, -86f), (8.5f, -88.5f), (-6f, -79f) }) Put(DF + "Environment/SM_Env_Rubble_Pile_0" + (1 + propRng.Next(3)) + ".prefab", V(x, z), PR() * 360f, One(1.2f), props);
Put(DF + "Props/SM_Prop_Barricade_01.prefab", V(9f, -82f), 75f, One(1.1f), props); Block(V(9f, -82f), new UnityEngine.Vector3(3f, 1.5f, 1.2f), 75f);
Put(DF + "Props/SM_Prop_Corpse_0" + (1 + propRng.Next(9)) + ".prefab", V(-8f, -83f), PR() * 360f, One(1f), props);
Put(DF + "Props/SM_Prop_Corpse_0" + (1 + propRng.Next(9)) + ".prefab", V(6.5f, -78.5f), PR() * 360f, One(1f), props);
Put(DF + "Props/SM_Prop_Cage_01.prefab", V(-10.5f, -92f), 30f, One(1f), props); Block(V(-10.5f, -92f), new UnityEngine.Vector3(1.6f, 2f, 1.6f), 30f);

// 1, the Hall of Arms: knights in armor down both sides, a crimson runner, candles
for (var z = -72f; z <= -58f; z += 4.5f)
    foreach (var side in new[] { -1f, 1f })
    {
        var x = side * 6.1f;
        if (side > 0 && UnityEngine.Mathf.Abs(z + 62.5f) < 3f) continue; // the chapel's door
        Put(DF + "Props/SM_Prop_Plinth_01.prefab", V(x, z), side > 0 ? 270f : 90f, One(1.3f), props);
        var st = Put(DF + "Props/SM_Prop_Statue_0" + (new[] { 1, 3, 4 })[propRng.Next(3)] + ".prefab", V(x, z), side > 0 ? 270f : 90f, One(1.25f), props);
        if (st) st.transform.position += UnityEngine.Vector3.up * 1.05f;
        Block(V(x, z), new UnityEngine.Vector3(1.2f, 2f, 1.2f), 0f);
    }
for (var z = -73.25f; z <= -56f; z += 3.5f) { var rug = Put(HM + "SM_Prop_Rug_Tileable_01.prefab", V(0f, z), 0f, new UnityEngine.Vector3(1.4f, 1f, 1.4f), props); if (rug) rug.transform.position += UnityEngine.Vector3.up * 0.03f; }
Ghostfire(0f, -57.5f, false);

// 2, the Chapel: pews facing an altar at the east wall, a pool of soul water, coffins, candles
Put(DF + "Props/SM_Prop_Altar_01.prefab", V(25.5f, -63.5f), 90f, One(1.5f), props); Block(V(25.5f, -63.5f), new UnityEngine.Vector3(1.8f, 1.2f, 3.4f), 0f);
foreach (var sz in new[] { -70.5f, -56.5f })
{
    Put(DF + "Props/SM_Prop_Plinth_01.prefab", V(26f, sz), 270f, One(1.3f), props);
    var st = Put(DF + "Props/SM_Prop_Statue_03.prefab", V(26f, sz), 270f, One(1.25f), props);
    if (st) st.transform.position += UnityEngine.Vector3.up * 1.05f;
    Block(V(26f, sz), new UnityEngine.Vector3(1.2f, 2f, 1.2f), 0f);
}
foreach (var dz in new[] { -2.4f, 2.4f }) { Put(DF + "Props/SM_Prop_Candle_Stand_02.prefab", V(24.3f, -63.5f + dz), 0f, One(1.4f), props); PointLight(V3(24.3f, 1.6f, -63.5f + dz), new UnityEngine.Color(1f, 0.7f, 0.4f), 6f, 1.2f, fxRoot); }
// a small font of soul water before the altar (the pack's pool, its glow kept low: full size it read as green slime)
Put(DF + "Environment/SM_Env_Spirit_Pool_01.prefab", V(22f, -63.5f), 0f, new UnityEngine.Vector3(0.38f, 0.5f, 0.38f), props); Block(V(22f, -63.5f), new UnityEngine.Vector3(2.2f, 0.5f, 2.2f), 0f);
PointLight(V3(22f, 1f, -63.5f), ghostGreen, 6f, 1.4f, lights);
for (var px = 13f; px <= 19f; px += 3f)
    foreach (var pz in new[] { -69.5f, -57.5f })
    {
        Put(DF + "Props/SM_Prop_Bench_01.prefab", V(px, pz), 90f, new UnityEngine.Vector3(1.4f, 1.1f, 1.2f), props);
        Put(DF + "Props/SM_Prop_Bench_01.prefab", V(px, pz + (pz < -63.5f ? 2.4f : -2.4f)), 90f, new UnityEngine.Vector3(1.4f, 1.1f, 1.2f), props);
        Block(V(px, pz + (pz < -63.5f ? 1.2f : -1.2f)), new UnityEngine.Vector3(1f, 0.8f, 4.6f), 0f);
    }
Put(DF + "Props/SM_Prop_Coffin_01.prefab", V(9.8f, -73f), 90f, One(1.1f), props);
Put(DF + "Props/SM_Prop_Coffin_01_Damaged_01.prefab", V(9.8f, -57.2f), 90f, One(1.1f), props);
Ghostfire(22.5f, -73f, false); Ghostfire(22.5f, -57f, false);

// 3, the Great Hall: the rune circle in the middle, feast tables down both sides, knights along the walls
var hallMid = V(5f, -35f);
var circle = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Tower/GreatHall/Models/GreatHallRuneCircle.fbx");
if (circle)
{
    var c = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(circle, scene);
    c.name = "Rune Circle";
    c.transform.SetParent(props, false);
    c.transform.localPosition = hallMid + UnityEngine.Vector3.up * 0.06f;
    // the export's up axis: turned until it lies flat
    var flattest = float.MaxValue; var flatTurn = UnityEngine.Quaternion.identity;
    foreach (var e in new[] { UnityEngine.Quaternion.identity, UnityEngine.Quaternion.Euler(-90f, 0f, 0f), UnityEngine.Quaternion.Euler(90f, 0f, 0f), UnityEngine.Quaternion.Euler(0f, 0f, 90f) })
    {
        c.transform.localRotation = e;
        var cr = c.GetComponentsInChildren<UnityEngine.MeshRenderer>(); var cb = cr[0].bounds; foreach (var q in cr) cb.Encapsulate(q.bounds);
        if (cb.size.y < flattest) { flattest = cb.size.y; flatTurn = e; }
    }
    c.transform.localRotation = flatTurn;
    { var cr = c.GetComponentsInChildren<UnityEngine.MeshRenderer>(); var cb = cr[0].bounds; foreach (var q in cr) cb.Encapsulate(q.bounds); c.transform.position += new UnityEngine.Vector3(0f, origin.y + 0.05f - cb.min.y, 0f); }
    foreach (var r in c.GetComponentsInChildren<UnityEngine.MeshRenderer>())
    {
        var ms = r.sharedMaterials; for (var i = 0; i < ms.Length; i++) ms[i] = runeMat; r.sharedMaterials = ms;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
else log.AppendLine("missing rune circle (run tools/tower/blender/tower_great_hall.py)");
PointLight(hallMid + UnityEngine.Vector3.up * 1.5f, new UnityEngine.Color(0.65f, 0.3f, 1f), 14f, 2.6f, lights);
var feast = new[] { "SM_Prop_Food_01", "SM_Prop_Food_02", "SM_Prop_Food_03", "SM_Prop_Food_04", "SM_Prop_Goblet_01", "SM_Prop_Goblet_02", "SM_Prop_Plate_01", "SM_Prop_Plate_02" };
foreach (var tx in new[] { -10.5f, 20.5f })
{
    for (var tz = -44f; tz <= -28f; tz += 8f)
    {
        // a long table of two, benches either side, food and candles, now and then a chair knocked over
        Put(DF + "Props/SM_Prop_Table_03.prefab", V(tx, tz), 90f, One(1.15f), props);
        Put(DF + "Props/SM_Prop_Table_03.prefab", V(tx, tz + 3.9f), 90f, One(1.15f), props);
        foreach (var bs in new[] { -1.5f, 1.5f }) { Put(DF + "Props/SM_Prop_Bench_01.prefab", V(tx + bs, tz), 90f, new UnityEngine.Vector3(2f, 1f, 1f), props); Put(DF + "Props/SM_Prop_Bench_01.prefab", V(tx + bs, tz + 3.9f), 90f, new UnityEngine.Vector3(2f, 1f, 1f), props); }
        Block(V(tx, tz + 1.95f), new UnityEngine.Vector3(3.8f, 1f, 8f), 0f);
        for (var k = 0; k < 5; k++)
        {
            var item = Put(DF + "Props/" + feast[propRng.Next(feast.Length)] + ".prefab", V(tx + (PR() - 0.5f) * 0.8f, tz - 1.3f + PR() * 6.4f), PR() * 360f, One(1.1f), props);
            if (item) item.transform.position += UnityEngine.Vector3.up * 1.1f;
        }
        var cs = Put(HM + "SM_Prop_Candle_Stand_02.prefab", V(tx, tz + 1.95f), PR() * 360f, One(1.6f), props);
        if (cs) cs.transform.position += UnityEngine.Vector3.up * 1.1f;
        PointLight(V3(tx, 2.4f, tz + 1.95f), new UnityEngine.Color(1f, 0.68f, 0.38f), 7f, 1.3f, fxRoot);
    }
}
foreach (var (x, z) in new[] { (-7.8f, -44f), (18f, -30f), (-13f, -17.5f) }) Put(DF + "Props/SM_Prop_Chair_01_Damaged_01.prefab", V(x, z), PR() * 360f, One(1.1f), props);
// knights along the west and east walls between the windows, ghostfire at the hall's corners and by the throne room's door
foreach (var z in new[] { -50f, -40f, -30f, -20f })
{
    foreach (var x in new[] { -16.2f, 26.2f })
    {
        Put(DF + "Props/SM_Prop_Plinth_01.prefab", V(x, z), x < 0 ? 90f : 270f, One(1.3f), props);
        var st = Put(DF + "Props/SM_Prop_Statue_0" + (new[] { 1, 3, 4 })[propRng.Next(3)] + ".prefab", V(x, z), x < 0 ? 90f : 270f, One(1.25f), props);
        if (st) st.transform.position += UnityEngine.Vector3.up * 1.05f;
        Block(V(x, z), new UnityEngine.Vector3(1.2f, 2f, 1.2f), 0f);
    }
}
Ghostfire(-14.5f, -52f, true); Ghostfire(24.5f, -52f, true); Ghostfire(-14.5f, -18f, true); Ghostfire(24.5f, -18f, true);
Ghostfire(0.5f, -17.5f, false); Ghostfire(9.5f, -17.5f, false);
foreach (var (x, z) in new[] { (-4f, -27f), (14f, -45f), (12f, -22f), (-2f, -50f) }) Put(DF + "Environment/SM_Env_Rubble_Brick_0" + (1 + propRng.Next(4)) + ".prefab", V(x, z), PR() * 360f, One(1.1f), props);

// 4, the Throne Room: a crimson runner to the throne on its dais, banners, the biggest fires
var throne = Put(DF + "Props/SM_Prop_Throne_01.prefab", V(5f, 12f), 180f, One(2.4f), props);
if (throne) throne.transform.position += UnityEngine.Vector3.up * 0.3f;
Block(V(5f, 12f), new UnityEngine.Vector3(4f, 3f, 2.6f), 0f);
for (var z = -13f; z <= 8f; z += 4f) { var rug = Put(HM + "SM_Prop_Rug_Tileable_01.prefab", V(5f, z), 0f, new UnityEngine.Vector3(1.6f, 1f, 1.6f), props); if (rug) rug.transform.position += UnityEngine.Vector3.up * 0.03f; }
Ghostfire(-1.5f, 10f, true); Ghostfire(11.5f, 10f, true);
Ghostfire(-9.5f, -11.5f, true); Ghostfire(19.5f, -11.5f, true);
foreach (var x in new[] { -6f, 16f })
{
    Put(DF + "Props/SM_Prop_Statue_0" + (new[] { 1, 3 })[propRng.Next(2)] + ".prefab", V(x, 12.5f), 180f, One(1.9f), props);
    Block(V(x, 12.5f), new UnityEngine.Vector3(1.4f, 2.5f, 1.4f), 0f);
}
foreach (var (x, z) in new[] { (-10f, 4f), (20f, -2f), (-8f, 13f) }) Put(DF + "Props/SM_Prop_Skull_Pile_0" + (1 + propRng.Next(3)) + ".prefab", V(x, z), PR() * 360f, One(1.2f), props);

// the void round the rooms: a dark floor just under them, so a cut wall shows darkness, never the sky
var voidPlane = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane);
voidPlane.name = "Void"; UnityEngine.Object.DestroyImmediate(voidPlane.GetComponent<UnityEngine.Collider>());
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(voidPlane, scene);
voidPlane.transform.SetParent(root.transform, false);
voidPlane.transform.localPosition = new UnityEngine.Vector3(5f, -0.3f, -40f);
voidPlane.transform.localScale = new UnityEngine.Vector3(40f, 1f, 40f);
var voidMat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Unlit"));
voidMat.color = new UnityEngine.Color(0.018f, 0.016f, 0.026f);
UnityEditor.AssetDatabase.DeleteAsset(dataFolder + "/Void.mat");
UnityEditor.AssetDatabase.CreateAsset(voidMat, dataFolder + "/Void.mat");
voidPlane.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = voidMat;
voidPlane.GetComponent<UnityEngine.MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
log.AppendLine("lights " + lightCount + ", placed " + placed);

// ---- the route: rooms with their arrival, monster spots and exit gate ----
UnityEngine.Vector3 W(float x, float z) => origin + new UnityEngine.Vector3(x, 0f, z);
var route = root.AddComponent<TowerRoute>();
var entries = new[] { V(0f, -90f), V(0f, -72f), V(10.5f, -62.5f), V(20f, -51.5f), V(5f, -11f) };
var counts = new[] { 5, 4, 5, 10, 0 };
var spotRng = new System.Random(29);
var zoneList = new System.Collections.Generic.List<TowerZone>();
var allSpots = new System.Collections.Generic.List<UnityEngine.Vector3>();
for (var i = 0; i < rooms.Count; i++)
{
    var r = rooms[i];
    var zgo = new UnityEngine.GameObject("Zone " + i + " - " + r.name); zgo.transform.SetParent(zonesRoot, false);
    var zone = zgo.AddComponent<TowerZone>();
    zone.Title = r.name;
    var entry = new UnityEngine.GameObject("Entry"); entry.transform.SetParent(zgo.transform, false);
    entry.transform.position = root.transform.TransformPoint(entries[i]);
    // facing into the room, toward its middle
    var mid = V((r.x0 + r.x1) / 2f, (r.z0 + r.z1) / 2f);
    entry.transform.rotation = UnityEngine.Quaternion.LookRotation((mid - entries[i]).sqrMagnitude > 0.1f ? (mid - entries[i]) : UnityEngine.Vector3.forward);
    zone.Entry = entry.transform;
    var spawns = new UnityEngine.GameObject("Spawns"); spawns.transform.SetParent(zgo.transform, false);
    zone.Spawns = spawns.transform;
    zone.Exit = i < gates.Length ? gates[i] : null;
    var made = 0; var tries = 0;
    while (made < counts[i] && tries++ < 800)
    {
        var x = r.x0 + 3f + (float)spotRng.NextDouble() * (r.x1 - r.x0 - 6f);
        var z = r.z0 + 3f + (float)spotRng.NextDouble() * (r.z1 - r.z0 - 6f);
        var at = V(x, z);
        if (blockers.Any(b => (b.at - at).magnitude < b.r)) continue;
        if ((at - entries[i]).magnitude < 6f) continue;
        if (allSpots.Any(s => (s - at).magnitude < 4.5f)) continue;
        if (i == 3 && (at - hallMid).magnitude < 2.5f) continue;
        allSpots.Add(at); made++;
        var spot = new UnityEngine.GameObject("Spot"); spot.transform.SetParent(spawns.transform, false);
        spot.transform.position = root.transform.TransformPoint(at);
        spot.transform.rotation = UnityEngine.Quaternion.Euler(0f, (float)spotRng.NextDouble() * 360f, 0f);
    }
    log.AppendLine("zone " + i + " " + r.name + " spots " + made);
    zoneList.Add(zone);
}
route.Zones = zoneList.ToArray();
floor.Route = route;
// the arrival when the route is not in play (a test scene opened by hand): the first room's
var spawn = new UnityEngine.GameObject("Spawn Point"); spawn.transform.SetParent(root.transform, false);
spawn.transform.position = W(0f, -90f);
floor.SpawnPoint = spawn.transform;
// the floor's own Spawns: every room's spots (the audit walks them; the route spawns from the rooms)
var allSpawns = new UnityEngine.GameObject("Spawns"); allSpawns.transform.SetParent(root.transform, false);
floor.Spawns = allSpawns.transform;
var bossPoint = new UnityEngine.GameObject("Boss Point"); bossPoint.transform.SetParent(root.transform, false);
bossPoint.transform.position = W(5f, 3f);
bossPoint.transform.rotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
floor.BossPoint = bossPoint.transform;
root.AddComponent<TowerCutaway>();

// ---- monster templates, hidden (band 1's looks and the Dark Fortress dead, so any floor of 1 to 10 can try it) ----
var templates = new UnityEngine.GameObject("Templates").transform; templates.SetParent(root.transform, false);
templates.gameObject.SetActive(false);
floor.Templates = templates;
var models = new[] { "home-bunny", "ravebridge-goblin", "home-metalon-green", "home-metalon-purple", "home-metalon-red", "ravebridge-skeleton", "home-cow", "ironhill-dragon-green" };
var allEnemies = UnityEngine.Object.FindObjectsByType<EnemyController>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
foreach (var m in models)
{
    var source = allEnemies.FirstOrDefault(x => x.gameObject.scene == main && x.Definition && x.Definition.Definition != null && x.Definition.Definition.Id == m && !x.IsRaidBoss && !x.IsDungeonBoss);
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
foreach (var dfId in new[] { "df-undead", "df-undead-soldier", "df-corpse", "df-wraith", "df-grave-knight", "df-behemoth", "df-inquisitor", "df-necromancer", "df-colossus", "df-plague", "df-plague-lord", "df-apothecary", "df-barbarian" })
{
    var dfPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Enemies/DarkFortress/" + dfId + ".prefab");
    if (!dfPrefab) { log.AppendLine("no prefab for " + dfId); continue; }
    var dfCopy = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(dfPrefab, scene);
    dfCopy.name = dfId;
    dfCopy.transform.SetParent(templates, false);
    dfCopy.transform.localPosition = UnityEngine.Vector3.zero;
    dfCopy.transform.localRotation = UnityEngine.Quaternion.identity;
}

// ---- navmesh: from the invisible slabs, walls and blockers only (the props' own colliders stay out of it) ----
var surface = nav.gameObject.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.agentTypeID = 0;
surface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
surface.layerMask = ~0;
surface.defaultArea = 0;
foreach (var g in gates) if (g) g.GetComponent<UnityEngine.AI.NavMeshObstacle>().enabled = false;
surface.BuildNavMesh();
// saved open (the audit walks the whole route); TowerFloor.Apply shuts them (TowerRoute.ResetRoute) when it fills the floor
var navPath = dataFolder + "/NavMesh.asset";
if (surface.navMeshData)
{
    UnityEditor.AssetDatabase.DeleteAsset(navPath);
    UnityEditor.AssetDatabase.CreateAsset(surface.navMeshData, navPath);
    log.AppendLine("navmesh bounds " + surface.navMeshData.sourceBounds);
}
else log.AppendLine("navmesh build gave nothing");
// the way through: arrival to every room's entry and to the boss
UnityEngine.AI.NavMesh.SamplePosition(W(0f, -90f), out var startHit, 3f, UnityEngine.AI.NavMesh.AllAreas);
foreach (var target in entries.Concat(new[] { V(5f, 3f) }))
{
    var path = new UnityEngine.AI.NavMeshPath();
    UnityEngine.AI.NavMesh.SamplePosition(origin + target, out var b, 3f, UnityEngine.AI.NavMesh.AllAreas);
    UnityEngine.AI.NavMesh.CalculatePath(startHit.position, b.position, UnityEngine.AI.NavMesh.AllAreas, path);
    log.AppendLine("path to " + target + ": " + path.status);
}
// the spots for the audit, copied under the floor's own Spawns after the bake
foreach (var z in zoneList) foreach (UnityEngine.Transform s in z.Spawns) { var c = new UnityEngine.GameObject("Spot " + z.Title); c.transform.SetParent(allSpawns.transform, false); c.transform.position = s.position; }

var scenePath = folder + "/Tower_GreatHall.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + scenePath);
} catch (System.Exception ex) { log.AppendLine("ERROR " + ex); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-greathall-build.txt"), log.ToString());
return null;
