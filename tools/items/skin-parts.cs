// Run with: unity command eval_file --file tools/items/skin-parts.cs (edit mode)
//
// Which character parts show the wearer's skin: fingerless gloves, a harness over a bare chest, sandals, an open
// collar. Writes Assets/Scripts/Game/Player/SyntyPlayerAppearance.SkinParts.cs, the list the game reads to give
// such a part the player's own skin color when an item puts it on (SyntyPlayerAppearance.WithSkin); every other
// armor part keeps its tier's shared material. Run it again when the Player prefab gets new parts.
//
// How: the character shader paints skin where the red channel of _Mask_03 is low (the same mask the website's
// character export reads). For every part of the Player prefab (the appearance's lists, and the belt and back
// parts that are in none) the mesh's triangles are looked up in that mask at their corners and their middle; a part
// shows skin when more than 1% of its surface does. The body's own parts (index 0: all skin) are left out: an item
// never paints those, they keep the body's material.
//
// The work is spread over editor frames; wait for Temp/skin-parts.done (the list, with each part's share of skin).
var project = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, ".."));
var donePath = System.IO.Path.Combine(project, "Temp", "skin-parts.done");
var outPath = System.IO.Path.Combine(project, "Assets", "Scripts", "Game", "Player", "SyntyPlayerAppearance.SkinParts.cs");
const float Share = 0.01f;
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "not in play mode";
if (System.IO.File.Exists(donePath)) System.IO.File.Delete(donePath);

var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Player.prefab");
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Materials/Rune.mat");
if (!prefab || !material) return "no Player prefab or no Assets/Materials/Rune.mat";
var mask = material.GetTexture("_Mask_03");
if (!mask) return "the tier material has no _Mask_03";

// the mask's stored values (an sRGB target undoes the sampler's decode), then as the shader sees them
const int Size = 512;
var importer = UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(mask)) as UnityEditor.TextureImporter;
var srgb = importer == null || importer.sRGBTexture;
var rt = UnityEngine.RenderTexture.GetTemporary(Size, Size, 0, UnityEngine.RenderTextureFormat.ARGB32, srgb ? UnityEngine.RenderTextureReadWrite.sRGB : UnityEngine.RenderTextureReadWrite.Linear);
UnityEngine.Graphics.Blit(mask, rt);
var previous = UnityEngine.RenderTexture.active;
UnityEngine.RenderTexture.active = rt;
var copy = new UnityEngine.Texture2D(Size, Size, UnityEngine.TextureFormat.RGBA32, false, !srgb);
copy.ReadPixels(new UnityEngine.Rect(0, 0, Size, Size), 0, 0);
copy.Apply();
UnityEngine.RenderTexture.active = previous;
UnityEngine.RenderTexture.ReleaseTemporary(rt);
var pixels = copy.GetPixels();
UnityEngine.Object.DestroyImmediate(copy);
float Lin(float c) => c <= 0.04045f ? c / 12.92f : UnityEngine.Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
bool Skin(UnityEngine.Vector2 uv)
{
    var x = UnityEngine.Mathf.Clamp((int)(UnityEngine.Mathf.Repeat(uv.x, 1f) * Size), 0, Size - 1);
    var y = UnityEngine.Mathf.Clamp((int)(UnityEngine.Mathf.Repeat(uv.y, 1f) * Size), 0, Size - 1);
    var r = pixels[y * Size + x].r;
    return 1f - (srgb ? Lin(r) : r) > 0.5f;
}

