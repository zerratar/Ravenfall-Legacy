// Ravebridge, dressed after Lumbridge (docs/references/ravebridge/web/README.md): cobbled roads joining the town,
// red tiled roofs, rail fences round the fields and woods round the town. Rerunnable: it clears its own group
// ("Dressing") and repaints the roads. Run in edit mode through the Unity CLI (eval_file); the island may be inactive.
var island = UnityEngine.GameObject.Find("============ WORLD/Islands").transform.Find("Ravebridge");
var old = island.Find("Dressing");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var dress = new UnityEngine.GameObject("Dressing").transform; dress.SetParent(island, false);
var terrains = island.GetComponentsInChildren<UnityEngine.Terrain>(true);
float Ground(float x, float z)
{
    foreach (var t in terrains)
    {
        var p = t.transform.position; var s = t.terrainData.size;
        if (x >= p.x && x <= p.x + s.x && z >= p.z && z <= p.z + s.z) return t.terrainData.GetInterpolatedHeight((x - p.x) / s.x, (z - p.z) / s.z) + p.y;
    }
    return 0f;
}
float Smooth(float e0, float e1, float v) { var k = UnityEngine.Mathf.Clamp01((v - e0) / (e1 - e0)); return k * k * (3 - 2 * k); }
float SegDist(UnityEngine.Vector2 p, UnityEngine.Vector2 a, UnityEngine.Vector2 b) { var ab = b - a; var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return (p - (a + ab * t)).magnitude; }
float LineDist(UnityEngine.Vector2 p, UnityEngine.Vector2[] line) { var d = float.MaxValue; for (var i = 0; i + 1 < line.Length; i++) d = UnityEngine.Mathf.Min(d, SegDist(p, line[i], line[i + 1])); return d; }
UnityEngine.Vector2 V(float x, float z) => new UnityEngine.Vector2(x, z);
var rng = new System.Random(17);
float R() => (float)rng.NextDouble();

// ---- roads: cobbles with a dirt rim, as the island's own roads ------------------------------------------------
var roads = new (UnityEngine.Vector2[] line, float width)[]
{
    // from the store's road west past Bob's axes, the kitchen and the house, out to the windmill
    (new[] { V(-1652, 1660), V(-1672, 1652), V(-1690, 1648), V(-1712, 1639), V(-1730, 1647), V(-1748, 1664), V(-1762, 1698), V(-1770, 1722), V(-1776, 1737) }, 4.2f),
    // up to the church
    (new[] { V(-1662, 1688), V(-1676, 1697), V(-1690, 1699) }, 3.6f),
    // across to the wooden bridge
    (new[] { V(-1655, 1667), V(-1642, 1672), V(-1630, 1673) }, 3.6f),
    // south along the castle's east wall, round to the swamp's mine
    (new[] { V(-1606, 1550), V(-1603, 1520), V(-1604, 1500), V(-1618, 1480), V(-1650, 1466), V(-1684, 1456) }, 3.8f),
};
var painted = 0;
void paintRoads((UnityEngine.Vector2[] line, float width)[] list)
{
foreach (var t in terrains)
{
    var data = t.terrainData; var res = data.alphamapResolution; var o = t.transform.position; var size = data.size;
    if (data.alphamapLayers < 8) continue;
    foreach (var (line, width) in list)
    {
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var p in line) { minX = UnityEngine.Mathf.Min(minX, p.x); maxX = UnityEngine.Mathf.Max(maxX, p.x); minZ = UnityEngine.Mathf.Min(minZ, p.y); maxZ = UnityEngine.Mathf.Max(maxZ, p.y); }
        var pad = width + 3f;
        int x0 = UnityEngine.Mathf.Clamp((int)((minX - pad - o.x) / size.x * res), 0, res), x1 = UnityEngine.Mathf.Clamp((int)((maxX + pad - o.x) / size.x * res) + 1, 0, res);
        int z0 = UnityEngine.Mathf.Clamp((int)((minZ - pad - o.z) / size.z * res), 0, res), z1 = UnityEngine.Mathf.Clamp((int)((maxZ + pad - o.z) / size.z * res) + 1, 0, res);
        if (x1 <= x0 || z1 <= z0) continue;
        var maps = data.GetAlphamaps(x0, z0, x1 - x0, z1 - z0);
        for (var j = 0; j < z1 - z0; j++)
        for (var i = 0; i < x1 - x0; i++)
        {
            var wx = o.x + (x0 + i + 0.5f) / res * size.x; var wz = o.z + (z0 + j + 0.5f) / res * size.z;
            var d = LineDist(V(wx, wz), line) + (UnityEngine.Mathf.PerlinNoise(wx * 0.35f, wz * 0.35f) - 0.5f) * 0.9f;
            var cobble = 1f - Smooth(width * 0.5f - 0.5f, width * 0.5f + 0.3f, d);
            var rim = (1f - Smooth(width * 0.5f + 0.3f, width * 0.5f + 1.6f, d)) * (1f - cobble);
            if (cobble + rim < 0.01f) continue;
            var keep = 1f - cobble - rim * 0.8f;
            for (var l = 0; l < maps.GetLength(2); l++) maps[j, i, l] *= keep;
            maps[j, i, 7] += cobble; maps[j, i, 0] += rim * 0.8f;
            painted++;
        }
        UnityEditor.Undo.RegisterCompleteObjectUndo(data, "Ravebridge roads");
        data.SetAlphamaps(x0, z0, maps);
    }
}
}
paintRoads(roads);

