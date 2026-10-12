// Shared header for the town hall tier builders. Concatenated in front of a hall_tierN.cs body.
var sb = new System.Text.StringBuilder("HALL|");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("HALL|refused"); return null; }
var mgr = UnityEngine.Object.FindAnyObjectByType<TownHallManager>(UnityEngine.FindObjectsInactive.Include);
UnityEngine.Terrain terrain = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Terrain>(UnityEngine.FindObjectsInactive.Include)) if (t.name == "Terrain") terrain = t;
System.Func<float, float, float> H = (x, z) => terrain.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + terrain.transform.position.y;
var plaza = new UnityEngine.Vector3(-23.64f, 0f, -109.72f); plaza.y = H(plaza.x, plaza.z);
System.Func<int, UnityEngine.Transform> tierRoot = n => mgr.transform.Find("TownHall_Tier_" + n);
var rng = new System.Random(4242);
System.Func<float, float, float> R = (a, b) => a + (float)rng.NextDouble() * (b - a);

// Each tier gets a fresh "Hall" container; everything the builder places goes in it.
System.Func<int, UnityEngine.Transform> freshHall = n =>
{
    var root = tierRoot(n);
    root.SetPositionAndRotation(plaza, UnityEngine.Quaternion.identity);
    var old = root.Find("Hall"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
    var hall = new UnityEngine.GameObject("Hall").transform; hall.SetParent(root, false);
    return hall;
};
System.Func<string, UnityEngine.GameObject> load = p =>
{
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p);
    if (!go) sb.Append("MISSING ").Append(p).Append(" || ");
    return go;
};
// Put(parent, prefab path, local position, yaw, uniform scale)
System.Func<UnityEngine.Transform, string, UnityEngine.Vector3, float, float, UnityEngine.GameObject> Put = (parent, path, pos, yaw, scale) =>
{
    var prefab = load(path); if (!prefab) return null;
    var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
    go.transform.localPosition = pos;
    go.transform.localRotation = UnityEngine.Quaternion.Euler(0, yaw, 0);
    go.transform.localScale = go.transform.localScale * scale;
    return go;
};
// Moves a kept object (found anywhere under the tier root) to a local spot on the tier.
System.Action<UnityEngine.Transform, string, UnityEngine.Vector3, float> Keep = (root, name, pos, yaw) =>
{
    UnityEngine.Transform found = null;
    foreach (var t in root.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == name && t.parent == root) found = t;
    if (!found) { sb.Append("no kept ").Append(name).Append(" || "); return; }
    found.localPosition = pos; found.localRotation = UnityEngine.Quaternion.Euler(0, yaw, 0);
};
const string FK = "Assets/PolygonFantasyKingdom/Prefabs/";
const string FKP = "Assets/PolygonFantasyKingdom/Prefabs/Props/";
const string FKB = "Assets/PolygonFantasyKingdom/Prefabs/Buildings/Preset_Houses/";
const string FKC = "Assets/PolygonFantasyKingdom/Prefabs/Castle/SM_Bld_Castle_";
const string KN = "Assets/PolygonKnights/Prefabs/";
const string AE = "Assets/PolygonAncientEmpire/Prefabs/";
const string VM = "Assets/Village/Models/"; // our own models, built by blender/*.py (run models_import.cs after a rebuild)

