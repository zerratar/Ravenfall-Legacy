// Gives the crypt and the frost vault their route of rooms (docs/tower-design.md section 22, docs/tower-worlds-proposal.md
// section 13): both layouts are the same five rooms, already rooms without doors. This adds, to the saved scene, a gated
// doorway between the rooms (a stone frame, two leaves, the wall closed to its sides, a seal and a carving obstacle),
// the rooms as TowerZones with their arrival and monster spots, and the TowerRoute on the floor. The seal is a curtain
// of light across the whole doorway (TowerWard: what says "shut" from the game's camera, the same at every sealed
// door) with the place's own barrier standing in it: a hedge of thorns in the crypt, a wall of ice in the vault:
//   0 the Entry (the arrival, 3 monsters)  ->  1 the Hall (8)  ->  2 the West crypt (5)  ->  3 the East crypt (8, the
//   hold: it refills)  ->  4 the Throne hall (the guardian's room, opened when an attempt is called).
// The scene named in %TEMP%/tower-route-scene.txt ("Tower_Crypt" or "Tower_FrostVault") is opened beside the open scene,
// changed, saved and closed. Run it after the layout's own builder (tower-crypt-build.cs, tower-frostvault-build.cs); it
// can be run again: what it made before goes first. The baked navmesh is not touched: the new wall pieces carve it
// (NavMeshObstacle), and the gates' obstacles are saved disabled, so the audit walks the whole route.
// The walls of these two layouts are low (the camera looks over them) and the gates stand no higher than a tall wall
// piece, so nothing is cut away here (no TowerCutaway). Writes %TEMP%/tower-rooms-route.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
try {
var layout = System.IO.File.ReadAllText(System.IO.Path.Combine(tmp, "tower-route-scene.txt")).Trim();
var frost = layout == "Tower_FrostVault";
var folder = "Assets/Scenes/Tower";
var dataFolder = folder + "/" + layout;
var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(folder + "/" + layout + ".unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
var floor = scene.GetRootGameObjects().Select(g => g.GetComponent<TowerFloor>()).First(f => f);
var root = floor.transform;
var origin = root.position;
log.AppendLine(layout + " root " + root.name + " at " + origin.ToString("F0"));

var e = "Assets/PolygonDungeon/Prefabs/Environments/";
var ve = "Assets/Synty/PolygonVikingRealm/Prefabs/Environment/";
var rng = new System.Random(frost ? 77 : 41);
float R() => (float)rng.NextDouble();
UnityEngine.Vector3 V(float x, float z) => new UnityEngine.Vector3(x, 0f, z);
UnityEngine.Transform Group(string name, UnityEngine.Transform parent) { var g = new UnityEngine.GameObject(name).transform; g.SetParent(parent, false); return g; }

// ---- what an earlier run made goes; its monster spots go back to the floor's own list first ----
var spawns = floor.Spawns ? floor.Spawns : Group("Spawns", root);
floor.Spawns = spawns;
var old = root.Find("Route");
if (old)
{
    foreach (var zone in old.GetComponentsInChildren<TowerZone>(true))
        if (zone.Spawns) foreach (var spot in zone.Spawns.Cast<UnityEngine.Transform>().ToList()) spot.SetParent(spawns, true);
    UnityEngine.Object.DestroyImmediate(old.gameObject);
}
foreach (var extra in spawns.Cast<UnityEngine.Transform>().Where(t => t.name.StartsWith("Route Spot")).ToList()) UnityEngine.Object.DestroyImmediate(extra.gameObject);
foreach (var r in root.GetComponents<TowerRoute>()) UnityEngine.Object.DestroyImmediate(r);
foreach (var c in root.GetComponents<TowerCutaway>()) UnityEngine.Object.DestroyImmediate(c);

var route = Group("Route", root);
var gatesRoot = Group("Gates", route);

// the rooms (local metres, as the builders lay them out)
var rooms = new (string name, string title, float x0, float z0, float x1, float z1)[]
{
    ("Entry", "The Entry", -10, -75, 10, -50),
    ("Hall", "The Hall", -15, -50, 15, 10),
    ("West crypt", frost ? "The West Vault" : "The West Crypt", -45, -40, -15, -10),
    ("East crypt", frost ? "The East Vault" : "The East Crypt", 15, -40, 45, -10),
    ("Throne hall", "The Throne Hall", -30, 10, 30, 60),
};
int RoomOf(UnityEngine.Vector3 local)
{
    for (var i = 0; i < rooms.Length; i++)
        if (local.x > rooms[i].x0 && local.x < rooms[i].x1 && local.z > rooms[i].z0 && local.z < rooms[i].z1) return i;
    return -1;
}

// ---- prefabs, without their own colliders (the navmesh and the camera's rays know the invisible ones only) ----
UnityEngine.GameObject Put(string path, UnityEngine.Transform parent, UnityEngine.Vector3 local, UnityEngine.Quaternion rotation, UnityEngine.Vector3 scale)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { log.AppendLine("missing " + path); return null; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    go.transform.SetParent(parent, false);
    go.transform.localPosition = local;
    go.transform.localRotation = rotation * prefab.transform.localRotation;
    go.transform.localScale = UnityEngine.Vector3.Scale(prefab.transform.localScale, scale);
    foreach (var c in go.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
    return go;
}

// ---- the seal's materials: this script's own, so the scenes' other materials stay as they are ----
UnityEngine.Material Mat(string name, System.Action<UnityEngine.Material> set, string shader)
{
    var path = dataFolder + "/" + name + ".mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    var s = UnityEngine.Shader.Find(shader);
    if (!m) { m = new UnityEngine.Material(s); UnityEditor.AssetDatabase.CreateAsset(m, path); }
    m.shader = s;
    set(m);
    UnityEditor.EditorUtility.SetDirty(m);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(m);
    return m;
}
// the frost vault's seal: pale ice with a glow from within (the vault's own ice shader), lighter than the vault's
// blue ice so the door does not look like one more ice prop
var sealIce = frost ? Mat("GateIce", m =>
{
    m.SetColor("_SurfaceColor", new UnityEngine.Color(0.86f, 0.96f, 1f));
    m.SetColor("_DeepColor", new UnityEngine.Color(0.22f, 0.5f, 0.78f));
    m.SetColor("_GlowColor", new UnityEngine.Color(0.45f, 0.8f, 1f));
    m.SetFloat("_Glow", 1.6f);
}, "Ravenfall/Ice") : null;
// an earlier version's wisp material is not used any more
{
    var oldWard = dataFolder + "/GateWard.mat";
    if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(oldWard)) UnityEditor.AssetDatabase.DeleteAsset(oldWard);
}
var sealColor = frost ? new UnityEngine.Color(0.6f, 0.86f, 1f) : new UnityEngine.Color(0.45f, 1f, 0.62f);
// the curtain of light: a cold white-blue in the vault, the crypt's grave green
var wardTint = frost ? new UnityEngine.Color(0.62f, 0.9f, 1f, 0.75f) : new UnityEngine.Color(0.4f, 1f, 0.58f, 0.62f);

// ---- gates ----
// a doorway: its middle, the way through it (toward the next room on the route), how wide the opening in the wall is
const float GateScale = 0.72f;          // the pack's large door frame is 5.9 m wide and 5.6 m high: here 4.3 by 4.0
const float FrameHalf = 2.97f * GateScale;
const float LeafWidth = 2.05f * GateScale;
var wallKinds = new[] { "SM_Env_Wall_01", "SM_Env_Wall_02", "SM_Env_Wall_03", "SM_Env_Wall_04", "SM_Env_Wall_05" };
var obstacles = new System.Collections.Generic.List<UnityEngine.AI.NavMeshObstacle>();

TowerGate Gate(string name, UnityEngine.Vector3 at, UnityEngine.Vector3 through, float opening)
{
    var gateGo = new UnityEngine.GameObject(name);
    gateGo.transform.SetParent(gatesRoot, false);
    gateGo.transform.localPosition = at;
    gateGo.transform.localRotation = UnityEngine.Quaternion.LookRotation(through);
    var gate = gateGo.AddComponent<TowerGate>();
    gate.Seconds = 2.2f;

    // the frame, and the wall closed between it and the doorway's sides: low pieces like the rooms' walls, each with
    // an invisible block that cuts the navmesh
    Put(e + "Walls/SM_Env_Door_Large_Frame_01.prefab", gateGo.transform, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.one * GateScale);
    foreach (var side in new[] { -1f, 1f })
    {
        var from = FrameHalf - 0.25f;           // a little under the frame's edge, so no gap shows
        var to = opening / 2f + 0.1f;
        var gap = to - from;
        var count = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.CeilToInt(gap / 5.2f));
        var each = gap / count;
        for (var k = 0; k < count; k++)
        {
            var mid = side * (from + each * (k + 0.5f));
            var height = 0.5f + R() * 0.12f;
            // two pieces back to back: a wall piece has its bricks on one face, and both rooms see this wall
            foreach (var turn in new[] { 0f, 180f })
            {
                var piece = Put(e + "Walls/" + wallKinds[rng.Next(wallKinds.Length)] + ".prefab", gateGo.transform, UnityEngine.Vector3.zero, UnityEngine.Quaternion.Euler(0f, turn, 0f), new UnityEngine.Vector3(each / 5.2f, height, 1f));
                if (!piece) continue;
                // the piece's pivot is at one end: placed by the middle of what it looks like
                var rs = piece.GetComponentsInChildren<UnityEngine.Renderer>();
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var want = gateGo.transform.TransformPoint(new UnityEngine.Vector3(mid, 0f, turn > 0f ? -0.12f : 0.12f));
                var off = want - b.center; off.y = 0f;
                piece.transform.position += off;
            }
            var block = new UnityEngine.GameObject("Wall Block"); block.transform.SetParent(gateGo.transform, false);
            block.transform.localPosition = new UnityEngine.Vector3(mid, 1.5f, 0f);
            var o = block.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            o.shape = UnityEngine.AI.NavMeshObstacleShape.Box; o.size = new UnityEngine.Vector3(each + 0.3f, 3f, 0.9f); o.carving = true; o.carveOnlyStationary = true;
            var bc = block.AddComponent<UnityEngine.BoxCollider>(); bc.size = new UnityEngine.Vector3(each, 3f, 0.8f);
        }
        // the frame's own posts stand on the floor too
        var post = new UnityEngine.GameObject("Frame Block"); post.transform.SetParent(gateGo.transform, false);
        post.transform.localPosition = new UnityEngine.Vector3(side * (LeafWidth + (FrameHalf - LeafWidth) / 2f), 1.5f, 0f);
        var po = post.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        po.shape = UnityEngine.AI.NavMeshObstacleShape.Box; po.size = new UnityEngine.Vector3(FrameHalf - LeafWidth + 0.2f, 3f, 1.2f); po.carving = true; po.carveOnlyStationary = true;
        var pc = post.AddComponent<UnityEngine.BoxCollider>(); pc.size = new UnityEngine.Vector3(FrameHalf - LeafWidth, 3f, 1.1f);
    }

    // two leaves hinged at the opening's sides, swinging into the next room. The pack's stone door is a pair: leaf 01
    // has its pivot at its right edge and runs to -x (the right leaf), leaf 02 its pivot at its left edge and runs to
    // +x (the left leaf). (Until 2026-11-24 the vault had leaf 02 on both hinges, placed as if it were leaf 01: its
    // doors stood open behind the ice, over the walls to both sides.)
    var leaves = new System.Collections.Generic.List<UnityEngine.Transform>();
    var yaws = new System.Collections.Generic.List<float>();
    foreach (var side in new[] { -1f, 1f })
    {
        var hinge = Group(side < 0 ? "Left Leaf" : "Right Leaf", gateGo.transform);
        hinge.localPosition = new UnityEngine.Vector3(side * LeafWidth, 0f, 0f);
        Put(e + "Walls/SM_Env_Door_Large_Stone_0" + (side > 0 ? "1" : "2") + ".prefab", hinge, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity, UnityEngine.Vector3.one * GateScale);
        leaves.Add(hinge);
        // away from the group, into the next room
        yaws.Add(side > 0 ? 100f : -100f);
    }
    gate.Leaves = leaves.ToArray();
    gate.OpenYaw = yaws.ToArray();

    // the seal, on the side the group comes from: it shrinks away as the door opens. First the curtain of light over
    // the whole doorway, as high as the leaves, close before them
    var seal = Group("Seal", gateGo.transform);
    seal.localPosition = new UnityEngine.Vector3(0f, 0f, -0.75f);
    var leafTop = 3.4f;
    {
        var lr = leaves[0].GetComponentsInChildren<UnityEngine.Renderer>();
        if (lr.Length > 0) { var lb = lr[0].bounds; foreach (var r in lr) lb.Encapsulate(r.bounds); leafTop = lb.max.y - origin.y; }
    }
    var wardGo = Group("Ward", seal);
    wardGo.localPosition = new UnityEngine.Vector3(0f, 0f, 0.38f);
    var ward = wardGo.gameObject.AddComponent<TowerWard>();
    // a little narrower than the two leaves: wider, its edges showed from the next room through the crack between
    // a leaf and the frame
    ward.Width = LeafWidth * 2f - 0.1f;
    // no higher than the leaves: over them it showed from the next room
    ward.Height = leafTop;
    ward.Apron = 2.2f;
    ward.Tint = wardTint;
    // a prop scaled so that it stands this high, its foot on the floor
    void Stand(UnityEngine.GameObject go, float high)
    {
        if (!go) return;
        var rs = go.GetComponentsInChildren<UnityEngine.Renderer>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        if (b.size.y > 0.01f) go.transform.localScale *= high / b.size.y;
        b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.position += UnityEngine.Vector3.up * (origin.y - 0.05f - b.min.y);
    }
    if (frost)
    {
        // the door frozen shut: a wall of pale ice across it, tall crystals shoulder to shoulder with shorter ones
        // before them (the pack's icicles, point up)
        for (var row = 0; row < 2; row++)
        {
            var count = row == 0 ? 7 : 6;
            for (var k = 0; k < count; k++)
            {
                var x = (row == 0 ? -1.5f : -1.25f) + k * 0.5f + (R() - 0.5f) * 0.12f;
                var high = row == 0 ? (k % 2 == 0 ? leafTop * 0.95f : leafTop * 0.74f) + R() * 0.3f : 1.1f + R() * 0.7f;
                var crystal = Put(ve + "SM_Env_Icicle_0" + (1 + rng.Next(4)) + ".prefab", seal, new UnityEngine.Vector3(x, 0f, row == 0 ? 0.1f + (R() - 0.5f) * 0.15f : -0.4f + (R() - 0.5f) * 0.2f),
                    UnityEngine.Quaternion.Euler(180f + (R() - 0.5f) * 10f, R() * 360f, (R() - 0.5f) * 10f), UnityEngine.Vector3.one);
                if (!crystal) continue;
                foreach (var r in crystal.GetComponentsInChildren<UnityEngine.MeshRenderer>()) { r.sharedMaterials = r.sharedMaterials.Select(_ => sealIce).ToArray(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                Stand(crystal, high);
            }
        }
    }
    else
    {
        // a hedge of thorns grown across the door, as high as a player and higher at its middle, dark against the
        // grave light behind it
        for (var row = 0; row < 2; row++)
        {
            var count = row == 0 ? 5 : 4;
            for (var k = 0; k < count; k++)
            {
                var x = (row == 0 ? -1.45f : -1.1f) + k * 0.72f + (R() - 0.5f) * 0.15f;
                var high = row == 0 ? 2.3f + (k == 2 ? 0.6f : 0f) + R() * 0.5f : 1.3f + R() * 0.5f;
                var bush = Put("Assets/PolygonDungeonRealms/Prefabs/Environments/SM_Env_Bramble_0" + (1 + rng.Next(3)) + ".prefab", seal, new UnityEngine.Vector3(x, 0f, row == 0 ? 0.05f + (R() - 0.5f) * 0.15f : -0.45f + (R() - 0.5f) * 0.2f),
                    UnityEngine.Quaternion.Euler(0f, R() * 360f, 0f), UnityEngine.Vector3.one);
                if (!bush) continue;
                foreach (var r in bush.GetComponentsInChildren<UnityEngine.MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Stand(bush, high);
            }
        }
    }
    // the seal's light, well before the door (close to it, it shone through into the next room)
    var sl = new UnityEngine.GameObject("Seal Light"); sl.transform.SetParent(gateGo.transform, false); sl.transform.localPosition = new UnityEngine.Vector3(0f, 1.6f, -2.6f);
    var light = sl.AddComponent<UnityEngine.Light>(); light.type = UnityEngine.LightType.Point; light.color = sealColor; light.range = 6.5f; light.intensity = frost ? 3.4f : 3f; light.shadows = UnityEngine.LightShadows.None;
    gate.Seal = seal.gameObject;
    gate.SealLight = light;

    // shut, it cuts the navmesh across the doorway (saved disabled: the game shuts it when it fills the floor)
    var obstacle = gateGo.AddComponent<UnityEngine.AI.NavMeshObstacle>();
    obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
    obstacle.size = new UnityEngine.Vector3(LeafWidth * 2f + 0.6f, 3f, 1.3f);
    obstacle.center = new UnityEngine.Vector3(0f, 1.5f, 0f);
    obstacle.carving = true;
    obstacle.carveOnlyStationary = false;
    obstacles.Add(obstacle);
    return gate;
}

var gates = new[]
{
    Gate("Gate 0 (Entry to Hall)", V(0f, -50f), UnityEngine.Vector3.forward, 10f),
    Gate("Gate 1 (Hall to West crypt)", V(-15f, -25f), UnityEngine.Vector3.left, 10f),
    Gate("Gate 2 (Hall to East crypt)", V(15f, -25f), UnityEngine.Vector3.right, 10f),
    Gate("Gate 3 (Hall to Throne hall)", V(0f, 10f), UnityEngine.Vector3.forward, 20f),
};

// ---- monster spots: the layout's own, by the room they stand in; the throne hall's go (the guardian's room has no
// monsters), the entry and the hold get new ones ----
var byRoom = new System.Collections.Generic.List<UnityEngine.Transform>[rooms.Length];
for (var i = 0; i < rooms.Length; i++) byRoom[i] = new System.Collections.Generic.List<UnityEngine.Transform>();
foreach (var spot in spawns.Cast<UnityEngine.Transform>().ToList())
{
    var room = RoomOf(root.InverseTransformPoint(spot.position));
    if (room < 0) { log.AppendLine("a spot outside every room at " + root.InverseTransformPoint(spot.position).ToString("F1")); continue; }
    byRoom[room].Add(spot);
}
log.AppendLine("spots found: " + string.Join(", ", rooms.Select((r, i) => r.name + " " + byRoom[i].Count)));
var want = new[] { 3, 8, 5, 8, 0 };
// the pillars the builders put in the rooms (nothing stands on them)
var pillars = new System.Collections.Generic.List<UnityEngine.Vector2>();
for (var z = -45f; z <= 5f; z += 10f) { pillars.Add(new UnityEngine.Vector2(-9f, z)); pillars.Add(new UnityEngine.Vector2(9f, z)); }
pillars.Add(new UnityEngine.Vector2(-30f, -25f)); pillars.Add(new UnityEngine.Vector2(30f, -25f));
var all = new System.Collections.Generic.List<UnityEngine.Vector3>();
for (var i = 0; i < rooms.Length; i++)
{
    // too many: the last ones go
    while (byRoom[i].Count > want[i]) { var last = byRoom[i][byRoom[i].Count - 1]; byRoom[i].RemoveAt(byRoom[i].Count - 1); UnityEngine.Object.DestroyImmediate(last.gameObject); }
    foreach (var s in byRoom[i]) all.Add(root.InverseTransformPoint(s.position));
}
for (var i = 0; i < rooms.Length; i++)
{
    var r = rooms[i];
    var tries = 0;
    while (byRoom[i].Count < want[i] && tries++ < 2000)
    {
        var x = r.x0 + 4f + R() * (r.x1 - r.x0 - 8f); var z = r.z0 + 4f + R() * (r.z1 - r.z0 - 8f);
        // the entry: its far half, off the arrival
        if (i == 0 && z < -63f) continue;
        if (pillars.Any(p => (p - new UnityEngine.Vector2(x, z)).magnitude < 2.4f)) continue;
        if (all.Any(s => (s - V(x, z)).magnitude < (i == 0 ? 4.5f : 5.5f))) continue;
        // not in a doorway's way
        if (gates.Any(g => (g.transform.localPosition - V(x, z)).magnitude < 5f)) continue;
        var go = new UnityEngine.GameObject("Route Spot"); go.transform.SetParent(spawns, false);
        go.transform.position = root.TransformPoint(V(x, z));
        go.transform.rotation = UnityEngine.Quaternion.Euler(0f, R() * 360f, 0f);
        byRoom[i].Add(go.transform);
        all.Add(V(x, z));
    }
    if (byRoom[i].Count < want[i]) log.AppendLine("ONLY " + byRoom[i].Count + " of " + want[i] + " spots in " + r.name);
}

// ---- the rooms as zones ----
// where players arrive in each room (local x, z, and the way they face: into the room)
var arrivals = new (float x, float z, float yaw)[]
{
    (0f, -66f, 0f),
    (0f, -45f, 0f),
    (-20f, -25f, 270f),
    (20f, -25f, 90f),
    (0f, 15f, 0f),
};
var zones = new TowerZone[rooms.Length];
for (var i = 0; i < rooms.Length; i++)
{
    var zoneGo = Group("Zone " + i + " - " + rooms[i].title, route);
    zoneGo.localPosition = V((rooms[i].x0 + rooms[i].x1) / 2f, (rooms[i].z0 + rooms[i].z1) / 2f);
    var zone = zoneGo.gameObject.AddComponent<TowerZone>();
    zone.Title = rooms[i].title;
    var entry = Group("Entry", zoneGo);
    entry.position = root.TransformPoint(V(arrivals[i].x, arrivals[i].z));
    entry.rotation = UnityEngine.Quaternion.Euler(0f, arrivals[i].yaw, 0f);
    zone.Entry = entry;
    var zoneSpawns = Group("Spawns", zoneGo);
    foreach (var s in byRoom[i]) { s.SetParent(zoneSpawns, true); s.name = "Spot"; }
    zone.Spawns = zoneSpawns;
    zones[i] = zone;
}
// each room's way on: the entry's door, then the west crypt's, the east crypt's, and from the hold the throne hall's
zones[0].Exit = gates[0];
zones[1].Exit = gates[1];
zones[2].Exit = gates[2];
zones[3].Exit = gates[3];
// the arrival stays the entry's (TowerFloor.Spawn answers the route's room once the floor has one)
if (floor.SpawnPoint) floor.SpawnPoint.position = root.TransformPoint(V(arrivals[0].x, arrivals[0].z));

var towerRoute = root.gameObject.AddComponent<TowerRoute>();
towerRoute.Zones = zones;
floor.Route = towerRoute;

// saved open: the audit walks the whole route; TowerFloor.Apply shuts the gates (TowerRoute.ResetRoute) when it fills the floor
foreach (var o in obstacles) o.enabled = false;

// a quick check with the baked navmesh: every room's arrival and every spot from the entry
{
    var bad = 0; var total = 0;
    UnityEngine.AI.NavMesh.SamplePosition(zones[0].Arrival, out var a, 3f, UnityEngine.AI.NavMesh.AllAreas);
    void Check(string what, UnityEngine.Vector3 p)
    {
        total++;
        var path = new UnityEngine.AI.NavMeshPath();
        if (!UnityEngine.AI.NavMesh.SamplePosition(p, out var b, 3f, UnityEngine.AI.NavMesh.AllAreas) || !UnityEngine.AI.NavMesh.CalculatePath(a.position, b.position, UnityEngine.AI.NavMesh.AllAreas, path) || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
        { bad++; log.AppendLine("  NOT REACHED " + what + " " + root.InverseTransformPoint(p).ToString("F1")); }
    }
    for (var i = 0; i < zones.Length; i++)
    {
        Check("arrival " + zones[i].Title, zones[i].Arrival);
        foreach (UnityEngine.Transform s in zones[i].Spawns) Check("spot in " + zones[i].Title, s.position);
    }
    Check("boss", floor.BossPoint ? floor.BossPoint.position : root.position);
    log.AppendLine("route check: " + total + " places, " + bad + " not reached (the wall blocks carve at run time: the audit and a play through say the rest)");
}
log.AppendLine("zones " + string.Join(", ", zones.Select(z => z.Title + " " + z.SpotCount)) + "; gates " + gates.Length);

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
// fixed ids for what this made (tools/tower/layouts, docs/tower-places-editor.md), then the owner's hand edits put back
TowerPlaceEdits.AfterBuild(scene, scene.path, log);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
log.AppendLine("saved " + layout);
} catch (System.Exception ex) { log.AppendLine("ERROR " + ex); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-rooms-route.txt"), log.ToString());
return null;