// ---- red tiled roofs on the town's houses (Lumbridge's look); the palette with the red tiles ---------------------
var red = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_02_A.mat");
var roofs = 0;
foreach (var name in new[] { "General Store", "House (Bob's axes)", "House", "Castle Kitchen" })
{
    var place = island.Find("Playable/Places/" + name);
    if (!place) continue;
    foreach (var r in place.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
    {
        var ms = r.sharedMaterials; var changed = false;
        for (var i = 0; i < ms.Length; i++)
            if (ms[i] && ms[i].name.StartsWith("PolygonFantasyKingdom_Mat_0") && !ms[i].name.Contains("Glass")) { ms[i] = red; changed = true; }
        if (changed) { UnityEditor.Undo.RecordObject(r, "Ravebridge roofs"); r.sharedMaterials = ms; roofs++; }
    }
}

// ---- what woods and fences keep clear of --------------------------------------------------------------------
var keepOut = new System.Collections.Generic.List<(UnityEngine.Vector2 at, float r)>();
foreach (var c in island.Find("Playable/Places").GetComponentsInChildren<UnityEngine.Renderer>(true))
{
    if (c.transform.IsChildOf(island.Find("Playable/Places/Swamp"))) continue;
    var b = c.bounds; keepOut.Add((V(b.center.x, b.center.z), UnityEngine.Mathf.Max(b.extents.x, b.extents.z) + 6f));
}
foreach (var n in new[] { "Small House", "Small Goblin House", "Farmer House" }) { var h = island.Find(n); if (h) keepOut.Add((V(h.position.x, h.position.z), 14f)); }
foreach (UnityEngine.Transform n in island.Find("Playable/NPCs")) keepOut.Add((V(n.position.x, n.position.z), 6f));
foreach (UnityEngine.Transform c in island.Find("Playable")) if (c.name.Contains("Lv.")) keepOut.Add((V(c.position.x, c.position.z), c.name.StartsWith("Fighting") ? 22f : 10f));
foreach (var f in island.GetComponentsInChildren<ResourceField>(true)) for (var i = 0; i < f.Count; i++) keepOut.Add((V(f.Instances[i].position.x, f.Instances[i].position.z), 5f));
bool Clear(float x, float z, float extra)
{
    if (x > -1712 && x < -1598 && z > 1490 && z < 1618) return false;           // the castle and its walls
    if (Ground(x, z) < 7.2f) return false;                                       // the river, its banks and the shore
    var p = V(x, z);
    foreach (var (line, width) in roads) if (LineDist(p, line) < width * 0.5f + 3f + extra) return false;
    foreach (var (at, r) in keepOut) if ((p - at).magnitude < r + extra) return false;
    return true;
}

// ---- more of the town (Lumbridge's pub, smithy and houses), each facing its road, only where its footprint is clear --
var town = new UnityEngine.GameObject("Town").transform; town.SetParent(dress, false);
string P(string n) => "Assets/PolygonFantasyKingdom/Prefabs/Buildings/Preset_Houses/" + n + "_Optimized.prefab";
var houses = new (string prefab, string name, float x, float z)[]
{
    (P("SM_Bld_Preset_Tavern_01"), "The Sheared Ram", -1706, 1672),
    (P("SM_Bld_Preset_Blacksmith_01"), "Smithy", -1650, 1703),
    (P("SM_Bld_Preset_House_03"), "House (west road)", -1726, 1624),
    (P("SM_Bld_Preset_House_06"), "House (windmill road)", -1774, 1680),
    (P("SM_Bld_Preset_Hut_01"), "Father Urhney's hut", -1722, 1432),
};
var built = "";
var spurs = new System.Collections.Generic.List<(UnityEngine.Vector2[] line, float width)>();
foreach (var (prefabPath, name, x, z) in houses)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
    if (!prefab) { built += name + " missing; "; continue; }
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, town);
    go.name = name;
    // face the nearest road (the presets' fronts look down -z)
    var best = V(x, z - 10); var bd = float.MaxValue;
    foreach (var (line, width) in roads)
        for (var i = 0; i + 1 < line.Length; i++)
        {
            var a = line[i]; var ab = line[i + 1] - a; var tt = UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(V(x, z) - a, ab) / ab.sqrMagnitude); var q = a + ab * tt;
            var dd = (q - V(x, z)).magnitude; if (dd < bd) { bd = dd; best = q; }
        }
    var to = best - V(x, z);
    var yaw = UnityEngine.Mathf.Atan2(to.x, to.y) * UnityEngine.Mathf.Rad2Deg + 180f;
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(x, Ground(x, z) - 0.1f, z), UnityEngine.Quaternion.Euler(0, yaw, 0));
    foreach (var r in go.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
    {
        var ms = r.sharedMaterials;
        for (var i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name.StartsWith("PolygonFantasyKingdom_Mat_0") && !ms[i].name.Contains("Glass") && !name.Contains("Urhney")) ms[i] = red;
        r.sharedMaterials = ms;
    }
    go.isStatic = true;
    var b = new UnityEngine.Bounds(go.transform.position, UnityEngine.Vector3.zero);
    foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>()) b.Encapsulate(r.bounds);
    keepOut.Add((V(b.center.x, b.center.z), UnityEngine.Mathf.Max(b.extents.x, b.extents.z) + 4f));
    built += name + " " + b.size.ToString("F0") + " road " + bd.ToString("F0") + "m; ";
    // a cobbled path from its door to the road (painted below with the other roads)
    var door = V(x, z) + to.normalized * (UnityEngine.Mathf.Max(b.extents.x, b.extents.z) * 0.8f);
    spurs.Add((new[] { door, best }, 2.6f));
    for (var k = 0f; k <= 1f; k += 0.2f) keepOut.Add((UnityEngine.Vector2.Lerp(door, best, k), 3.5f)); // no trees on the path
}
paintRoads(spurs.ToArray());

