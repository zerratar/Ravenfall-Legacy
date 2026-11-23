// Run with: unity command eval_file --file tools/characters/export-gear-web.cs --timeout 120 (edit mode; never in play mode)
//
// Everything a character can WEAR, for the website's Equipment tab (RavenNest's wwwroot/js/rf-character-gear.js), beside
// the bodies that tools/characters/export-characters-web.cs writes. Into RavenNest's wwwroot/characters/gear/:
//
// - parts/<name>.bin   every armor part of the Player prefab (torsos, arms, hands, hips, legs, helmets, hats, masks,
//                      hoods, pauldrons, elbow and knee pads, what hangs on the belt and the back, capes), one small
//                      file each, skinned to the body export's skeleton and in its space (three.js: z mirrored, the
//                      triangles turned). A header says how many vertices and where the numbers lie; the page fetches
//                      only the parts a character wears.
// - props/<name>.bin   every item that is a model of its own held or worn on a bone (weapons, shields, novelty hats,
//                      necklaces): its meshes as one, in the space of the bone's child the game puts it in.
// - textures/          the armor's own picture and masks (the same atlas as the body's: albedo.jpg as it is in the
//                      file, masks-c/d/e.png for the seven colors a tier's material sets), and the props' pictures.
// - gear.json          the part lists by the game's own names and order (SyntyPlayerAppearance's fields: an item's
//                      model ids are indices into them), every material by its ItemMaterial name with its colors,
//                      the parts that show the wearer's skin, the hair that is swapped under hats, the props with
//                      their materials, and the bones.
// - stances/<name>.json how a character stands with a weapon in hand (the animation pack's idle for a sword, a two
//                      handed sword, a bow, a staff, a spear, an axe, a shield), in the body export's animation layout.
//
// Nothing here names an item or a gear line: the page reads an item's model ids, material, HeadMask and prefab from
// the server and looks them up in gear.json, as the game does in its prefab. A new gear line is new materials (found
// here by walking ItemMaterial) and new item rows; run this again and it is there. The props to export are the
// prefabs the server's items name (asked from the local server; the list is kept in tools/characters/gear-prefabs.txt
// and read from there when no server answers).
//
// Only reads assets, and no open scene is touched: the one copy of the player it makes (to sample the stances) lives in
// a preview scene of its own and is destroyed after. The work is spread over editor frames, so the eval
// returns at once: wait for Temp/export-gear-web.txt to start with "done" or "failed" (it says "running n/m" meanwhile).
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "not in play mode";
var project = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, ".."));
var site = System.IO.Path.GetFullPath(System.IO.Path.Combine(project, "../RavenNest/src/RavenNest.Blazor/wwwroot/characters"));
var outDir = System.IO.Path.Combine(site, "gear");
var reportPath = System.IO.Path.Combine(project, "Temp", "export-gear-web.txt");
var prefabListPath = System.IO.Path.Combine(project, "tools", "characters", "gear-prefabs.txt");
System.IO.File.WriteAllText(reportPath, "running 0/0");
var inv = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.#####", inv);
string Q(string s) => s == null ? "null" : "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
var report = new System.Text.StringBuilder();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;

// ---- Unity (left-handed) to three.js (right-handed): z mirrored --------------------------------------------------------
UnityEngine.Vector3 V(UnityEngine.Vector3 v) => new UnityEngine.Vector3(v.x, v.y, -v.z);
UnityEngine.Quaternion R(UnityEngine.Quaternion q) => new UnityEngine.Quaternion(-q.x, -q.y, q.z, q.w);

// ---- The body export's skeleton: the same bones in the same order, and its bind poses ------------------------------------
var charactersJson = System.IO.Path.Combine(site, "characters.json");
if (!System.IO.File.Exists(charactersJson)) return "no characters.json: run export-characters-web.cs first";
var skeletonText = System.IO.File.ReadAllText(charactersJson);
{
    var at = skeletonText.IndexOf("\"skeleton\":[");
    var end = skeletonText.IndexOf("\"parts\":", at);
    skeletonText = skeletonText.Substring(at, end - at);
}
var refNames = new System.Collections.Generic.List<string>();
var refInverse = new System.Collections.Generic.List<UnityEngine.Matrix4x4>();
foreach (System.Text.RegularExpressions.Match bone in System.Text.RegularExpressions.Regex.Matches(skeletonText, "\\{\"name\":\"([^\"]*)\".*?\"inverse\":\\[([^\\]]*)\\]\\}"))
{
    refNames.Add(bone.Groups[1].Value);
    var a = bone.Groups[2].Value.Split(',');
    var m = new UnityEngine.Matrix4x4();
    // written as S m S, column-major: read back the same way
    for (var c = 0; c < 4; c++) for (var r = 0; r < 4; r++) m[r, c] = (r == 2 ? -1f : 1f) * float.Parse(a[c * 4 + r], inv) * (c == 2 ? -1f : 1f);
    refInverse.Add(m);
}
if (refNames.Count == 0) return "the skeleton of characters.json could not be read";

var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Player.prefab");
var app = prefab.GetComponentInChildren<SyntyPlayerAppearance>(true);
var animator = prefab.GetComponentInChildren<UnityEngine.Animator>(true);
var rootBone = animator.transform.Find("Root") ?? prefab.transform.Find("Root");
if (!rootBone) return "no Root bone";
var bones = new System.Collections.Generic.List<UnityEngine.Transform>();
var boneIndex = new System.Collections.Generic.Dictionary<UnityEngine.Transform, int>();
void Collect(UnityEngine.Transform t)
{
    boneIndex[t] = bones.Count;
    bones.Add(t);
    foreach (UnityEngine.Transform c in t) if (!c.GetComponent<UnityEngine.Renderer>()) Collect(c);
}
Collect(rootBone);
if (bones.Count != refNames.Count) return "the prefab has " + bones.Count + " bones, characters.json " + refNames.Count + ": run export-characters-web.cs again";
for (var i = 0; i < bones.Count; i++) if (bones[i].name != refNames[i]) return "bone " + i + " is " + bones[i].name + " here and " + refNames[i] + " in characters.json: run export-characters-web.cs again";

// The body export has a bind pose only for the bones its own meshes use; for the others (the cape's and the
// attachments') it wrote the bone's place in the prefab, in meters under the root's scale, where the meshes are bound
// in the model's own centimeters. A vertex between two such bones cannot be moved to both at once, so the armor brings
// its own bind pose for those bones (gear.json "inverses"), and the page skins the armor with them.
var space = rootBone.parent;
var gearInverse = new UnityEngine.Matrix4x4?[bones.Count];
var placeOnly = new bool[bones.Count];
bool Near(UnityEngine.Matrix4x4 x, UnityEngine.Matrix4x4 y, float by)
{
    for (var k = 0; k < 16; k++) if (UnityEngine.Mathf.Abs(x[k] - y[k]) > by) return false;
    return true;
}
for (var i = 0; i < bones.Count; i++)
    placeOnly[i] = Near(refInverse[i], bones[i].worldToLocalMatrix * (space ? space.localToWorldMatrix : UnityEngine.Matrix4x4.identity), 1e-3f);

