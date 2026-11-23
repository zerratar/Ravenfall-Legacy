// Run with: unity command eval_file --file tools/dark-fortress-armor/armor-play.cs (PLAY mode, a player joined)
//
// Dresses the first player of the session in item sets through the game's real equip code (PlayerEquipment.EquipAll,
// the mesh combiner included) and captures it in the world with the game's own animations. Visual only: the items are
// made in memory from the server's item rows (nothing is given, nothing is saved), and at the end the player wears
// its own equipment again (PlayerEquipment.UpdateAppearance from its inventory) and gets its own appearance back.
//
// Job file Temp/armor-play.job, one step a line:
//   look <sex> <skin #hex> <item name>; <item name>; ...   dress (male/female), skin color, then these items
//   anim idle|run|combat|attack|chop|mine|fish             the animation the player shows (its own logic is paused)
//   wait <seconds>
//   shot <png path> <yaw> <pitch> <distance> [height]      a picture from a camera of its own (yaw 0 = in front)
// Wait for Temp/armor-play.done (the log).
var project = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, ".."));
var jobPath = System.IO.Path.Combine(project, "Temp", "armor-play.job");
var donePath = System.IO.Path.Combine(project, "Temp", "armor-play.done");
if (!UnityEditor.EditorApplication.isPlaying) return "play mode only";
if (System.IO.File.Exists(donePath)) System.IO.File.Delete(donePath);
var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
var players = gm.Players.GetAllPlayers();
if (players.Count == 0) return "no player";
var p = players[0];
var app = p.Appearance;
var log = new System.Text.StringBuilder();
var steps = new System.Collections.Generic.Queue<string>(System.IO.File.ReadAllLines(jobPath));
var inv = System.Globalization.CultureInfo.InvariantCulture;

// what to give back afterwards
var ownGender = app.Gender; var ownSkin = app.SkinColor; var ownCape = app.CapeVisible;
app.SetCapeVisible(false); // the armor's back, not the clan's cape
var wasEnabled = p.enabled;
p.enabled = false; // its own task logic would take the animation back
var animator = p.Animations;
float until = 0f;
var lastLook = new System.Collections.Generic.List<GameInventoryItem>();

void Look(string spec)
{
    var parts = spec.Split(new[] { ' ' }, 3);
    app.Gender = parts[0] == "female" ? RavenNest.Models.Gender.Female : RavenNest.Models.Gender.Male;
    UnityEngine.ColorUtility.TryParseHtmlString(parts[1], out var skin);
    app.SkinColor = skin;
    var list = new System.Collections.Generic.List<GameInventoryItem>();
    foreach (var raw in parts[2].Split(';'))
    {
        var name = raw.Trim();
        if (name.Length == 0) continue;
        var item = System.Linq.Enumerable.FirstOrDefault(gm.Items.GetItems(), x => x.Name == name);
        if (item == null) { log.AppendLine("no item " + name); continue; }
        list.Add(new GameInventoryItem(p, new RavenNest.Models.InventoryItem { Id = System.Guid.NewGuid(), ItemId = item.Id, Equipped = true, Amount = 1 }, item));
    }
    // the look before goes first, as the inventory takes items off (a head piece that is a model stays otherwise)
    foreach (var old in lastLook) p.Equipment.Unequip(old);
    lastLook = list;
    p.Equipment.EquipAll(list);
    log.AppendLine("look " + spec + " -> " + list.Count + " items");
}

void Anim(string name)
{
    animator.ResetAnimationStates();
    animator.EndCombat(); animator.EndWoodcutting(); animator.EndFishing(); animator.EndMining();
    if (name != "run") animator.StopMoving(true);
    switch (name)
    {
        case "run": animator.StartMoving(true); break;
        case "combat": animator.StartCombat(0, false); break;
        case "attack": animator.StartCombat(0, false); animator.Attack(0, 0, false); break;
        case "chop": animator.StartWoodcutting(); animator.Chop(0); break;
        case "mine": animator.StartMining(); animator.Mine(); break;
        case "fish": animator.StartFishing(); animator.Fish(); break;
        default: animator.StopMoving(true); break;
    }
}