// ---- rail fences: the windmill's wheat field, and the field edge north of the road to the windmill -------------
var fences = new UnityEngine.GameObject("Fences").transform; fences.SetParent(dress, false);
var rail = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Fence_03.prefab");
var broken = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Fence_04.prefab");
var segments = 0;
void Fence(UnityEngine.Vector2[] line, float gapFrom = -1f, float gapTo = -1f)
{
    var walked = 0f;
    for (var i = 0; i + 1 < line.Length; i++)
    {
        var a = line[i]; var b = line[i + 1]; var len = (b - a).magnitude; var dir = (b - a) / len;
        var n = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(len / 2.5f)); var step = len / n;
        for (var k = 0; k < n; k++, walked += step)
        {
            if (walked >= gapFrom && walked < gapTo) continue;   // a gate gap
            var p = a + dir * (k * step + step * 0.5f);
            var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(R() < 0.06f ? broken : rail, fences);
            var yaw = UnityEngine.Mathf.Atan2(dir.x, dir.y) * UnityEngine.Mathf.Rad2Deg - 90f;
            go.transform.SetPositionAndRotation(new UnityEngine.Vector3(p.x, Ground(p.x, p.y) - 0.05f, p.y), UnityEngine.Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = new UnityEngine.Vector3(step / 2.5f, 1f, 1f);
            go.isStatic = true;
            segments++;
        }
    }
}
// the crop field west of the windmill road, its gate on the road side
Fence(new[] { V(-1782, 1712), V(-1782, 1732), V(-1806, 1732), V(-1806, 1700), V(-1782, 1700), V(-1782, 1712) }, 0f, 4f);
// and its crop: yellow clumps in loose rows
var crops = new UnityEngine.GameObject("Crop Field").transform; crops.SetParent(dress, false);
var clump = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_CropField_Clump_01.prefab");
var clump2 = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_CropField_Clump_02.prefab");
for (var cz = 1702f; cz < 1731f; cz += 2.1f)
for (var cx = -1804.5f; cx < -1783f; cx += 2.0f)
{
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(R() < 0.5f ? clump : clump2, crops);
    var px = cx + (R() - 0.5f) * 0.6f; var pz = cz + (R() - 0.5f) * 0.6f;
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(px, Ground(px, pz) - 0.05f, pz), UnityEngine.Quaternion.Euler(0, R() * 360f, 0));
    go.transform.localScale = UnityEngine.Vector3.one * UnityEngine.Mathf.Lerp(0.9f, 1.15f, R());
    go.isStatic = true;
}
keepOut.Add((V(-1794, 1716), 20f));
// the field edge along the road, north side, with an opening every so often
Fence(new[] { V(-1736, 1662), V(-1752, 1678), V(-1760, 1700) }, 10f, 14f);