// ---- The part lists ------------------------------------------------------------------------------------------------------
// the belt and back parts are found by the game the first time an item wears one: the same code finds them here
typeof(SyntyPlayerAppearance).GetMethod("FindLooseAttachments", bf).Invoke(app, null);
var listNames = new System.Collections.Generic.List<string>();
var lists = new System.Collections.Generic.Dictionary<string, UnityEngine.GameObject[]>();
// the body export has these (a character's own head, face and hair)
var bodyLists = new System.Collections.Generic.HashSet<string> { "maleHeads", "femaleHeads", "maleEyebrows", "femaleEyebrows", "maleFacialHairs", "femaleFacialHairs", "hairs", "ears" };
foreach (var field in typeof(SyntyPlayerAppearance).GetFields(bf))
{
    if (field.FieldType != typeof(UnityEngine.GameObject[]) || bodyLists.Contains(field.Name)) continue;
    listNames.Add(field.Name);
    lists[field.Name] = (UnityEngine.GameObject[])field.GetValue(app) ?? new UnityEngine.GameObject[0];
}
string FileName(string name)
{
    var sb = new System.Text.StringBuilder();
    foreach (var ch in name) sb.Append(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_');
    return sb.ToString();
}
bool IsBodyPart(string name) => name.EndsWith("_00", System.StringComparison.Ordinal);
var partFiles = new System.Collections.Generic.Dictionary<UnityEngine.GameObject, string>();
var usedNames = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
var partQueue = new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach (var name in listNames)
{
    foreach (var go in lists[name])
    {
        if (!go || partFiles.ContainsKey(go)) continue;
        var file = FileName(go.name);
        // the body's own parts (bare arms, plain underclothes) are the body export's
        if (IsBodyPart(go.name)) { partFiles[go] = file; continue; }
        while (!usedNames.Add(file)) file += "_";
        partFiles[go] = file;
        partQueue.Add(go);
    }
}

// ---- The props: the prefabs the server's items name ---------------------------------------------------------------------
var prefabPaths = new System.Collections.Generic.SortedSet<string>(System.StringComparer.Ordinal);
var itemSource = "the list file";
try
{
    using (var web = new System.Net.WebClient())
    {
        var itemsJson = web.DownloadString("http://localhost/api/items");
        // pets and boats are not drawn on the character
        foreach (System.Text.RegularExpressions.Match item in System.Text.RegularExpressions.Regex.Matches(itemsJson, "\\{\"id\":.*?\"modified\":[^}]*\\}"))
        {
            var text = item.Value;
            var type = System.Text.RegularExpressions.Regex.Match(text, "\"type\":(\\d+)");
            if (!type.Success || type.Groups[1].Value == "20" || type.Groups[1].Value == "37") continue;
            foreach (System.Text.RegularExpressions.Match p in System.Text.RegularExpressions.Regex.Matches(text, "\"(?:genericPrefab|malePrefab|femalePrefab)\":\"([^\"]+)\""))
                prefabPaths.Add(p.Groups[1].Value);
        }
    }
    if (prefabPaths.Count > 0)
    {
        itemSource = "the local server";
        System.IO.File.WriteAllText(prefabListPath, "# The item prefabs the website's gear export writes (tools/characters/export-gear-web.cs): what the server's items\n# name as their model. Written by the export whenever the local server answers; read by it when none does.\n" + string.Join("\n", prefabPaths) + "\n");
    }
}
catch (System.Exception e) { report.Append("the local server did not answer (" + e.Message + ")\n"); }
if (prefabPaths.Count == 0 && System.IO.File.Exists(prefabListPath))
    foreach (var line in System.IO.File.ReadAllLines(prefabListPath)) if (line.Trim().Length > 0 && line[0] != '#') prefabPaths.Add(line.Trim());
var propQueue = new System.Collections.Generic.List<string>(prefabPaths);

// ---- Files ---------------------------------------------------------------------------------------------------------------
var partsDir = System.IO.Path.Combine(outDir, "parts");
var propsDir = System.IO.Path.Combine(outDir, "props");
var texDir = System.IO.Path.Combine(outDir, "textures");
foreach (var dir in new[] { partsDir, propsDir, texDir })
{
    if (System.IO.Directory.Exists(dir)) foreach (var old in System.IO.Directory.GetFiles(dir)) System.IO.File.Delete(old);
    System.IO.Directory.CreateDirectory(dir);
}
long partBytes = 0, propBytes = 0, texBytes = 0;
var partJson = new System.Collections.Generic.Dictionary<string, string>();
var rebound = 0;

short P16(float x, float a, float b) => (short)UnityEngine.Mathf.RoundToInt(b > a ? (x - a) / (b - a) * 65535f - 32768f : 0f);
ushort U16(float x, float a, float b) => (ushort)UnityEngine.Mathf.RoundToInt(b > a ? UnityEngine.Mathf.Clamp01((x - a) / (b - a)) * 65535f : 0f);
sbyte N8(float x) => (sbyte)UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp(x, -1f, 1f) * 127f);

// One armor part: every skinned mesh under it as one, in the body export's layout behind a header.
//   'RFG1', vertices, indices (uint32 each), min xyz, max xyz, uv min, uv max (float32)
//   positions int16 x3 over the bounds (padded to 4 bytes), normals int8 x4, uvs uint16 x2 over their range,
//   four bone indices and four weights (uint8), the triangles (uint16)
// A part with a picture of its own (the Dark Fortress armor, named "..._DF<Set>_<colorway>"): its first submesh is the
// part (drawn with that picture, SyntyPlayerAppearance.SetPartMaterials), its second, if any, its bare skin, written as
// "<file>__skin" and drawn with the body's material. gear.json's ownParts says which picture and which skin file.
var ownParts = new System.Collections.Generic.SortedDictionary<string, string>(System.StringComparer.Ordinal);
var ownTextures = new System.Collections.Generic.Dictionary<UnityEngine.Texture, string>();
string OwnTexture(UnityEngine.Texture t)
{
    if (!t) return null;
    if (ownTextures.TryGetValue(t, out var have)) return have;
    var file = "df-" + FileName(t.name).ToLowerInvariant() + ".jpg";
    SaveTexture(Pixels(t, 1024, 1024), 1024, 1024, file, true);
    return ownTextures[t] = "textures/" + file;
}
void WritePartOrOwn(UnityEngine.GameObject go)
{
    var r = go.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>(true);
    if (!go.name.Contains("_DF") || !r || !r.sharedMesh) { WritePart(go); return; }
    var file = partFiles[go];
    WritePart(go, 0);
    string skinFile = null;
    if (r.sharedMesh.subMeshCount > 1) { skinFile = file + "__skin"; WritePart(go, 1, skinFile); }
    var m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
    UnityEngine.Texture albedo = null, glow = null;
    if (m)
    {
        foreach (var name in new[] { "_Albedo_Map", "_BaseMap", "_MainTex" }) if (!albedo && m.HasProperty(name)) albedo = m.GetTexture(name);
        foreach (var name in new[] { "_Emission_Map", "_EmissionMap" }) if (!glow && m.HasProperty(name)) glow = m.GetTexture(name);
    }
    ownParts[file] = "{\"tex\":" + Q(OwnTexture(albedo)) + ",\"glow\":" + Q(OwnTexture(glow)) + ",\"skin\":" + Q(skinFile) + "}";
}

