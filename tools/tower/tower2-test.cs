// The second band in play: step from %TEMP%/tower2-step.txt
//   spawn N         : N bots
//   join            : every bot !tower (the stream's floor)
//   town [key]      : every bot !tower town (key: mossgate, rimeholt)
//   load N          : the loaded floor goes and floor N loads (training on it), bots go along
//   state           : what is loaded, who is where, the floor's enemies
//   cam name dist h : a shot from behind the first bot in the tower into Assets/Temp/<name>.png
var tmp = System.IO.Path.GetTempPath();
var parts = System.IO.File.ReadAllText(System.IO.Path.Combine(tmp, "tower2-step.txt")).Trim().Split(' ');
var outPath = System.IO.Path.Combine(tmp, "tower2-test.txt");
var gm = GameManager.Instance;
var tower = TowerManager.Instance;
var bots = gm.Players.GetAllBots().ToList();
var sb = new System.Text.StringBuilder();
string State()
{
    var s = new System.Text.StringBuilder();
    s.AppendLine("stream floor " + tower.StreamFloor + " current " + tower.CurrentFloor + " phase " + tower.Phase + " scene " + tower.LoadedScene + " town " + tower.TownScene + " open town " + tower.CurrentTown?.Name);
    s.AppendLine("bots " + bots.Count + ": in tower " + bots.Count(b => b.towerHandler != null && b.towerHandler.InTower && !b.towerHandler.InTown) + ", in town " + bots.Count(b => b.towerHandler != null && b.towerHandler.InTown));
    var floor = TowerFloor.Loaded;
    if (floor) s.AppendLine("floor " + floor.Definition?.Number + " " + floor.Definition?.Name + " enemies " + string.Join(", ", floor.Enemies.Where(e => e).GroupBy(e => e.Name).Select(g => g.Key + " x" + g.Count() + " lvl " + g.First().Stats.CombatLevel + " scale " + g.First().transform.localScale.x.ToString("0.00") + " el " + g.First().CombatDefinition?.Element)));
    var town = TowerTown.Loaded;
    if (town) s.AppendLine("town " + town.Key + " people " + string.Join(", ", town.People.Select(p => p.Key + (p.gameObject.activeInHierarchy ? "" : "(hidden)"))) + " rest spots " + (town.RestSpots ? town.RestSpots.childCount : 0));
    return s.ToString();
}
if (parts[0] == "spawn")
{
    var n = int.Parse(parts[1]);
    async System.Threading.Tasks.Task Spawn() { for (var i = 0; i < n; ++i) await gm.SpawnBotPlayer(); System.IO.File.WriteAllText(outPath, "bots " + gm.Players.GetAllBots().Count); }
    System.IO.File.Delete(outPath);
    _ = Spawn();
    return "spawning";
}
if (parts[0] == "join") { foreach (var b in bots) sb.Append(b.Name + ":" + tower.Join(b) + " "); sb.AppendLine(); }
if (parts[0] == "town")
{
    var wanted = parts.Length > 1 ? RavenNest.Combat.Tower.Town(parts[1]) : null;
    foreach (var b in bots) sb.Append(tower.JoinTown(b, wanted) + " ");
    sb.AppendLine();
}
if (parts[0] == "load")
{
    var n = int.Parse(parts[1]);
    var inTower = bots.Where(b => b.towerHandler != null && b.towerHandler.InTower && !b.towerHandler.InTown).ToList();
    foreach (var b in inTower) tower.Leave(b);
    tower.Unload();
    sb.AppendLine("unloading, then load " + n + " (run 'load2 " + n + "')");
}
if (parts[0] == "load2")
{
    tower.LoadFloor(int.Parse(parts[1]));
    sb.AppendLine("loading " + parts[1]);
}
if (parts[0] == "enter") { foreach (var b in bots) sb.Append(tower.Enter(b) + " "); sb.AppendLine(); }
if (parts[0] == "cam")
{
    var who = bots.FirstOrDefault(b => b.towerHandler != null && b.towerHandler.InTower) ?? bots.First();
    var dist = parts.Length > 2 ? float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) : 9f;
    var h = parts.Length > 3 ? float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture) : 6f;
    var go = UnityEngine.GameObject.Find("T2Cam"); if (!go) go = new UnityEngine.GameObject("T2Cam");
    var cam = go.GetComponent<UnityEngine.Camera>(); if (!cam) cam = go.AddComponent<UnityEngine.Camera>();
    cam.enabled = false; cam.fieldOfView = 50; cam.farClipPlane = 1500f;
    var main = UnityEngine.Camera.main;
    if (main) { cam.cullingMask = main.cullingMask; cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor; }
    var place = (TowerPlace)TowerTown.Loaded;
    if (who.towerHandler != null && !who.towerHandler.InTown && TowerFloor.Loaded) place = TowerFloor.Loaded;
    var centre = place ? place.Centre : who.Position;
    var dir = (centre - who.Position); dir.y = 0; if (dir.sqrMagnitude < 1) dir = UnityEngine.Vector3.forward; dir.Normalize();
    go.transform.position = who.Position - dir * dist + UnityEngine.Vector3.up * h;
    go.transform.LookAt(who.Position + dir * 6f + UnityEngine.Vector3.up * 1f);
    var rt = new UnityEngine.RenderTexture(1280, 720, 24);
    cam.targetTexture = rt; cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = null; cam.targetTexture = null; rt.Release();
    System.IO.Directory.CreateDirectory("Assets/Temp");
    System.IO.File.WriteAllBytes("Assets/Temp/" + parts[1] + ".png", tex.EncodeToPNG());
    sb.AppendLine("saved " + parts[1] + " from " + who.Name + " at " + who.Position.ToString("F0"));
}
sb.Append(State());
return sb.ToString();
