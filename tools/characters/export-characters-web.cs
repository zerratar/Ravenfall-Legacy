// Run with: unity command eval_file --file tools/characters/export-characters-web.cs --timeout 300 (edit mode)
//
// The characters for the website's character customizer (RavenNest's wwwroot/js/rf-character-designer.js), from the
// game's Player prefab, into RavenNest's wwwroot/characters/:
// - meshes/c<n>.bin: every part the customizer offers, skinned, in three.js space (z mirrored, the triangles turned):
//   positions as int16 within the mesh's bounds, normals as int8 (and a spare byte), uvs as uint16 within their range,
//   four bone indices (uint8, into the skeleton) and four weights (uint8, summing to 255), the triangles as uint16;
// - characters.json: the meshes, the skeleton (every bone under Root with its rest pose and its bind pose inverse), the
//   parts (each gender's base body, heads, eyebrows and beards; the hairs fit both), the animations (the game's idle,
//   a bored fidget and the cheer, sampled at 30 fps) and the material: the colours the game never changes are baked
//   into textures/base.jpg; the ones a player picks are masks (textures/masks-a.png: skin, stubble, hair;
//   textures/masks-b.png: war paint, scar, the fifth mask), the same lerps as the game's Synty shader graph.
// Writes the outcome to Temp/export-characters-web.txt as well (the eval times out long before it is done).
var outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../../RavenNest/src/RavenNest.Blazor/wwwroot/characters"));
var reportPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Temp/export-characters-web.txt"));
System.IO.File.WriteAllText(reportPath, "running");
var meshDir = System.IO.Path.Combine(outDir, "meshes");
System.IO.Directory.CreateDirectory(System.IO.Path.Combine(outDir, "textures"));
if (System.IO.Directory.Exists(meshDir)) foreach (var old in System.IO.Directory.GetFiles(meshDir, "*.bin")) System.IO.File.Delete(old);
System.IO.Directory.CreateDirectory(meshDir);

var inv = System.Globalization.CultureInfo.InvariantCulture;
string F(float v) => v.ToString("0.#####", inv);
string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
var report = new System.Text.StringBuilder();