void WritePart(UnityEngine.GameObject go, int only = -1, string fileName = null)
{
    var pos = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var nrm = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var uvs = new System.Collections.Generic.List<UnityEngine.Vector2>();
    var skin = new System.Collections.Generic.List<byte>();
    var tris = new System.Collections.Generic.List<int>();
    foreach (var r in go.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
    {
        var mesh = r.sharedMesh;
        if (!mesh) continue;
        var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv; var weights = mesh.boneWeights; var binds = mesh.bindposes;
        var rb = r.bones;
        var slot = new int[rb.Length];
        // a part bound to the bones in another pose than the body's is moved to the body's: its vertices by what
        // each of their bones differs
        var delta = new UnityEngine.Matrix4x4[rb.Length];
        var differs = false;
        for (var i = 0; i < rb.Length; i++)
        {
            if (!rb[i] || !boneIndex.TryGetValue(rb[i], out var b)) throw new System.Exception(go.name + " has a bone outside the skeleton: " + (rb[i] ? rb[i].name : "null"));
            slot[i] = b;
            delta[i] = UnityEngine.Matrix4x4.identity;
            if (i >= binds.Length) continue;
            if (placeOnly[b] && gearInverse[b] == null && !Near(refInverse[b], binds[i], 2e-3f)) gearInverse[b] = binds[i];
            var d = (gearInverse[b] ?? refInverse[b]).inverse * binds[i];
            var off = 0f;
            for (var k = 0; k < 16; k++) off = UnityEngine.Mathf.Max(off, UnityEngine.Mathf.Abs(d[k] - UnityEngine.Matrix4x4.identity[k]));
            if (off > 2e-3f) { delta[i] = d; differs = true; }
        }
        if (differs) rebound++;
        var first = pos.Count;
        for (var i = 0; i < v.Length; i++)
        {
            var bw = i < weights.Length ? weights[i] : new UnityEngine.BoneWeight { boneIndex0 = 0, weight0 = 1f };
            int[] idx = { bw.boneIndex0, bw.boneIndex1, bw.boneIndex2, bw.boneIndex3 };
            float[] wt = { bw.weight0, bw.weight1, bw.weight2, bw.weight3 };
            var sum = wt[0] + wt[1] + wt[2] + wt[3];
            var p = v[i]; var q = i < n.Length ? n[i] : UnityEngine.Vector3.up;
            if (differs && sum > 0f)
            {
                var p2 = UnityEngine.Vector3.zero; var q2 = UnityEngine.Vector3.zero;
                for (var k = 0; k < 4; k++)
                {
                    if (wt[k] <= 0f || idx[k] >= delta.Length) continue;
                    p2 += delta[idx[k]].MultiplyPoint3x4(p) * (wt[k] / sum);
                    q2 += delta[idx[k]].MultiplyVector(q) * (wt[k] / sum);
                }
                p = p2; q = q2.normalized;
            }
            pos.Add(V(p)); nrm.Add(V(q)); uvs.Add(i < uv.Length ? uv[i] : UnityEngine.Vector2.zero);
            var bytes = new int[4]; var total = 0;
            for (var k = 0; k < 4; k++) { bytes[k] = sum > 0f ? UnityEngine.Mathf.RoundToInt(wt[k] / sum * 255f) : (k == 0 ? 255 : 0); total += bytes[k]; }
            bytes[0] += 255 - total; // the rounding goes to the strongest
            for (var k = 0; k < 4; k++) skin.Add((byte)(bytes[k] > 0 && idx[k] < slot.Length ? slot[idx[k]] : 0));
            for (var k = 0; k < 4; k++) skin.Add((byte)UnityEngine.Mathf.Clamp(bytes[k], 0, 255));
        }
        for (var s = 0; s < mesh.subMeshCount; s++)
        {
            if (only >= 0 && s != only) continue;
            var t = mesh.GetTriangles(s);
            // the mirror turns every triangle inside out: turned back
            for (var k = 0; k + 2 < t.Length; k += 3) { tris.Add(first + t[k]); tris.Add(first + t[k + 2]); tris.Add(first + t[k + 1]); }
        }
    }
    if (pos.Count == 0) { report.Append("no mesh in " + go.name + "\n"); partFiles.Remove(go); return; }
    if (pos.Count > 65535) throw new System.Exception(go.name + " has too many vertices for uint16 indices");
    var lo = pos[0]; var hi = lo;
    foreach (var p in pos) { lo = UnityEngine.Vector3.Min(lo, p); hi = UnityEngine.Vector3.Max(hi, p); }
    var uvLo = uvs[0]; var uvHi = uvLo;
    foreach (var q in uvs) { uvLo = UnityEngine.Vector2.Min(uvLo, q); uvHi = UnityEngine.Vector2.Max(uvHi, q); }
    var ms = new System.IO.MemoryStream();
    var w = new System.IO.BinaryWriter(ms);
    w.Write((uint)0x31474652); w.Write((uint)pos.Count); w.Write((uint)tris.Count);
    w.Write(lo.x); w.Write(lo.y); w.Write(lo.z); w.Write(hi.x); w.Write(hi.y); w.Write(hi.z);
    w.Write(uvLo.x); w.Write(uvLo.y); w.Write(uvHi.x); w.Write(uvHi.y);
    foreach (var p in pos) { w.Write(P16(p.x, lo.x, hi.x)); w.Write(P16(p.y, lo.y, hi.y)); w.Write(P16(p.z, lo.z, hi.z)); }
    while (ms.Position % 4 != 0) w.Write((byte)0);
    foreach (var q in nrm) { w.Write(N8(q.x)); w.Write(N8(q.y)); w.Write(N8(q.z)); w.Write((sbyte)0); }
    foreach (var q in uvs) { w.Write(U16(q.x, uvLo.x, uvHi.x)); w.Write(U16(q.y, uvLo.y, uvHi.y)); }
    w.Write(skin.ToArray());
    foreach (var t in tris) w.Write((ushort)t);
    w.Flush();
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(partsDir, (fileName ?? partFiles[go]) + ".bin"), ms.ToArray());
    partBytes += ms.Length;
}