// the parts: every renderer under the prefab's part groups, once
var parts = new System.Collections.Generic.List<UnityEngine.SkinnedMeshRenderer>();
var seen = new System.Collections.Generic.HashSet<string>();
foreach (var r in prefab.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
{
    if (!r.sharedMesh || !r.name.StartsWith("Chr_") || !seen.Add(r.name)) continue;
    parts.Add(r);
}

var found = new System.Collections.Generic.SortedDictionary<string, float>(System.StringComparer.Ordinal);
var next = 0;
var sw = System.Diagnostics.Stopwatch.StartNew();
UnityEditor.EditorApplication.CallbackFunction step = null;
step = () =>
{
    try
    {
        var started = sw.ElapsedMilliseconds;
        while (next < parts.Count && sw.ElapsedMilliseconds - started < 200)
        {
            var part = parts[next++];
            // the body's own parts: Chr_Torso_Male_00, Chr_HandLeft_Female_00 (helmets and attachments count from 01)
            if (part.name.EndsWith("_00")) continue;
            // forearms 10 and 15 (15 is the plate every woman wears): their 4% of skin is the caps inside the elbow
            // and the wrist, never seen (looked at from every side), and a material of her own for every woman in
            // plate would be a second mesh for nothing
            if (part.name.StartsWith("Chr_ArmLower") && (part.name.EndsWith("_10") || part.name.EndsWith("_15"))) continue;
            var mesh = part.sharedMesh;
            var v = mesh.vertices; var uv = mesh.uv; var t = mesh.triangles;
            if (uv.Length != v.Length) continue;
            float total = 0f, skin = 0f;
            for (var i = 0; i + 2 < t.Length; i += 3)
            {
                int a = t[i], b = t[i + 1], c = t[i + 2];
                var area = UnityEngine.Vector3.Cross(v[b] - v[a], v[c] - v[a]).magnitude;
                total += area;
                var hits = (Skin(uv[a]) ? 1 : 0) + (Skin(uv[b]) ? 1 : 0) + (Skin(uv[c]) ? 1 : 0) + (Skin((uv[a] + uv[b] + uv[c]) / 3f) ? 1 : 0);
                skin += area * hits / 4f;
            }
            if (total > 0f && skin / total > Share) found[part.name] = skin / total;
        }
        if (next < parts.Count) return;
        UnityEditor.EditorApplication.update -= step;

        var nl = "\r\n";
        var code = new System.Text.StringBuilder();
        code.Append("using System.Collections.Generic;" + nl + nl);
        code.Append("// Made by tools/items/skin-parts.cs from the character meshes and the shader's skin mask: do not edit by hand, run" + nl);
        code.Append("// the tool again when the Player prefab gets new parts." + nl);
        code.Append("public partial class SyntyPlayerAppearance" + nl + "{" + nl);
        code.Append("    /// <summary>" + nl);
        code.Append("    /// The parts that show the wearer's skin (fingerless gloves, a harness over a bare chest, sandals), by the name" + nl);
        code.Append("    /// of the part. An item that puts one on paints it in the player's own skin color: see WithSkin." + nl);
        code.Append("    /// </summary>" + nl);
        code.Append("    private static readonly HashSet<string> SkinParts = new HashSet<string>" + nl + "    {" + nl);
        foreach (var kv in found) code.Append("        \"" + kv.Key + "\"," + nl);
        code.Append("    };" + nl + "}" + nl);
        var bytes = new System.Collections.Generic.List<byte>(new byte[] { 0xEF, 0xBB, 0xBF });
        bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(code.ToString()));
        System.IO.File.WriteAllBytes(outPath, bytes.ToArray());

        var log = new System.Text.StringBuilder("ok: " + found.Count + " of " + parts.Count + " parts show skin, in " + sw.ElapsedMilliseconds + " ms\n");
        foreach (var kv in found) log.Append(kv.Key + " " + (kv.Value * 100f).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%\n");
        System.IO.File.WriteAllText(donePath, log.ToString());
    }
    catch (System.Exception e)
    {
        UnityEditor.EditorApplication.update -= step;
        System.IO.File.WriteAllText(donePath, "FAILED: " + e);
    }
};
UnityEditor.EditorApplication.update += step;
return "started on " + parts.Count + " parts; wait for " + donePath;
