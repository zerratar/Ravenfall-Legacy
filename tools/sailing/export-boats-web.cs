// Run with: unity command eval_file --file tools/sailing/export-boats-web.cs --timeout 120 (edit mode), after
// tools/sailing/make-pirate-realm-boats.cs
//
// The boats for the website's boat designer (docs/own-boats.md, "Customizing boats", phase 4): everything the game's
// BoatAssembler builds boats from, for three.js (RavenNest's wwwroot/js/rf-boat-designer.js), into RavenNest's
// wwwroot/boats/:
// - meshes/m<n>.bin: every mesh once, packed small: positions as int16 within the mesh's bounds, normals as int8 (and a
//   spare byte), uvs as uint16 within their range, the triangles as uint16 (uint32 for a mesh past 65535 vertices), so
//   a page loads only the parts it shows;
// - boats.json: the meshes' files and ranges, the materials, and the parts: each ship size's hulls, fronts,
//   middles, backs, figureheads, rams and rig, the Rowboat and the Fishing Boat, the flag and its pole; a part is its
//   meshes with their matrices against the part's root (only what the prefab has switched on; never the locators);
// - textures/: each material baked to one picture: its texture times its tint, with its symbol (a sail's emblem, a
//   flag's skull) laid over it; pictures with holes (frayed sails and flags) as PNG, the rest as JPG.
// Rerun after the parts change; the website serves the files as they are.
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../../RavenNest/src/RavenNest.Blazor/wwwroot/boats"));
System.IO.Directory.CreateDirectory(System.IO.Path.Combine(outDir, "textures"));
var meshDir = System.IO.Path.Combine(outDir, "meshes");
if (System.IO.Directory.Exists(meshDir)) foreach (var old in System.IO.Directory.GetFiles(meshDir, "*.bin")) System.IO.File.Delete(old);
System.IO.Directory.CreateDirectory(meshDir);
var oldBin = System.IO.Path.Combine(outDir, "boats.bin");
if (System.IO.File.Exists(oldBin)) System.IO.File.Delete(oldBin);
var parts = UnityEditor.AssetDatabase.LoadAssetAtPath<BoatParts>("Assets/World/Sailing/BoatParts.asset");
if (!parts) return "no BoatParts (run make-pirate-realm-boats.cs)";

var inv = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.#####", inv);
string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

long totalBytes = 0;
var meshJson = new System.Collections.Generic.Dictionary<string, string>();
var materialJson = new System.Collections.Generic.Dictionary<string, string>();
var meshIds = new System.Collections.Generic.Dictionary<UnityEngine.Mesh, string>();

string MeshId(UnityEngine.Mesh mesh)
{
    if (meshIds.TryGetValue(mesh, out var id)) return id;
    id = "m" + meshIds.Count;
    meshIds[mesh] = id;
    var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv;
    var lo = v.Length > 0 ? v[0] : UnityEngine.Vector3.zero; var hi = lo;
    foreach (var p in v) { lo = UnityEngine.Vector3.Min(lo, p); hi = UnityEngine.Vector3.Max(hi, p); }
    var uvLo = new UnityEngine.Vector2(float.MaxValue, float.MaxValue); var uvHi = new UnityEngine.Vector2(float.MinValue, float.MinValue);
    foreach (var q in uv) { uvLo = UnityEngine.Vector2.Min(uvLo, q); uvHi = UnityEngine.Vector2.Max(uvHi, q); }
    if (uv.Length == 0) { uvLo = UnityEngine.Vector2.zero; uvHi = UnityEngine.Vector2.one; }
    short P(float x, float a, float b) => (short)UnityEngine.Mathf.RoundToInt(b > a ? (x - a) / (b - a) * 65535f - 32768f : 0f);
    ushort U(float x, float a, float b) => (ushort)UnityEngine.Mathf.RoundToInt(b > a ? (x - a) / (b - a) * 65535f : 0f);
    sbyte N(float x) => (sbyte)UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp(x, -1f, 1f) * 127f);
    var ms = new System.IO.MemoryStream();
    var w = new System.IO.BinaryWriter(ms);
    foreach (var p in v) { w.Write(P(p.x, lo.x, hi.x)); w.Write(P(p.y, lo.y, hi.y)); w.Write(P(p.z, lo.z, hi.z)); }
    var normalsAt = ms.Position;
    for (var i = 0; i < v.Length; i++) { var q = i < n.Length ? n[i] : UnityEngine.Vector3.up; w.Write(N(q.x)); w.Write(N(q.y)); w.Write(N(q.z)); w.Write((sbyte)0); }
    var uvAt = ms.Position;
    for (var i = 0; i < v.Length; i++) { var q = i < uv.Length ? uv[i] : UnityEngine.Vector2.zero; w.Write(U(q.x, uvLo.x, uvHi.x)); w.Write(U(q.y, uvLo.y, uvHi.y)); }
    var wide = v.Length > 65535;
    while (ms.Position % 4 != 0) w.Write((byte)0);
    var indexAt = ms.Position;
    var subs = new System.Collections.Generic.List<string>();
    var start = 0;
    for (var s = 0; s < mesh.subMeshCount; s++)
    {
        var tris = mesh.GetTriangles(s);
        subs.Add("[" + start + "," + tris.Length + "]");
        start += tris.Length;
        foreach (var t in tris) { if (wide) w.Write((uint)t); else w.Write((ushort)t); }
    }
    w.Flush();
    System.IO.File.WriteAllBytes(System.IO.Path.Combine(meshDir, id + ".bin"), ms.ToArray());
    totalBytes += ms.Length;
    meshJson[id] = "{\"vertices\":" + v.Length + ",\"min\":[" + F(lo.x) + "," + F(lo.y) + "," + F(lo.z) + "],\"max\":[" + F(hi.x) + "," + F(hi.y) + "," + F(hi.z) + "],\"uvMin\":[" + F(uvLo.x) + "," + F(uvLo.y) + "],\"uvMax\":[" + F(uvHi.x) + "," + F(uvHi.y) + "],\"normals\":" + normalsAt + ",\"uvs\":" + uvAt + ",\"indices\":" + indexAt + ",\"wide\":" + (wide ? "true" : "false") + ",\"subs\":[" + string.Join(",", subs) + "]}";
    return id;
}

