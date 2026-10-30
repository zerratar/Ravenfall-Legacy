// Run with: unity command eval_file --file tools/ravebridge/lumbridge-places.cs (after lumbridge-move.cs and its paint)
//
// Puts Ravebridge's gameplay (Playable: spawn, NPCs, skill chunks, hero mode) back on the island after the Lumbridge
// move (docs/lumbridge-move-plan.md, phase 4). Names and keys stay: the quests, loot, hero mode and shop find things
// by name.
// - What stood against the old castle moves with the castle, exactly: the castle came over from Ravebridge itself,
//   scaled, so old point -> new castle + scale x (point - old castle). Spawn, camera target, duke, cook, kitchen
//   (Cooking), the hero mode captain and courtyard.
// - The rest goes to its OSRS place in the new layout, found from the layout itself: the NPCs at their building's
//   door (the dressing's footpaths start at the doors), the ghost by the church's gravestones, Mining on the swamp
//   mine's rocks, Fishing at the river's mouth, Woodcutting in the thickest wood west of the castle, the cows in the
//   cow field, the goblins by the goblin house, the wave gates past the bridge ends, at Al Kharid's toll gate and on
//   the roads to the swamp and the mill; Farming, Gathering and the crafting stations on the nearest open, level ground
//   by their building.
// - Trees and gather spots are kept in world space (ResourceField): every instance is moved and set on the ground.
// Rerunnable on the moved MainWorld only from the move's result (it moves things from where they are).
var sb = new System.Text.StringBuilder();
var stage = "start";
var logPath = System.IO.Path.GetFullPath("Temp/lumbridge-places.txt");
if (UnityEditor.EditorApplication.isPlaying) return "stop play mode first";
try
{
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/MainWorld.unity") return "open MainWorld first";
var island = UnityEngine.GameObject.Find("============ WORLD/Islands/Ravebridge").transform;
var root = island.Find("Lumbridge");
var playable = island.Find("Playable");
if (!root || !playable) return "run lumbridge-move.cs first";
const float Tile = 2.5f;
var offset = root.position;   // Lumbridge's (0, 0, 0)

float Ground(float x, float z) => TerrainGround.TryHeight(new UnityEngine.Vector3(x, 0f, z), out var h) ? h : 0f;
UnityEngine.Vector3 OnGround(UnityEngine.Vector3 p) => new UnityEngine.Vector3(p.x, Ground(p.x, p.z), p.z);
UnityEngine.Bounds BoundsOf(UnityEngine.Transform t)
{
    var b = new UnityEngine.Bounds(t.position, UnityEngine.Vector3.zero); var first = true;
    foreach (var r in t.GetComponentsInChildren<UnityEngine.Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
    return b;
}
UnityEngine.Transform Find(UnityEngine.Transform under, string name)
{
    foreach (var t in under.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == name) return t;
    return null;
}
// everything standing on the island, for finding open ground: buildings, fences along their lines, and props over
// 1.2 m (flowers, small bushes and ferns are cleared from a spot instead)
var standing = new System.Collections.Generic.List<UnityEngine.Vector3>();
foreach (UnityEngine.Transform g in root)
{
    if (g.name == "Terrain" || g.name == "Water" || g.name == "Roads" || g.name == "House Paths" || g.name == "Courtyard Paths" || g.name == "Shore Reeds") continue;
    foreach (UnityEngine.Transform c in g)
    {
        var fence = c.GetComponent<WorldFence>();
        if (fence)
        {
            for (var i = 0; i + 1 < fence.Points.Count; i++)
            {
                var a = c.TransformPoint(fence.Points[i]); var b2 = c.TransformPoint(fence.Points[i + 1]);
                for (var f = 0f; f <= 1f; f += 0.1f) standing.Add(UnityEngine.Vector3.Lerp(a, b2, f));
            }
            continue;
        }
        if (g.name.StartsWith("Objects") && BoundsOf(c).size.y < 1.2f) continue;
        standing.Add(c.position);
    }
    if (g.name == "Ravenfall Buildings" || g.name == "Ravebridge Castle" || g.name == "Ravebridge Bridges")
        foreach (UnityEngine.Transform c in g) { var b = BoundsOf(c); for (var x = b.min.x; x <= b.max.x; x += 3f) for (var z = b.min.z; z <= b.max.z; z += 3f) standing.Add(new UnityEngine.Vector3(x, 0, z)); }
}
var roadPoints = new System.Collections.Generic.List<UnityEngine.Vector2>();
foreach (var groupName in new[] { "Roads", "House Paths", "Courtyard Paths" })
{
    var g = root.Find(groupName); if (!g) continue;
    foreach (var r in g.GetComponentsInChildren<WorldRoad>()) foreach (var s in r.Samples) roadPoints.Add(s);
}
// open, level ground near a point: nothing standing within clear m, the ground within 2.5 m of level over it, dry, off
// the roads
UnityEngine.Vector3 OpenSpot(UnityEngine.Vector3 near, float clear, float within)
{
    var best = near; var bestD = float.MaxValue;
    for (var dx = -within; dx <= within; dx += 2f)
    for (var dz = -within; dz <= within; dz += 2f)
    {
        var p = new UnityEngine.Vector3(near.x + dx, 0, near.z + dz);
        var d = dx * dx + dz * dz;
        if (d >= bestD || d > within * within) continue;
        float lo = float.MaxValue, hi = float.MinValue;
        for (var a = 0; a < 12; a++)
        {
            var q = p + UnityEngine.Quaternion.Euler(0, a * 30f, 0) * UnityEngine.Vector3.forward * clear;
            var y = Ground(q.x, q.z); lo = UnityEngine.Mathf.Min(lo, y); hi = UnityEngine.Mathf.Max(hi, y);
        }
        var yc = Ground(p.x, p.z);
        if (hi - lo > 2.5f || UnityEngine.Mathf.Min(lo, yc) < -2.5f) continue;
        var free = true;
        foreach (var s in standing) { var sx = s.x - p.x; var sz = s.z - p.z; if (sx * sx + sz * sz < clear * clear) { free = false; break; } }
        if (!free) continue;
        foreach (var r in roadPoints) { var rx = r.x - p.x; var rz = r.y - p.z; if (rx * rx + rz * rz < 4f * 4f) { free = false; break; } }
        if (!free) continue;
        best = p; bestD = d;
    }
    if (bestD == float.MaxValue) sb.Append("(no open spot near " + near.ToString("F0") + ") ");
    best.y = Ground(best.x, best.z);
    // taken now
    for (var a = 0; a < 8; a++) standing.Add(best + UnityEngine.Quaternion.Euler(0, a * 45f, 0) * UnityEngine.Vector3.forward * clear * 0.6f);
    standing.Add(best);
    return best;
}
// moves a chunk (or anything) so its old reference point lands on a new one, then sets every enemy, rock and station
// in it on the ground
void MoveTo(UnityEngine.Transform t, UnityEngine.Vector3 from, UnityEngine.Vector3 to, bool settle)
{
    var delta = to - from; delta.y = 0f;
    var field = t.GetComponentInChildren<ResourceField>(true);
    // (the field's own transform moves too, but its instances are in world space)
    t.position += delta;
    if (field)
    {
        for (var i = 0; i < field.Count; i++)
        {
            var inst = field.Instances[i];
            inst.position += delta;
            inst.position.y = Ground(inst.position.x, inst.position.z);
            field.Set(i, inst);
        }
        field.Changed();
        UnityEditor.EditorUtility.SetDirty(field);
    }
    if (!settle) return;
    foreach (var e in t.GetComponentsInChildren<EnemyController>(true))
    {
        // on dry ground: one that came down in the river walks in towards the others
        var p = e.transform.position;
        for (var step = 0; step < 20 && Ground(p.x, p.z) < -2.5f; step++) p = UnityEngine.Vector3.Lerp(p, to, 0.2f);
        e.transform.position = OnGround(p);
    }
    foreach (var fp in t.GetComponentsInChildren<UnityEngine.Transform>(true)) if (fp.name.StartsWith("FarmingPatch")) fp.position = OnGround(fp.position);
    foreach (var r in t.GetComponentsInChildren<RockController>(true)) r.transform.position = OnGround(r.transform.position);
    foreach (var c in t.GetComponentsInChildren<CraftingStation>(true)) c.transform.position = OnGround(c.transform.position);
}
UnityEngine.Vector3 CentreOf(UnityEngine.Transform t)
{
    var field = t.GetComponentInChildren<ResourceField>(true);
    var sum = UnityEngine.Vector3.zero; var n = 0;
    if (field) foreach (var inst in field.Instances) { sum += inst.position; n++; }
    else
    {
        foreach (var e in t.GetComponentsInChildren<EnemyController>(true)) { sum += e.transform.position; n++; }
        foreach (var r in t.GetComponentsInChildren<RockController>(true)) { sum += r.transform.position; n++; }
        foreach (var c in t.GetComponentsInChildren<CraftingStation>(true)) { sum += c.transform.position; n++; }
    }
    return n > 0 ? sum / n : t.position;
}
void Face(UnityEngine.Transform t, UnityEngine.Vector3 towards)
{
    var d = towards - t.position; d.y = 0;
    if (d.sqrMagnitude > 0.01f) t.rotation = UnityEngine.Quaternion.LookRotation(d, UnityEngine.Vector3.up);
}

// ---- with the castle ------------------------------------------------------------------------------------------------
stage = "castle";
var oldCastle = new UnityEngine.Vector3(-1703.519f, -6.44f, 1521.58f);   // Ravebridge's Castle group before the move
var castle = Find(root.Find("Ravebridge Castle"), "Castle");
var k = castle.lossyScale.x;
UnityEngine.Vector3 Castled(UnityEngine.Vector3 p) => castle.position + k * (p - oldCastle);
var castled = 0;
foreach (var path in new[] { "SpawnPosition", "Camera Rotation Target", "NPCs/duke", "NPCs/cook", "Hero mode/captain", "Cooking Lv. 1" })
{
    var t = playable.Find(path); if (!t) { sb.Append("no " + path + "; "); continue; }
    t.position = Castled(t.position); castled++;
}
foreach (UnityEngine.Transform c in playable.Find("Hero mode/Courtyard")) { c.position = Castled(c.position); castled++; }
sb.Append("with the castle " + castled + "; ");

// ---- doors ------------------------------------------------------------------------------------------------------------
stage = "doors";
var buildingsJson = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(@"G:\GitHub\rs-map-viewer\out\buildings.json"));
var facing = new System.Collections.Generic.Dictionary<int, UnityEngine.Vector3>();
foreach (var b in buildingsJson)
{
    var side = 3; var doors = b["doors"].ToObject<int[][]>();
    if (doors.Length > 0) { var tiles = new System.Collections.Generic.HashSet<long>(); foreach (var t in b["tiles"].ToObject<int[][]>()) tiles.Add(((long)t[0] << 20) | (uint)t[1]); var d = doors[0]; side = tiles.Contains(((long)d[0] << 20) | (uint)d[1]) ? d[2] : (d[2] + 2) % 4; }
    facing[(int)b["id"]] = side == 0 ? UnityEngine.Vector3.left : side == 1 ? UnityEngine.Vector3.forward : side == 2 ? UnityEngine.Vector3.right : UnityEngine.Vector3.back;
}
UnityEngine.Transform Building(int id)
{
    foreach (UnityEngine.Transform c in root.Find("Ravenfall Buildings")) if (c.name.StartsWith("Building " + id + " ")) return c;
    return null;
}
// a building's door and the way out of it: where its footpath starts, else the middle of the front
(UnityEngine.Vector3 At, UnityEngine.Vector3 Out) Door(int id)
{
    var paths = root.Find("House Paths");
    var path = paths ? paths.Find("Path to Building " + id) : null;
    if (path)
    {
        var road = path.GetComponent<WorldRoad>();
        var a = road.WorldPoint(0); var b2 = road.WorldPoint(1);
        var o = b2 - a; o.y = 0;
        return (a, o.normalized);
    }
    var bt = Building(id); var bb = BoundsOf(bt); var dir = facing[id];
    var half = UnityEngine.Mathf.Abs(UnityEngine.Vector3.Dot(bb.extents, new UnityEngine.Vector3(UnityEngine.Mathf.Abs(dir.x), 0, UnityEngine.Mathf.Abs(dir.z))));
    return (OnGround(new UnityEngine.Vector3(bb.center.x, 0, bb.center.z) + dir * (half + 0.5f)), dir);
}
void AtDoor(string npc, int id, float sideways, float outward)
{
    var t = playable.Find("NPCs/" + npc); if (!t) { sb.Append("no " + npc + "; "); return; }
    var (at, o) = Door(id);
    var right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, o);
    t.position = OnGround(at + o * outward + right * sideways);
    Face(t, t.position + o);
    standing.Add(t.position);
}
AtDoor("shopkeeper", 9, 0f, 1.8f);      // the general store
AtDoor("priest", 18, 1.5f, 2.5f);      // the church
AtDoor("miller", 4, 0f, 2f);           // the windmill
AtDoor("bentnose", 20, -2.5f, 3f);     // the goblin house, a general either side of its door
AtDoor("wartfist", 20, 2.5f, 3f);

