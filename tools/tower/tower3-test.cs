// Town work, votes and the way after a clear, in play: step from %TEMP%/tower3-step.txt
//   spawn N          : N bots
//   skills           : every bot gets a skill, round the list (woodcutting, mining, ... ranged, magic, healing)
//   town [key]       : every bot !tower town
//   join             : every bot !tower
//   work             : who is in town, their skill, their work place and whether they are at it
//   vote KIND [n]    : n bots (default all) vote: climb, close, boss, floor:N; reports each result
//   force KIND       : the streamer's choice (climb, close, floor:N)
//   afterclear       : the way after a clear, from the loaded floor to the next (no server call)
//   shot KIND name   : a render of the town's work place of that kind into Assets/Temp/<name>.png
//   state
var tmp = System.IO.Path.GetTempPath();
var parts = System.IO.File.ReadAllText(System.IO.Path.Combine(tmp, "tower3-step.txt")).Trim().Split(' ');
var gm = GameManager.Instance;
var tower = TowerManager.Instance;
var bots = gm.Players.GetAllBots().ToList();
var sb = new System.Text.StringBuilder();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
AdminControlData.NoChatBotMessages = true;
string State()
{
    var s = new System.Text.StringBuilder();
    s.AppendLine("stream floor " + tower.StreamFloor + " current " + tower.CurrentFloor + " phase " + tower.Phase + " scene " + tower.LoadedScene + " town " + tower.TownScene + " open town " + tower.CurrentTown?.Name);
    s.AppendLine("bots " + bots.Count + ": on floor " + bots.Count(b => b.towerHandler != null && b.towerHandler.InTower && !b.towerHandler.InTown) + ", in town " + bots.Count(b => b.towerHandler != null && b.towerHandler.InTown) + ", outside " + bots.Count(b => b.towerHandler == null || !b.towerHandler.InTower));
    return s.ToString();
}
var skillTasks = new[] { ("woodcutting", ""), ("mining", ""), ("fishing", ""), ("farming", ""), ("cooking", ""), ("crafting", ""), ("alchemy", ""), ("gathering", ""), ("fighting", "attack"), ("fighting", "strength"), ("fighting", "ranged"), ("fighting", "magic"), ("fighting", "healing"), ("fighting", "defense") };
if (parts[0] == "spawn")
{
    var n = int.Parse(parts[1]);
    async System.Threading.Tasks.Task Spawn() { for (var i = 0; i < n; ++i) await gm.SpawnBotPlayer(); }
    _ = Spawn();
    return "spawning";
}
if (parts[0] == "skills")
{
    for (var i = 0; i < bots.Count; i++) { var (t, a) = skillTasks[i % skillTasks.Length]; bots[i].SetTask(t, a, true); sb.Append(bots[i].Name + "=" + bots[i].ActiveSkill + " "); }
    sb.AppendLine();
}
if (parts[0] == "town")
{
    var wanted = parts.Length > 1 ? RavenNest.Combat.Tower.Town(parts[1]) : null;
    foreach (var b in bots) sb.Append(tower.JoinTown(b, wanted) + " ");
    sb.AppendLine();
}
if (parts[0] == "join") { foreach (var b in bots) sb.Append(tower.Join(b) + " "); sb.AppendLine(); }
if (parts[0] == "work")
{
    var working = typeof(TowerHandler).GetField("working", bf);
    foreach (var b in bots.Where(b => b.towerHandler != null && b.towerHandler.InTown))
    {
        var spot = b.towerHandler.WorkSpot;
        var d = spot ? UnityEngine.Vector3.Distance(b.Position, spot.position) : -1f;
        sb.AppendLine(b.Name + " " + b.ActiveSkill + " -> " + (spot ? spot.parent.name + "/" + spot.name : "strolling") + " at " + d.ToString("0.0") + " m, working " + working.GetValue(b.towerHandler) + ", anim " + b.Animations.State + ", pos " + b.Position.ToString("F0") + ", trains away " + b.towerHandler.TrainsAway);
    }
}
if (parts[0] == "vote" || parts[0] == "force")
{
    var kind = parts[1];
    var force = parts[0] == "force";
    var n = parts.Length > 2 ? int.Parse(parts[2]) : bots.Count;
    foreach (var b in bots.Take(force ? 1 : n))
    {
        int needed;
        string r;
        if (kind == "climb") r = tower.Climb(b, force, out needed).ToString();
        else if (kind == "close") r = tower.Close(b, force, out needed).ToString();
        else if (kind == "boss") r = tower.CallBoss(b, force, out needed).ToString();
        else r = tower.RequestMove(b, int.Parse(kind.Split(':')[1]), force, out needed).ToString();
        sb.Append(r + "(" + needed + ") ");
        if (r != "Voted") break;
    }
    sb.AppendLine();
    sb.AppendLine("votes needed for " + tower.AllPlayers().Count + " in the tower: " + TowerManager.VotesNeeded(tower.AllPlayers().Count));
}
if (parts[0] == "afterclear")
{
    var def = tower.Definition;
    var next = RavenNest.Combat.Tower.Get(tower.CurrentFloor + 1);
    var town = RavenNest.Combat.Tower.TownFor(next.Number);
    var routine = (System.Collections.IEnumerator)typeof(TowerManager).GetMethod("AfterClearRoutine", bf).Invoke(tower, new object[] { def, next, town });
    tower.StartCoroutine(routine);
    sb.AppendLine("after clear of " + def.Number + " to " + next.Number + " (" + next.Layout + ", was " + def.Layout + "), town " + town.Name);
}
if (parts[0] == "shot")
{
    var town = TowerTown.Loaded;
    var place = town.Work.Cast<UnityEngine.Transform>().FirstOrDefault(t => t.name == parts[1]);
    var stand = place.GetChild(0);
    var go = UnityEngine.GameObject.Find("T3Cam"); if (!go) go = new UnityEngine.GameObject("T3Cam");
    var cam = go.GetComponent<UnityEngine.Camera>(); if (!cam) cam = go.AddComponent<UnityEngine.Camera>();
    cam.enabled = false; cam.fieldOfView = 45; cam.farClipPlane = 600f;
    var main = UnityEngine.Camera.main;
    if (main) { cam.cullingMask = main.cullingMask; cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor; }
    var c = UnityEngine.Vector3.zero; foreach (UnityEngine.Transform s in place) c += s.position; c /= place.childCount;
    var fwd = stand.forward; fwd.y = 0; fwd.Normalize();
    var off = parts.Length > 3 ? new UnityEngine.Vector3(float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture), float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture), float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture)) : new UnityEngine.Vector3(3f, 5f, -7f);
    go.transform.position = c + off;
    go.transform.LookAt(c + UnityEngine.Vector3.up * 0.8f);
    var rt = new UnityEngine.RenderTexture(1280, 720, 24);
    cam.targetTexture = rt; cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = null; cam.targetTexture = null; rt.Release();
    System.IO.Directory.CreateDirectory("Assets/Temp");
    System.IO.File.WriteAllBytes("Assets/Temp/" + parts[2] + ".png", tex.EncodeToJPG(88));
    sb.AppendLine("saved " + parts[2]);
}
sb.Append(State());
return sb.ToString();
