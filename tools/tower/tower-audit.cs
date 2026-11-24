// Opens a tower scene (path in %TEMP%/tower-audit-scene.txt) additively in edit mode and checks that everything in it can
// be walked to from where players arrive: monster spots, the boss spot (and its room), people, rest spots, strolls,
// work spots and every road point. Writes %TEMP%/tower-audit.txt; closes the scene after.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
var path = System.IO.File.ReadAllText(System.IO.Path.Combine(tmp, "tower-audit-scene.txt")).Trim();
var inv = System.Globalization.CultureInfo.InvariantCulture;
try
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    var place = scene.GetRootGameObjects().Select(g => g.GetComponent<TowerPlace>()).FirstOrDefault(f => f);
    var o = place.transform.position;
    log.AppendLine(path + " root " + place.name + " at " + o.ToString("F0"));
    var start = place.Spawn;
    if (!UnityEngine.AI.NavMesh.SamplePosition(start, out var sh, 4f, -1)) { log.AppendLine("SPAWN NOT ON NAVMESH " + start); }
    else start = sh.position;
    var bad = 0; var total = 0;
    string Fmt(UnityEngine.Vector3 p) { var d = p - o; return "(" + d.x.ToString("F1", inv) + "," + d.z.ToString("F1", inv) + ")"; }
    void Check(string what, UnityEngine.Vector3 p, float near = 2f, bool quiet = false)
    {
        total++;
        if (!UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 6f, -1)) { bad++; log.AppendLine("  OFF " + what + " " + Fmt(p) + " no navmesh within 6 m"); return; }
        var off = UnityEngine.Vector3.Distance(new UnityEngine.Vector3(p.x, hit.position.y, p.z), hit.position);
        var path2 = new UnityEngine.AI.NavMeshPath();
        UnityEngine.AI.NavMesh.CalculatePath(start, hit.position, -1, path2);
        var room = UnityEngine.AI.NavMesh.FindClosestEdge(hit.position, out var edge, -1) ? edge.distance : -1f;
        var problem = path2.status != UnityEngine.AI.NavMeshPathStatus.PathComplete || off > near;
        if (problem) bad++;
        if (problem || !quiet) log.AppendLine((problem ? "  BAD " : "  ok  ") + what + " " + Fmt(p) + " " + path2.status + " off " + off.ToString("F1", inv) + " room " + room.ToString("F1", inv));
    }
    if (place is TowerFloor floor)
    {
        if (floor.Spawns) { var i = 0; foreach (UnityEngine.Transform s in floor.Spawns) Check("monster " + (i++), s.position, 2f, true); log.AppendLine(" monster spots " + floor.Spawns.childCount); }
        if (floor.BossPoint) Check("boss", floor.BossPoint.position, 2f);
        if (floor.Route)
        {
            // a floor of rooms: every room's arrival and its own monster spots (the gates are saved open, so the whole
            // route is walked from the first room's arrival)
            var roomSpots = 0;
            foreach (var z in floor.Route.Zones)
            {
                if (!z) continue;
                if (z.Entry) Check("room arrival " + z.Title, z.Entry.position, 2f);
                if (z.Spawns) foreach (UnityEngine.Transform s in z.Spawns) { Check("monster in " + z.Title, s.position, 2f, true); roomSpots++; }
                if (z.Exit && z.Exit.GetComponent<UnityEngine.AI.NavMeshObstacle>() && z.Exit.GetComponent<UnityEngine.AI.NavMeshObstacle>().enabled) { bad++; log.AppendLine("  BAD gate saved shut: " + z.Exit.name); }
                // its leaves: shut they lie in the doorway (between their hinge and its middle), open they stand in
                // the next room (the vault's leaves were the pack's other half on the wrong hinge: open while "shut")
                if (z.Exit)
                {
                    var g = z.Exit;
                    for (var i = 0; i < g.Leaves.Length; i++)
                    {
                        var leaf = g.Leaves[i];
                        if (!leaf) continue;
                        total++;
                        var lr = leaf.GetComponentsInChildren<UnityEngine.Renderer>();
                        if (lr.Length == 0) { bad++; log.AppendLine("  BAD leaf without a mesh: " + g.name); continue; }
                        var lb = lr[0].bounds; foreach (var r in lr) lb.Encapsulate(r.bounds);
                        var hinge = g.transform.InverseTransformPoint(leaf.position);
                        var centre = g.transform.InverseTransformPoint(lb.center);
                        var yaw = i < g.OpenYaw.Length ? g.OpenYaw[i] : 90f;
                        var open = UnityEngine.Quaternion.Euler(0f, yaw, 0f) * (centre - hinge);
                        var shutOk = UnityEngine.Mathf.Abs(centre.x) < UnityEngine.Mathf.Abs(hinge.x) - 0.2f && UnityEngine.Mathf.Abs(centre.z) < 0.3f;
                        var openOk = open.z > 0.3f;
                        if (!shutOk || !openOk) { bad++; log.AppendLine("  BAD leaf of " + g.name + ": shut at x " + centre.x.ToString("F2") + " (hinge " + hinge.x.ToString("F2") + ")" + (shutOk ? "" : " NOT IN THE DOORWAY") + ", open toward z " + open.z.ToString("F2") + (openOk ? "" : " NOT INTO THE NEXT ROOM")); }
                        else log.AppendLine("  ok  leaf of " + g.name + " shut x " + centre.x.ToString("F2") + " open z " + open.z.ToString("F2"));
                    }
                }
            }
            log.AppendLine(" rooms " + floor.Route.Zones.Length + ", their monster spots " + roomSpots);
        }
    }
    if (place is TowerTown town)
    {
        foreach (var n in town.GetComponentsInChildren<IslandNpc>(true)) Check("npc " + n.Key, n.transform.position + n.transform.forward * 1.5f, 2f);
        if (town.RestSpots) foreach (UnityEngine.Transform s in town.RestSpots) Check("rest " + s.name, s.position, 1.5f, true);
        if (town.Strolls) foreach (UnityEngine.Transform s in town.Strolls) Check("stroll", s.position, 1.5f, true);
        var work = town.transform.Find("Work");
        if (work) foreach (UnityEngine.Transform s in work.GetComponentsInChildren<UnityEngine.Transform>()) if (s.name.StartsWith("Stand")) Check("work " + s.parent.name + "/" + s.name, s.position, 1.5f, true);
    }
    var roads = place.GetComponentInChildren<SceneRoads>(true);
    if (roads)
    {
        for (var r = 0; r < roads.Roads.Count; r++)
        {
            var road = roads.Roads[r];
            for (var k = 0; k < road.Points.Count; k++)
            {
                var p2 = road.Points[k];
                var w = o + new UnityEngine.Vector3(p2.x, 0f, p2.y);
                if (UnityEngine.Physics.Raycast(w + UnityEngine.Vector3.up * 60f, UnityEngine.Vector3.down, out var rh, 120f, roads.Ground)) w = rh.point;
                Check("road " + r + "." + k, w, 1.5f, true);
            }
            // a dangling end: neither near another road nor at the edge of the place
            foreach (var end in new[] { road.Points[0], road.Points[road.Points.Count - 1] })
            {
                var nearest = float.MaxValue;
                for (var r2 = 0; r2 < roads.Roads.Count; r2++)
                {
                    if (r2 == r) continue;
                    var pts = roads.Roads[r2].Points;
                    for (var k = 0; k + 1 < pts.Count; k++)
                    {
                        var a = pts[k]; var b = pts[k + 1]; var ab = b - a; var t = UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(end - a, ab) / UnityEngine.Mathf.Max(0.001f, ab.sqrMagnitude));
                        nearest = UnityEngine.Mathf.Min(nearest, (a + ab * t - end).magnitude);
                    }
                }
                log.AppendLine("  road " + r + " type " + road.Type + " end (" + end.x.ToString("F1", inv) + "," + end.y.ToString("F1", inv) + ") nearest other road " + nearest.ToString("F1", inv));
            }
        }
    }
    log.AppendLine("checked " + total + ", problems " + bad);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
catch (System.Exception e) { log.AppendLine("ERROR " + e); }
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-audit.txt"), log.ToString());
return null;