void Shot(string[] a)
{
    var path = a[1];
    float yaw = float.Parse(a[2], inv), pitch = float.Parse(a[3], inv), dist = float.Parse(a[4], inv);
    float height = a.Length > 5 ? float.Parse(a[5], inv) : 1.0f;
    var go = new UnityEngine.GameObject("armor-play-cam");
    var cam = go.AddComponent<UnityEngine.Camera>();
    cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
    // only the player (the world's lighting, without the trees and houses around it), on a plain background
    cam.cullingMask = 1 << p.gameObject.layer;
    cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    cam.backgroundColor = new UnityEngine.Color(0.13f, 0.145f, 0.17f);
    var t = p.transform;
    var target = t.position + UnityEngine.Vector3.up * height;
    var dir = UnityEngine.Quaternion.Euler(pitch, t.eulerAngles.y + 180f + yaw, 0f) * UnityEngine.Vector3.forward; // from the camera to the player
    go.transform.position = target - dir * dist;
    go.transform.rotation = UnityEngine.Quaternion.LookRotation(dir, UnityEngine.Vector3.up);
    // a soft key light from the camera's side for the player only: the island's own light can be night or lava glow
    var lightGo = new UnityEngine.GameObject("armor-play-light");
    var key = lightGo.AddComponent<UnityEngine.Light>();
    key.type = UnityEngine.LightType.Directional; key.intensity = 0.9f; key.cullingMask = cam.cullingMask; key.shadows = UnityEngine.LightShadows.None;
    lightGo.transform.rotation = UnityEngine.Quaternion.LookRotation(UnityEngine.Quaternion.Euler(25f, 30f, 0f) * dir, UnityEngine.Vector3.up);
    var rt = new UnityEngine.RenderTexture(900, 1200, 24, UnityEngine.RenderTextureFormat.ARGB32);
    rt.antiAliasing = 4;
    cam.targetTexture = rt;
    cam.Render();
    var prev = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(900, 1200, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 900, 1200), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
    System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    UnityEngine.Object.Destroy(tex); cam.targetTexture = null; rt.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(go); UnityEngine.Object.Destroy(lightGo);
    log.AppendLine("shot " + path);
}

void Restore()
{
    foreach (var old in lastLook) p.Equipment.Unequip(old);
    app.Gender = ownGender; app.SkinColor = ownSkin; app.SetCapeVisible(ownCape);
    p.Equipment.UpdateAppearance();
    animator.ResetAnimationStates();
    p.enabled = wasEnabled;
}

UnityEditor.EditorApplication.CallbackFunction step = null;
step = () =>
{
    try
    {
        if (!UnityEditor.EditorApplication.isPlaying) { UnityEditor.EditorApplication.update -= step; return; }
        if (UnityEngine.Time.realtimeSinceStartup < until) return;
        while (steps.Count > 0)
        {
            var line = steps.Dequeue().Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var a = line.Split(' ');
            if (a[0] == "look") { Look(line.Substring(5)); until = UnityEngine.Time.realtimeSinceStartup + 0.6f; return; }
            if (a[0] == "anim") { Anim(a[1]); continue; }
            if (a[0] == "wait") { until = UnityEngine.Time.realtimeSinceStartup + float.Parse(a[1], inv); return; }
            if (a[0] == "shot") { Shot(a); return; }
        }
        Restore();
        UnityEditor.EditorApplication.update -= step;
        System.IO.File.WriteAllText(donePath, "done\n" + log);
    }
    catch (System.Exception e)
    {
        UnityEditor.EditorApplication.update -= step;
        try { Restore(); } catch { }
        System.IO.File.WriteAllText(donePath, "failed: " + e + "\n" + log);
    }
};
UnityEditor.EditorApplication.update += step;
return "started " + steps.Count + " steps on " + p.Name;
