// Run with: unity command eval_file --file tools/items/class-gear-icons.cs (edit mode; never in play mode)
//
// The icons of the class gear (v0.9.7): one 256 px picture with a transparent background for every piece of every
// tier of every line (tools/items/class-gear-parts.txt: 576 items in twelve lines), rendered from the item's real parts in its tier
// material. The parts come from SyntyPlayerAppearance.Get, the game's own model of an item on its own (what a
// treasure chest shows), so a robe has no pauldrons here either; the material comes from ItemManager.GetMaterial.
// Framed like the icons of the metal tiers: a chest is the torso with its sleeves and shoulders in the T pose, gloves
// are one hand with its forearm, leggings the hips, boots the pair, a head piece seen from the front quarter.
// A piece is drawn with what its tier wears (plumes, wings, pouches, pauldrons), except a chest's back piece.
//
// Written to Assets/Resources/Items/<slug>.png (imported as sprites with the settings of rune-chest.png) and copied
// to RavenNest's wwwroot/imgs/items/<slug>.png; slug = the item's name ("<Tier> <piece name>") in lower case,
// apostrophes removed, spaces as hyphens. Assets/Resources is not versioned: the icons live on the build machine
// and on the website.
//
// Everything is in an isolated preview scene and destroyed afterwards. The work is spread over editor frames (one
// icon per frame, then the imports), so the eval returns at once: wait for Temp/class-gear-icons.done (the log).
//
// For a trial run, Temp/class-gear-icons.job may hold "out=<folder>" (the pictures go there only: no import, no
// copy) and "only=<part of a slug>".
var project = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, ".."));
var partsPath = System.IO.Path.Combine(project, "tools", "items", "class-gear-parts.txt");
var jobPath = System.IO.Path.Combine(project, "Temp", "class-gear-icons.job");
var donePath = System.IO.Path.Combine(project, "Temp", "class-gear-icons.done");
var iconFolder = "Assets/Resources/Items";
var websiteFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(project, "..", "RavenNest", "src", "RavenNest.Blazor", "wwwroot", "imgs", "items"));
var likeIcon = iconFolder + "/rune-chest.png";
const int Size = 256;      // the icon
const int Super = 4;       // rendered this many times as large and averaged down
const float Fill = 0.86f;  // how much of the picture the item takes
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

