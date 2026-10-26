// Ravebridge, dressed after Lumbridge (docs/references/ravebridge/web/README.md): roads drawn with the road tool
// (WorldRoad, grey cobbles as Lumbridge's stone roads), red tiled roofs, more of the town, fenced fields and farms, woods
// and trees, and parapets on the stone bridge. Rerunnable: it clears its own group ("Dressing") and the path paint.
// Run ravebridge-layout.cs first (the church, graveyard and Bob's axes in Lumbridge's places). Edit mode, Unity CLI:
// unity command eval_file --file tools/ravebridge/ravebridge-dress.cs --timeout 180
var island = UnityEngine.GameObject.Find("============ WORLD/Islands").transform.Find("Ravebridge");
var old = island.Find("Dressing");
if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var dress = new UnityEngine.GameObject("Dressing").transform; dress.SetParent(island, false);
var fieldGroup = new UnityEngine.GameObject("Fields").transform; fieldGroup.SetParent(dress, false);
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
float SegDist(UnityEngine.Vector2 p, UnityEngine.Vector2 a, UnityEngine.Vector2 b) { var ab = b - a; var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return (p - (a + ab * t)).magnitude; }
float LineDist(UnityEngine.Vector2 p, UnityEngine.Vector2[] line) { var d = float.MaxValue; for (var i = 0; i + 1 < line.Length; i++) d = UnityEngine.Mathf.Min(d, SegDist(p, line[i], line[i + 1])); return d; }
UnityEngine.Vector2 V(float x, float z) => new UnityEngine.Vector2(x, z);
var rng = new System.Random(17);
float R() => (float)rng.NextDouble();
bool InCastle(float x, float z) => x > -1712 && x < -1598 && z > 1490 && z < 1618;
var log = "";

// ---- 1. the path paint comes off: every painted road is drawn with the road tool now ---------------------------
// cobbles (7) and footpath (1) go back to grass (2) outside the castle; the dirt rims (0) the first pass painted
// beside its roads go too (the desert and the beaches keep theirs: they are far from these lines)
var oldLines = new[]
{
    new[] { V(-1652, 1660), V(-1672, 1652), V(-1690, 1648), V(-1712, 1639), V(-1730, 1647), V(-1748, 1664), V(-1762, 1698), V(-1770, 1722), V(-1776, 1737) },
    new[] { V(-1662, 1688), V(-1676, 1697), V(-1690, 1699) },
    new[] { V(-1655, 1667), V(-1642, 1672), V(-1630, 1673) },
    new[] { V(-1606, 1550), V(-1603, 1520), V(-1604, 1500), V(-1618, 1480), V(-1650, 1466), V(-1684, 1456) },
};
var oldHouses = new[] { V(-1706, 1672), V(-1650, 1703), V(-1726, 1624), V(-1774, 1680), V(-1722, 1432) };
var cleared = 0;
foreach (var t in terrains)
{
    var data = t.terrainData; var res = data.alphamapResolution; var o = t.transform.position; var size = data.size;
    if (data.alphamapLayers < 8) continue;
    int x0 = UnityEngine.Mathf.Clamp((int)((-1840 - o.x) / size.x * res), 0, res), x1 = UnityEngine.Mathf.Clamp((int)((-1420 - o.x) / size.x * res), 0, res);
    int z0 = UnityEngine.Mathf.Clamp((int)((1370 - o.z) / size.z * res), 0, res), z1 = UnityEngine.Mathf.Clamp((int)((1900 - o.z) / size.z * res), 0, res);
    if (x1 <= x0 || z1 <= z0) continue;
    var maps = data.GetAlphamaps(x0, z0, x1 - x0, z1 - z0);
    for (var j = 0; j < z1 - z0; j++)
    for (var i = 0; i < x1 - x0; i++)
    {
        var wx = o.x + (x0 + i + 0.5f) / res * size.x; var wz = o.z + (z0 + j + 0.5f) / res * size.z;
        if (InCastle(wx, wz)) continue;
        var move = maps[j, i, 7] + maps[j, i, 1];
        maps[j, i, 7] = 0f; maps[j, i, 1] = 0f;
        var p = V(wx, wz);
        var nearOld = false;
        foreach (var line in oldLines) if (LineDist(p, line) < 7f) { nearOld = true; break; }
        if (!nearOld) foreach (var h in oldHouses) if ((p - h).magnitude < 26f) { nearOld = true; break; }
        if (nearOld) { move += maps[j, i, 0]; maps[j, i, 0] = 0f; }
        if (move <= 0f) continue;
        maps[j, i, 2] += move; cleared++;
    }
    UnityEditor.Undo.RegisterCompleteObjectUndo(data, "Ravebridge paths");
    data.SetAlphamaps(x0, z0, maps);
}
log += "path paint cleared " + cleared + " texels; ";

