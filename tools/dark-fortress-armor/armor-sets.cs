// Run with: unity command eval_file --file tools/dark-fortress-armor/armor-sets.cs (edit mode; never in play mode)
//
// Puts the Dark Fortress armor sets (docs/dark-fortress-armor-test.md, "Wired in") on the player: builds the meshes
// from the JSON that tools/dark-fortress-armor/dump_sets.py writes (Temp/dark-fortress-armor/DF<Set>_<Sex>.json),
// saves them as assets in Assets/World/DarkFortressArmor/Meshes, and adds one part per piece and colorway to the part
// arrays of every prefab that has a SyntyPlayerAppearance with them (Player, Player Appearance Items, Item Display),
// at the end of each array. Rerunning it replaces the meshes and keeps the parts where they are (matched by name), so
// the indices the items name never move. The prefabs are not versioned: this script is the record.
//
// The meshes are made to sit exactly like the MFH parts they replace:
//   - positions: the JSON is in Blender's world (meters, Z up, facing -Y); the MFH meshes have a space of their own.
//     The mapping (an axis permutation with signs and a scale) is found by matching the MFH male torso, which the JSON
//     carries in Blender's space, against Chr_Torso_Male_00's mesh.
//   - bones: the player's own, by name (the Dark Fortress finger names Finger_01_L are Hand_L's Finger_01 ...).
//   - bind poses: the MFH parts' own for each bone, so a piece deforms exactly as the game's parts do.
// Each part has the set's material (the pack's colorway) and, when it shows skin, a second submesh with the MFH body
// material: SyntyPlayerAppearance gives that slot the player's own skin color.
// Work is spread over editor frames; wait for Temp/dark-fortress-armor/armor-sets.done (it holds the log).
var project = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, ".."));
var jsonDir = System.IO.Path.Combine(project, "Temp", "dark-fortress-armor");
var donePath = System.IO.Path.Combine(jsonDir, "armor-sets.done");
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "not in play mode";
if (System.IO.File.Exists(donePath)) System.IO.File.Delete(donePath);
var log = new System.Text.StringBuilder();

// the sets, and the colorway each tier wears: the part's name gets the colorway ("Chr_Torso_Male_DFKnight_01B")
var sets = new[] { "DFKnight", "DFBarbarian", "DFNecromancer" };
var colorways = new System.Collections.Generic.Dictionary<string, string[]>
{
    { "DFKnight", new[] { "01B", "01A" } },      // Dark Knight tier 7 (Voidknight: violet), tier 8 (Abyssal)
    { "DFBarbarian", new[] { "02A", "01A" } },   // Barbarian tier 7 (Stormborn: blue), tier 8
    { "DFNecromancer", new[] { "01B", "01A" } }, // Mage tier 7 (Voidweaver: violet), tier 8 (Starcaller)
};
const string MatDir = "Assets/Synty/PolygonDarkFortress/Materials/Alts/PolygonDarkFortress_Mat_";
const string BodyMat = "Assets/PolygonFantasyHeroCharacters/Materials/FantasyHero.mat";
const string MeshDir = "Assets/World/DarkFortressArmor/Meshes";
var prefabs = new[] { "Assets/Prefabs/Player.prefab", "Assets/Prefabs/Player Appearance Items.prefab", "Assets/Prefabs/Item Display.prefab" };