// ---- the keeper at the wooden bridge, the ghost in the graveyard ------------------------------------------------------
stage = "bridge and graveyard";
var wooden = root.Find("Ravebridge Bridges/Wooden Bridge");
var wb = BoundsOf(wooden);
var keeper = playable.Find("NPCs/keeper");
// on the town's side (west), a step off the planks
keeper.position = OnGround(new UnityEngine.Vector3(wb.min.x - 2.5f, 0, wb.center.z + 2f));
Face(keeper, new UnityEngine.Vector3(wb.center.x, 0, wb.center.z));
var church = Building(18); var churchAt = BoundsOf(church).center;
var graves = new System.Collections.Generic.List<UnityEngine.Vector3>();
foreach (UnityEngine.Transform c in root.Find("Objects other"))
    if (c.name.StartsWith("SM_Prop_Gravestone") && UnityEngine.Vector3.Distance(c.position, churchAt) < 70f) graves.Add(c.position);
var graveyard = churchAt;
if (graves.Count > 0) { graveyard = UnityEngine.Vector3.zero; foreach (var g in graves) graveyard += g; graveyard /= graves.Count; }
var ghost = playable.Find("NPCs/ghost");
ghost.position = OnGround(graveyard);
Face(ghost, churchAt);
sb.Append("graveyard from " + graves.Count + " gravestones; ");