// ---- Textures --------------------------------------------------------------------------------------------------------------
bool IsSrgb(UnityEngine.Texture t)
{
    var importer = UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(t)) as UnityEditor.TextureImporter;
    return importer == null || importer.sRGBTexture;
}
UnityEngine.Color[] Pixels(UnityEngine.Texture t, int width, int height)
{
    // the stored values, as in the file (an sRGB target undoes the sampler's decode)
    var srgb = IsSrgb(t);
    var rt = UnityEngine.RenderTexture.GetTemporary(width, height, 0, UnityEngine.RenderTextureFormat.ARGB32, srgb ? UnityEngine.RenderTextureReadWrite.sRGB : UnityEngine.RenderTextureReadWrite.Linear);
    UnityEngine.Graphics.Blit(t, rt);
    var prev = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGBA32, false, !srgb);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
    tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    UnityEngine.RenderTexture.ReleaseTemporary(rt);
    var px = tex.GetPixels();
    UnityEngine.Object.DestroyImmediate(tex);
    return px;
}
float Lin(float c) => c <= 0.04045f ? c / 12.92f : UnityEngine.Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
void SaveTexture(UnityEngine.Color[] px, int width, int height, string file, bool jpg)
{
    var tex = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false, true);
    tex.SetPixels(px); tex.Apply();
    var bytes = jpg ? tex.EncodeToJPG(90) : tex.EncodeToPNG();
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(texDir, file), bytes);
    texBytes += bytes.Length;
    UnityEngine.Object.DestroyImmediate(tex);
}
string Hex(UnityEngine.Color c) => "\"#" + UnityEngine.ColorUtility.ToHtmlStringRGB(c) + "\"";

// ---- The materials: every ItemMaterial, as the game finds them (ItemManager.GetMaterial) ----------------------------------
var hero = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyHeroCharacters/Materials/FantasyHero.mat");
UnityEngine.Material[] tierMaterials = null;
var itemManager = UnityEngine.Object.FindAnyObjectByType<ItemManager>();
if (itemManager) tierMaterials = (UnityEngine.Material[])typeof(ItemManager).GetField("itemMaterials", bf).GetValue(itemManager);
if (tierMaterials == null || tierMaterials.Length == 0)
{
    // the list is on the ItemManager of the main scene: read from its file when another scene is open
    var found = new System.Collections.Generic.List<UnityEngine.Material>();
    var inside = false;
    foreach (var line in System.IO.File.ReadLines(System.IO.Path.Combine(project, "Assets/Scenes/MainWorld.unity")))
    {
        if (!inside) { if (line.TrimEnd() == "  itemMaterials:") inside = true; continue; }
        var g = System.Text.RegularExpressions.Regex.Match(line, "^  - \\{fileID: \\d+, guid: ([0-9a-f]{32})");
        if (!g.Success) break;
        found.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(g.Groups[1].Value)));
    }
    tierMaterials = found.ToArray();
    report.Append("tier materials read from the MainWorld scene file: " + tierMaterials.Length + "\n");
}
// the textures every gear material is expected to share with the body's (one atlas): others are named in the report
string[] atlasProps = { "_Texture", "_Mask_01", "_Mask_02", "_Mask_03", "_Mask_04" };
bool SameAtlas(UnityEngine.Material m)
{
    foreach (var p in atlasProps) if (!m.HasProperty(p) || m.GetTexture(p) != hero.GetTexture(p)) return false;
    return true;
}
string[] colorProps = { "_Color_Primary", "_Color_Secondary", "_Color_Leather_Primary", "_Color_Leather_Secondary", "_Color_Metal_Primary", "_Color_Metal_Secondary", "_Color_Metal_Dark", "_Color_Skin", "_Color_Hair", "_Color_Stubble", "_Color_Scar", "_Color_BodyArt", "_Color_Eyes" };
string MaterialJson(UnityEngine.Material m)
{
    var colors = new System.Collections.Generic.List<string>();
    foreach (var p in colorProps) colors.Add(m.HasProperty(p) ? Hex(m.GetColor(p)) : "null");
    float Fl(string p, float d) => m.HasProperty(p) ? m.GetFloat(p) : d;
    // the graph reads the factor alone: its _Glow_Enabled switch is wired to nothing
    var glow = Fl("_Glow_Factor", 0f);
    var emission = UnityEngine.Mathf.Max(Fl("_Emission", 0f), Fl("_Emmision", 0f));
    return "{\"c\":[" + string.Join(",", colors) + "],\"metallic\":" + F(Fl("_Metallic", 0f)) + ",\"smoothness\":" + F(Fl("_Smoothness", 0.2f)) + ",\"bodyArt\":" + F(Fl("_BodyArt_Amount", 0f))
        + ",\"glow\":" + F(glow) + ",\"emission\":" + F(emission) + ",\"glowColor\":" + (m.HasProperty("_Color1") ? Hex(m.GetColor("_Color1")) : "null") + ",\"shader\":" + Q(m.shader.name) + ",\"atlas\":" + (SameAtlas(m) ? "true" : "false") + "}";
}
var materialJson = new System.Collections.Generic.List<string>();
var otherAtlas = new System.Collections.Generic.List<string>();
foreach (RavenNest.Models.ItemMaterial value in System.Enum.GetValues(typeof(RavenNest.Models.ItemMaterial)))
{
    var index = (int)value - 1;
    if (index < 0) continue;
    var m = index < tierMaterials.Length ? tierMaterials[index] : UnityEngine.Resources.Load<UnityEngine.Material>("ClassGear/" + value);
    if (!m) { report.Append("no material for " + value + "\n"); continue; }
    if (!SameAtlas(m)) otherAtlas.Add(value.ToString());
    materialJson.Add(Q(value.ToString()) + ":" + MaterialJson(m).Insert(1, "\"v\":" + (int)value + ","));
}
if (otherAtlas.Count > 0) report.Append("materials with other textures than the body's atlas (drawn with the atlas all the same): " + string.Join(", ", otherAtlas) + "\n");
// the cape's own material (a picture over a cloth), and the body's for the parts that keep it
var capeList = lists.ContainsKey("capes") ? lists["capes"] : new UnityEngine.GameObject[0];
UnityEngine.Material capeMaterial = null;
foreach (var cape in capeList) { var cr = cape ? cape.GetComponent<UnityEngine.SkinnedMeshRenderer>() : null; if (cr && cr.sharedMaterial) { capeMaterial = cr.sharedMaterial; break; } }

// the atlas: the armor's picture as it is, and the seven masks a tier's colors go through (the body export bakes the
// body's colors in; here every material has its own)
void WriteAtlas()
{
    const int Size = 1024;
    UnityEngine.Texture T(string name) => hero.GetTexture(name);
    var albedo = Pixels(T("_Texture"), Size, Size);
    var m1 = Pixels(T("_Mask_01"), Size, Size); var s1 = IsSrgb(T("_Mask_01"));
    var m2 = Pixels(T("_Mask_02"), Size, Size); var s2 = IsSrgb(T("_Mask_02"));
    var m4 = Pixels(T("_Mask_04"), Size, Size); var s4 = IsSrgb(T("_Mask_04"));
    float D(float c, bool srgb) => srgb ? Lin(c) : c;
    var c1 = new UnityEngine.Color[albedo.Length]; var c2 = new UnityEngine.Color[albedo.Length]; var c3 = new UnityEngine.Color[albedo.Length];
    for (var i = 0; i < albedo.Length; i++)
    {
        albedo[i].a = 1f;
        // masks-c: cloth, trim, mail (primary, secondary, dark metal); masks-d: leather, dark leather; masks-e: plate, metal trim
        c1[i] = new UnityEngine.Color(1f - D(m1[i].r, s1), 1f - D(m1[i].g, s1), 1f - D(m2[i].b, s2), 1f);
        c2[i] = new UnityEngine.Color(1f - D(m4[i].r, s4), 1f - D(m4[i].g, s4), 0f, 1f);
        c3[i] = new UnityEngine.Color(1f - D(m2[i].r, s2), 1f - D(m2[i].g, s2), 0f, 1f);
    }
    SaveTexture(albedo, Size, Size, "albedo.jpg", true);
    SaveTexture(c1, Size, Size, "masks-c.png", false);
    SaveTexture(c2, Size, Size, "masks-d.png", false);
    SaveTexture(c3, Size, Size, "masks-e.png", false);
}

