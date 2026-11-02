// Run with: unity command eval_file --file tools/village-rebuild/decor-pictures.cs   (the main scene open)
//
// The website's pictures of the village decor (its town page, RavenNest's TownService.GetDecorAsync): every piece the
// village decor layer knows (the scene's list, and Resources/Decor's newer pieces) rendered from three quarters above
// on a clear background to RavenNest's wwwroot/images/decor/<key>.png, 256 px. Each is drawn twice, on black and on
// white, and the two give the alpha (a URP preview writes no alpha of its own). Rerun after adding or changing pieces.
// Hundreds of renders in one eval once crashed the editor, so it draws 15 pieces a run: Temp/decor-pictures-from.txt
// holds where the next run starts (deleted after the last); run it until it says it is done.
var outDir = @"G:\Ravenfall\Projects\RavenNest\src\RavenNest.Blazor\wwwroot\images\decor";
System.IO.Directory.CreateDirectory(outDir);
const int Size = 256;
var log = new System.Text.StringBuilder();

var layer = UnityEngine.Object.FindAnyObjectByType<VillageDecor>(UnityEngine.FindObjectsInactive.Include);
if (!layer) return "no VillageDecor in the open scene";
var pieces = new System.Collections.Generic.SortedDictionary<string, UnityEngine.GameObject>(System.StringComparer.Ordinal);
var list = new UnityEditor.SerializedObject(layer).FindProperty("prefabs");
for (var i = 0; i < list.arraySize; i++)
{
    var entry = list.GetArrayElementAtIndex(i);
    var key = entry.FindPropertyRelative("Key").stringValue;
    var prefab = entry.FindPropertyRelative("Prefab").objectReferenceValue as UnityEngine.GameObject;
    if (!string.IsNullOrEmpty(key) && prefab) pieces[key] = prefab;
}
foreach (var prefab in UnityEngine.Resources.LoadAll<UnityEngine.GameObject>("Decor"))
    if (prefab.name.StartsWith("Decor_")) pieces[prefab.name.Substring("Decor_".Length)] = prefab;

UnityEngine.Texture2D Shot(UnityEditor.PreviewRenderUtility pr, UnityEngine.Color background)
{
    pr.camera.backgroundColor = background;
    pr.BeginStaticPreview(new UnityEngine.Rect(0, 0, Size, Size));
    pr.Render(true);
    var tex = pr.EndStaticPreview();
    // read back through a render texture: the preview's texture may not be readable
    var rt = UnityEngine.RenderTexture.GetTemporary(Size, Size, 0, UnityEngine.RenderTextureFormat.ARGB32);
    UnityEngine.Graphics.Blit(tex, rt);
    var prev = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = rt;
    var read = new UnityEngine.Texture2D(Size, Size, UnityEngine.TextureFormat.RGBA32, false);
    read.ReadPixels(new UnityEngine.Rect(0, 0, Size, Size), 0, 0);
    read.Apply();
    UnityEngine.RenderTexture.active = prev;
    UnityEngine.RenderTexture.ReleaseTemporary(rt);
    UnityEngine.Object.DestroyImmediate(tex);
    return read;
}

var fromFile = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Temp/decor-pictures-from.txt");
var from = System.IO.File.Exists(fromFile) ? int.Parse(System.IO.File.ReadAllText(fromFile).Trim()) : 0;
const int PerRun = 15;
var done = 0;
var index = -1;
foreach (var kv in pieces)
{
    index++;
    if (index < from || index >= from + PerRun) continue;
    var pr = new UnityEditor.PreviewRenderUtility();
    try
    {
        var go = pr.InstantiatePrefabInScene(kv.Value);
        // daylight look: no fires (the lit pieces' torches start hidden anyway), no particles
        foreach (var ps in go.GetComponentsInChildren<UnityEngine.ParticleSystem>(true)) ps.gameObject.SetActive(false);
        foreach (var a in go.GetComponentsInChildren<UnityEngine.Animator>(true)) a.enabled = false;
        var b = new UnityEngine.Bounds(); var first = true;
        foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        if (first) { log.AppendLine(kv.Key + ": nothing drawn"); continue; }
        var cam = pr.camera;
        cam.fieldOfView = 26f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1000f;
        cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
        var dir = new UnityEngine.Vector3(0.55f, 0.42f, -1f).normalized;
        var radius = b.extents.magnitude;
        cam.transform.position = b.center + dir * (radius * 1.06f / UnityEngine.Mathf.Sin(13f * UnityEngine.Mathf.Deg2Rad));
        cam.transform.LookAt(b.center);
        pr.lights[0].intensity = 1.25f;
        pr.lights[0].color = new UnityEngine.Color(1f, 0.96f, 0.88f);
        pr.lights[0].transform.rotation = UnityEngine.Quaternion.Euler(42f, -35f, 0f);
        pr.lights[1].intensity = 0.55f;
        pr.lights[1].color = new UnityEngine.Color(0.7f, 0.78f, 1f);
        pr.ambientColor = new UnityEngine.Color(0.42f, 0.42f, 0.47f);

        var black = Shot(pr, UnityEngine.Color.black);
        var white = Shot(pr, UnityEngine.Color.white);
        var outTex = new UnityEngine.Texture2D(Size, Size, UnityEngine.TextureFormat.RGBA32, false);
        var cb = black.GetPixels();
        var cw = white.GetPixels();
        var px = new UnityEngine.Color[cb.Length];
        for (var p = 0; p < px.Length; p++)
        {
            // on black a pixel is c * a; on white c * a + (1 - a)
            var alpha = UnityEngine.Mathf.Clamp01(1f - ((cw[p].r - cb[p].r) + (cw[p].g - cb[p].g) + (cw[p].b - cb[p].b)) / 3f);
            px[p] = alpha > 0.004f ? new UnityEngine.Color(cb[p].r / alpha, cb[p].g / alpha, cb[p].b / alpha, alpha) : new UnityEngine.Color(0f, 0f, 0f, 0f);
        }
        outTex.SetPixels(px);
        outTex.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, kv.Key + ".png"), outTex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(black);
        UnityEngine.Object.DestroyImmediate(white);
        UnityEngine.Object.DestroyImmediate(outTex);
        done++;
    }
    finally
    {
        pr.Cleanup();
    }
}
var next = from + PerRun;
if (next < pieces.Count) System.IO.File.WriteAllText(fromFile, next.ToString(System.Globalization.CultureInfo.InvariantCulture));
else if (System.IO.File.Exists(fromFile)) System.IO.File.Delete(fromFile);
return done + " drawn (" + from + " to " + System.Math.Min(next, pieces.Count) + " of " + pieces.Count + ")" + (next < pieces.Count ? ", run again" : ", done") + "\n" + log;