// ---- 2. the roads, as Lumbridge's: from the castle gate north to the store and west to the windmill, south past
// Bob's axes and the church to the swamp, and on the east bank north past the goblins to the farms --------------------
var roads = new System.Collections.Generic.List<(string name, UnityEngine.Vector2[] line, float width)>
{
    ("Road to the windmill", new[] { V(-1656, 1651), V(-1672, 1652), V(-1690, 1648), V(-1712, 1639), V(-1730, 1647), V(-1748, 1664), V(-1762, 1698), V(-1770, 1722), V(-1776, 1736) }, 3.5f),
    ("Road to the wooden bridge", new[] { V(-1655, 1666), V(-1642, 1672), V(-1629, 1673) }, 3.2f),
    ("Road to the church and the swamp", new[] { V(-1611, 1551), V(-1606, 1530), V(-1607, 1508), V(-1614, 1494), V(-1622, 1484), V(-1622, 1462), V(-1626, 1450), V(-1642, 1444), V(-1666, 1446), V(-1688, 1452) }, 3.5f),
    ("East bank road", new[] { V(-1550, 1577), V(-1533, 1581), V(-1521, 1592), V(-1519, 1610), V(-1518, 1626), V(-1521, 1642), V(-1528, 1660), V(-1527, 1678), V(-1536, 1690), V(-1544, 1700), V(-1551, 1710), V(-1559, 1722), V(-1559, 1760), V(-1558, 1782), V(-1554, 1790), V(-1548, 1800), V(-1540, 1812), V(-1531, 1825), V(-1525, 1836), V(-1513, 1842), V(-1496, 1845), V(-1485, 1850), V(-1470, 1857) }, 3.2f),
    ("Road to the desert gate", new[] { V(-1521, 1592), V(-1506, 1593), V(-1490, 1593) }, 3.0f),
    ("Path to the farmhouse", new[] { V(-1559, 1738), V(-1566, 1737), V(-1573, 1737) }, 2.6f),
    ("Road north past the fields", new[] { V(-1668, 1687), V(-1680, 1700), V(-1692, 1714), V(-1704, 1736), V(-1722, 1739), V(-1750, 1738), V(-1776, 1736) }, 3.2f),
};

// ---- 3. red tiled roofs on the town's houses (Lumbridge's look): the palette with the red tiles --------------------
var red = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyKingdom/Materials/PolygonFantasyKingdom_Mat_02_A.mat");
var roofs = 0;
foreach (var name in new[] { "General Store", "House (Bob's axes)", "House", "Castle Kitchen", "Church" })
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

// ---- what new things keep clear of ----------------------------------------------------------------------------
var keepOut = new System.Collections.Generic.List<(UnityEngine.Vector2 at, float r)>();
foreach (var c in island.Find("Playable/Places").GetComponentsInChildren<UnityEngine.Renderer>(true))
{
    if (c.transform.IsChildOf(island.Find("Playable/Places/Swamp"))) continue;
    var b = c.bounds; keepOut.Add((V(b.center.x, b.center.z), UnityEngine.Mathf.Max(b.extents.x, b.extents.z) + 5f));
}
foreach (var n in new[] { "Small House", "Small Goblin House", "Farmer House" }) { var h = island.Find(n); if (h) keepOut.Add((V(h.position.x, h.position.z), 14f)); }
foreach (UnityEngine.Transform n in island.Find("Playable/NPCs")) keepOut.Add((V(n.position.x, n.position.z), 6f));
foreach (UnityEngine.Transform c in island.Find("Playable"))
    if (c.name.Contains("Lv."))
        foreach (var e in c.GetComponentsInChildren<UnityEngine.Transform>(true))
            if (e != c && (e.GetComponent<EnemyController>() || e.name.Contains("Patch") || e.name.Contains("Rock") || e.name.Contains("Fishing")))
                keepOut.Add((V(e.position.x, e.position.z), e.GetComponent<EnemyController>() ? 8f : 5f));
