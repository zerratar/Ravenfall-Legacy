// Run with: unity command eval_file --file tools/dark-fortress/dark-fortress-icons.cs (edit mode; never in play mode)
//
// The icons of the Dark Fortress items (docs/dark-fortress-design.md): every item in tools/dark-fortress/dark-fortress-icons.txt
// (slug | prefab | framing), rendered from the item's own Resources prefab, 256 px with a transparent background,
// lit and supersampled like the class gear icons (tools/items/class-gear-icons.cs). Weapons lie on the diagonal, blade
// or head up and to the right; shields and head pieces are seen from the front quarter; the hand cannon from its side.
//
// Written to Assets/Resources/Items/<slug>.png (imported as sprites like rune-chest.png) and copied to RavenNest's
// wwwroot/imgs/items/<slug>.png. Assets/Resources is not versioned: the icons live on the build machine and the website.
// Isolated preview scene; one icon per editor frame, then the imports: wait for Temp/dark-fortress-icons.done.
// Temp/dark-fortress-icons.job may hold "out=<folder>" for a trial run (pictures only there) and "only=<part of a slug>".
var project = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, ".."));
var listPath = System.IO.Path.Combine(project, "tools", "dark-fortress", "dark-fortress-icons.txt");
var jobPath = System.IO.Path.Combine(project, "Temp", "dark-fortress-icons.job");
var donePath = System.IO.Path.Combine(project, "Temp", "dark-fortress-icons.done");
var iconFolder = "Assets/Resources/Items";
var websiteFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(project, "..", "RavenNest", "src", "RavenNest.Blazor", "wwwroot", "imgs", "items"));
var likeIcon = iconFolder + "/rune-chest.png";
const int Size = 256;
const int Super = 4;
const float Fill = 0.86f;
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "not in play mode";
if (System.IO.File.Exists(donePath)) System.IO.File.Delete(donePath);
string trialOut = null, only = null;
if (System.IO.File.Exists(jobPath))
{
    foreach (var raw in System.IO.File.ReadAllLines(jobPath))
    {
        var line = raw.Trim();
        if (line.StartsWith("out=")) trialOut = line.Substring(4).Trim();
        if (line.StartsWith("only=")) only = line.Substring(5).Trim();
    }
}
if (trialOut == null && !System.IO.Directory.Exists(websiteFolder)) return "no website folder: " + websiteFolder;
var icons = new System.Collections.Generic.List<string[]>();
foreach (var raw in System.IO.File.ReadAllLines(listPath))
{
    var line = raw.Trim();
    if (line.Length == 0 || line[0] == '#') continue;
    var c = line.Split('|');
    if (only != null && !c[0].Contains(only)) continue;
    icons.Add(c);
}
var sceneWasDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
var log = new System.Text.StringBuilder();
var written = new System.Collections.Generic.List<string>();
var pru = new UnityEditor.PreviewRenderUtility();
var sw = System.Diagnostics.Stopwatch.StartNew();
var next = 0;
var importNext = 0;
UnityEngine.Color Hex(string h) { UnityEngine.ColorUtility.TryParseHtmlString(h, out var c); return c; }