// ---- Props ---------------------------------------------------------------------------------------------------------------
var propJson = new System.Collections.Generic.List<string>();
var propMaterials = new System.Collections.Generic.List<string>();
var propMaterialIndex = new System.Collections.Generic.Dictionary<UnityEngine.Material, int>();
var propTextures = new System.Collections.Generic.Dictionary<UnityEngine.Texture, string>();
var propShaders = new System.Collections.Generic.Dictionary<string, int>();
string PropTexture(UnityEngine.Texture t)
{
    if (!t) return null;
    if (propTextures.TryGetValue(t, out var file)) return file;
    var width = UnityEngine.Mathf.Min(t.width, 1024); var height = UnityEngine.Mathf.Min(t.height, 1024);
    file = "p-" + FileName(t.name).ToLowerInvariant() + ".jpg";
    while (System.IO.File.Exists(System.IO.Path.Combine(texDir, file))) file = file.Replace(".jpg", "_.jpg");
    var px = Pixels(t, width, height);
    for (var i = 0; i < px.Length; i++) px[i].a = 1f;
    SaveTexture(px, width, height, file, true);
    propTextures[t] = file;
    return file;
}
int PropMaterial(UnityEngine.Material m)
{
    if (!m) return -1;
    if (propMaterialIndex.TryGetValue(m, out var index)) return index;
    propShaders[m.shader.name] = (propShaders.TryGetValue(m.shader.name, out var n) ? n : 0) + 1;
    string json;
    if (m.HasProperty("_Color_Primary") && m.HasProperty("_Mask_01") && SameAtlas(m))
    {
        // drawn with the armor's shader (a helmet that is a model of its own)
        json = "{\"gear\":" + MaterialJson(m) + "}";
    }
    else if (m.shader.name == "Weapon")
    {
        // the bows' and staffs' shader (Assets/Shaders/Weapon.shader): four colors laid out over the uv in bands, and a
        // glow that is its picture times a color
        UnityEngine.Color Col(string name) => m.HasProperty(name) ? m.GetColor(name) : UnityEngine.Color.black;
        var glowMap = m.HasProperty("_TextureSample1") ? m.GetTexture("_TextureSample1") : null;
        var st = m.HasProperty("_TextureSample1") ? m.GetTextureScale("_TextureSample1") : UnityEngine.Vector2.one;
        var so = m.HasProperty("_TextureSample1") ? m.GetTextureOffset("_TextureSample1") : UnityEngine.Vector2.zero;
        json = "{\"weapon\":{\"handle\":" + Hex(Col("_HandleColor")) + ",\"edge\":" + Hex(Col("_EdgeColor")) + ",\"body\":" + Hex(Col("_BodyColor")) + ",\"inner\":" + Hex(Col("_InnerEdgeColor"))
            + ",\"glow\":" + Hex(Col("_Emission")) + ",\"glowMap\":" + Q(PropTexture(glowMap)) + ",\"st\":[" + F(st.x) + "," + F(st.y) + "," + F(so.x) + "," + F(so.y) + "]},\"shader\":\"Weapon\",\"name\":" + Q(m.name) + "}";
    }
    else
    {
        UnityEngine.Texture map = null;
        foreach (var name in new[] { "_BaseMap", "_MainTex", "_Texture", "_Albedo", "_BaseColorMap", "_Base_Texture", "_Albedo_Map" }) if (m.HasProperty(name) && m.GetTexture(name)) { map = m.GetTexture(name); break; }
        if (!map)
        {
            // a shader of its own: its first picture
            var sh = m.shader;
            for (var i = 0; i < sh.GetPropertyCount() && !map; i++) if (sh.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture && m.GetTexture(sh.GetPropertyName(i)) is UnityEngine.Texture2D t2) map = t2;
        }
        var tint = UnityEngine.Color.white;
        foreach (var name in new[] { "_BaseColor", "_Color", "_Tint", "_MainColor" }) if (m.HasProperty(name)) { tint = m.GetColor(name); break; }
        var emission = UnityEngine.Color.black;
        if (m.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionColor")) emission = m.GetColor("_EmissionColor");
        float Fl(string p, float d) => m.HasProperty(p) ? m.GetFloat(p) : d;
        json = "{\"map\":" + Q(PropTexture(map)) + ",\"color\":" + Hex(tint) + ",\"metallic\":" + F(Fl("_Metallic", 0f)) + ",\"smoothness\":" + F(Fl("_Smoothness", Fl("_Glossiness", 0.2f)))
            + ",\"emission\":" + (emission.maxColorComponent > 0.01f ? Hex(emission) : "null") + ",\"shader\":" + Q(m.shader.name) + ",\"name\":" + Q(m.name) + "}";
    }
    index = propMaterials.Count;
    propMaterials.Add(json);
    propMaterialIndex[m] = index;
    return index;
}

// One prop: the meshes of the prefab as one, in the space of the prefab's root (the root's own place under the bone is
// written beside it: the game overrides it for necklaces).
//   'RFP1', vertices, indices, groups (uint32 each), min xyz, max xyz, uv min, uv max (float32), a group's first index
//   and count (uint32 x2, one per material), positions int16 x3 (padded to 4), normals int8 x4, uvs uint16 x2,
//   triangles (uint16)
void WriteProp(string path)
{
    var p = UnityEngine.Resources.Load<UnityEngine.GameObject>(path);
    if (!p) { report.Append("no prefab " + path + "\n"); return; }
    var root = p.transform;
    var toRoot = root.worldToLocalMatrix;
    // the first level of detail only
    var skip = new System.Collections.Generic.HashSet<UnityEngine.Renderer>();
    foreach (var lod in p.GetComponentsInChildren<UnityEngine.LODGroup>(true))
    {
        var levels = lod.GetLODs();
        for (var l = 1; l < levels.Length; l++) foreach (var r in levels[l].renderers) if (r) skip.Add(r);
        if (levels.Length > 0) foreach (var r in levels[0].renderers) skip.Remove(r);
    }
    var byMaterial = new System.Collections.Generic.SortedDictionary<int, System.Collections.Generic.List<int>>();
    var pos = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var nrm = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var uvs = new System.Collections.Generic.List<UnityEngine.Vector2>();
    foreach (var r in p.GetComponentsInChildren<UnityEngine.Renderer>(true))
    {
        if (skip.Contains(r) || !r.enabled || r is UnityEngine.ParticleSystemRenderer || r is UnityEngine.TrailRenderer || r is UnityEngine.LineRenderer) continue;
        // switched off in the prefab: not shown in the game either
        var on = true;
        for (var t = r.transform; t && t != root.parent; t = t.parent) if (!t.gameObject.activeSelf) { on = false; break; }
        if (!on) continue;
        UnityEngine.Mesh mesh = null;
        if (r is UnityEngine.SkinnedMeshRenderer sk) mesh = sk.sharedMesh;
        else { var mf = r.GetComponent<UnityEngine.MeshFilter>(); mesh = mf ? mf.sharedMesh : null; }
        if (!mesh) continue;
        var matrix = toRoot * r.transform.localToWorldMatrix;
        var normalMatrix = matrix.inverse.transpose;
        var flipped = matrix.determinant < 0f;
        var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv;
        var first = pos.Count;
        for (var i = 0; i < v.Length; i++)
        {
            pos.Add(V(matrix.MultiplyPoint3x4(v[i])));
            nrm.Add(V(normalMatrix.MultiplyVector(i < n.Length ? n[i] : UnityEngine.Vector3.up).normalized));
            uvs.Add(i < uv.Length ? uv[i] : UnityEngine.Vector2.zero);
        }
        var mats = r.sharedMaterials;
        for (var s = 0; s < mesh.subMeshCount; s++)
        {
            var material = PropMaterial(s < mats.Length ? mats[s] : (mats.Length > 0 ? mats[mats.Length - 1] : null));
            if (material < 0) continue;
            if (!byMaterial.TryGetValue(material, out var list)) byMaterial[material] = list = new System.Collections.Generic.List<int>();
            var t = mesh.GetTriangles(s);
            for (var k = 0; k + 2 < t.Length; k += 3)
            {
                // the mirror turns a triangle inside out, and so does a mirrored child: turned back once
                list.Add(first + t[k]);
                if (flipped) { list.Add(first + t[k + 1]); list.Add(first + t[k + 2]); }
                else { list.Add(first + t[k + 2]); list.Add(first + t[k + 1]); }
            }
        }
    }
    if (pos.Count == 0) { report.Append("no mesh in " + path + "\n"); return; }
    if (pos.Count > 65535) { report.Append(path + " has " + pos.Count + " vertices: left out\n"); return; }
    var lo = pos[0]; var hi = lo;
    foreach (var q in pos) { lo = UnityEngine.Vector3.Min(lo, q); hi = UnityEngine.Vector3.Max(hi, q); }
    var uvLo = uvs[0]; var uvHi = uvLo;
    foreach (var q in uvs) { uvLo = UnityEngine.Vector2.Min(uvLo, q); uvHi = UnityEngine.Vector2.Max(uvHi, q); }
    var total = 0;
    foreach (var g in byMaterial) total += g.Value.Count;
    var ms = new System.IO.MemoryStream();
    var w = new System.IO.BinaryWriter(ms);
    w.Write((uint)0x31504652); w.Write((uint)pos.Count); w.Write((uint)total); w.Write((uint)byMaterial.Count);
    w.Write(lo.x); w.Write(lo.y); w.Write(lo.z); w.Write(hi.x); w.Write(hi.y); w.Write(hi.z);
    w.Write(uvLo.x); w.Write(uvLo.y); w.Write(uvHi.x); w.Write(uvHi.y);
    var start = 0;
    foreach (var g in byMaterial) { w.Write((uint)start); w.Write((uint)g.Value.Count); start += g.Value.Count; }
    foreach (var q in pos) { w.Write(P16(q.x, lo.x, hi.x)); w.Write(P16(q.y, lo.y, hi.y)); w.Write(P16(q.z, lo.z, hi.z)); }
    while (ms.Position % 4 != 0) w.Write((byte)0);
    foreach (var q in nrm) { w.Write(N8(q.x)); w.Write(N8(q.y)); w.Write(N8(q.z)); w.Write((sbyte)0); }
    foreach (var q in uvs) { w.Write(U16(q.x, uvLo.x, uvHi.x)); w.Write(U16(q.y, uvLo.y, uvHi.y)); }
    foreach (var g in byMaterial) foreach (var t in g.Value) w.Write((ushort)t);
    w.Flush();
    var file = FileName(path.Replace('/', '_').Replace(' ', '_')).ToLowerInvariant();
    while (!usedNames.Add("prop:" + file)) file += "_";
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(propsDir, file + ".bin"), ms.ToArray());
    propBytes += ms.Length;
    var rp = V(root.localPosition); var rq = R(root.localRotation); var rs = root.localScale;
    propJson.Add(Q(path) + ":{\"f\":" + Q(file) + ",\"p\":[" + F(rp.x) + "," + F(rp.y) + "," + F(rp.z) + "],\"q\":[" + F(rq.x) + "," + F(rq.y) + "," + F(rq.z) + "," + F(rq.w) + "],\"s\":[" + F(rs.x) + "," + F(rs.y) + "," + F(rs.z) + "],\"m\":[" + string.Join(",", byMaterial.Keys) + "]}");
}