foreach (var f in island.GetComponentsInChildren<ResourceField>(true)) for (var i = 0; i < f.Count; i++) keepOut.Add((V(f.Instances[i].position.x, f.Instances[i].position.z), 5f));
// the cow field, the farm's fences and the castle's own fences stay open inside
foreach (UnityEngine.Transform c in island.Find("Environment"))
    if (c.name.StartsWith("Fence") && c.gameObject.activeSelf)
        foreach (UnityEngine.Transform piece in c) keepOut.Add((V(piece.position.x, piece.position.z), 3f));
var fields = new System.Collections.Generic.List<UnityEngine.Rect>
{
    new UnityEngine.Rect(-1616, 1430, 34, 36),    // the graveyard
    new UnityEngine.Rect(-1612, 1462, 30, 34),    // the church and its yard
};
bool InField(float x, float z, float pad) { foreach (var f in fields) if (x > f.xMin - pad && x < f.xMax + pad && z > f.yMin - pad && z < f.yMax + pad) return true; return false; }
bool Clear(float x, float z, float extra)
{
    if (InCastle(x, z)) return false;
    if (Ground(x, z) < 7.2f) return false;                                       // the river, its banks and the shore
    if (InField(x, z, 2f + extra)) return false;
    var p = V(x, z);
    foreach (var (_, line, width) in roads) if (LineDist(p, line) < width * 0.5f + 3f + extra) return false;
    foreach (var (at, r) in keepOut) if ((p - at).magnitude < r + extra) return false;
    return true;
}
UnityEngine.GameObject Put(string path, UnityEngine.Transform parent, float x, float z, float yaw, float scale = 1f, float sink = 0.05f)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) throw new System.Exception("missing " + path);
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(x, Ground(x, z) - sink, z), UnityEngine.Quaternion.Euler(0, yaw, 0));
    go.transform.localScale = UnityEngine.Vector3.one * scale;
    go.isStatic = true;
    return go;
}
string K(string n) => "Assets/PolygonFantasyKingdom/Prefabs/" + n + ".prefab";
// tilled soil: the dirt layer (0) painted into a rectangle, soft at its edge
void Soil(UnityEngine.Rect r)
{
    foreach (var t in terrains)
    {
        var data = t.terrainData; var res = data.alphamapResolution; var o = t.transform.position; var size = data.size;
        int x0 = UnityEngine.Mathf.Clamp((int)((r.xMin - 2 - o.x) / size.x * res), 0, res), x1 = UnityEngine.Mathf.Clamp((int)((r.xMax + 2 - o.x) / size.x * res), 0, res);
        int z0 = UnityEngine.Mathf.Clamp((int)((r.yMin - 2 - o.z) / size.z * res), 0, res), z1 = UnityEngine.Mathf.Clamp((int)((r.yMax + 2 - o.z) / size.z * res), 0, res);
        if (x1 <= x0 || z1 <= z0) continue;
        var maps = data.GetAlphamaps(x0, z0, x1 - x0, z1 - z0);
        for (var j = 0; j < z1 - z0; j++)
        for (var i = 0; i < x1 - x0; i++)
        {
            var wx = o.x + (x0 + i + 0.5f) / res * size.x; var wz = o.z + (z0 + j + 0.5f) / res * size.z;
            var inside = UnityEngine.Mathf.Min(UnityEngine.Mathf.Min(wx - r.xMin, r.xMax - wx), UnityEngine.Mathf.Min(wz - r.yMin, r.yMax - wz));
            var w = UnityEngine.Mathf.Clamp01((inside + 0.5f) / 1.5f) * 0.95f;
            if (w <= 0f) continue;
            for (var l = 0; l < maps.GetLength(2); l++) maps[j, i, l] *= 1f - w;
            maps[j, i, 0] += w;
        }
        UnityEditor.Undo.RegisterCompleteObjectUndo(data, "Ravebridge soil");
        data.SetAlphamaps(x0, z0, maps);
    }
}
// a cabbage field: soil, and cabbages in rows across it
void Cabbages(UnityEngine.Rect r)
{
    Soil(r);
    for (var rx = r.xMin + 1.6f; rx < r.xMax - 1f; rx += 1.8f)
    for (var rz = r.yMin + 1.4f; rz < r.yMax - 1f; rz += 1.3f)
        if (R() < 0.88f) Put(R() < 0.5f ? K("Props/SM_Prop_Cabbage_01") : K("Props/SM_Prop_Cabbage_02"), fieldGroup, rx + (R() - 0.5f) * 0.25f, rz + (R() - 0.5f) * 0.2f, R() * 360f, UnityEngine.Mathf.Lerp(1.1f, 1.5f, R()), 0.02f);
}