// Copies a kit mesh without the triangles that lie wholly above a height (mesh space), into a mesh asset
// under Assets/Village/Derived. The asset is reused on reruns, so scene references to it stay valid.
// Derived/ is git-ignored like the kits it comes from.
System.Func<UnityEngine.Mesh, float, string, UnityEngine.Mesh> TrimAbove = (src, maxY, name) =>
{
    if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Village/Derived")) UnityEditor.AssetDatabase.CreateFolder("Assets/Village", "Derived");
    var path = "Assets/Village/Derived/" + name + ".asset";
    var dst = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
    if (!dst) { dst = new UnityEngine.Mesh(); UnityEditor.AssetDatabase.CreateAsset(dst, path); }
    dst.Clear(); dst.name = name;
    dst.indexFormat = src.indexFormat;
    var v = src.vertices;
    dst.vertices = v; dst.normals = src.normals; dst.tangents = src.tangents; dst.uv = src.uv; dst.colors = src.colors;
    dst.subMeshCount = src.subMeshCount;
    for (var s = 0; s < src.subMeshCount; ++s)
    {
        var t = src.GetTriangles(s); var keep = new System.Collections.Generic.List<int>(t.Length);
        for (var i = 0; i < t.Length; i += 3)
            if (v[t[i]].y <= maxY || v[t[i + 1]].y <= maxY || v[t[i + 2]].y <= maxY) { keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]); }
        dst.SetTriangles(keep, s);
    }
    dst.RecalculateBounds();
    UnityEditor.EditorUtility.SetDirty(dst);
    return dst;
};

// Ground materials for patches under the halls, made once from the terrain's own textures.
System.Func<string, string, UnityEngine.Color, UnityEngine.Material> groundMat = (name, layerName, tint) =>
{
    var path = "Assets/Materials/Village/" + name + ".mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (!m)
    {
        if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Materials/Village")) UnityEditor.AssetDatabase.CreateFolder("Assets/Materials", "Village");
        m = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
        UnityEditor.AssetDatabase.CreateAsset(m, path);
    }
    foreach (var l in terrain.terrainData.terrainLayers) if (l && l.name == layerName) m.SetTexture("_BaseMap", l.diffuseTexture);
    m.SetColor("_BaseColor", tint); m.SetFloat("_Smoothness", 0f);
    UnityEditor.EditorUtility.SetDirty(m);
    return m;
};
var dirtMat = groundMat("Village_Dirt", "Terrain_Meadow_Dirt_01", new UnityEngine.Color(0.85f, 0.78f, 0.66f));
var cobbleMat = groundMat("Village_Cobble", "Terrain_Meadow_Footpath_Tile_01", new UnityEngine.Color(0.95f, 0.93f, 0.9f));

// A flat, ragged disc (or rounded rectangle when sx != sz) lying just above the ground, saved inside the scene.
System.Func<UnityEngine.Transform, string, UnityEngine.Vector3, float, float, float, UnityEngine.Material, float, UnityEngine.GameObject> Patch = (parent, name, center, sx, sz, jag, mat, tile) =>
{
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(parent, false); go.transform.localPosition = center;
    const int N = 64;
    var verts = new UnityEngine.Vector3[N + 1]; var uvs = new UnityEngine.Vector2[N + 1]; var tris = new int[N * 3];
    verts[0] = new UnityEngine.Vector3(0, 0.05f, 0);
    var seed = R(0f, 100f);
    for (var i = 0; i < N; ++i)
    {
        var a = i * UnityEngine.Mathf.PI * 2f / N;
        var n = 1f + (UnityEngine.Mathf.PerlinNoise(seed + UnityEngine.Mathf.Cos(a) * 1.7f, seed + UnityEngine.Mathf.Sin(a) * 1.7f) - 0.5f) * 2f * jag;
        // superellipse so large patches read as squarish plazas
        var c = UnityEngine.Mathf.Cos(a); var s = UnityEngine.Mathf.Sin(a);
        var p = 0.6f; var x = UnityEngine.Mathf.Sign(c) * UnityEngine.Mathf.Pow(UnityEngine.Mathf.Abs(c), p) * sx * n; var z = UnityEngine.Mathf.Sign(s) * UnityEngine.Mathf.Pow(UnityEngine.Mathf.Abs(s), p) * sz * n;
        verts[i + 1] = new UnityEngine.Vector3(x, 0.05f, z);
    }
    for (var i = 0; i <= N; ++i) { var w = go.transform.TransformPoint(verts[i]); uvs[i] = new UnityEngine.Vector2(w.x / tile, w.z / tile); }
    for (var i = 0; i < N; ++i) { tris[i * 3] = 0; tris[i * 3 + 1] = 1 + (i + 1) % N; tris[i * 3 + 2] = 1 + i; }
    var mesh = new UnityEngine.Mesh { name = name, vertices = verts, uv = uvs, triangles = tris };
    mesh.RecalculateNormals(); mesh.RecalculateBounds();
    go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
    var mr = go.AddComponent<UnityEngine.MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    return go;
};

