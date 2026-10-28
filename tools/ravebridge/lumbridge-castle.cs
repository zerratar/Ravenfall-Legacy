// Run with: unity command eval_file --file tools/ravebridge/lumbridge-castle.cs (after lumbridge-swap.cs)
//
// Brings Ravebridge's own castle and its stone and wooden bridges from MainWorld into the Ravenfall Lumbridge
// (Assets/Scenes/LumbridgeRavenfall.unity): the user liked them better than the generated ones (2026-10-28). MainWorld
// is opened beside it, copied from and closed again unsaved.
// - The castle: Ravebridge's "Castle" group, its gate east as the map's, scaled to the map's compound (tiles 63 to 95
//   by 63 to 100) and set on its middle. Ravebridge's castle stands on a slope, so the ground under it (and a margin
//   round it, blended) is copied too, at the same scale. The generated castle, its walls, floors, roofs, props and
//   the roads that only run inside it are taken out first, and the compound's ground is painted grass.
// - The bridges: the stone one (its rock pieces and the dressing's parapets) over the map's stone bridge east of the
//   castle, the wooden one over the map's bridge to the north, each stretched along its length to span the map's
//   river and set with its deck at the map's deck; the generated decks and parapets come out.
// Rerunnable: what it brings lives under "Ravebridge Castle" and "Ravebridge Bridges" and is made again; the ground is
// imprinted again from MainWorld each time.
var sb = new System.Text.StringBuilder();
var stage = "start";
UnityEngine.SceneManagement.Scene main = default;
try
{

const string ScenePath = "Assets/Scenes/LumbridgeRavenfall.unity";
const float Tile = 2.5f;
var target = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (target.path != ScenePath) return "open " + ScenePath + " first";
if (target.isDirty) return "save the scene first";
var root = UnityEngine.GameObject.Find("Lumbridge (Ravenfall)").transform;
var terrain = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Terrain>();
var td = terrain.terrainData;
System.Func<float, float, float> groundAt = (x, z) => terrain.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + terrain.transform.position.y;

// the map's places (tiles)
const int CX0 = 63, CX1 = 95, CY0 = 63, CY1 = 100;   // the castle compound, curtain wall to gatehouse
System.Func<float, float, bool> inCompound = (wx, wz) => wx >= CX0 * Tile && wx < (CX1 + 1) * Tile && wz >= CY0 * Tile && wz < (CY1 + 1) * Tile;
// the bridges: tiles along x, two rows
var stoneTiles = (x0: 104, x1: 114, y0: 89, y1: 90);
var woodTiles = (x0: 95, x1: 103, y0: 125, y1: 126);

// ---- MainWorld beside it -------------------------------------------------------------------------------------------
stage = "open MainWorld";
main = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainWorld.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(target);
UnityEngine.Transform rb = null;
foreach (var r in main.GetRootGameObjects()) { var t = r.transform.Find("Islands/Ravebridge"); if (t) rb = t; }
if (!rb) return "no Ravebridge in MainWorld";
var rbCastle = rb.Find("Castle");
var rbEnv = rb.Find("Environment");
var rbParapets = rb.Find("Dressing/Bridge Parapets");
var rbWood = rbEnv ? rbEnv.Find("Wooden Bridge") : null;
// Ravebridge's ground
UnityEngine.Terrain[] rbTerrains = rb.GetComponentsInChildren<UnityEngine.Terrain>(true);
System.Func<float, float, float> rbGround = (x, z) =>
{
    foreach (var t in rbTerrains)
    {
        var p = t.transform.position; var s = t.terrainData.size;
        if (x >= p.x && z >= p.z && x <= p.x + s.x && z <= p.z + s.z) return t.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + p.y;
    }
    return float.NaN;
};
System.Func<UnityEngine.Transform[], UnityEngine.Bounds> boundsOf = ts =>
{
    var b = new UnityEngine.Bounds(); var first = true;
    foreach (var t in ts) foreach (var r in t.GetComponentsInChildren<UnityEngine.Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
    return b;
};
// the highest mesh point near a point (a bridge's deck)
System.Func<UnityEngine.Transform[], UnityEngine.Vector3, float, float> topNear = (ts, at, radius) =>
{
    var best = float.MinValue;
    foreach (var t in ts)
        foreach (var mf in t.GetComponentsInChildren<UnityEngine.MeshFilter>())
        {
            if (!mf.sharedMesh || !mf.sharedMesh.isReadable) continue;
            var m = mf.transform.localToWorldMatrix;
            foreach (var v in mf.sharedMesh.vertices)
            {
                var w = m.MultiplyPoint3x4(v);
                if ((w.x - at.x) * (w.x - at.x) + (w.z - at.z) * (w.z - at.z) <= radius * radius) best = UnityEngine.Mathf.Max(best, w.y);
            }
        }
    return best;
};

// ---- take out the generated castle and bridges ----------------------------------------------------------------------
stage = "clear";
System.Func<float, float, bool> onStoneBridge = (wx, wz) => wx >= stoneTiles.x0 * Tile && wx < (stoneTiles.x1 + 1) * Tile && wz >= (stoneTiles.y0 - 0.5f) * Tile && wz < (stoneTiles.y1 + 1.5f) * Tile;
System.Func<float, float, bool> onWoodBridge = (wx, wz) => wx >= woodTiles.x0 * Tile && wx < (woodTiles.x1 + 1) * Tile && wz >= (woodTiles.y0 - 0.5f) * Tile && wz < (woodTiles.y1 + 1.5f) * Tile;
// (half a tile wider than the compound: the curtain wall stands on its edge lines)
System.Func<float, float, bool> nearCompound = (wx, wz) => wx >= (CX0 - 0.6f) * Tile && wx < (CX1 + 1.6f) * Tile && wz >= (CY0 - 0.6f) * Tile && wz < (CY1 + 1.6f) * Tile;
System.Func<float, float, bool> gone = (wx, wz) => nearCompound(wx, wz) || onStoneBridge(wx, wz) || onWoodBridge(wx, wz);
// the map's deck heights, read off the generated decks before they go
UnityEngine.Physics.SyncTransforms();
System.Func<float, float, float> deckAt = (wx, wz) =>
{
    var best = float.MinValue;
    foreach (var h in UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(wx, 200f, wz), UnityEngine.Vector3.down, 400f))
        if (h.collider is UnityEngine.MeshCollider && h.collider.transform.IsChildOf(root)) best = UnityEngine.Mathf.Max(best, h.point.y);
    return best;
};
var stoneMid = new UnityEngine.Vector3((stoneTiles.x0 + stoneTiles.x1 + 1) * 0.5f * Tile, 0, (stoneTiles.y0 + stoneTiles.y1 + 1) * 0.5f * Tile);
var woodMid = new UnityEngine.Vector3((woodTiles.x0 + woodTiles.x1 + 1) * 0.5f * Tile, 0, (woodTiles.y0 + woodTiles.y1 + 1) * 0.5f * Tile);
// the decks' tops from the map (the bridge tiles' own heights, as the builder scales them), so reruns, with the
// generated decks gone, still find them
const float Unit = Tile / 128f, HillScale = 0.6f;
var mapJson = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(@"G:\GitHub\rs-map-viewer\out\lumbridge.json"));
float stoneDeck = float.MinValue, woodDeck = float.MinValue;
foreach (var bt in mapJson["bridges"].ToObject<int[][]>())
{
    if (bt.Length < 6) continue;
    var top = UnityEngine.Mathf.Max(UnityEngine.Mathf.Max(bt[2], bt[3]), UnityEngine.Mathf.Max(bt[4], bt[5])) * Unit * HillScale;
    if (bt[0] >= stoneTiles.x0 && bt[0] <= stoneTiles.x1 && bt[1] >= stoneTiles.y0 && bt[1] <= stoneTiles.y1) stoneDeck = UnityEngine.Mathf.Max(stoneDeck, top);
    if (bt[0] >= woodTiles.x0 && bt[0] <= woodTiles.x1 && bt[1] >= woodTiles.y0 && bt[1] <= woodTiles.y1) woodDeck = UnityEngine.Mathf.Max(woodDeck, top);
}
if (stoneDeck == float.MinValue) stoneDeck = deckAt(stoneMid.x, stoneMid.z);
if (woodDeck == float.MinValue) woodDeck = deckAt(woodMid.x, woodMid.z);
var removed = 0;
foreach (UnityEngine.Transform group in root)
{
    if (!(group.name.StartsWith("Walls") || group.name.StartsWith("Objects"))) continue;
    var kill = new System.Collections.Generic.List<UnityEngine.GameObject>();
    foreach (UnityEngine.Transform c in group)
    {
        var b = new UnityEngine.Bounds(c.position, UnityEngine.Vector3.zero);
        foreach (var r in c.GetComponentsInChildren<UnityEngine.Renderer>()) b.Encapsulate(r.bounds);
        if (gone(b.center.x, b.center.z)) kill.Add(c.gameObject);
    }
    foreach (var k in kill) UnityEngine.Object.DestroyImmediate(k);
    removed += kill.Count;
}
var cut = 0;
foreach (var name in new[] { "Roofs", "Floors" })
{
    var g = root.Find(name);
    if (!g) continue;
    foreach (var mf in g.GetComponentsInChildren<UnityEngine.MeshFilter>())
    {
        var mesh = mf.sharedMesh;
        if (!mesh) continue;
        var copy = UnityEngine.Object.Instantiate(mesh); copy.name = mesh.name;
        var verts = copy.vertices;
        for (var si = 0; si < copy.subMeshCount; si++)
        {
            var tris = copy.GetTriangles(si);
            var keep = new System.Collections.Generic.List<int>();
            for (var i = 0; i < tris.Length; i += 3)
            {
                var c = (mf.transform.TransformPoint(verts[tris[i]]) + mf.transform.TransformPoint(verts[tris[i + 1]]) + mf.transform.TransformPoint(verts[tris[i + 2]])) / 3f;
                if (gone(c.x, c.z)) { cut++; continue; }
                keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]);
            }
            copy.SetTriangles(keep.ToArray(), si);
        }
        copy.RecalculateBounds();
        mf.sharedMesh = copy;
        var col = mf.GetComponent<UnityEngine.MeshCollider>(); if (col) col.sharedMesh = copy;
    }
}
// roads only inside the compound
var roadsOff = 0;
var roadsGroup = root.Find("Roads");
if (roadsGroup)
    foreach (var road in roadsGroup.GetComponentsInChildren<WorldRoad>(true))
    {
        var inside = 0;
        for (var i = 0; i < road.Points.Count; i++) { var w = road.transform.TransformPoint(road.Points[i]); if (inCompound(w.x, w.z)) inside++; }
        if (road.Points.Count > 0 && inside * 10 >= road.Points.Count * 7) { road.gameObject.SetActive(false); roadsOff++; }
    }