// ---- 4. more of the town (Lumbridge's pub, smithy and houses), each facing its road, with a path to its door -----
var town = new UnityEngine.GameObject("Town").transform; town.SetParent(dress, false);
string P(string n) => K("Buildings/Preset_Houses/" + n + "_Optimized");
var houses = new (string prefab, string name, float x, float z)[]
{
    (P("SM_Bld_Preset_Tavern_01"), "The Sheared Ram", -1706, 1672),
    (P("SM_Bld_Preset_Blacksmith_01"), "Smithy", -1650, 1703),
    (P("SM_Bld_Preset_House_03"), "House (west road)", -1726, 1624),
    (P("SM_Bld_Preset_House_06"), "House (windmill road)", -1774, 1680),
    (P("SM_Bld_Preset_Hut_01"), "Father Urhney's hut", -1722, 1432),
};
foreach (var (prefabPath, name, x, z) in houses)
{
    var best = V(x, z - 10); var bd = float.MaxValue;
    foreach (var (_, line, width) in roads)
        for (var i = 0; i + 1 < line.Length; i++)
        {
            var a = line[i]; var ab = line[i + 1] - a; var tt = UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(V(x, z) - a, ab) / ab.sqrMagnitude); var q = a + ab * tt;
            var dd = (q - V(x, z)).magnitude; if (dd < bd) { bd = dd; best = q; }
        }
    var to = best - V(x, z);
    // the presets' fronts face local +z
    var go = Put(prefabPath, town, x, z, UnityEngine.Mathf.Atan2(to.x, to.y) * UnityEngine.Mathf.Rad2Deg, 1f, 0.1f);
    go.name = name;
    foreach (var r in go.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
    {
        var ms = r.sharedMaterials;
        for (var i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name.StartsWith("PolygonFantasyKingdom_Mat_0") && !ms[i].name.Contains("Glass") && !name.Contains("Urhney")) ms[i] = red;
        r.sharedMaterials = ms;
    }
    var b = new UnityEngine.Bounds(go.transform.position, UnityEngine.Vector3.zero);
    foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>()) b.Encapsulate(r.bounds);
    keepOut.Add((V(b.center.x, b.center.z), UnityEngine.Mathf.Max(b.extents.x, b.extents.z) + 4f));
    var door = V(x, z) + to.normalized * (UnityEngine.Mathf.Min(b.extents.x, b.extents.z) + 0.5f);
    if (bd > 3f) roads.Add(("Path to " + name, new[] { door, best }, 2.6f));
}

// ---- 5. lay the roads -----------------------------------------------------------------------------------------
var roadGroup = new UnityEngine.GameObject("Roads").transform; roadGroup.SetParent(dress, false);
foreach (var (name, line, width) in roads)
{
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(roadGroup, false);
    go.transform.position = new UnityEngine.Vector3(line[0].x, Ground(line[0].x, line[0].y), line[0].y);
    var road = go.AddComponent<WorldRoad>();
    road.RoadSurface = WorldRoad.Surface.Cobbles;
    road.Width = width;
    foreach (var p in line) road.AddPoint(go.transform.InverseTransformPoint(new UnityEngine.Vector3(p.x, Ground(p.x, p.y), p.y)));
    if (name.StartsWith("Path to ")) road.StartRound = 0f;   // stops at the doorstep
    road.Rebuild();
}
log += roads.Count + " roads; ";