// ---- The stances: how a character stands with a weapon in hand ---------------------------------------------------------------
// The game's idle with a weapon out is the animation pack's own for that weapon (the animator's Weapon number, from
// ItemTypeExtensions.GetAnimation). One file each, in the body export's animation layout, fetched when a weapon shows.
var stanceClips = new[]
{
    new[] { "armed", "Armed/RPG-Character@Armed-Idle.FBX" },
    new[] { "sword2", "2Hand-Sword/RPG-Character@2Hand-Sword-Idle.FBX" },
    new[] { "bow", "2Hand-Bow/RPG-Character@2Hand-Bow-Idle.FBX" },
    new[] { "staff", "Staff/RPG-Character@Staff-Idle.fbx" },
    new[] { "spear", "2Hand-Spear/RPG-Character@2Hand-Spear-Idle.FBX" },
    new[] { "axe2", "2Hand-Axe/RPG-Character@2Hand-Axe-Idle.FBX" },
    new[] { "shield", "Shield/RPG-Character@Shield-Idle.fbx" },
};
var stanceDir = System.IO.Path.Combine(outDir, "stances");
var stanceJson = new System.Collections.Generic.List<string>();
long stanceBytes = 0;
UnityEngine.GameObject stanceCharacter = null;
UnityEngine.SceneManagement.Scene stanceScene = default;
System.Collections.Generic.List<UnityEngine.Transform> stanceBones = null;
UnityEngine.Quaternion[] stanceRest = null;
UnityEngine.Transform stanceChest = null;
var stanceChestRest = UnityEngine.Quaternion.identity;
void EndStances()
{
    if (stanceCharacter) UnityEngine.Object.DestroyImmediate(stanceCharacter);
    stanceCharacter = null;
    if (stanceScene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(stanceScene);
    stanceScene = default;
}
void WriteStance(string name, string file)
{
    if (!stanceCharacter)
    {
        // a copy of the player in a scene of its own: the open scene is not touched
        System.IO.Directory.CreateDirectory(stanceDir);
        foreach (var old in System.IO.Directory.GetFiles(stanceDir, "*.json")) System.IO.File.Delete(old);
        stanceScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        stanceCharacter = UnityEngine.Object.Instantiate(prefab);
        stanceCharacter.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(stanceCharacter, stanceScene);
        stanceCharacter.transform.SetPositionAndRotation(UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
        stanceCharacter.transform.localScale = UnityEngine.Vector3.one;
        var a = stanceCharacter.GetComponentInChildren<UnityEngine.Animator>(true);
        var root = a.transform.Find("Root") ?? stanceCharacter.transform.Find("Root");
        stanceBones = new System.Collections.Generic.List<UnityEngine.Transform>();
        void Walk(UnityEngine.Transform t) { stanceBones.Add(t); foreach (UnityEngine.Transform c in t) if (!c.GetComponent<UnityEngine.Renderer>()) Walk(c); }
        Walk(root);
        if (stanceBones.Count != bones.Count) throw new System.Exception("the copy has " + stanceBones.Count + " bones, the prefab " + bones.Count);
        stanceRest = new UnityEngine.Quaternion[stanceBones.Count];
        for (var i = 0; i < stanceBones.Count; i++) stanceRest[i] = stanceBones[i].localRotation;
        stanceChest = stanceBones.Find(b => b.name == "Spine_03");
        if (stanceChest) stanceChestRest = stanceChest.rotation;
    }
    var path = "Assets/RPG Character Animation Pack/Animations/" + file;
    UnityEngine.AnimationClip clip = null;
    foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)) if (o is UnityEngine.AnimationClip c && !c.name.StartsWith("__preview")) { clip = c; break; }
    if (!clip) { report.Append("no clip " + path + "\n"); return; }
    var animated = stanceCharacter.GetComponentInChildren<UnityEngine.Animator>(true).gameObject;
    const int Fps = 15;
    var frames = UnityEngine.Mathf.Max(2, UnityEngine.Mathf.RoundToInt(clip.length * Fps) + 1);
    var rots = new UnityEngine.Quaternion[frames, stanceBones.Count];
    var hipsAt = new UnityEngine.Vector3[frames];
    var hips = stanceBones.FindIndex(b => b.name == "Hips");
    // a stance turns the whole body (a clip's root motion lands on the animated object itself): that turn is kept on
    // the Root bone, which hangs right under it; where the clip walks to is left out (the figure stays on its spot)
    var stood = animated.transform.localRotation;
    var turned = 0f;
    // a weapon's stance stands side-on (the chest turned from where the character faces, the head still looking
    // ahead): how far, so the page can turn the figure to show its front
    var facing = 0f;
    UnityEditor.AnimationMode.StartAnimationMode();
    try
    {
        for (var f = 0; f < frames; f++)
        {
            UnityEditor.AnimationMode.BeginSampling();
            UnityEditor.AnimationMode.SampleAnimationClip(animated, clip, UnityEngine.Mathf.Min(clip.length, f / (float)Fps));
            UnityEditor.AnimationMode.EndSampling();
            for (var b = 0; b < stanceBones.Count; b++) rots[f, b] = stanceBones[b].localRotation;
            if (stanceBones[0].parent == animated.transform)
            {
                var turn = UnityEngine.Quaternion.Inverse(stood) * animated.transform.localRotation;
                rots[f, 0] = turn * rots[f, 0];
                turned = UnityEngine.Mathf.Max(turned, UnityEngine.Quaternion.Angle(UnityEngine.Quaternion.identity, turn));
            }
            if (hips >= 0) hipsAt[f] = stanceBones[hips].localPosition;
            if (stanceChest)
            {
                var ahead = (stanceChest.rotation * UnityEngine.Quaternion.Inverse(stanceChestRest)) * UnityEngine.Vector3.forward;
                facing += UnityEngine.Mathf.Atan2(ahead.x, ahead.z) * UnityEngine.Mathf.Rad2Deg / frames;
            }
        }
    }
    finally { UnityEditor.AnimationMode.StopAnimationMode(); }
    animated.transform.localRotation = stood;
    var tracks = new System.Collections.Generic.List<string>();
    for (var b = 0; b < stanceBones.Count; b++)
    {
        // bones the clip leaves alone keep their rest pose
        var moves = false;
        for (var f = 0; f < frames && !moves; f++) moves = UnityEngine.Quaternion.Angle(rots[f, b], stanceRest[b]) > 0.05f;
        if (!moves) continue;
        var vals = new System.Collections.Generic.List<string>();
        for (var f = 0; f < frames; f++)
        {
            var q = R(rots[f, b]);
            vals.Add(q.x.ToString("0.####", inv)); vals.Add(q.y.ToString("0.####", inv)); vals.Add(q.z.ToString("0.####", inv)); vals.Add(q.w.ToString("0.####", inv));
        }
        tracks.Add("{\"bone\":" + b + ",\"q\":[" + string.Join(",", vals) + "]}");
    }
    var hipVals = new System.Collections.Generic.List<string>();
    for (var f = 0; f < frames; f++) { var v = V(hipsAt[f]); hipVals.Add(v.x.ToString("0.###", inv)); hipVals.Add(v.y.ToString("0.###", inv)); hipVals.Add(v.z.ToString("0.###", inv)); }
    var text = "{\"fps\":" + Fps + ",\"frames\":" + frames + ",\"loop\":true,\"hips\":" + hips + ",\"hipsP\":[" + string.Join(",", hipVals) + "],\"tracks\":[" + string.Join(",", tracks) + "]}";
    System.IO.File.WriteAllText(System.IO.Path.Combine(stanceDir, name + ".json"), text);
    stanceBytes += text.Length;
    stanceJson.Add(Q(name) + ":{\"file\":" + Q("stances/" + name + ".json") + ",\"turn\":" + facing.ToString("0.#", inv) + "}");
    report.Append("stance " + name + ": " + frames + " frames, " + tracks.Count + " bones move, the chest faces " + facing.ToString("0", inv) + " degrees to the side, " + (text.Length / 1024) + " KB\n");
}