// ---- woods: a forest west of the castle, a belt toward the swamp, clumps along the roads and the river --------
string K(string n) => "Assets/PolygonFantasyKingdom/Prefabs/Environments/" + n + ".prefab";
var leafy = new[] { K("SM_Env_Tree_Round_01"), K("SM_Env_Tree_Round_02"), K("SM_Env_Tree_Round_03"), K("SM_Env_Tree_Round_04"), K("SM_Env_Tree_Large_01"), K("SM_Env_Tree_Large_02"), K("SM_Env_Tree_Thin_01"), K("SM_Env_Tree_Thin_03") };
var dead = new[] { K("SM_Env_Tree_Dead_01"), K("SM_Env_Tree_Dead_02"), K("SM_Env_Tree_Dead_03") };
var trees = new UnityEngine.GameObject("Woods").transform; trees.SetParent(dress, false);
var placedTrees = new System.Collections.Generic.List<UnityEngine.Vector2>();
void Tree(float x, float z, bool forest)
{
    if (!Clear(x, z, 0f)) return;
    foreach (var q in placedTrees) if ((q - V(x, z)).sqrMagnitude < 5.5f * 5.5f) return;
    var set = forest && R() < 0.12f ? dead : leafy;
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(set[rng.Next(set.Length)]);
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, trees);
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(x, Ground(x, z) - 0.15f, z), UnityEngine.Quaternion.Euler(0, R() * 360f, 0));
    // the large trees are 20 m crowns at full size; the others vary round 1
    go.transform.localScale = UnityEngine.Vector3.one * (prefab.name.Contains("Large") ? UnityEngine.Mathf.Lerp(0.6f, 0.8f, R()) : UnityEngine.Mathf.Lerp(0.8f, 1.2f, R()));
    go.isStatic = true;
    placedTrees.Add(V(x, z));
}
// (area, spacing, how much of it is wooded): Perlin decides the clumps, so the woods have glades
var areas = new (float x0, float x1, float z0, float z1, float step, float cover)[]
{
    (-1800, -1714, 1480, 1625, 6.5f, 0.62f),   // the forest west of the castle
    (-1760, -1600, 1468, 1494, 7.0f, 0.45f),   // a belt between the castle and the swamp
    (-1800, -1700, 1625, 1700, 11f, 0.40f),    // clumps round the house and the woodcutting trees
    (-1740, -1640, 1705, 1800, 12f, 0.35f),    // scattered north of the church toward the river
    (-1800, -1745, 1755, 1820, 9f, 0.45f),     // behind the windmill
};
foreach (var a in areas)
    for (var z = a.z0; z < a.z1; z += a.step)
    for (var x = a.x0; x < a.x1; x += a.step)
    {
        var px = x + (R() - 0.5f) * a.step * 0.9f; var pz = z + (R() - 0.5f) * a.step * 0.9f;
        var n = UnityEngine.Mathf.PerlinNoise(px * 0.035f + 11f, pz * 0.035f + 3f);
        if (n < 1f - a.cover) continue;
        Tree(px, pz, a.cover > 0.5f);
    }

// ---- the stone bridge's parapets: Lumbridge's bridge has low stone walls; stone railings on both edges of the deck --------------
var parapets = new UnityEngine.GameObject("Bridge Parapets").transform; parapets.SetParent(dress, false);
var deck = new UnityEngine.Bounds(); var gotDeck = false;
foreach (UnityEngine.Transform c in island.Find("Environment"))
    if (c.name.StartsWith("SM_Env_Rock_Bridge"))
        foreach (var r in c.GetComponentsInChildren<UnityEngine.Renderer>()) { if (!gotDeck) { deck = r.bounds; gotDeck = true; } else deck.Encapsulate(r.bounds); }
var railing = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PolygonFantasyKingdom/Prefabs/Castle/SM_Bld_Castle_Battlements_06_Short.prefab");
UnityEngine.Physics.SyncTransforms();
var railings = 0;
if (gotDeck)
    foreach (var side in new[] { deck.min.z + 0.33f, deck.max.z - 0.33f })
        for (var rx = deck.min.x + 3.2f; rx + 1.88f < deck.max.x - 2.8f; rx += 3.7f)
        {
            var top = deck.max.y;
            if (UnityEngine.Physics.Raycast(new UnityEngine.Vector3(rx + 1.88f, deck.max.y + 5f, side), UnityEngine.Vector3.down, out var hit, 20f)) top = hit.point.y;
            var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(railing, parapets);
            go.transform.SetPositionAndRotation(new UnityEngine.Vector3(rx + 1.88f, top, side), UnityEngine.Quaternion.identity);
            go.isStatic = true;
            railings++;
        }
built += "deck " + deck.min.ToString("F1") + "-" + deck.max.ToString("F1") + ", railings " + railings + "; ";

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
return "roads painted " + painted + " texels, roofs " + roofs + ", trees " + placedTrees.Count + ", fence segments " + segments + " | " + built;