// ---- skills ----------------------------------------------------------------------------------------------------------------
stage = "mining";
{
    // the swamp mine: the thickest cluster of the map's mining rocks in the south
    var rocks = new System.Collections.Generic.List<UnityEngine.Transform>();
    foreach (UnityEngine.Transform c in root.Find("Objects other"))
        if (c.name.StartsWith("SM_Env_Rock_Chunk") && c.position.z - offset.z < 150f) rocks.Add(c);
    UnityEngine.Transform best = null; var bestN = -1;
    foreach (var r in rocks) { var n = 0; foreach (var o in rocks) if (UnityEngine.Vector3.Distance(r.position, o.position) < 15f) n++; if (n > bestN) { bestN = n; best = r; } }
    var mining = playable.Find("Mining Lv. 1");
    var ours = mining.GetComponentsInChildren<RockController>(true);
    var mine = best.position;
    MoveTo(mining, CentreOf(mining), mine, true);
    // each of the chunk's rocks on one of the map's, which gives way to it
    var near = rocks.FindAll(r => UnityEngine.Vector3.Distance(r.position, mine) < 15f);
    near.Sort((a, b) => UnityEngine.Vector3.Distance(a.position, mine).CompareTo(UnityEngine.Vector3.Distance(b.position, mine)));
    for (var i = 0; i < ours.Length && i < near.Count; i++) { ours[i].transform.position = OnGround(near[i].position); UnityEngine.Object.DestroyImmediate(near[i].gameObject); }
    sb.Append("mine on " + bestN + " rocks at " + mine.ToString("F0") + "; ");
}
stage = "fishing";
{
    // in the river's middle south of the stone bridge, where it is narrow (fishers need a bank within reach): the spots
    // in a line along it
    var river = root.GetComponentInChildren<RiverWater>(true);
    var mesh = river.Mesh; var m = river.transform.localToWorldMatrix;
    var surface = new System.Collections.Generic.List<UnityEngine.Vector3>();
    foreach (var v in mesh.vertices) surface.Add(m.MultiplyPoint3x4(v));
    var sbb = BoundsOf(root.Find("Ravebridge Bridges/Stone Bridge"));
    UnityEngine.Vector3 Middle(float z, float nearX)
    {
        var sum = UnityEngine.Vector3.zero; var n = 0;
        foreach (var w in surface) if (UnityEngine.Mathf.Abs(w.z - z) < 2f && UnityEngine.Mathf.Abs(w.x - nearX) < 25f) { sum += w; n++; }
        return n > 0 ? sum / n : new UnityEngine.Vector3(nearX, -3.31f, z);
    }
    var fishing = playable.Find("Fishing Lv. 1");
    var spots = fishing.GetComponentsInChildren<FishingController>(true);
    var x = sbb.center.x;
    for (var z = sbb.center.z; z > sbb.center.z - 60f; z -= 2f) x = Middle(z, x).x;
    var target = Middle(sbb.center.z - 60f, x);
    fishing.position = new UnityEngine.Vector3(target.x, fishing.position.y, target.z);
    var pulled = 0;
    for (var i = 0; i < spots.Length; i++)
    {
        var z = target.z + (i - (spots.Length - 1) * 0.5f) * 9f;
        var p = Middle(z, target.x);
        spots[i].transform.position = new UnityEngine.Vector3(p.x, spots[i].transform.position.y, p.z);
        pulled++;
    }
    sb.Append("fishing in the river at " + target.ToString("F0") + " (" + pulled + " spots along it); ");
}
stage = "woodcutting";
{
    // the thickest wood west of the castle
    var cc = BoundsOf(castle).center;
    var trees = new System.Collections.Generic.List<UnityEngine.Transform>();
    foreach (UnityEngine.Transform c in root.Find("Objects trees"))
        if (c.name.Contains("Tree_Round") || c.name.Contains("Tree_Large"))
            if (c.position.x < cc.x - 50f && c.position.x > cc.x - 160f && UnityEngine.Mathf.Abs(c.position.z - cc.z) < 90f) trees.Add(c);
    UnityEngine.Transform best = null; var bestN = -1;
    foreach (var t in trees) { var n = 0; foreach (var o in trees) if (UnityEngine.Vector3.Distance(t.position, o.position) < 20f) n++; if (n > bestN) { bestN = n; best = t; } }
    var wood = playable.Find("Woodcutting Lv. 1");
    MoveTo(wood, CentreOf(wood), best.position, false);
    // the map's own trees give way to the field's
    var field = wood.GetComponentInChildren<ResourceField>(true);
    var gone = 0;
    foreach (var t in trees.ToArray())
        foreach (var inst in field.Instances)
            if (t && UnityEngine.Vector2.Distance(new UnityEngine.Vector2(t.position.x, t.position.z), new UnityEngine.Vector2(inst.position.x, inst.position.z)) < 4f) { UnityEngine.Object.DestroyImmediate(t.gameObject); gone++; break; }
    foreach (var inst in field.Instances) standing.Add(inst.position);
    sb.Append("woodcutting in a wood of " + bestN + " (" + gone + " trees made way); ");
}
stage = "cows and goblins";
{
    var cows = new System.Collections.Generic.List<UnityEngine.Vector3>();
    foreach (UnityEngine.Transform c in root.Find("Objects other")) if (c.name == "Cow" && c.position.x - offset.x > 250f) cows.Add(c.position);
    var field = UnityEngine.Vector3.zero; foreach (var c in cows) field += c; field = cows.Count > 0 ? field / cows.Count : offset + new UnityEngine.Vector3(305f, 0, 340f);
    var cowChunk = playable.Find("Fighting Lv. 1 (cows)");
    MoveTo(cowChunk, CentreOf(cowChunk), field, true);
    var (gAt, gOut) = Door(20);
    var goblins = playable.Find("Fighting Lv. 5");
    MoveTo(goblins, CentreOf(goblins), OpenSpot(gAt + gOut * 20f, 6f, 30f), true);
    var soul = playable.Find("Fighting Lv. 15 (soul)");
    // the swamp, between Father Urhney's hut and the swamp shack
    var swamp = (BoundsOf(Building(2)).center + BoundsOf(Building(7)).center) * 0.5f;
    MoveTo(soul, CentreOf(soul), OpenSpot(swamp, 5f, 40f), true);
    sb.Append("cows on " + cows.Count + " dairy cows' field; ");
}
stage = "farm and stations";
{
    var (fAt, fOut) = Door(6);   // Fred's farm
    void ClearSmall(UnityEngine.Vector3 at, float radius)
    {
        var small = new System.Collections.Generic.List<UnityEngine.GameObject>();
        foreach (UnityEngine.Transform c in root.Find("Objects other"))
            if (UnityEngine.Vector2.Distance(new UnityEngine.Vector2(c.position.x, c.position.z), new UnityEngine.Vector2(at.x, at.z)) < radius && BoundsOf(c).size.y < 1.2f) small.Add(c.gameObject);
        foreach (var g in small) UnityEngine.Object.DestroyImmediate(g);
    }
    var farming = playable.Find("Farming Lv. 1");
    var farmAt = OpenSpot(fAt + fOut * 25f, 13f, 90f);
    MoveTo(farming, CentreOf(farming), farmAt, true);
    ClearSmall(farmAt, 15f);
    var gathering = playable.Find("Gathering Lv. 1");
    var gatherAt = OpenSpot(fAt + fOut * 12f, 9f, 50f);
    MoveTo(gathering, CentreOf(gathering), gatherAt, true);
    ClearSmall(gatherAt, 10f);
    var (sAt, sOut) = Door(9);   // the general store
    var right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, sOut);
    foreach (var (name, side) in new[] { ("Crafting Lv. 1", 7f), ("Alchemy Lv. 1", -7f) })
    {
        var t = playable.Find(name);
        MoveTo(t, CentreOf(t), OpenSpot(sAt + sOut * 5f + right * side, 2.5f, 20f), true);
    }
}