sb.Append("taken out " + removed + " pieces, " + cut + " triangles, " + roadsOff + " roads; decks stone " + stoneDeck.ToString("F2") + " wood " + woodDeck.ToString("F2") + "; ");

// ---- the castle ----------------------------------------------------------------------------------------------------
stage = "castle";
var oldCastle = root.Find("Ravebridge Castle"); if (oldCastle) UnityEngine.Object.DestroyImmediate(oldCastle.gameObject);
var cb = boundsOf(new[] { rbCastle });
float mapW = (CX1 - CX0 + 1) * Tile, mapD = (CY1 - CY0 + 1) * Tile;
var scale = UnityEngine.Mathf.Min(mapW / cb.size.x, mapD / cb.size.z);
var rbBase = rbGround(cb.center.x, cb.center.z);
var centre = new UnityEngine.Vector3((CX0 + CX1 + 1) * 0.5f * Tile, 0, (CY0 + CY1 + 1) * 0.5f * Tile);
var baseHere = groundAt(centre.x, centre.z);
// the ground: Ravebridge's under the castle and a margin round it, scaled, onto ours, blending out over the margin
{
    const float Margin = 12f;
    var res = td.heightmapResolution;
    var hs = td.GetHeights(0, 0, res, res);
    var span = td.size.y; var baseY = terrain.transform.position.y;
    float hx = cb.extents.x * scale, hz = cb.extents.z * scale;
    var imprinted = 0;
    for (var j = 0; j < res; j++)
    for (var i = 0; i < res; i++)
    {
        var wx = i / (float)(res - 1) * td.size.x; var wz = j / (float)(res - 1) * td.size.z;
        var dx = UnityEngine.Mathf.Abs(wx - centre.x) - hx; var dz = UnityEngine.Mathf.Abs(wz - centre.z) - hz;
        var outside = UnityEngine.Mathf.Max(dx, dz);
        if (outside > Margin) continue;
        // the matching point in Ravebridge
        var rx = cb.center.x + (wx - centre.x) / scale; var rz = cb.center.z + (wz - centre.z) / scale;
        var rg = rbGround(rx, rz);
        if (float.IsNaN(rg)) continue;
        var y = baseHere + (rg - rbBase) * scale;
        var blend = outside <= 0f ? 1f : UnityEngine.Mathf.SmoothStep(1f, 0f, outside / Margin);
        var was = baseY + hs[j, i] * span;
        hs[j, i] = UnityEngine.Mathf.Clamp01((UnityEngine.Mathf.Lerp(was, y, blend) - baseY) / span);
        imprinted++;
    }
    td.SetHeights(0, 0, hs);
    sb.Append("ground imprinted " + imprinted + "; ");
}
var castleRoot = new UnityEngine.GameObject("Ravebridge Castle");
castleRoot.transform.SetParent(root, false);
// a pivot at the castle's middle on Ravebridge's ground, so scaling and moving keep it standing on the copied ground
var pivot = new UnityEngine.GameObject("Pivot").transform;
pivot.SetParent(castleRoot.transform, false);
pivot.position = new UnityEngine.Vector3(cb.center.x, rbBase, cb.center.z);
var castleCopy = UnityEngine.Object.Instantiate(rbCastle.gameObject);
castleCopy.name = "Castle";
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(castleCopy, target);
castleCopy.transform.SetPositionAndRotation(rbCastle.position, rbCastle.rotation);
castleCopy.transform.localScale = rbCastle.lossyScale;
castleCopy.transform.SetParent(pivot, true);
pivot.position = new UnityEngine.Vector3(centre.x, baseHere, centre.z);
pivot.localScale = UnityEngine.Vector3.one * scale;
sb.Append("castle x" + scale.ToString("F2") + " from " + cb.size.ToString("F0") + "; ");