// ---- Materials, baked ----------------------------------------------------------------------------------------------
UnityEngine.Texture TexOf(UnityEngine.Material m, params string[] names)
{
    foreach (var n in names) if (m.HasProperty(n) && m.GetTexture(n)) return m.GetTexture(n);
    return null;
}
UnityEngine.Color ColorOf(UnityEngine.Material m, UnityEngine.Color fallback, params string[] names)
{
    foreach (var n in names) if (m.HasProperty(n)) return m.GetColor(n);
    return fallback;
}
UnityEngine.Color[] Pixels(UnityEngine.Texture t, int size)
{
    var rt = UnityEngine.RenderTexture.GetTemporary(size, size, 0, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
    UnityEngine.Graphics.Blit(t, rt);
    var prev = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(size, size, UnityEngine.TextureFormat.RGBA32, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, size, size), 0, 0);
    tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    UnityEngine.RenderTexture.ReleaseTemporary(rt);
    var px = tex.GetPixels();
    UnityEngine.Object.DestroyImmediate(tex);
    return px;
}
string MaterialId(UnityEngine.Material m)
{
    if (!m) return "none";
    var id = m.name;
    if (materialJson.ContainsKey(id)) return id;
    var main = TexOf(m, "_Albedo_Map", "_Main_Texture", "_BaseMap", "_MainTex", "_Albedo");
    var symbol = TexOf(m, "_Symbol_Texture");
    var tint = ColorOf(m, UnityEngine.Color.white, "_Color_Tint", "_BaseColor", "_Color");
    var symbolTint = ColorOf(m, UnityEngine.Color.white, "_Symbol_Color_Tint");
    var clip = (m.HasProperty("_AlphaClip") && m.GetFloat("_AlphaClip") > 0.5f) || m.shader.name.Contains("Fabric");
    var fabric = m.shader.name.Contains("Fabric");
    string file = null;
    if (main)
    {
        const int Size = 512;
        var px = Pixels(main, Size);
        var sym = symbol ? Pixels(symbol, Size) : null;
        for (var i = 0; i < px.Length; i++)
        {
            var c = px[i] * tint; c.a = px[i].a;
            if (sym != null) { var s = sym[i]; var over = new UnityEngine.Color(s.r * symbolTint.r, s.g * symbolTint.g, s.b * symbolTint.b); c = UnityEngine.Color.Lerp(c, new UnityEngine.Color(over.r, over.g, over.b, c.a), s.a); }
            px[i] = c;
        }
        var tex = new UnityEngine.Texture2D(Size, Size, UnityEngine.TextureFormat.RGBA32, false);
        tex.SetPixels(px); tex.Apply();
        var safe = System.Text.RegularExpressions.Regex.Replace(id, "[^A-Za-z0-9_-]", "_");
        file = "textures/" + safe + (clip ? ".png" : ".jpg");
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, file), clip ? tex.EncodeToPNG() : tex.EncodeToJPG(88));
        UnityEngine.Object.DestroyImmediate(tex);
    }
    var color = main ? UnityEngine.Color.white : tint;
    materialJson[id] = "{\"texture\":" + (file != null ? Q(file) : "null") + ",\"color\":[" + F(color.r) + "," + F(color.g) + "," + F(color.b) + "],\"clip\":" + (clip ? "true" : "false") + ",\"doubleSided\":" + (fabric ? "true" : "false") + "}";
    return id;
}

