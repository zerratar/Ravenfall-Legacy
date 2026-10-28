// Run with: unity command eval_file --file tools/ravebridge/lumbridge-dressing.cs (after lumbridge-ground.cs)
//
// Small details round the Ravenfall Lumbridge's swapped houses (Assets/Scenes/LumbridgeRavenfall.unity), which stood
// bare on the grass:
// - a dirt footpath from each front door to the nearest road in front of it, with the road tool, never across a fence
//   or another house (none where the door is already on a road or the nearest road is far);
// - a little dressing at the front corners: barrels, crates, sacks, a flower pot or a bench.
// The door is the middle of the front: the presets are single meshes, and they face out through the map's door (the
// swap tool's rule, repeated here). Rerunnable: the paths live under "House Paths", the props under "House Dressing".
var sb = new System.Text.StringBuilder();
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/LumbridgeRavenfall.unity") return "open LumbridgeRavenfall first";
if (UnityEditor.EditorApplication.isPlaying) return "stop play mode first";
const float Tile = 2.5f, PathWidth = 1.6f, Reach = 35f;
var root = UnityEngine.GameObject.Find("Lumbridge (Ravenfall)").transform;
var houses = root.Find("Ravenfall Buildings");
if (!houses) return "no Ravenfall Buildings: run lumbridge-swap.cs first";
foreach (var name in new[] { "House Paths", "House Dressing" }) { var o = root.Find(name); if (o) UnityEngine.Object.DestroyImmediate(o.gameObject); }
var paths = new UnityEngine.GameObject("House Paths").transform; paths.SetParent(root, false);
var dressing = new UnityEngine.GameObject("House Dressing").transform; dressing.SetParent(root, false);

// which way each building faces, as the swap tool turned it
var buildings = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(@"G:\GitHub\rs-map-viewer\out\buildings.json"));
var facing = new System.Collections.Generic.Dictionary<int, UnityEngine.Vector3>();
foreach (var b in buildings)
{
    var side = 3;
    var doors = b["doors"].ToObject<int[][]>();
    if (doors.Length > 0)
    {
        var tiles = new System.Collections.Generic.HashSet<long>();
        foreach (var t in b["tiles"].ToObject<int[][]>()) tiles.Add(((long)t[0] << 20) | (uint)t[1]);
        var d = doors[0];
        side = tiles.Contains(((long)d[0] << 20) | (uint)d[1]) ? d[2] : (d[2] + 2) % 4;
    }
    facing[(int)b["id"]] = side == 0 ? UnityEngine.Vector3.left : side == 1 ? UnityEngine.Vector3.forward : side == 2 ? UnityEngine.Vector3.right : UnityEngine.Vector3.back;
}