// ---- tiny JSON reader (the dump is plain: objects, arrays, numbers, strings) ----------------------------------------
object ParseJson(string s)
{
    int i = 0;
    void Ws() { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
    object Val()
    {
        Ws();
        var c = s[i];
        if (c == '{')
        {
            var d = new System.Collections.Generic.Dictionary<string, object>(); i++; Ws();
            if (s[i] == '}') { i++; return d; }
            while (true) { Ws(); var k = (string)Val(); Ws(); i++; d[k] = Val(); Ws(); if (s[i] == ',') { i++; continue; } i++; return d; }
        }
        if (c == '[')
        {
            var l = new System.Collections.Generic.List<object>(); i++; Ws();
            if (s[i] == ']') { i++; return l; }
            while (true) { l.Add(Val()); Ws(); if (s[i] == ',') { i++; continue; } i++; return l; }
        }
        if (c == '"')
        {
            var sb = new System.Text.StringBuilder(); i++;
            while (s[i] != '"') { if (s[i] == '\\') i++; sb.Append(s[i]); i++; }
            i++; return sb.ToString();
        }
        int st = i;
        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
        return double.Parse(s.Substring(st, i - st), System.Globalization.CultureInfo.InvariantCulture);
    }
    return Val();
}
float F(object o) => (float)(double)o;

// ---- the data (read in the first editor frames: the JSON is ~10 MB) --------------------------------------------------
var data = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
var files = new System.Collections.Generic.List<string>();
foreach (var set in sets)
{
    foreach (var sex in new[] { "Male", "Female" })
    {
        var path = System.IO.Path.Combine(jsonDir, set + "_" + sex + ".json");
        if (!System.IO.File.Exists(path)) return "missing " + path + " (run dump_sets.py into Temp/dark-fortress-armor)";
        files.Add(path);
    }
}

var queue = new System.Collections.Generic.Queue<System.Action>();
UnityEngine.Matrix4x4 map = UnityEngine.Matrix4x4.identity; // Blender world -> MFH mesh space
bool flip = false;
var meshes = new System.Collections.Generic.Dictionary<string, UnityEngine.Mesh>(); // piece name -> mesh asset
var boneOrder = new System.Collections.Generic.Dictionary<string, string[]>();      // piece name -> bone names
var indexOf = new System.Collections.Generic.Dictionary<string, int>();               // part name -> index (same in every prefab)

foreach (var file in files)
{
    var f = file;
    queue.Enqueue(() => data.Add((System.Collections.Generic.Dictionary<string, object>)ParseJson(System.IO.File.ReadAllText(f))));
}

// step 1: the space, on the player prefab's own parts
queue.Enqueue(() =>
{
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(prefabs[0]);
    try
    {
        var app = root.GetComponentInChildren<SyntyPlayerAppearance>(true);
        var so = new UnityEditor.SerializedObject(app);
        var torso0 = (UnityEngine.GameObject)so.FindProperty("maleTorso").GetArrayElementAtIndex(0).objectReferenceValue;
        var tm = torso0.GetComponent<UnityEngine.SkinnedMeshRenderer>().sharedMesh.vertices;
        var refl = (System.Collections.Generic.List<object>)data[0]["reference"];
        var rp = new UnityEngine.Vector3[refl.Count / 3];
        for (int k = 0; k < rp.Length; k++) rp[k] = new UnityEngine.Vector3(F(refl[k * 3]), F(refl[k * 3 + 1]), F(refl[k * 3 + 2]));
        // the 48 signed axis permutations; the scale from the extents; the best by mean nearest distance
        UnityEngine.Bounds B(System.Collections.Generic.IEnumerable<UnityEngine.Vector3> ps) { var b = new UnityEngine.Bounds(); bool first = true; foreach (var p in ps) { if (first) { b = new UnityEngine.Bounds(p, UnityEngine.Vector3.zero); first = false; } else b.Encapsulate(p); } return b; }
        var tb = B(tm);
        double best = double.MaxValue;
        var perms = new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
        foreach (var pm in perms)
        {
            for (int sg = 0; sg < 8; sg++)
            {
                var m = UnityEngine.Matrix4x4.zero; m[3, 3] = 1;
                for (int r = 0; r < 3; r++) m[r, pm[r]] = ((sg >> r) & 1) == 1 ? -1 : 1;
                var mapped = new UnityEngine.Vector3[rp.Length];
                for (int k = 0; k < rp.Length; k++) mapped[k] = m.MultiplyVector(rp[k]);
                var mb = B(mapped);
                var sc = tb.size.y / UnityEngine.Mathf.Max(1e-6f, mb.size.y);
                var off = tb.center - mb.center * sc;
                double err = 0;
                for (int k = 0; k < mapped.Length; k += 3)
                {
                    var q = mapped[k] * sc + off; float dmin = float.MaxValue;
                    foreach (var v in tm) { var d = (v - q).sqrMagnitude; if (d < dmin) dmin = d; }
                    err += UnityEngine.Mathf.Sqrt(dmin);
                }
                err /= (mapped.Length / 3);
                if (err < best)
                {
                    best = err;
                    map = UnityEngine.Matrix4x4.TRS(off, UnityEngine.Quaternion.identity, UnityEngine.Vector3.one * sc) * m;
                    flip = m.determinant < 0;
                }
            }
        }
        // the torso is symmetric, so left and right are told apart by the hand: Blender's +X is the character's left,
        // which must land where the MFH left hand is
        var handLeft = (UnityEngine.GameObject)so.FindProperty("maleHandsLeft").GetArrayElementAtIndex(0).objectReferenceValue;
        var handX = handLeft.GetComponent<UnityEngine.SkinnedMeshRenderer>().sharedMesh.bounds.center.x;
        var probeX = map.MultiplyPoint3x4(new UnityEngine.Vector3(0.9f, 0f, 1.36f)).x;
        if (UnityEngine.Mathf.Sign(probeX) != UnityEngine.Mathf.Sign(handX))
        {
            map = UnityEngine.Matrix4x4.Scale(new UnityEngine.Vector3(-1f, 1f, 1f)) * map;
            flip = !flip;
        }
        log.AppendLine("space: mean error " + (best).ToString("0.0000") + " (mesh units), scale " + map.lossyScale + ", mirrored " + flip);
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
});

// step 2: the meshes
queue.Enqueue(() =>
{
    if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/World/DarkFortressArmor")) UnityEditor.AssetDatabase.CreateFolder("Assets/World", "DarkFortressArmor");
    if (!UnityEditor.AssetDatabase.IsValidFolder(MeshDir)) UnityEditor.AssetDatabase.CreateFolder("Assets/World/DarkFortressArmor", "Meshes");
    var rot = map; // normals: the linear part, normalized after
    foreach (var d in data)
    {
        foreach (System.Collections.Generic.Dictionary<string, object> p in (System.Collections.Generic.List<object>)d["parts"])
        {
            var name = (string)p["name"];
            var vl = (System.Collections.Generic.List<object>)p["verts"];
            var nl = (System.Collections.Generic.List<object>)p["normals"];
            var ul = (System.Collections.Generic.List<object>)p["uvs"];
            var wl = (System.Collections.Generic.List<object>)p["weights"];
            var subs = (System.Collections.Generic.List<object>)p["sub"];
            var bones = ((System.Collections.Generic.List<object>)p["bones"]).ConvertAll(x => (string)x).ToArray();
            int n = vl.Count / 3;
            var V = new UnityEngine.Vector3[n]; var N = new UnityEngine.Vector3[n]; var U = new UnityEngine.Vector2[n]; var W = new UnityEngine.BoneWeight[n];
            for (int k = 0; k < n; k++)
            {
                V[k] = map.MultiplyPoint3x4(new UnityEngine.Vector3(F(vl[k * 3]), F(vl[k * 3 + 1]), F(vl[k * 3 + 2])));
                N[k] = rot.MultiplyVector(new UnityEngine.Vector3(F(nl[k * 3]), F(nl[k * 3 + 1]), F(nl[k * 3 + 2]))).normalized;
                U[k] = new UnityEngine.Vector2(F(ul[k * 2]), F(ul[k * 2 + 1]));
                var row = (System.Collections.Generic.List<object>)wl[k];
                var bw = new UnityEngine.BoneWeight();
                for (int j = 0; j < row.Count / 2; j++)
                {
                    int bi = (int)(double)row[j * 2]; float w = F(row[j * 2 + 1]);
                    if (j == 0) { bw.boneIndex0 = bi; bw.weight0 = w; }
                    else if (j == 1) { bw.boneIndex1 = bi; bw.weight1 = w; }
                    else if (j == 2) { bw.boneIndex2 = bi; bw.weight2 = w; }
                    else { bw.boneIndex3 = bi; bw.weight3 = w; }
                }
                W[k] = bw;
            }
            var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(MeshDir + "/" + name + ".asset");
            var isNew = !mesh;
            if (isNew) mesh = new UnityEngine.Mesh();
            mesh.Clear();
            mesh.name = name;
            mesh.indexFormat = n > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = V; mesh.normals = N; mesh.uv = U; mesh.boneWeights = W;
            var skinTris = ((System.Collections.Generic.List<object>)subs[1]).Count;
            mesh.subMeshCount = skinTris > 0 ? 2 : 1;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var tl = (System.Collections.Generic.List<object>)subs[s];
                var tris = new int[tl.Count];
                for (int k = 0; k < tl.Count; k += 3)
                {
                    tris[k] = (int)(double)tl[k];
                    tris[k + 1] = (int)(double)tl[flip ? k + 2 : k + 1];
                    tris[k + 2] = (int)(double)tl[flip ? k + 1 : k + 2];
                }
                mesh.SetTriangles(tris, s);
            }
            mesh.RecalculateBounds();
            if (isNew) UnityEditor.AssetDatabase.CreateAsset(mesh, MeshDir + "/" + name + ".asset");
            meshes[name] = mesh;
            boneOrder[name] = bones;
        }
    }
    UnityEditor.AssetDatabase.SaveAssets();
    log.AppendLine("meshes: " + meshes.Count);
});

// step 3..: each prefab
foreach (var prefabPath in prefabs)
{
    var pp = prefabPath;
    queue.Enqueue(() =>
    {
        if (!System.IO.File.Exists(System.IO.Path.Combine(project, pp))) { log.AppendLine("no " + pp); return; }
        var root = UnityEditor.PrefabUtility.LoadPrefabContents(pp);
        try
        {
            var app = root.GetComponentInChildren<SyntyPlayerAppearance>(true);
            if (!app) { log.AppendLine(pp + ": no appearance"); return; }
            var so = new UnityEditor.SerializedObject(app);
            // the arrays a part's name prefix belongs to ("Chr_LegRight_Male" -> maleLegsLeft: the male file names
            // its legs the other way round)
            var arrays = new[] { "maleTorso", "femaleTorso", "maleArmUpperRight", "maleArmUpperLeft", "femaleArmUpperRight",
                "femaleArmUpperLeft", "maleArmLowerRight", "maleArmLowerLeft", "femaleArmLowerRight", "femaleArmLowerLeft",
                "maleHandsRight", "maleHandsLeft", "femaleHandsRight", "femaleHandsLeft", "maleHips", "femaleHips",
                "maleLegsRight", "maleLegsLeft", "femaleLegsRight", "femaleLegsLeft" };
            var prefixArray = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var a in arrays)
            {
                var prop = so.FindProperty(a);
                if (prop == null || prop.arraySize < 2) continue;
                var e = (UnityEngine.GameObject)prop.GetArrayElementAtIndex(1).objectReferenceValue;
                if (!e) continue;
                var nm = e.name; var cut = nm.LastIndexOf('_');
                prefixArray[nm.Substring(0, cut)] = a;
            }
            // the player's bones by name; fingers as Finger_01_L / _R
            var torso0 = (UnityEngine.GameObject)so.FindProperty("maleTorso").GetArrayElementAtIndex(0).objectReferenceValue;
            var refSmr = torso0.GetComponent<UnityEngine.SkinnedMeshRenderer>();
            var skeletonRoot = refSmr.rootBone;
            while (skeletonRoot.parent && skeletonRoot.parent.name != torso0.transform.parent.name && skeletonRoot.name != "Root") skeletonRoot = skeletonRoot.parent;
            var boneByName = new System.Collections.Generic.Dictionary<string, UnityEngine.Transform>();
            foreach (var t in skeletonRoot.GetComponentsInChildren<UnityEngine.Transform>(true))
            {
                // the right hand's fingers are "Finger_01 1" in the prefab
                var key = t.name.EndsWith(" 1") ? t.name.Substring(0, t.name.Length - 2) : t.name;
                for (var up = t.parent; up; up = up.parent)
                {
                    if (up.name == "Hand_L") { key += "_L"; break; }
                    if (up.name == "Hand_R") { key += "_R"; break; }
                }
                if (!boneByName.ContainsKey(key)) boneByName[key] = t;
            }
            // the MFH bind pose of every bone, from the parts that use it (same renderer space as torso 0)
            var bindOf = new System.Collections.Generic.Dictionary<UnityEngine.Transform, UnityEngine.Matrix4x4>();
            var refL2W = torso0.transform.localToWorldMatrix;
            foreach (var smr in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
            {
                if (!smr.sharedMesh || !smr.name.StartsWith("Chr_")) continue;
                if (smr.name.Contains("_DF")) continue;
                if (!AlmostEqual(smr.transform.localToWorldMatrix, refL2W)) continue;
                var bp = smr.sharedMesh.bindposes;
                for (int k = 0; k < smr.bones.Length && k < bp.Length; k++)
                    if (smr.bones[k] && !bindOf.ContainsKey(smr.bones[k])) bindOf[smr.bones[k]] = bp[k];
            }
            var bodyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(BodyMat);
            int added = 0, updated = 0;
            var report = new System.Text.StringBuilder();
            foreach (var set in sets)
            {
                for (int tier = 0; tier < 2; tier++)
                {
                    var cw = colorways[set][tier];
                    var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(MatDir + cw.Substring(0, 2) + "_" + cw.Substring(2) + ".mat");
                    if (!mat) { log.AppendLine("no material " + cw); continue; }
                    foreach (var kv in meshes)
                    {
                        if (!kv.Key.EndsWith("_" + set)) continue;
                        var prefix = kv.Key.Substring(0, kv.Key.Length - set.Length - 1); // Chr_Torso_Male
                        if (!prefixArray.TryGetValue(prefix, out var arrayName)) { log.AppendLine("no array for " + prefix); continue; }
                        var prop = so.FindProperty(arrayName);
                        var sib = (UnityEngine.GameObject)prop.GetArrayElementAtIndex(1).objectReferenceValue;
                        var partName = kv.Key + "_" + cw;
                        // the part, if a run before made it: same place in the array
                        int index = -1;
                        for (int k = 0; k < prop.arraySize; k++)
                        {
                            var e = (UnityEngine.GameObject)prop.GetArrayElementAtIndex(k).objectReferenceValue;
                            if (e && e.name == partName) { index = k; break; }
                        }
                        UnityEngine.GameObject go;
                        if (index >= 0) { go = (UnityEngine.GameObject)prop.GetArrayElementAtIndex(index).objectReferenceValue; updated++; }
                        else
                        {
                            go = new UnityEngine.GameObject(partName);
                            go.transform.SetParent(sib.transform.parent, false);
                            go.transform.localPosition = sib.transform.localPosition;
                            go.transform.localRotation = sib.transform.localRotation;
                            go.transform.localScale = sib.transform.localScale;
                            go.layer = sib.layer;
                            go.SetActive(false);
                            prop.arraySize++;
                            index = prop.arraySize - 1;
                            prop.GetArrayElementAtIndex(index).objectReferenceValue = go;
                            added++;
                        }
                        var sibSmr = sib.GetComponent<UnityEngine.SkinnedMeshRenderer>();
                        var smr = go.GetComponent<UnityEngine.SkinnedMeshRenderer>();
                        if (!smr) smr = go.AddComponent<UnityEngine.SkinnedMeshRenderer>();
                        var names = boneOrder[kv.Key];
                        var bones = new UnityEngine.Transform[names.Length];
                        var binds = new UnityEngine.Matrix4x4[names.Length];
                        for (int k = 0; k < names.Length; k++)
                        {
                            if (!boneByName.TryGetValue(names[k], out bones[k])) { log.AppendLine(partName + ": no bone " + names[k]); continue; }
                            if (!bindOf.TryGetValue(bones[k], out binds[k]))
                            {
                                // a bone no MFH part is skinned to: its bind pose from the prefab's rest
                                binds[k] = bones[k].worldToLocalMatrix * go.transform.localToWorldMatrix;
                                log.AppendLine(partName + ": bind pose of " + names[k] + " from the rest pose");
                            }
                        }
                        if (!AlmostEqual(go.transform.localToWorldMatrix, refL2W)) log.AppendLine(partName + ": not in the MFH parts' space");
                        var mesh = kv.Value;
                        mesh.bindposes = binds;
                        smr.sharedMesh = mesh;
                        smr.bones = bones;
                        smr.rootBone = sibSmr ? sibSmr.rootBone : bones[0];
                        smr.sharedMaterials = mesh.subMeshCount > 1 ? new[] { mat, bodyMat } : new[] { mat };
                        if (sibSmr)
                        {
                            smr.quality = sibSmr.quality;
                            smr.updateWhenOffscreen = sibSmr.updateWhenOffscreen;
                            smr.shadowCastingMode = sibSmr.shadowCastingMode;
                            smr.receiveShadows = sibSmr.receiveShadows;
                            smr.localBounds = sibSmr.localBounds;
                        }
                        report.AppendLine("  " + arrayName + "[" + index + "] " + partName + (mesh.subMeshCount > 1 ? " +skin" : ""));
                        if (indexOf.TryGetValue(arrayName + "/" + partName, out var was) && was != index)
                            log.AppendLine("INDEX DIFFERS in " + pp + ": " + arrayName + " " + partName + " " + index + " (was " + was + ")");
                        indexOf[arrayName + "/" + partName] = index;
                    }
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, pp);
            log.AppendLine(pp + ": added " + added + ", updated " + updated);
            if (pp == prefabs[0]) log.Append(report);
        }
        finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
    });
}
queue.Enqueue(() => { foreach (var m in meshes.Values) UnityEditor.EditorUtility.SetDirty(m); UnityEditor.AssetDatabase.SaveAssets(); });

bool AlmostEqual(UnityEngine.Matrix4x4 a, UnityEngine.Matrix4x4 b)
{
    for (int k = 0; k < 16; k++) if (UnityEngine.Mathf.Abs(a[k] - b[k]) > 1e-3f) return false;
    return true;
}

UnityEditor.EditorApplication.CallbackFunction step = null;
step = () =>
{
    try
    {
        if (queue.Count > 0) { queue.Dequeue()(); return; }
        UnityEditor.EditorApplication.update -= step;
        System.IO.File.WriteAllText(donePath, "done\n" + log);
    }
    catch (System.Exception e)
    {
        UnityEditor.EditorApplication.update -= step;
        System.IO.File.WriteAllText(donePath, "failed: " + e + "\n" + log);
    }
};
UnityEditor.EditorApplication.update += step;
return "started; wait for " + donePath;