// ---- The manifest ----------------------------------------------------------------------------------------------------------
void WriteManifest()
{
    var json = new System.Text.StringBuilder();
    var version = System.DateTime.UtcNow.ToString("yyyyMMddHHmm", inv);
    json.Append("{\"version\":" + version + ",\n");
    json.Append("\"bones\":[" + string.Join(",", System.Linq.Enumerable.Select(bones, b => Q(b.name))) + "],\n");
    // the armor's own bind pose for the bones the body export has none for: S m S, column-major, as the body export writes
    var inverseJson = new System.Collections.Generic.List<string>();
    for (var b = 0; b < bones.Count; b++)
    {
        if (gearInverse[b] == null) continue;
        var m = gearInverse[b].Value;
        var a = new string[16];
        for (var c = 0; c < 4; c++) for (var r = 0; r < 4; r++) a[c * 4 + r] = F((r == 2 ? -1f : 1f) * m[r, c] * (c == 2 ? -1f : 1f));
        inverseJson.Add("\"" + b + "\":[" + string.Join(",", a) + "]");
    }
    json.Append("\"inverses\":{" + string.Join(",", inverseJson) + "},\n");
    report.Append("bones with the armor's own bind pose: " + string.Join(", ", System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(System.Linq.Enumerable.Range(0, bones.Count), b => gearInverse[b] != null), b => bones[b].name)) + "\n");
    json.Append("\"textures\":{\"albedo\":\"textures/albedo.jpg\",\"masksC\":\"textures/masks-c.png\",\"masksD\":\"textures/masks-d.png\",\"masksE\":\"textures/masks-e.png\"},\n");
    // an item's model ids are indices into these, by the game's own names; null where a list has no part
    var listJson = new System.Collections.Generic.List<string>();
    foreach (var name in listNames)
        listJson.Add(Q(name) + ":[" + string.Join(",", System.Linq.Enumerable.Select(lists[name], go => go && partFiles.TryGetValue(go, out var file) ? Q(file) : "null")) + "]");
    json.Append("\"lists\":{" + string.Join(",\n", listJson) + "},\n");
    // the parts that show the wearer's skin: painted in the player's own skin color (SyntyPlayerAppearance.WithSkin)
    var skinParts = (System.Collections.Generic.HashSet<string>)typeof(SyntyPlayerAppearance).GetField("SkinParts", sf).GetValue(null);
    var skinFiles = new System.Collections.Generic.SortedSet<string>(System.StringComparer.Ordinal);
    foreach (var kv in partFiles) if (skinParts.Contains(kv.Key.name)) skinFiles.Add(kv.Value);
    json.Append("\"skinParts\":[" + string.Join(",", System.Linq.Enumerable.Select(skinFiles, Q)) + "],\n");
    json.Append("\"ownParts\":{" + string.Join(",\n", System.Linq.Enumerable.Select(ownParts, kv => Q(kv.Key) + ":" + kv.Value)) + "},\n");
    // hair under a hat (SyntyPlayerAppearance.HairUnder): the styles that come through each measured hat, and the flat ones worn instead
    var through = (System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>)typeof(SyntyPlayerAppearance).GetField("HairThroughHat", sf).GetValue(null);
    var longHair = (System.Collections.Generic.HashSet<int>)typeof(SyntyPlayerAppearance).GetField("LongHair", sf).GetValue(null);
    var flatShort = (int)typeof(SyntyPlayerAppearance).GetField("FlatShortHair", sf).GetRawConstantValue();
    var flatLong = (int)typeof(SyntyPlayerAppearance).GetField("FlatLongHair", sf).GetRawConstantValue();
    json.Append("\"hair\":{\"flatShort\":" + flatShort + ",\"flatLong\":" + flatLong + ",\"long\":[" + string.Join(",", System.Linq.Enumerable.OrderBy(longHair, x => x)) + "],\"through\":{"
        + string.Join(",", System.Linq.Enumerable.Select(System.Linq.Enumerable.OrderBy(through, x => x.Key), kv => "\"" + kv.Key + "\":[" + string.Join(",", System.Linq.Enumerable.OrderBy(kv.Value, x => x)) + "]")) + "}},\n");
    json.Append("\"materials\":{" + string.Join(",\n", materialJson) + "},\n");
    // a cape is its picture (the clan's cape image in the game; the prefab's own here) over uv * scale + offset
    float CapeFloat(string name, float d) => capeMaterial && capeMaterial.HasProperty(name) ? capeMaterial.GetFloat(name) : d;
    json.Append("\"cape\":" + (capeMaterial
        ? "{\"shader\":" + Q(capeMaterial.shader.name) + ",\"name\":" + Q(capeMaterial.name) + ",\"map\":" + Q(capeMaterial.HasProperty("_Texture") ? PropTexture(capeMaterial.GetTexture("_Texture")) : null)
            + ",\"scale\":" + F(CapeFloat("_Scale", 1f)) + ",\"offset\":" + F(CapeFloat("_Offset", 0f)) + "}"
        : "null") + ",\n");
    json.Append("\"stances\":{" + string.Join(",", stanceJson) + "},\n");
    json.Append("\"propMaterials\":[" + string.Join(",\n", propMaterials) + "],\n");
    json.Append("\"props\":{" + string.Join(",\n", propJson) + "}}");
    System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "gear.json"), json.ToString());
    report.Insert(0, "done: " + (partQueue.Count) + " parts (" + (partBytes / 1024) + " KB, " + rebound + " bound to another pose and moved), " + propJson.Count + " of " + propQueue.Count + " props from " + itemSource + " (" + (propBytes / 1024) + " KB), "
        + propMaterials.Count + " prop materials, " + materialJson.Count + " materials, textures " + (texBytes / 1024) + " KB, " + stanceJson.Count + " stances (" + (stanceBytes / 1024) + " KB), manifest " + (json.Length / 1024) + " KB, version " + version + ", to " + outDir + "\n"
        + "prop shaders: " + string.Join(", ", System.Linq.Enumerable.Select(propShaders, kv => kv.Key + " x" + kv.Value)) + "\n"
        + "cape material: " + (capeMaterial ? capeMaterial.name + " (" + capeMaterial.shader.name + ")" : "none") + "\n");
}