// ---- wave gates -------------------------------------------------------------------------------------------------------------
stage = "gates";
{
    var gates = playable.Find("Hero mode/Wave gates");
    UnityEngine.Vector3 NearestRoad(UnityEngine.Vector3 p)
    {
        var best = new UnityEngine.Vector2(p.x, p.z); var bd = float.MaxValue;
        foreach (var r in roadPoints) { var d = (r - new UnityEngine.Vector2(p.x, p.z)).sqrMagnitude; if (d < bd) { bd = d; best = r; } }
        return OnGround(new UnityEngine.Vector3(best.x, 0, best.y));
    }
    var stone = BoundsOf(root.Find("Ravebridge Bridges/Stone Bridge"));
    gates.Find("Stone bridge").position = NearestRoad(new UnityEngine.Vector3(stone.max.x + 12f, 0, stone.center.z));
    gates.Find("Wooden bridge").position = NearestRoad(new UnityEngine.Vector3(wb.max.x + 12f, 0, wb.center.z));
    // Al Kharid's toll gate (the map's, tile 132 x 91), on the desert side
    // (the toll gate itself was never placed: the map's gate there is not one of the fence gates)
    var toll = offset + new UnityEngine.Vector3(132.5f * Tile, 0, 91.5f * Tile);
    gates.Find("Desert gate").position = NearestRoad(toll + new UnityEngine.Vector3(12f, 0, 0));
    var cc = BoundsOf(castle).center;
    gates.Find("Swamp").position = NearestRoad(new UnityEngine.Vector3(cc.x + 10f, 0, cc.z - 110f));
    var mill = BoundsOf(Building(4)).center;
    gates.Find("Mill road").position = NearestRoad((cc + mill) * 0.5f);
    foreach (UnityEngine.Transform g in gates) sb.Append(g.name + " " + g.position.ToString("F0") + ", ");
    sb.Append("; ");
}

// ---- gates stand open: no collider, so the navmesh runs through the gaps in the fences -----------------------------------
stage = "gates open";
{
    var opened = 0;
    var gateGroup = root.Find("Walls gate");
    if (gateGroup) foreach (var c in gateGroup.GetComponentsInChildren<UnityEngine.Collider>(true)) { UnityEngine.Object.DestroyImmediate(c); opened++; }
    sb.Append("gate colliders taken off " + opened + "; ");
}

// ---- save -----------------------------------------------------------------------------------------------------------------
stage = "save";
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText(logPath, "[lumbridge-places] " + sb);
return sb.ToString();
}
catch (System.Exception ex)
{
    System.IO.File.WriteAllText(logPath, "[lumbridge-places] failed at " + stage + ": " + ex + "\n" + sb);
    return "failed at " + stage + ": " + ex + "\n" + sb;
}