// the compound's ground: grass where the generated castle had paved it (the castle brings its own floors)
{
    var res = td.alphamapResolution; var layers = td.alphamapLayers;
    var alpha = td.GetAlphamaps(0, 0, res, res);
    for (var j = 0; j < res; j++)
    for (var i = 0; i < res; i++)
    {
        var wx = (i + 0.5f) / res * td.size.x; var wz = (j + 0.5f) / res * td.size.z;
        if (!inCompound(wx, wz)) continue;
        for (var l = 0; l < layers; l++) alpha[j, i, l] = l == 2 ? 1f : 0f;
    }
    td.SetAlphamaps(0, 0, alpha);
}

// ---- the bridges ---------------------------------------------------------------------------------------------------
stage = "bridges";
var oldBridges = root.Find("Ravebridge Bridges"); if (oldBridges) UnityEngine.Object.DestroyImmediate(oldBridges.gameObject);
var bridgesRoot = new UnityEngine.GameObject("Ravebridge Bridges");
bridgesRoot.transform.SetParent(root, false);
System.Action<string, UnityEngine.Transform[], UnityEngine.Vector3, float, float, float> bridge = (name, parts, mid, deckY, mapSpan, rbSpan) =>
{
    var b = boundsOf(parts);
    var rbDeck = topNear(parts, b.center, 1.5f);
    if (rbDeck == float.MinValue) rbDeck = b.max.y;
    var holder = new UnityEngine.GameObject(name).transform;
    holder.SetParent(bridgesRoot.transform, false);
    holder.position = new UnityEngine.Vector3(b.center.x, rbDeck, b.center.z);
    foreach (var p in parts)
    {
        var copy = UnityEngine.Object.Instantiate(p.gameObject);
        copy.name = p.name;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, target);
        copy.transform.SetPositionAndRotation(p.position, p.rotation);
        copy.transform.localScale = p.lossyScale;
        copy.transform.SetParent(holder, true);
    }
    // its deck on the map's, stretched along its length (x, as both lie) to the map's river
    holder.position = new UnityEngine.Vector3(mid.x, deckY, mid.z);
    holder.localScale = new UnityEngine.Vector3(mapSpan / rbSpan, 1f, 1f);
    sb.Append(name + " x" + (mapSpan / rbSpan).ToString("F2") + " deck " + rbDeck.ToString("F1") + "->" + deckY.ToString("F1") + "; ");
};
var stoneParts = new System.Collections.Generic.List<UnityEngine.Transform>();
foreach (UnityEngine.Transform c in rbEnv) if (c.name.StartsWith("SM_Env_Rock_Bridge")) stoneParts.Add(c);
if (rbParapets) stoneParts.Add(rbParapets);
// the stone bridge's ends stand on the banks: its span from end to end (Ravebridge's ends 30 m apart) onto the map's
// bridge tiles, end tile to end tile
bridge("Stone Bridge", stoneParts.ToArray(), stoneMid, stoneDeck, (stoneTiles.x1 - stoneTiles.x0 + 1) * Tile, 30f);
if (rbWood) bridge("Wooden Bridge", new[] { rbWood }, woodMid, woodDeck, (woodTiles.x1 - woodTiles.x0 + 3) * Tile, boundsOf(new[] { rbWood }).size.x);

// ---- done ----------------------------------------------------------------------------------------------------------
stage = "save";
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(main, true);
main = default;
UnityEditor.EditorUtility.SetDirty(td);
UnityEditor.AssetDatabase.SaveAssetIfDirty(td);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(target);
UnityEngine.Debug.Log("[lumbridge-castle] " + sb);
return sb.ToString();

}
catch (System.Exception ex)
{
    if (main.IsValid() && main.isLoaded) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(main, true);
    UnityEngine.Debug.Log("[lumbridge-castle] failed at " + stage + ": " + ex);
    return "failed at " + stage + ": " + ex + "\n" + sb;
}