UnityEngine.Bounds BoundsOf(UnityEngine.Transform t)
{
    var b = new UnityEngine.Bounds(t.position, UnityEngine.Vector3.zero); var first = true;
    foreach (var r in t.GetComponentsInChildren<UnityEngine.Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
    return b;
}
var houseBounds = new System.Collections.Generic.List<UnityEngine.Bounds>();
foreach (UnityEngine.Transform h in houses) houseBounds.Add(BoundsOf(h));

// the roads' curves, and the fences' lines, in x and z
var roadPoints = new System.Collections.Generic.List<UnityEngine.Vector2>();
foreach (var groupName in new[] { "Roads", "Courtyard Paths" })
{
    var g = root.Find(groupName); if (!g) continue;
    foreach (var r in g.GetComponentsInChildren<WorldRoad>()) foreach (var s in r.Samples) roadPoints.Add(s);
}
var fenceLines = new System.Collections.Generic.List<(UnityEngine.Vector2, UnityEngine.Vector2)>();
foreach (var f in UnityEngine.Object.FindObjectsByType<WorldFence>())
    for (var i = 0; i + 1 < f.Points.Count; i++)
    {
        var a = f.transform.TransformPoint(f.Points[i]); var c = f.transform.TransformPoint(f.Points[i + 1]);
        fenceLines.Add((new UnityEngine.Vector2(a.x, a.z), new UnityEngine.Vector2(c.x, c.z)));
    }
bool Crosses(UnityEngine.Vector2 p, UnityEngine.Vector2 q, UnityEngine.Vector2 a, UnityEngine.Vector2 c)
{
    float Cross(UnityEngine.Vector2 u, UnityEngine.Vector2 v) => u.x * v.y - u.y * v.x;
    var r = q - p; var s = c - a; var den = Cross(r, s);
    if (UnityEngine.Mathf.Abs(den) < 1e-6f) return false;
    var t = Cross(a - p, s) / den; var u2 = Cross(a - p, r) / den;
    return t > 0f && t < 1f && u2 > 0f && u2 < 1f;
}
bool FenceClear(UnityEngine.Vector2 p, UnityEngine.Vector2 q)
{
    foreach (var (a, c) in fenceLines) if (Crosses(p, q, a, c)) return false;
    return true;
}
bool Clear(UnityEngine.Vector2 p, UnityEngine.Vector2 q, int self)
{
    if (!FenceClear(p, q)) return false;
    // not through another house with a metre to spare, nor back through its own
    for (var i = 0; i < houseBounds.Count; i++)
    {
        if (self < 0 && i == -1 - self) continue;   // (-1 - i: house i is left out altogether)
        var b = houseBounds[i]; b.Expand(i == self ? new UnityEngine.Vector3(-0.2f, 100f, -0.2f) : new UnityEngine.Vector3(2f, 100f, 2f));
        for (var k = 0; k <= 10; k++)
        {
            var m = UnityEngine.Vector2.Lerp(p, q, k / 10f);
            if (b.Contains(new UnityEngine.Vector3(m.x, b.center.y, m.y))) return false;
        }
    }
    return true;
}

// the props, Fantasy Kingdom's where it has them
var prefabs = new System.Collections.Generic.Dictionary<string, UnityEngine.GameObject>();
UnityEngine.GameObject Prefab(string name)
{
    if (prefabs.TryGetValue(name, out var pf)) return pf;
    foreach (var folders in new[] { new[] { "Assets/PolygonFantasyKingdom" }, null })
    {
        var guids = folders == null ? UnityEditor.AssetDatabase.FindAssets(name + " t:prefab") : UnityEditor.AssetDatabase.FindAssets(name + " t:prefab", folders);
        foreach (var gid in guids)
        {
            var p = UnityEditor.AssetDatabase.GUIDToAssetPath(gid);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == name) { pf = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p); break; }
        }
        if (pf) break;
    }
    prefabs[name] = pf;
    return pf;
}
var sets = new[]
{
    new[] { "SM_Prop_Barrel_01", "SM_Prop_Barrel_02" },
    new[] { "SM_Prop_Crate_01", "SM_Prop_Barrel_01" },
    new[] { "SM_Prop_Sack_01", "SM_Prop_Crate_02" },
    new[] { "SM_Prop_Pot_Plant_01" },
    new[] { "SM_Prop_Bench_01" },
    new[] { "SM_Prop_Barrel_Stack_01" },
};
var rng = new System.Random(3226);
var placed = 0; var made = 0; var skipped = new System.Collections.Generic.List<string>();