// Standing torch from the kept template; as a campfire the pole is hidden and the flame made bigger.
var torchTemplate = mgr.transform.Find("Village Keepsakes/Torch Template");
System.Func<UnityEngine.Transform, UnityEngine.Vector3, bool, UnityEngine.GameObject> Fire = (parent, pos, campfire) =>
{
    var go = UnityEngine.Object.Instantiate(torchTemplate.gameObject, parent, false);
    go.name = campfire ? "Campfire Flame" : "Torch";
    go.transform.localPosition = pos; go.transform.localRotation = UnityEngine.Quaternion.identity; go.transform.localScale = UnityEngine.Vector3.one;
    go.SetActive(true);
    if (campfire)
    {
        go.GetComponent<UnityEngine.MeshRenderer>().enabled = false;
        var fx = go.transform.Find("Fire_01_FX"); fx.localPosition = new UnityEngine.Vector3(0, 0.15f, 0); fx.localScale = UnityEngine.Vector3.one * 2.6f;
    }
    return go;
};

// Fits the tier's click collider (on the tier root) around the given building.
System.Action<UnityEngine.Transform, UnityEngine.GameObject> FitCollider = (root, building) =>
{
    var box = root.GetComponent<UnityEngine.BoxCollider>();
    var rs = building.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    box.center = root.InverseTransformPoint(b.center);
    box.size = new UnityEngine.Vector3(b.size.x, b.size.y, b.size.z);
};

// Castle kit walls span -5..0 on their local x. Lays pieces[i] along +x (axis 0) or +z (axis 1) from a start point.
System.Action<UnityEngine.Transform, string[], float, float, int, float> WallRun = (parent, pieces, x0, z0, axis, y) =>
{
    for (var i = 0; i < pieces.Length; ++i)
    {
        if (string.IsNullOrEmpty(pieces[i])) continue;
        if (axis == 0) Put(parent, FKC + pieces[i] + ".prefab", new UnityEngine.Vector3(x0 + 5f * (i + 1), y, z0), 0, 1f);
        else Put(parent, FKC + pieces[i] + ".prefab", new UnityEngine.Vector3(x0, y, z0 + 5f * i), 90, 1f);
    }
};
// Round tower: segments stacked 5 m apart, then a cone cap.
System.Action<UnityEngine.Transform, string, string, UnityEngine.Vector3, int> Tower = (parent, seg, cap, pos, levels) =>
{
    for (var i = 0; i < levels; ++i) Put(parent, FKC + seg + ".prefab", pos + new UnityEngine.Vector3(0, i * 5f, 0), 0, 1f);
    Put(parent, FKC + cap + ".prefab", pos + new UnityEngine.Vector3(0, levels * 5f, 0), 0, 1f);
};
// Sets a hat on top of the wearer, sized by scale and sunk a little into the head.
System.Func<UnityEngine.GameObject, string, float, float, UnityEngine.GameObject> HatOn = (wearer, hatPath, scale, sink) =>
{
    var rs = wearer.GetComponentsInChildren<UnityEngine.Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    var hat = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(load(hatPath), wearer.transform.parent);
    hat.transform.position = new UnityEngine.Vector3(b.center.x, b.max.y - sink, b.center.z);
    hat.transform.rotation = wearer.transform.rotation;
    hat.transform.localScale = UnityEngine.Vector3.one * scale;
    return hat;
};
System.Action<UnityEngine.Transform, UnityEngine.Vector3, float, float> Banner = (parent, top, yaw, length) =>
{
    var flag = Put(parent, FKP + "Banners/SM_Prop_Flag_11.prefab", top, yaw, 1f);
    flag.transform.localScale = new UnityEngine.Vector3(1f, length, 1f);
};
