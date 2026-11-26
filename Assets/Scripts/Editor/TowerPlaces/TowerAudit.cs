using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
///     The tower audit (tools/tower/tower-audit.cs, Ravenfall > Tower Places' Check): in an open tower scene, can
///     everything be walked to from where players arrive? Monster spots, the boss spot, every room's arrival and
///     monster spots and its gate's leaves, the town's people, rest spots, strolls, work stands and road points; and
///     where each road ends. Each line says ok or BAD (or OFF: no walking ground near), with the spot's place.
/// </summary>
public static class TowerAudit
{
    public sealed class Line
    {
        public bool Problem;
        public string Text;
        /// <summary>Where the problem is, when it has a place.</summary>
        public Vector3? At;
    }

    public sealed class Result
    {
        public string Header = "";
        public readonly List<Line> Lines = new List<Line>();
        public int Checked;
        public int Problems;

        /// <summary>The report as the eval script wrote it.</summary>
        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Header);
            foreach (var l in Lines) sb.AppendLine(l.Text);
            sb.AppendLine("checked " + Checked + ", problems " + Problems);
            return sb.ToString();
        }
    }

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Runs the audit on a loaded scene (its navmesh must be loaded too: edit mode does that).</summary>
    public static Result Run(Scene scene)
    {
        var result = new Result();
        var place = scene.GetRootGameObjects().Select(g => g.GetComponent<TowerPlace>()).FirstOrDefault(f => f);
        if (!place)
        {
            result.Header = scene.path + " has no TowerFloor or TowerTown at its root";
            result.Problems = 1;
            return result;
        }
        var o = place.transform.position;
        result.Header = scene.path + " root " + place.name + " at " + o.ToString("F0");
        var start = place.Spawn;
        if (!NavMesh.SamplePosition(start, out var sh, 4f, -1))
        {
            result.Lines.Add(new Line { Problem = true, Text = "SPAWN NOT ON NAVMESH " + start, At = start });
            result.Problems++;
        }
        else start = sh.position;

        string Fmt(Vector3 p) { var d = p - o; return "(" + d.x.ToString("F1", Inv) + "," + d.z.ToString("F1", Inv) + ")"; }
        void Add(bool problem, string text, Vector3? at)
        {
            result.Lines.Add(new Line { Problem = problem, Text = text, At = at });
        }
        void Check(string what, Vector3 p, float near = 2f, bool quiet = false)
        {
            result.Checked++;
            if (!NavMesh.SamplePosition(p, out var hit, 6f, -1))
            {
                result.Problems++;
                Add(true, "  OFF " + what + " " + Fmt(p) + " no navmesh within 6 m", p);
                return;
            }
            var off = Vector3.Distance(new Vector3(p.x, hit.position.y, p.z), hit.position);
            var path = new NavMeshPath();
            NavMesh.CalculatePath(start, hit.position, -1, path);
            var room = NavMesh.FindClosestEdge(hit.position, out var edge, -1) ? edge.distance : -1f;
            var problem = path.status != NavMeshPathStatus.PathComplete || off > near;
            if (problem) result.Problems++;
            if (problem || !quiet) Add(problem, (problem ? "  BAD " : "  ok  ") + what + " " + Fmt(p) + " " + path.status + " off " + off.ToString("F1", Inv) + " room " + room.ToString("F1", Inv), p);
        }

        if (place is TowerFloor floor)
        {
            if (floor.Spawns)
            {
                var i = 0;
                foreach (Transform s in floor.Spawns) Check("monster " + (i++), s.position, 2f, true);
                Add(false, " monster spots " + floor.Spawns.childCount, null);
            }
            if (floor.BossPoint) Check("boss", floor.BossPoint.position, 2f);
            if (floor.Route)
            {
                // a floor of rooms: every room's arrival and its own monster spots (the gates are saved open, so the
                // whole route is walked from the first room's arrival)
                var roomSpots = 0;
                foreach (var z in floor.Route.Zones)
                {
                    if (!z) continue;
                    if (z.Entry) Check("room arrival " + z.Title, z.Entry.position, 2f);
                    if (z.Spawns) foreach (Transform s in z.Spawns) { Check("monster in " + z.Title, s.position, 2f, true); roomSpots++; }
                    if (z.Exit && z.Exit.GetComponent<NavMeshObstacle>() && z.Exit.GetComponent<NavMeshObstacle>().enabled)
                    {
                        result.Problems++;
                        Add(true, "  BAD gate saved shut: " + z.Exit.name, z.Exit.transform.position);
                    }
                    // its leaves: shut they lie in the doorway (between their hinge and its middle), open they stand
                    // in the next room (the vault's leaves were the pack's other half on the wrong hinge)
                    if (z.Exit)
                    {
                        var g = z.Exit;
                        for (var i = 0; i < g.Leaves.Length; i++)
                        {
                            var leaf = g.Leaves[i];
                            if (!leaf) continue;
                            result.Checked++;
                            var lr = leaf.GetComponentsInChildren<Renderer>();
                            if (lr.Length == 0)
                            {
                                result.Problems++;
                                Add(true, "  BAD leaf without a mesh: " + g.name, g.transform.position);
                                continue;
                            }
                            var lb = lr[0].bounds; foreach (var r in lr) lb.Encapsulate(r.bounds);
                            var hinge = g.transform.InverseTransformPoint(leaf.position);
                            var centre = g.transform.InverseTransformPoint(lb.center);
                            var yaw = i < g.OpenYaw.Length ? g.OpenYaw[i] : 90f;
                            var open = Quaternion.Euler(0f, yaw, 0f) * (centre - hinge);
                            var shutOk = Mathf.Abs(centre.x) < Mathf.Abs(hinge.x) - 0.2f && Mathf.Abs(centre.z) < 0.3f;
                            var openOk = open.z > 0.3f;
                            if (!shutOk || !openOk)
                            {
                                result.Problems++;
                                Add(true, "  BAD leaf of " + g.name + ": shut at x " + centre.x.ToString("F2") + " (hinge " + hinge.x.ToString("F2") + ")" + (shutOk ? "" : " NOT IN THE DOORWAY") + ", open toward z " + open.z.ToString("F2") + (openOk ? "" : " NOT INTO THE NEXT ROOM"), leaf.position);
                            }
                            else Add(false, "  ok  leaf of " + g.name + " shut x " + centre.x.ToString("F2") + " open z " + open.z.ToString("F2"), leaf.position);
                        }
                    }
                }
                Add(false, " rooms " + floor.Route.Zones.Length + ", their monster spots " + roomSpots, null);
            }
        }
        if (place is TowerTown town)
        {
            foreach (var n in town.GetComponentsInChildren<IslandNpc>(true)) Check("npc " + n.Key, n.transform.position + n.transform.forward * 1.5f, 2f);
            if (town.RestSpots) foreach (Transform s in town.RestSpots) Check("rest " + s.name, s.position, 1.5f, true);
            if (town.Strolls) foreach (Transform s in town.Strolls) Check("stroll", s.position, 1.5f, true);
            var work = town.transform.Find("Work");
            if (work) foreach (var s in work.GetComponentsInChildren<Transform>()) if (s.name.StartsWith("Stand")) Check("work " + s.parent.name + "/" + s.name, s.position, 1.5f, true);
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
                    var w = o + new Vector3(p2.x, 0f, p2.y);
                    if (Physics.Raycast(w + Vector3.up * 60f, Vector3.down, out var rh, 120f, roads.Ground)) w = rh.point;
                    Check("road " + r + "." + k, w, 1.5f, true);
                }
                if (road.Points.Count == 0) continue;
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
                            var a = pts[k]; var b = pts[k + 1]; var ab = b - a;
                            var t = Mathf.Clamp01(Vector2.Dot(end - a, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
                            nearest = Mathf.Min(nearest, (a + ab * t - end).magnitude);
                        }
                    }
                    Add(false, "  road " + r + " type " + road.Type + " end (" + end.x.ToString("F1", Inv) + "," + end.y.ToString("F1", Inv) + ") nearest other road " + nearest.ToString("F1", Inv), o + new Vector3(end.x, 0f, end.y));
                }
            }
        }
        return result;
    }
}