void RenderIcon(string[] icon)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(icon[1]);
    if (!prefab) { log.Append("NO PREFAB " + icon[1] + "\n"); return; }
    var holder = new UnityEngine.GameObject("icon");
    holder.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
    pru.AddSingleGO(holder);
    try
    {
        // the model as the prefab holds it, in its own space (the bone-space transform of the root left out)
        var model = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(prefab, holder.transform);
        model.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
        model.transform.localPosition = UnityEngine.Vector3.zero;
        model.transform.localRotation = UnityEngine.Quaternion.identity;
        model.transform.localScale = UnityEngine.Vector3.one;
        var kind = icon[2];
        // the model's own frame: y along the weapon (Synty weapons stand on their grip), x across its face
        if (kind == "weapon") holder.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, -45f);
        else if (kind == "bow") holder.transform.rotation = UnityEngine.Quaternion.Euler(0f, 90f, 0f);
        float yaw = kind == "head" ? 32f : kind == "shield" ? 20f : kind == "bow" ? 15f : 0f;
        float pitch = kind == "head" ? 8f : kind == "bow" ? 10f : 4f;
        var rotation = UnityEngine.Quaternion.Euler(pitch, 180f + yaw, 0f);
        var right = rotation * UnityEngine.Vector3.right; var up = rotation * UnityEngine.Vector3.up; var forward = rotation * UnityEngine.Vector3.forward;

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        var drawn = 0;
        foreach (var mf in holder.GetComponentsInChildren<UnityEngine.MeshFilter>())
        {
            if (!mf.sharedMesh) continue;
            drawn++;
            var m = mf.transform.localToWorldMatrix;
            foreach (var v in mf.sharedMesh.vertices)
            {
                var p = m.MultiplyPoint3x4(v);
                var x = UnityEngine.Vector3.Dot(p, right); var y = UnityEngine.Vector3.Dot(p, up); var z = UnityEngine.Vector3.Dot(p, forward);
                if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; if (z < minZ) minZ = z; if (z > maxZ) maxZ = z;
            }
        }
        if (drawn == 0) { log.Append("NO MESH " + icon[0] + "\n"); return; }
        var center = right * ((minX + maxX) * 0.5f) + up * ((minY + maxY) * 0.5f) + forward * ((minZ + maxZ) * 0.5f);
        var extent = UnityEngine.Mathf.Max(maxX - minX, maxY - minY);

        var cam = pru.camera;
        cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
        cam.orthographic = true;
        cam.orthographicSize = extent * 0.5f / Fill;
        cam.nearClipPlane = 0.01f; cam.farClipPlane = 40f;
        cam.transform.rotation = rotation;
        cam.transform.position = center - forward * 10f;
        var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (data) { data.renderPostProcessing = false; data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None; }
        var lightBase = UnityEngine.Quaternion.Euler(pitch, 180f + yaw, 0f);
        pru.lights[0].intensity = 1.3f; pru.lights[0].color = Hex("#FFF6EA");
        pru.lights[0].transform.rotation = lightBase * UnityEngine.Quaternion.Euler(34f, 28f, 0f);
        pru.lights[0].shadows = UnityEngine.LightShadows.None;
        pru.lights[1].intensity = 0.6f; pru.lights[1].color = Hex("#D6E2FF");
        pru.lights[1].transform.rotation = lightBase * UnityEngine.Quaternion.Euler(6f, -50f, 0f);
        // the fortress is dark iron: a little more ambient than the class gear's so the pieces read on dark panels
        pru.ambientColor = Hex("#7A808C");

        var big = Size * Super;
        UnityEngine.Color32[] Shot(UnityEngine.Color background)
        {
            cam.backgroundColor = background;
            pru.BeginStaticPreview(new UnityEngine.Rect(0, 0, big, big));
            pru.Render(true, false);
            var tex = pru.EndStaticPreview();
            var px = tex.GetPixels32();
            UnityEngine.Object.DestroyImmediate(tex);
            return px;
        }
        var onBlack = Shot(UnityEngine.Color.black);
        var onWhite = Shot(UnityEngine.Color.white);
        var small = new UnityEngine.Color32[Size * Size];
        var covered = 0;
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                int r = 0, g = 0, b = 0, a = 0;
                for (var dy = 0; dy < Super; dy++)
                {
                    for (var dx = 0; dx < Super; dx++)
                    {
                        var i = (y * Super + dy) * big + x * Super + dx;
                        var k = onBlack[i]; var w = onWhite[i];
                        var alpha = 255 - ((w.r - k.r) + (w.g - k.g) + (w.b - k.b)) / 3;
                        a += alpha < 0 ? 0 : alpha > 255 ? 255 : alpha;
                        r += k.r; g += k.g; b += k.b;
                    }
                }
                if (a == 0) { small[y * Size + x] = new UnityEngine.Color32(0, 0, 0, 0); continue; }
                small[y * Size + x] = new UnityEngine.Color32((byte)System.Math.Min(255, r * 255 / a), (byte)System.Math.Min(255, g * 255 / a), (byte)System.Math.Min(255, b * 255 / a), (byte)(a / (Super * Super)));
                covered++;
            }
        }
        if (covered < Size * Size / 50) log.Append("NEARLY EMPTY " + icon[0] + "\n");
        var result = new UnityEngine.Texture2D(Size, Size, UnityEngine.TextureFormat.RGBA32, false);
        result.SetPixels32(small);
        var png = UnityEngine.ImageConversion.EncodeToPNG(result);
        UnityEngine.Object.DestroyImmediate(result);
        if (trialOut != null)
        {
            System.IO.Directory.CreateDirectory(trialOut);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(trialOut, icon[0] + ".png"), png);
        }
        else
        {
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(project, iconFolder, icon[0] + ".png"), png);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(websiteFolder, icon[0] + ".png"), png);
            written.Add(iconFolder + "/" + icon[0] + ".png");
        }
    }
    finally
    {
        UnityEngine.Object.DestroyImmediate(holder);
    }
}

bool ImportSome()
{
    var like = UnityEditor.AssetImporter.GetAtPath(likeIcon) as UnityEditor.TextureImporter;
    var settings = new UnityEditor.TextureImporterSettings();
    if (like) like.ReadTextureSettings(settings);
    var started = sw.ElapsedMilliseconds;
    while (importNext < written.Count && sw.ElapsedMilliseconds - started < 400)
    {
        var path = written[importNext++];
        UnityEditor.AssetDatabase.ImportAsset(path);
        var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (!importer) { log.Append("NOT IMPORTED " + path + "\n"); continue; }
        if (like)
        {
            importer.SetTextureSettings(settings);
            importer.SetPlatformTextureSettings(like.GetDefaultPlatformTextureSettings());
        }
        importer.textureType = UnityEditor.TextureImporterType.Sprite;
        importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
    return importNext < written.Count;
}

void Finish(string outcome)
{
    pru.Cleanup();
    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    if (scene.isDirty != sceneWasDirty) log.Append("THE OPEN SCENE'S UNSAVED STATE CHANGED: was " + sceneWasDirty + ", is " + scene.isDirty + "\n");
    System.IO.File.WriteAllText(donePath, outcome + "\n" + log);
}

UnityEditor.EditorApplication.CallbackFunction step = null;
step = () =>
{
    try
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("play mode started");
        if (next < icons.Count) { RenderIcon(icons[next++]); return; }
        if (ImportSome()) return;
        UnityEditor.EditorApplication.update -= step;
        Finish("ok: " + icons.Count + " icons in " + sw.ElapsedMilliseconds + " ms" + (trialOut != null ? " (trial run to " + trialOut + ")" : ", " + written.Count + " imported as sprites and copied to " + websiteFolder));
    }
    catch (System.Exception e)
    {
        UnityEditor.EditorApplication.update -= step;
        Finish("FAILED at icon " + next + " (" + (next > 0 && next <= icons.Count ? icons[next - 1][0] : "-") + "): " + e);
    }
};
UnityEditor.EditorApplication.update += step;
return "started " + icons.Count + " icons; wait for " + donePath;