// ---- Parts --------------------------------------------------------------------------------------------------------
bool Shown(UnityEngine.Transform t, UnityEngine.Transform root)
{
    for (var x = t; x; x = x.parent) { if (!x.gameObject.activeSelf) return false; if (x == root) break; }
    return true;
}
string Part(UnityEngine.GameObject prefab)
{
    if (!prefab) return "null";
    var items = new System.Collections.Generic.List<string>();
    var root = prefab.transform;
    var toRoot = root.worldToLocalMatrix;
    foreach (var r in prefab.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
    {
        if (!Shown(r.transform, root) || r.name.Contains("Locator")) continue;
        var mf = r.GetComponent<UnityEngine.MeshFilter>();
        if (!mf || !mf.sharedMesh) continue;
        var m = toRoot * r.transform.localToWorldMatrix;
        var mats = System.Linq.Enumerable.Select(r.sharedMaterials, x => Q(MaterialId(x)));
        var mtx = new string[16];
        for (var c = 0; c < 4; c++) for (var row = 0; row < 4; row++) mtx[c * 4 + row] = F(m[row, c]);
        items.Add("{\"name\":" + Q(r.name) + ",\"mesh\":" + Q(MeshId(mf.sharedMesh)) + ",\"materials\":[" + string.Join(",", mats) + "],\"matrix\":[" + string.Join(",", mtx) + "]}");
    }
    return "[" + string.Join(",", items) + "]";
}
string Parts(UnityEngine.GameObject[] list) => "[" + string.Join(",", System.Linq.Enumerable.Select(list ?? new UnityEngine.GameObject[0], Part)) + "]";
string Ship(BoatParts.Ship s) => "{\"hull\":" + Parts(s.Hulls) + ",\"front\":" + Parts(s.Fronts) + ",\"middle\":" + Parts(s.Middles) + ",\"back\":" + Parts(s.Backs) + ",\"figurehead\":" + Parts(s.Figureheads) + ",\"ram\":" + Parts(s.Rams) + ",\"rig\":" + Part(s.Rig) + "}";
string Mats(UnityEngine.Material[] list) => "[" + string.Join(",", System.Linq.Enumerable.Select(list, m => Q(MaterialId(m)))) + "]";

var small = Ship(parts.Small);
var medium = Ship(parts.Medium);
var large = Ship(parts.Large);
var rowboat = Part(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Sailing/Boats/Rowboat.prefab"));
var fishing = Part(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/World/Sailing/Boats/FishingBoat.prefab"));
var flag = Part(parts.Flag);
var pole = Part(parts.FlagPole);
var sails = Mats(parts.Sails);
var flags = Mats(parts.Flags);
var woods = Mats(parts.Woods);

var json = new System.Text.StringBuilder();
json.Append("{\"version\":" + System.DateTime.UtcNow.ToString("yyyyMMddHHmm", inv) + ",\n");
json.Append("\"meshes\":{" + string.Join(",", System.Linq.Enumerable.Select(meshJson, kv => Q(kv.Key) + ":" + kv.Value)) + "},\n");
json.Append("\"materials\":{" + string.Join(",", System.Linq.Enumerable.Select(materialJson, kv => Q(kv.Key) + ":" + kv.Value)) + "},\n");
json.Append("\"ships\":{\"Sloop\":" + small + ",\n\"Brigantine\":" + medium + ",\n\"Galleon\":" + large + "},\n");
json.Append("\"boats\":{\"Rowboat\":" + rowboat + ",\"Fishing Boat\":" + fishing + "},\n");
json.Append("\"flag\":" + flag + ",\"flagPole\":" + pole + ",\n\"sails\":" + sails + ",\"flags\":" + flags + ",\"woods\":" + woods + "}");
System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "boats.json"), json.ToString());
return "exported " + meshIds.Count + " meshes (" + (totalBytes / 1024) + " KB), " + materialJson.Count + " materials to " + outDir;