var index = -1;
foreach (UnityEngine.Transform h in houses)
{
    index++;
    var m = System.Text.RegularExpressions.Regex.Match(h.name, @"^Building (\d+)");
    if (!m.Success) continue;
    var id = int.Parse(m.Groups[1].Value);
    if (h.name.Contains("Destroyed")) continue;   // the ruin in the woods: no one lives there
    if (!facing.TryGetValue(id, out var dir)) continue;
    var b = houseBounds[index];
    var right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, dir);
    var halfFront = UnityEngine.Mathf.Abs(UnityEngine.Vector3.Dot(b.extents, new UnityEngine.Vector3(UnityEngine.Mathf.Abs(right.x), 0, UnityEngine.Mathf.Abs(right.z))));
    var halfDepth = UnityEngine.Mathf.Abs(UnityEngine.Vector3.Dot(b.extents, new UnityEngine.Vector3(UnityEngine.Mathf.Abs(dir.x), 0, UnityEngine.Mathf.Abs(dir.z))));
    var front = new UnityEngine.Vector3(b.center.x, 0, b.center.z) + dir * halfDepth;
    // the door: where the front sticks out furthest at knee height (its steps), else the middle; "front" is the eaves'
    // line, so the wall's own face is found the same way (how far it stands out from there, negative behind)
    float doorAlong = 0f, doorOut = 0f, wallOut = 0f;
    {
        var temp = new System.Collections.Generic.List<UnityEngine.MeshCollider>();
        foreach (var mf in h.GetComponentsInChildren<UnityEngine.MeshFilter>())
        {
            if (!mf.sharedMesh) continue;
            var mc = mf.gameObject.AddComponent<UnityEngine.MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc);
        }
        UnityEngine.Physics.SyncTransforms();
        var ts = new System.Collections.Generic.List<float>(); var outs = new System.Collections.Generic.List<float>();
        for (var t = -halfFront + 0.4f; t <= halfFront - 0.4f; t += 0.25f)
        {
            var o = front + right * t + dir * 3f;
            o.y = WorldRoad.GroundHeight(new UnityEngine.Vector3(o.x, 1000f, o.z)) + 0.3f;
            var best = float.MaxValue;
            foreach (var hit in UnityEngine.Physics.RaycastAll(o, -dir, 3f + halfDepth * 2f))
                if (temp.Contains(hit.collider as UnityEngine.MeshCollider)) best = UnityEngine.Mathf.Min(best, hit.distance);
            if (best < float.MaxValue) { ts.Add(t); outs.Add(3f - best); }
        }
        foreach (var mc in temp) UnityEngine.Object.DestroyImmediate(mc);
        if (outs.Count > 4)
        {
            var sorted = new System.Collections.Generic.List<float>(outs); sorted.Sort();
            wallOut = sorted[sorted.Count / 2];
            doorOut = wallOut;
            // the longest run standing 0.4 m or more out of the wall
            int bestStart = -1, bestLen = 0, runStart = -1;
            for (var i = 0; i < outs.Count; i++)
            {
                if (outs[i] > wallOut + 0.4f) { if (runStart < 0) runStart = i; if (i - runStart + 1 > bestLen) { bestLen = i - runStart + 1; bestStart = runStart; } }
                else runStart = -1;
            }
            if (bestLen >= 2)
            {
                doorAlong = (ts[bestStart] + ts[bestStart + bestLen - 1]) * 0.5f;
                doorOut = float.MinValue; for (var i = bestStart; i < bestStart + bestLen; i++) doorOut = UnityEngine.Mathf.Max(doorOut, outs[i]);
            }
        }
    }
    var doorAt = front + right * doorAlong + dir * (doorOut + 0.3f);
    var door = new UnityEngine.Vector2(doorAt.x, doorAt.z);

    // ---- the path: to the nearest road point in front of the door that it can reach in a straight line
    {
        var d2 = new UnityEngine.Vector2(dir.x, dir.z);
        var candidates = new System.Collections.Generic.List<(float, UnityEngine.Vector2)>();
        foreach (var q in roadPoints)
        {
            var dist = UnityEngine.Vector2.Distance(q, door);
            if (dist > Reach || UnityEngine.Vector2.Dot(q - door, d2) < 0.5f) continue;
            candidates.Add((dist, q));
        }
        candidates.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        if (candidates.Count == 0) skipped.Add(id + " no road in reach");
        else if (candidates[0].Item1 < 2.5f) skipped.Add(id + " door on a road");
        else
        {
            var found = false;
            for (var c = 0; c < candidates.Count && c < 400 && !found; c += 2)
            {
                var q = candidates[c].Item2; var dist = candidates[c].Item1;
                // out of the door first, then on to the road
                var bend = door + d2 * UnityEngine.Mathf.Min(1.5f, dist * 0.3f);
                // (the first leg starts under its own eaves: only the other houses count there)
                if (!Clear(door, bend, -1 - index) || !Clear(bend, q, index)) continue;
                var go = new UnityEngine.GameObject("Path to Building " + id);
                go.transform.SetParent(paths, false);
                go.transform.position = new UnityEngine.Vector3(door.x, 0f, door.y);
                var road = go.AddComponent<WorldRoad>();
                road.RoadSurface = WorldRoad.Surface.Dirt;
                road.Width = PathWidth;
                foreach (var p in new[] { door, bend, q }) road.AddPoint(go.transform.InverseTransformPoint(new UnityEngine.Vector3(p.x, 0f, p.y)));
                road.StartRound = 0.4f;
                road.Rebuild();
                made++; found = true;
            }
            if (!found) skipped.Add(id + " fenced in");
        }
    }

    // ---- the dressing: one set at each front corner, or one corner left bare
    foreach (var s in new[] { -1f, 1f })
    {
        if (rng.NextDouble() < 0.3) continue;
        var set = sets[rng.Next(sets.Length)];
        var along = s * UnityEngine.Mathf.Max(0.8f, halfFront - 1.1f);
        if (UnityEngine.Mathf.Abs(along - doorAlong) < 1.8f) continue;   // (not in front of the door)
        var corner = front + right * along + dir * (wallOut + 0.9f);
        for (var k = 0; k < set.Length; k++)
        {
            var pf = Prefab(set[k]); if (!pf) continue;
            var at = corner + right * s * (k * 0.9f) + dir * (k * 0.25f);
            if (!FenceClear(door, new UnityEngine.Vector2(at.x, at.z))) continue;   // (not over a fence line by the door)
            at.y = WorldRoad.GroundHeight(new UnityEngine.Vector3(at.x, 1000f, at.z));
            var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf, dressing);
            // benches face out; the rest turn a little at random
            var yaw = UnityEngine.Quaternion.LookRotation(set[k].Contains("Bench") ? -right * s : dir).eulerAngles.y + (set[k].Contains("Bench") ? 0f : (float)rng.NextDouble() * 50f - 25f);
            o.transform.SetPositionAndRotation(at, UnityEngine.Quaternion.Euler(0, yaw, 0));
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(o, UnityEditor.StaticEditorFlags.BatchingStatic);
            placed++;
        }
    }
}
sb.Append("paths " + made + ", props " + placed + "; no path: " + string.Join(", ", skipped) + "; ");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return sb.ToString();