// ---- the items ------------------------------------------------------------------------------------------------------
// in the order of their materials in ItemMaterial (eight a line, from Apprentice on)
var lines = new[] { "Mage", "Healer", "Ranger", "Melee", "Tank", "Rogue", "Barbarian", "Samurai", "Paladin", "DarkKnight", "WitchHunter", "Monk" };
var icons = new System.Collections.Generic.List<string[]>(); // slug, type, material, male ids, mask
// a row is for the tiers its last column names ("3-5", "8"), or for all of them without one
bool RowIsFor(string[] row, int tier)
{
    if (row.Length < 7 || row[6].Length == 0) return true;
    var range = row[6].Split('-');
    var from = int.Parse(range[0]);
    var to = range.Length > 1 ? int.Parse(range[1]) : from;
    return tier >= from && tier <= to;
}
for (var li = 0; li < lines.Length; li++)
{
    for (var tier = 1; tier <= 8; tier++)
    {
        var material = (RavenNest.Models.ItemMaterial)((int)RavenNest.Models.ItemMaterial.Apprentice + li * 8 + tier - 1);
        foreach (var raw in System.IO.File.ReadAllLines(partsPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var c = line.Split('|');
            if (c.Length != 6 && c.Length != 7) return "bad row in class-gear-parts.txt: " + line;
            for (var i = 0; i < c.Length; i++) c[i] = c[i].Trim();
            if (!string.Equals(c[0], lines[li], System.StringComparison.OrdinalIgnoreCase) || !RowIsFor(c, tier)) continue;
            var name = material + " " + c[2];
            var slug = name.ToLowerInvariant().Replace("'", "").Replace(" ", "-");
            if (only != null && !slug.Contains(only)) continue;
            icons.Add(new[] { slug, c[1], material.ToString(), c[3], c[5] });
        }
    }
}
if (icons.Count == 0) return "no items";

// ---- the run --------------------------------------------------------------------------------------------------------
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Player.prefab");
if (!prefab) return "no Assets/Prefabs/Player.prefab";
var getModel = typeof(SyntyPlayerAppearance).GetMethod("Get", bf, null, new[] { typeof(RavenNest.Models.ItemType), typeof(RavenNest.Models.ItemMaterial), typeof(string), typeof(RavenNest.Models.HeadMask) }, null);
if (getModel == null) return "SyntyPlayerAppearance.Get(type, material, model ids, mask) not found";
var log = new System.Text.StringBuilder();
var temp = new System.Collections.Generic.List<UnityEngine.Object>();
var pru = new UnityEditor.PreviewRenderUtility();
SyntyPlayerAppearance source = null;
var next = 0;
var importNext = 0;
var written = new System.Collections.Generic.List<string>();
var sw = System.Diagnostics.Stopwatch.StartNew();
var sceneWasDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
UnityEngine.Color Hex(string h) { UnityEngine.ColorUtility.TryParseHtmlString(h, out var c); return c; }

void Setup()
{
    // the item manager that hands out the materials: the metal tiers' list as on the scene's, so the class gear's
    // come from Resources through the real GetMaterial
    var go = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags("Class Gear Icon Items", UnityEngine.HideFlags.HideAndDontSave);
    temp.Add(go);
    pru.AddSingleGO(go);
    var itemManager = go.AddComponent<ItemManager>();
    var names = System.Enum.GetNames(typeof(RavenNest.Models.ItemMaterial));
    var metal = new UnityEngine.Material[(int)RavenNest.Models.ItemMaterial.Apprentice - 1];
    for (var i = 0; i < metal.Length; i++)
    {
        var name = names[i + 1];
        if (name == "Abraxas") name = "Ultima";
        if (name == "ElderAdamantite") name = "ElderAdamantitite";
        metal[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Materials/" + name + ".mat");
    }
    typeof(ItemManager).GetField("itemMaterials", bf).SetValue(itemManager, metal);

    // the character the parts are copied from: in its own pose (the T pose), nothing of it drawn
    var character = pru.InstantiatePrefabInScene(prefab);
    temp.Add(character);
    character.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
    character.transform.SetPositionAndRotation(UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
    character.transform.localScale = UnityEngine.Vector3.one;
    foreach (var r in character.GetComponentsInChildren<UnityEngine.Renderer>(true)) r.enabled = false;
    foreach (var c in character.GetComponentsInChildren<UnityEngine.Light>(true)) c.enabled = false;
    foreach (var c in character.GetComponentsInChildren<UnityEngine.Canvas>(true)) c.enabled = false;
    foreach (var c in character.GetComponentsInChildren<UnityEngine.Camera>(true)) c.enabled = false;
    source = character.GetComponentInChildren<SyntyPlayerAppearance>(true);
    typeof(SyntyPlayerAppearance).GetField("itemManager", bf).SetValue(source, itemManager);
    source.gameManager = null;
}

void RenderIcon(string[] icon)
{
    var type = (RavenNest.Models.ItemType)System.Enum.Parse(typeof(RavenNest.Models.ItemType), icon[1]);
    var material = (RavenNest.Models.ItemMaterial)System.Enum.Parse(typeof(RavenNest.Models.ItemMaterial), icon[2]);
    var mask = RavenNest.Models.HeadMask.None;
    foreach (var flag in icon[4].Split(',')) mask |= (RavenNest.Models.HeadMask)System.Enum.Parse(typeof(RavenNest.Models.HeadMask), flag.Trim());

    var model = (UnityEngine.GameObject)getModel.Invoke(source, new object[] { type, material, icon[3], mask });
    if (!model) { log.Append("NO MODEL " + icon[0] + "\n"); return; }
    model.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
    pru.AddSingleGO(model);
    try
    {
        // what is drawn: every part of the item (the plume on a helm, the pouches on a belt); of gloves, the left hand
        // and forearm only, like the other glove icons; of a chest, not what it wears on the back (a cloak or a pack
        // is behind the torso from here, and a cloak is so long that the chest would be a third of its picture)
        var drawn = new System.Collections.Generic.List<UnityEngine.SkinnedMeshRenderer>();
        foreach (var r in model.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
        {
            var show = (type != RavenNest.Models.ItemType.Gloves || r.name.Contains("Left")) && !r.name.StartsWith("Chr_BackAttachment_");
            r.enabled = show;
            r.updateWhenOffscreen = true;
            if (show) drawn.Add(r);
        }
        if (drawn.Count == 0) { log.Append("NO PARTS " + icon[0] + "\n"); return; }
        var wrongMaterial = drawn.Find(r => !r.sharedMaterial || r.sharedMaterial.name != icon[2]);
        if (wrongMaterial) log.Append("WRONG MATERIAL " + icon[0] + ": " + (wrongMaterial.sharedMaterial ? wrongMaterial.sharedMaterial.name : "none") + "\n");

        // the view: the character faces +z, so the camera stands on that side, turned and raised a little
        float yaw, pitch, roll = 0f;
        switch (type)
        {
            case RavenNest.Models.ItemType.Chest: yaw = 12f; pitch = 8f; break;
            case RavenNest.Models.ItemType.Gloves: yaw = 8f; pitch = 62f; roll = -68f; break; // seen from above (the back of the hand), the arm of the T pose stood up
            case RavenNest.Models.ItemType.Leggings: yaw = 16f; pitch = 8f; break;
            case RavenNest.Models.ItemType.Boots: yaw = 24f; pitch = 14f; break;
            default: yaw = 32f; pitch = 8f; break; // head pieces
        }
        var rotation = UnityEngine.Quaternion.Euler(pitch, 180f + yaw, 0f) * UnityEngine.Quaternion.Euler(0f, 0f, roll);
        var right = rotation * UnityEngine.Vector3.right; var up = rotation * UnityEngine.Vector3.up; var forward = rotation * UnityEngine.Vector3.forward;

        // framed on the vertices as they are skinned now
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        var baked = new UnityEngine.Mesh();
        var vertices = new System.Collections.Generic.List<UnityEngine.Vector3>();
        foreach (var r in drawn)
        {
            r.BakeMesh(baked, true);
            baked.GetVertices(vertices);
            var toWorld = UnityEngine.Matrix4x4.TRS(r.transform.position, r.transform.rotation, UnityEngine.Vector3.one);
            foreach (var v in vertices)
            {
                var p = toWorld.MultiplyPoint3x4(v);
                var x = UnityEngine.Vector3.Dot(p, right); var y = UnityEngine.Vector3.Dot(p, up); var z = UnityEngine.Vector3.Dot(p, forward);
                if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; if (z < minZ) minZ = z; if (z > maxZ) maxZ = z;
            }
        }
        UnityEngine.Object.DestroyImmediate(baked);
        var center = right * ((minX + maxX) * 0.5f) + up * ((minY + maxY) * 0.5f) + forward * ((minZ + maxZ) * 0.5f);
        var extent = UnityEngine.Mathf.Max(maxX - minX, maxY - minY);

        var cam = pru.camera;
        cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
        cam.orthographic = true;
        cam.orthographicSize = extent * 0.5f / Fill;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
        cam.transform.rotation = rotation;
        cam.transform.position = center - forward * 10f;
        var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (data) { data.renderPostProcessing = false; data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None; }
        // lit from the world's upper left front whatever the camera's roll, with a soft fill
        var lightBase = UnityEngine.Quaternion.Euler(pitch, 180f + yaw, 0f);
        pru.lights[0].intensity = 1.3f; pru.lights[0].color = Hex("#FFF6EA");
        pru.lights[0].transform.rotation = lightBase * UnityEngine.Quaternion.Euler(34f, 28f, 0f);
        pru.lights[0].shadows = UnityEngine.LightShadows.None;
        pru.lights[1].intensity = 0.6f; pru.lights[1].color = Hex("#D6E2FF");
        pru.lights[1].transform.rotation = lightBase * UnityEngine.Quaternion.Euler(6f, -50f, 0f);
        pru.ambientColor = Hex("#666B76");

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
        // no alpha comes out of a preview: the same picture on black and on white gives it
        var onBlack = Shot(UnityEngine.Color.black);
        var onWhite = Shot(UnityEngine.Color.white);
        var small = new UnityEngine.Color32[Size * Size];
        var covered = 0;
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                int r = 0, g = 0, b = 0, a = 0; // colors on black are already weighted by their alpha
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
                // un-premultiplied: the sum of the colors over the sum of the alphas
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
        UnityEngine.Object.DestroyImmediate(model);
    }
}

// imported as sprites, with the settings of an icon that is there already
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
    foreach (var o in temp) if (o) UnityEngine.Object.DestroyImmediate(o);
    temp.Clear();
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
        if (source == null) { Setup(); return; }
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