// ---- 6. fenced fields: the windmill's crop, Fred's cabbages and sheep pen, a field north of the town --------------
var rail = K("Props/SM_Prop_Fence_03"); var broken = K("Props/SM_Prop_Fence_04");
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
            if (walked >= gapFrom && walked < gapTo) continue;   // the gate
            var p = a + dir * (k * step + step * 0.5f);
            var go = Put(R() < 0.06f ? broken : rail, fieldGroup, p.x, p.y, UnityEngine.Mathf.Atan2(dir.x, dir.y) * UnityEngine.Mathf.Rad2Deg - 90f, 1f, 0.05f);
            go.transform.localScale = new UnityEngine.Vector3(step / 2.5f, 1f, 1f);
            segments++;
        }
    }
}
// walks a rectangle from its south west corner: south side, east side, north side, west side; the gate is measured along that walk
void FenceRect(UnityEngine.Rect r, float gapFrom, float gapTo)
{
    Fence(new[] { V(r.xMin, r.yMin), V(r.xMax, r.yMin), V(r.xMax, r.yMax), V(r.xMin, r.yMax), V(r.xMin, r.yMin) }, gapFrom, gapTo);
    fields.Add(r);
}
var clump = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_CropField_Clump_01.prefab";
var clump2 = "Assets/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_CropField_Clump_02.prefab";
void Crop(UnityEngine.Rect r)
{
    for (var cz = r.yMin + 2f; cz < r.yMax - 1f; cz += 2.1f)
    for (var cx = r.xMin + 1.5f; cx < r.xMax - 1f; cx += 2.0f)
        Put(R() < 0.5f ? clump : clump2, fieldGroup, cx + (R() - 0.5f) * 0.6f, cz + (R() - 0.5f) * 0.6f, R() * 360f, UnityEngine.Mathf.Lerp(0.9f, 1.15f, R()));
}
// the windmill's crop field, west of its road; the gate on the road (east) side
var crop = new UnityEngine.Rect(-1806, 1700, 24, 32);
FenceRect(crop, 24f + 10f, 24f + 14f);
Crop(crop);
// Fred's cabbage field, west of the farm road and south of the farmhouse: tilled rows with cabbages; gate east
var cabbages = new UnityEngine.Rect(-1600, 1752, 26, 18);
FenceRect(cabbages, 26f + 6f, 26f + 10f);
Cabbages(cabbages);
// the sheep pen beside it, with hay and a cart (Ravenfall has no sheep yet; the pen waits for them); gate east
var pen = new UnityEngine.Rect(-1600, 1776, 20, 16);
FenceRect(pen, 20f + 5f, 20f + 9f);
Put(K("Props/SM_Prop_Hay_Pile_02"), fieldGroup, pen.xMin + 4f, pen.yMin + 4f, 30f);
Put(K("Props/SM_Prop_Hay_Bale_01"), fieldGroup, pen.xMax - 4f, pen.yMax - 3f, 80f);
Put(K("Props/SM_Prop_Hay_Cart_01"), fieldGroup, pen.xMax + 3f, pen.yMin + 3f, 0f);
// a field north of the town where the church used to stand, at the end of the store's road north; gate south east
var north = new UnityEngine.Rect(-1726, 1706, 30, 26);
FenceRect(north, 22f, 27f);
Crop(north);
// two cabbage fields north of the loop road, gates on the road (south) side
var cabbagesWest = new UnityEngine.Rect(-1766, 1743, 26, 22);
FenceRect(cabbagesWest, 10f, 15f);
Cabbages(cabbagesWest);
var cabbagesEast = new UnityEngine.Rect(-1732, 1744, 24, 20);
FenceRect(cabbagesEast, 9f, 14f);
Cabbages(cabbagesEast);
log += "fence segments " + segments + "; ";