var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Player.prefab");
var character = UnityEngine.Object.Instantiate(prefab);
character.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
character.transform.SetPositionAndRotation(UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
character.transform.localScale = UnityEngine.Vector3.one;
try
{
    var app = character.GetComponentInChildren<SyntyPlayerAppearance>(true);
    var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    UnityEngine.GameObject[] List(string field) => (UnityEngine.GameObject[])typeof(SyntyPlayerAppearance).GetField(field, bf).GetValue(app) ?? new UnityEngine.GameObject[0];
    var animator = character.GetComponentInChildren<UnityEngine.Animator>(true);
    var rootBone = animator.transform.Find("Root") ?? character.transform.Find("Root");
    if (!rootBone) throw new System.Exception("no Root bone");
    var space = rootBone.parent; // the bones' rest poses are against this

    // ---- Unity (left-handed) to three.js (right-handed): z mirrored --------------------------------------------------
    UnityEngine.Vector3 V(UnityEngine.Vector3 v) => new UnityEngine.Vector3(v.x, v.y, -v.z);
    UnityEngine.Quaternion R(UnityEngine.Quaternion q) => new UnityEngine.Quaternion(-q.x, -q.y, q.z, q.w);
    string M(UnityEngine.Matrix4x4 m)
    {
        // S m S, column-major for three's Matrix4.fromArray
        float s(int i) => i == 2 ? -1f : 1f;
        var a = new string[16];
        for (var c = 0; c < 4; c++) for (var r = 0; r < 4; r++) a[c * 4 + r] = F(s(r) * m[r, c] * s(c));
        return "[" + string.Join(",", a) + "]";
    }

    // ---- The skeleton ------------------------------------------------------------------------------------------------
    var bones = new System.Collections.Generic.List<UnityEngine.Transform>();
    var boneIndex = new System.Collections.Generic.Dictionary<UnityEngine.Transform, int>();
    void Collect(UnityEngine.Transform t)
    {
        // equipment slots and attachments carry no skin; only real bones (and Root)
        boneIndex[t] = bones.Count;
        bones.Add(t);
        foreach (UnityEngine.Transform c in t) if (!c.GetComponent<UnityEngine.Renderer>()) Collect(c);
    }
    Collect(rootBone);
    var inverses = new UnityEngine.Matrix4x4?[bones.Count];
    var bindMismatches = 0;

    // ---- Meshes -------------------------------------------------------------------------------------------------------
    long totalBytes = 0;
    var meshJson = new System.Collections.Generic.Dictionary<string, string>();
    var meshIds = new System.Collections.Generic.Dictionary<UnityEngine.Mesh, string>();
    string MeshId(UnityEngine.SkinnedMeshRenderer r)
    {
        var mesh = r.sharedMesh;
        if (meshIds.TryGetValue(mesh, out var id)) return id;
        id = "c" + meshIds.Count;
        meshIds[mesh] = id;
        var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv; var weights = mesh.boneWeights; var binds = mesh.bindposes;
        var rb = r.bones;
        var slot = new int[rb.Length];
        for (var i = 0; i < rb.Length; i++)
        {
            if (!rb[i] || !boneIndex.TryGetValue(rb[i], out var b)) throw new System.Exception(r.name + " has a bone outside the skeleton: " + (rb[i] ? rb[i].name : "null"));
            slot[i] = b;
            if (i >= binds.Length) continue;
            if (inverses[b] == null) inverses[b] = binds[i];
            else
            {
                var d = 0f; var e = inverses[b].Value;
                for (var k = 0; k < 16; k++) d = UnityEngine.Mathf.Max(d, UnityEngine.Mathf.Abs(e[k] - binds[i][k]));
                if (d > 1e-3f) bindMismatches++;
            }
        }
        var lo = V(v.Length > 0 ? v[0] : UnityEngine.Vector3.zero); var hi = lo;
        foreach (var p0 in v) { var p = V(p0); lo = UnityEngine.Vector3.Min(lo, p); hi = UnityEngine.Vector3.Max(hi, p); }
        var uvLo = new UnityEngine.Vector2(float.MaxValue, float.MaxValue); var uvHi = new UnityEngine.Vector2(float.MinValue, float.MinValue);
        foreach (var q in uv) { uvLo = UnityEngine.Vector2.Min(uvLo, q); uvHi = UnityEngine.Vector2.Max(uvHi, q); }
        if (uv.Length == 0) { uvLo = UnityEngine.Vector2.zero; uvHi = UnityEngine.Vector2.one; }
        short P(float x, float a, float b) => (short)UnityEngine.Mathf.RoundToInt(b > a ? (x - a) / (b - a) * 65535f - 32768f : 0f);
        ushort U(float x, float a, float b) => (ushort)UnityEngine.Mathf.RoundToInt(b > a ? (x - a) / (b - a) * 65535f : 0f);
        sbyte N(float x) => (sbyte)UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp(x, -1f, 1f) * 127f);
        var ms = new System.IO.MemoryStream();
        var w = new System.IO.BinaryWriter(ms);
        foreach (var p0 in v) { var p = V(p0); w.Write(P(p.x, lo.x, hi.x)); w.Write(P(p.y, lo.y, hi.y)); w.Write(P(p.z, lo.z, hi.z)); }
        while (ms.Position % 4 != 0) w.Write((byte)0);
        var normalsAt = ms.Position;
        for (var i = 0; i < v.Length; i++) { var q = V(i < n.Length ? n[i] : UnityEngine.Vector3.up); w.Write(N(q.x)); w.Write(N(q.y)); w.Write(N(q.z)); w.Write((sbyte)0); }
        var uvAt = ms.Position;
        for (var i = 0; i < v.Length; i++) { var q = i < uv.Length ? uv[i] : UnityEngine.Vector2.zero; w.Write(U(q.x, uvLo.x, uvHi.x)); w.Write(U(q.y, uvLo.y, uvHi.y)); }
        var skinAt = ms.Position;
        for (var i = 0; i < v.Length; i++)
        {
            var bw = i < weights.Length ? weights[i] : new UnityEngine.BoneWeight { boneIndex0 = 0, weight0 = 1f };
            int[] idx = { bw.boneIndex0, bw.boneIndex1, bw.boneIndex2, bw.boneIndex3 };
            float[] wt = { bw.weight0, bw.weight1, bw.weight2, bw.weight3 };
            var sum = wt[0] + wt[1] + wt[2] + wt[3];
            var bytes = new int[4]; var total = 0;
            for (var k = 0; k < 4; k++) { bytes[k] = sum > 0f ? UnityEngine.Mathf.RoundToInt(wt[k] / sum * 255f) : (k == 0 ? 255 : 0); total += bytes[k]; }
            bytes[0] += 255 - total; // the rounding goes to the strongest
            for (var k = 0; k < 4; k++) w.Write((byte)(bytes[k] > 0 && idx[k] < slot.Length ? slot[idx[k]] : 0));
            for (var k = 0; k < 4; k++) w.Write((byte)UnityEngine.Mathf.Clamp(bytes[k], 0, 255));
        }
        var indexAt = ms.Position;
        var subs = new System.Collections.Generic.List<string>();
        var start = 0;
        for (var s = 0; s < mesh.subMeshCount; s++)
        {
            var tris = mesh.GetTriangles(s);
            subs.Add("[" + start + "," + tris.Length + "]");
            start += tris.Length;
            // the mirror turns every triangle inside out: turned back
            for (var t = 0; t + 2 < tris.Length; t += 3) { w.Write((ushort)tris[t]); w.Write((ushort)tris[t + 2]); w.Write((ushort)tris[t + 1]); }
        }
        if (v.Length > 65535) throw new System.Exception(r.name + " has too many vertices for uint16 indices");
        w.Flush();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(meshDir, id + ".bin"), ms.ToArray());
        totalBytes += ms.Length;
        meshJson[id] = "{\"name\":" + Q(mesh.name) + ",\"vertices\":" + v.Length + ",\"min\":[" + F(lo.x) + "," + F(lo.y) + "," + F(lo.z) + "],\"max\":[" + F(hi.x) + "," + F(hi.y) + "," + F(hi.z) + "],\"uvMin\":[" + F(uvLo.x) + "," + F(uvLo.y) + "],\"uvMax\":[" + F(uvHi.x) + "," + F(uvHi.y) + "],\"normals\":" + normalsAt + ",\"uvs\":" + uvAt + ",\"skin\":" + skinAt + ",\"indices\":" + indexAt + ",\"subs\":[" + string.Join(",", subs) + "]}";
        return id;
    }

    // a part: the meshes of its skinned renderers (one for almost every part)
    string Part(UnityEngine.GameObject go)
    {
        if (!go) return "null";
        var ids = new System.Collections.Generic.List<string>();
        foreach (var r in go.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)) if (r.sharedMesh) ids.Add(Q(MeshId(r)));
        return ids.Count == 0 ? "null" : "{\"name\":" + Q(go.name) + ",\"meshes\":[" + string.Join(",", ids) + "]}";
    }
    string Parts(string field) => "[" + string.Join(",", System.Linq.Enumerable.Select(List(field), Part)) + "]";
    string Body(string g)
    {
        // the base body: every body slot's first part (bare skin and plain underclothes)
        string[] slots = { "Torso", "ArmUpperRight", "ArmUpperLeft", "ArmLowerRight", "ArmLowerLeft", "HandsRight", "HandsLeft", "Hips", "LegsRight", "LegsLeft" };
        return "[" + string.Join(",", System.Linq.Enumerable.Select(slots, s => Part(List(g + s)[0]))) + "]";
    }

    var partsJson = "{\"male\":{\"body\":" + Body("male") + ",\"heads\":" + Parts("maleHeads") + ",\"eyebrows\":" + Parts("maleEyebrows") + ",\"beards\":" + Parts("maleFacialHairs") + "},\n"
        + "\"female\":{\"body\":" + Body("female") + ",\"heads\":" + Parts("femaleHeads") + ",\"eyebrows\":" + Parts("femaleEyebrows") + ",\"beards\":" + Parts("femaleFacialHairs") + "},\n"
        + "\"hairs\":" + Parts("hairs") + ",\"ears\":" + Parts("ears") + "}";

    // ---- The skeleton, written (rest poses as in the prefab) -----------------------------------------------------------
    var restRot = new UnityEngine.Quaternion[bones.Count];
    var restPos = new UnityEngine.Vector3[bones.Count];
    var boneJson = new System.Collections.Generic.List<string>();
    for (var i = 0; i < bones.Count; i++)
    {
        var t = bones[i];
        restRot[i] = t.localRotation; restPos[i] = t.localPosition;
        var parent = t.parent && boneIndex.TryGetValue(t.parent, out var pi) ? pi : -1;
        var p = V(t.localPosition); var q = R(t.localRotation); var sc = t.localScale;
        var inverse = inverses[i] ?? (t.worldToLocalMatrix * space.localToWorldMatrix);
        boneJson.Add("{\"name\":" + Q(t.name) + ",\"parent\":" + parent + ",\"p\":[" + F(p.x) + "," + F(p.y) + "," + F(p.z) + "],\"q\":[" + F(q.x) + "," + F(q.y) + "," + F(q.z) + "," + F(q.w) + "],\"s\":[" + F(sc.x) + "," + F(sc.y) + "," + F(sc.z) + "],\"inverse\":" + M(inverse) + "}");
    }

    // ---- Animations: the humanoid clips sampled on this rig ----------------------------------------------------------
    string Sample(string path, bool loop)
    {
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(path);
        if (!clip) foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)) if (o is UnityEngine.AnimationClip c && !c.name.StartsWith("__preview")) { clip = c; break; }
        if (!clip) { report.Append("no clip " + path + "\n"); return "null"; }
        const int Fps = 30;
        var frames = UnityEngine.Mathf.Max(2, UnityEngine.Mathf.RoundToInt(clip.length * Fps) + 1);
        var rots = new UnityEngine.Quaternion[frames, bones.Count];
        var hipsAt = new UnityEngine.Vector3[frames];
        var hips = bones.FindIndex(b => b.name == "Hips");
        UnityEditor.AnimationMode.StartAnimationMode();
        try
        {
            for (var f = 0; f < frames; f++)
            {
                UnityEditor.AnimationMode.BeginSampling();
                UnityEditor.AnimationMode.SampleAnimationClip(animator.gameObject, clip, UnityEngine.Mathf.Min(clip.length, f / (float)Fps));
                UnityEditor.AnimationMode.EndSampling();
                for (var b = 0; b < bones.Count; b++) rots[f, b] = bones[b].localRotation;
                if (hips >= 0) hipsAt[f] = bones[hips].localPosition;
            }
        }
        finally { UnityEditor.AnimationMode.StopAnimationMode(); }
        var tracks = new System.Collections.Generic.List<string>();
        for (var b = 0; b < bones.Count; b++)
        {
            // bones the clip leaves alone keep their rest pose
            var moves = false;
            for (var f = 0; f < frames && !moves; f++) moves = UnityEngine.Quaternion.Angle(rots[f, b], restRot[b]) > 0.05f;
            if (!moves) continue;
            var vals = new System.Collections.Generic.List<string>();
            for (var f = 0; f < frames; f++)
            {
                var q = R(rots[f, b]);
                vals.Add(F(q.x)); vals.Add(F(q.y)); vals.Add(F(q.z)); vals.Add(F(q.w));
            }
            tracks.Add("{\"bone\":" + b + ",\"q\":[" + string.Join(",", vals) + "]}");
        }
        var hipVals = new System.Collections.Generic.List<string>();
        for (var f = 0; f < frames; f++) { var p = V(hipsAt[f]); hipVals.Add(F(p.x)); hipVals.Add(F(p.y)); hipVals.Add(F(p.z)); }
        return "{\"fps\":" + Fps + ",\"frames\":" + frames + ",\"loop\":" + (loop ? "true" : "false") + ",\"hips\":" + hips + ",\"hipsP\":[" + string.Join(",", hipVals) + "],\"tracks\":[" + string.Join(",", tracks) + "]}";
    }
    var idle = Sample("Assets/RPG Character Animation Pack/Animations/Relax/RPG-Character@Idle.FBX", true);
    var bored = Sample("Assets/Animations/Crafting Mecanim Animation Pack/Animations/Crafter@Idle-Bored1.FBX", false);
    var cheer = Sample("Assets/Animations/Crafting Mecanim Animation Pack/Animations/Crafter@Cheer2.FBX", false);
    // back to the rest pose
    for (var i = 0; i < bones.Count; i++) { bones[i].localRotation = restRot[i]; bones[i].localPosition = restPos[i]; }

    // ---- The material: the fixed colours baked, the picked ones as masks ---------------------------------------------
    var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/PolygonFantasyHeroCharacters/Materials/FantasyHero.mat");
    UnityEngine.Texture Tex(string name) => material.GetTexture(name);
    bool IsSrgb(UnityEngine.Texture t)
    {
        var importer = UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(t)) as UnityEditor.TextureImporter;
        return importer == null || importer.sRGBTexture;
    }
    UnityEngine.Color[] Pixels(UnityEngine.Texture t, int size)
    {
        // the stored values, as in the file (an sRGB target undoes the sampler's decode)
        var srgb = IsSrgb(t);
        var rt = UnityEngine.RenderTexture.GetTemporary(size, size, 0, UnityEngine.RenderTextureFormat.ARGB32, srgb ? UnityEngine.RenderTextureReadWrite.sRGB : UnityEngine.RenderTextureReadWrite.Linear);
        UnityEngine.Graphics.Blit(t, rt);
        var prev = UnityEngine.RenderTexture.active;
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(size, size, UnityEngine.TextureFormat.RGBA32, false, !srgb);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, size, size), 0, 0);
        tex.Apply();
        UnityEngine.RenderTexture.active = prev;
        UnityEngine.RenderTexture.ReleaseTemporary(rt);
        var px = tex.GetPixels();
        UnityEngine.Object.DestroyImmediate(tex);
        return px;
    }
    // what the shader sees: an sRGB texture's values decoded to linear
    float Lin(float c) => c <= 0.04045f ? c / 12.92f : UnityEngine.Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
    float Srgb(float c) => c <= 0.0031308f ? c * 12.92f : 1.055f * UnityEngine.Mathf.Pow(c, 1f / 2.4f) - 0.055f;
    UnityEngine.Color Decode(UnityEngine.Color c, bool srgb) => srgb ? new UnityEngine.Color(Lin(c.r), Lin(c.g), Lin(c.b), c.a) : c;

    const int Size = 1024;
    var albedo = Pixels(Tex("_Texture"), Size); var albedoSrgb = IsSrgb(Tex("_Texture"));
    var m1 = Pixels(Tex("_Mask_01"), Size); var s1 = IsSrgb(Tex("_Mask_01"));
    var m2 = Pixels(Tex("_Mask_02"), Size); var s2 = IsSrgb(Tex("_Mask_02"));
    var m3 = Pixels(Tex("_Mask_03"), Size); var s3 = IsSrgb(Tex("_Mask_03"));
    var m4 = Pixels(Tex("_Mask_04"), Size); var s4 = IsSrgb(Tex("_Mask_04"));
    var m5 = Pixels(Tex("Texture2D_291B1623"), Size); var s5 = IsSrgb(Tex("Texture2D_291B1623"));
    UnityEngine.Color Col(string n) => material.GetColor(n).linear;
    var primary = Col("_Color_Primary"); var secondary = Col("_Color_Secondary");
    var leather1 = Col("_Color_Leather_Primary"); var leather2 = Col("_Color_Leather_Secondary");
    var metal1 = Col("_Color_Metal_Primary"); var metal2 = Col("_Color_Metal_Secondary"); var metalDark = Col("_Color_Metal_Dark");
    var basePx = new UnityEngine.Color[albedo.Length];
    var maskA = new UnityEngine.Color[albedo.Length];
    var maskB = new UnityEngine.Color[albedo.Length];
    var fifthUsed = 0;
    for (var i = 0; i < albedo.Length; i++)
    {
        var a = Decode(albedo[i], albedoSrgb);
        var k1 = Decode(m1[i], s1); var k2 = Decode(m2[i], s2); var k3 = Decode(m3[i], s3); var k4 = Decode(m4[i], s4); var k5 = Decode(m5[i], s5);
        var c = a;
        c = UnityEngine.Color.Lerp(c, primary, 1f - k1.r);
        c = UnityEngine.Color.Lerp(c, secondary, 1f - k1.g);
        c = UnityEngine.Color.Lerp(c, leather1, 1f - k4.r);
        c = UnityEngine.Color.Lerp(c, leather2, 1f - k4.g);
        c = UnityEngine.Color.Lerp(c, metal1, 1f - k2.r);
        c = UnityEngine.Color.Lerp(c, metal2, 1f - k2.g);
        c = UnityEngine.Color.Lerp(c, metalDark, 1f - k2.b);
        basePx[i] = new UnityEngine.Color(Srgb(c.r), Srgb(c.g), Srgb(c.b), 1f);
        maskA[i] = new UnityEngine.Color(1f - k3.r, 1f - k3.g, 1f - k4.b, 1f);
        maskB[i] = new UnityEngine.Color(1f - k1.b, 1f - k3.b, 1f - k5.r, 1f);
        if (k5.r < 0.99f) fifthUsed++;
    }
    void Save(UnityEngine.Color[] px, string file, bool jpg)
    {
        var tex = new UnityEngine.Texture2D(Size, Size, UnityEngine.TextureFormat.RGB24, false, true);
        tex.SetPixels(px); tex.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "textures", file), jpg ? tex.EncodeToJPG(92) : tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }
    Save(basePx, "base.jpg", true);
    Save(maskA, "masks-a.png", false);
    Save(maskB, "masks-b.png", false);
    string C(UnityEngine.Color c) => "[" + F(c.r) + "," + F(c.g) + "," + F(c.b) + "]";

    var json = new System.Text.StringBuilder();
    json.Append("{\"version\":" + System.DateTime.UtcNow.ToString("yyyyMMddHHmm", inv) + ",\n");
    json.Append("\"material\":{\"base\":\"textures/base.jpg\",\"masksA\":\"textures/masks-a.png\",\"masksB\":\"textures/masks-b.png\",\"scar\":" + C(Col("_Color_Scar")) + ",\"fifth\":[1,0,0],\"bodyArtAmount\":" + F(material.GetFloat("_BodyArt_Amount")) + ",\"smoothness\":" + F(material.GetFloat("_Smoothness")) + ",\"metallic\":" + F(material.GetFloat("_Metallic")) + "},\n");
    json.Append("\"skeleton\":[" + string.Join(",\n", boneJson) + "],\n");
    json.Append("\"parts\":" + partsJson + ",\n");
    json.Append("\"animations\":{\"idle\":" + idle + ",\n\"bored\":" + bored + ",\n\"cheer\":" + cheer + "},\n");
    json.Append("\"meshes\":{" + string.Join(",\n", System.Linq.Enumerable.Select(meshJson, kv => Q(kv.Key) + ":" + kv.Value)) + "}}");
    System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "characters.json"), json.ToString());
    report.Append("exported " + meshIds.Count + " meshes (" + (totalBytes / 1024) + " KB), " + bones.Count + " bones, bind pose mismatches " + bindMismatches + ", fifth mask pixels " + fifthUsed + ", srgb masks " + s1 + s2 + s3 + s4 + s5 + " albedo " + albedoSrgb + ", body art amount " + F(material.GetFloat("_BodyArt_Amount")) + " to " + outDir);
}
catch (System.Exception e)
{
    report.Append("failed: " + e);
}
finally
{
    UnityEngine.Object.DestroyImmediate(character);
}
System.IO.File.WriteAllText(reportPath, report.ToString());
return report.ToString();