// ---- The work, a few milliseconds a frame ------------------------------------------------------------------------------------
var stage = 0; var at2 = 0;
var totalWork = partQueue.Count + propQueue.Count + 2;
UnityEditor.EditorApplication.CallbackFunction step = null;
step = () =>
{
    try
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 40)
        {
            if (stage == 0) { WriteAtlas(); stage = 1; at2 = 0; }
            else if (stage == 1) { if (at2 < partQueue.Count) WritePartOrOwn(partQueue[at2++]); else { stage = 2; at2 = 0; } }
            else if (stage == 2) { if (at2 < propQueue.Count) WriteProp(propQueue[at2++]); else { stage = 3; at2 = 0; } }
            else if (stage == 3)
            {
                if (at2 < stanceClips.Length) { WriteStance(stanceClips[at2][0], stanceClips[at2][1]); at2++; break; }
                EndStances(); stage = 4;
            }
            else
            {
                WriteManifest();
                UnityEditor.EditorApplication.update -= step;
                System.IO.File.WriteAllText(reportPath, report.ToString());
                return;
            }
        }
        System.IO.File.WriteAllText(reportPath, "running " + (stage == 1 ? at2 : stage == 2 ? partQueue.Count + at2 : 0) + "/" + totalWork);
    }
    catch (System.Exception e)
    {
        UnityEditor.EditorApplication.update -= step;
        try { EndStances(); } catch { }
        System.IO.File.WriteAllText(reportPath, "failed: " + e + "\n" + report);
    }
};
UnityEditor.EditorApplication.update += step;
return "started: " + partQueue.Count + " parts, " + propQueue.Count + " props; wait for " + reportPath;