// ---- 7. woods and trees: Lumbridge has them everywhere: a forest west of the castle, clumps along every road and
// the river, trees in the farms' corners and on the east bank ----------------------------------------------------
var leafy = new[] { K("Environments/SM_Env_Tree_Round_01"), K("Environments/SM_Env_Tree_Round_02"), K("Environments/SM_Env_Tree_Round_03"), K("Environments/SM_Env_Tree_Round_04"), K("Environments/SM_Env_Tree_Large_01"), K("Environments/SM_Env_Tree_Large_02"), K("Environments/SM_Env_Tree_Thin_01"), K("Environments/SM_Env_Tree_Thin_03") };
var dead = new[] { K("Environments/SM_Env_Tree_Dead_01"), K("Environments/SM_Env_Tree_Dead_02"), K("Environments/SM_Env_Tree_Dead_03") };
var trees = new UnityEngine.GameObject("Woods").transform; trees.SetParent(dress, false);
var placedTrees = new System.Collections.Generic.List<UnityEngine.Vector2>();
void Tree(float x, float z, bool forest)
{
    if (!Clear(x, z, 0f)) return;
    foreach (var q in placedTrees) if ((q - V(x, z)).sqrMagnitude < 5.5f * 5.5f) return;
    var set = forest && R() < 0.12f ? dead : leafy;
    var path = set[rng.Next(set.Length)];
    // the large trees are 20 m crowns at full size; the others vary round 1
    Put(path, trees, x, z, R() * 360f, path.Contains("Large") ? UnityEngine.Mathf.Lerp(0.6f, 0.8f, R()) : UnityEngine.Mathf.Lerp(0.8f, 1.2f, R()), 0.15f);
    placedTrees.Add(V(x, z));
}
// (area, spacing, how much of it is wooded): Perlin decides the clumps, so the woods have glades
var areas = new (float x0, float x1, float z0, float z1, float step, float cover)[]
{
    (-1800, -1714, 1480, 1625, 6.5f, 0.62f),   // the forest west of the castle
    (-1760, -1630, 1468, 1494, 7.0f, 0.40f),   // a belt between the castle and the swamp
    (-1800, -1700, 1625, 1700, 10f, 0.45f),    // round the house and the woodcutting trees
    (-1760, -1640, 1735, 1830, 10f, 0.42f),    // north of the town toward the river bend
    (-1830, -1760, 1750, 1840, 9f, 0.48f),     // behind the windmill
    (-1650, -1600, 1620, 1720, 11f, 0.30f),    // along the river's west bank north of the bridge
    (-1590, -1470, 1600, 1700, 9f, 0.38f),     // the east bank round the goblins
    (-1600, -1470, 1700, 1760, 10f, 0.30f),    // the east bank south of the farm
    (-1600, -1470, 1795, 1880, 10f, 0.36f),    // the east bank north of the farm
    (-1660, -1575, 1405, 1440, 8f, 0.40f),     // south of the graveyard, toward the swamp
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
// a few along each road, as Lumbridge's roads have
foreach (var (_, line, width) in roads)
    for (var i = 0; i + 1 < line.Length; i++)
    {
        var a = line[i]; var b = line[i + 1]; var len = (b - a).magnitude; var dir = (b - a) / len; var side = V(dir.y, -dir.x);
        for (var s = 4f; s < len; s += 11f)
            if (R() < 0.45f) { var p = a + dir * s + side * (R() < 0.5f ? 1f : -1f) * (width * 0.5f + 4.5f + R() * 2f); Tree(p.x, p.y, false); }
    }
// and single trees sprinkled over the open country, as Lumbridge's map is dotted with them
for (var z = 1395f; z < 1880f; z += 15f)
for (var x = -1830f; x < -1440f; x += 15f)
    if (R() < 0.28f) Tree(x + (R() - 0.5f) * 12f, z + (R() - 0.5f) * 12f, false);
log += "trees " + placedTrees.Count + "; ";

// ---- 8. the stone bridge's parapets: stone balustrades on both edges of the deck -----------------------------------
var parapets = new UnityEngine.GameObject("Bridge Parapets").transform; parapets.SetParent(dress, false);
var deck = new UnityEngine.Bounds(); var gotDeck = false;
foreach (UnityEngine.Transform c in island.Find("Environment"))
    if (c.name.StartsWith("SM_Env_Rock_Bridge"))
        foreach (var r in c.GetComponentsInChildren<UnityEngine.Renderer>()) { if (!gotDeck) { deck = r.bounds; gotDeck = true; } else deck.Encapsulate(r.bounds); }
var railing = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(K("Castle/SM_Bld_Castle_Battlements_06_Short"));
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
log += "parapets " + railings + "; roofs " + roofs;

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
return log;
