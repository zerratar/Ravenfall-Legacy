// Run with: unity command eval_file --file tools/ravebridge/lumbridge-courtyard.cs (after lumbridge-castle.cs)
//
// The castle courtyard's paths in the Ravenfall Lumbridge (Assets/Scenes/LumbridgeRavenfall.unity), with the road
// tool: cobbles from the road outside, through the gate, up to the foot of the keep's stairs, and narrower ones off it
// to the two fountains. Every point comes from where the castle's own pieces stand (it was brought over
// from Ravebridge, scaled), so the paths follow it wherever it is. Rerunnable: they live under "Courtyard Paths".
var sb = new System.Text.StringBuilder();
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/LumbridgeRavenfall.unity") return "open LumbridgeRavenfall first";
var castle = UnityEngine.GameObject.Find("Lumbridge (Ravenfall)/Ravebridge Castle");
if (!castle) return "no castle: run lumbridge-castle.cs first";
UnityEngine.Transform Find(string name)
{
    foreach (var t in castle.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == name) return t;
    return null;
}
UnityEngine.Bounds BoundsOf(UnityEngine.Transform t)
{
    var b = new UnityEngine.Bounds(t.position, UnityEngine.Vector3.zero); var first = true;
    foreach (var r in t.GetComponentsInChildren<UnityEngine.Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
    return b;
}
var leftTower = Find("Left Tower Front"); var rightTower = Find("Right Tower Front");
var stairs = Find("SM_Bld_Stairs_Straight_Wide_Edges_01");
if (!leftTower || !rightTower || !stairs) return "castle pieces not found";
var gate = (BoundsOf(leftTower).center + BoundsOf(rightTower).center) * 0.5f;
var sb2 = BoundsOf(stairs);
// the stairs climb west to the keep: their foot is on their east side
var foot = new UnityEngine.Vector3(sb2.max.x + 0.6f, 0, sb2.center.z);
var outside = gate + new UnityEngine.Vector3(7f, 0, 0);
var inside = gate + new UnityEngine.Vector3(-5f, 0, 0);
var middle = new UnityEngine.Vector3((inside.x + foot.x) * 0.5f, 0, (inside.z + foot.z) * 0.5f);

var root = UnityEngine.GameObject.Find("Lumbridge (Ravenfall)").transform;
var old = root.Find("Courtyard Paths"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
var group = new UnityEngine.GameObject("Courtyard Paths").transform;
group.SetParent(root, false);
void Road(string name, UnityEngine.Vector3[] line, float width, float startRound, float endRound)
{
    var go = new UnityEngine.GameObject(name);
    go.transform.SetParent(group, false);
    go.transform.position = new UnityEngine.Vector3(line[0].x, 0f, line[0].z);
    var road = go.AddComponent<WorldRoad>();
    road.RoadSurface = WorldRoad.Surface.Cobbles;
    road.Width = width;
    foreach (var p in line) road.AddPoint(go.transform.InverseTransformPoint(new UnityEngine.Vector3(p.x, 0f, p.z)));
    road.StartRound = startRound;
    road.EndRound = endRound;
    road.Rebuild();
    sb.Append(name + " " + line.Length + " points; ");
}
// the town's roads to the gate: short stubs that ended at the generated gate (the map's gate stood a few metres off
// this castle's) are switched off, and the way in starts where the nearest road still ends or passes
var town = root.Find("Roads");
var start = outside;
if (town)
{
    foreach (var r in town.GetComponentsInChildren<WorldRoad>())
    {
        if (r.Points.Count > 3) continue;
        foreach (var lp in new[] { r.Points[0], r.Points[r.Points.Count - 1] })
        {
            var w = r.transform.TransformPoint(lp); w.y = 0;
            if (UnityEngine.Vector3.Distance(w, new UnityEngine.Vector3(gate.x, 0, gate.z)) < 12f) { r.gameObject.SetActive(false); sb.Append("stub " + r.name + " off; "); break; }
        }
    }
    var best = 30f;
    foreach (var r in town.GetComponentsInChildren<WorldRoad>())
        for (var i = 0; i < r.Points.Count; i++)
        {
            var w = r.transform.TransformPoint(r.Points[i]); w.y = 0;
            // outside the wall, on the gate's side
            if (w.x < gate.x + 2f) continue;
            var d = UnityEngine.Vector3.Distance(w, new UnityEngine.Vector3(gate.x, 0, gate.z));
            if (d < best) { best = d; start = w; }
        }
}
var approach = new UnityEngine.Vector3(gate.x + 6f, 0, UnityEngine.Mathf.Lerp(start.z, gate.z, 0.75f));
// the main way: square at the stairs' foot, so it meets the bottom step
Road("Gate to keep", new[] { start, approach, gate, inside, middle, foot }, 4f, 1f, 0f);
// branches from the main way to the fountains and the well, round where they meet them
// (not the well: at this scale it stands outside the curtain wall)
foreach (var name in new[] { "SM_Env_Fountain_01 (7)", "SM_Env_Fountain_01 (1)" })
{
    var t = Find(name);
    if (!t) { sb.Append("no " + name + "; "); continue; }
    var b = BoundsOf(t);
    var to = new UnityEngine.Vector3(b.center.x, 0, b.center.z);
    // from the main way's nearest point, curving out and stopping at the basin's edge
    var from = new UnityEngine.Vector3(UnityEngine.Mathf.Clamp(to.x, foot.x, inside.x), 0, UnityEngine.Mathf.Lerp(inside.z, foot.z, UnityEngine.Mathf.InverseLerp(inside.x, foot.x, UnityEngine.Mathf.Clamp(to.x, foot.x, inside.x))));
    var dir = (to - from); var len = dir.magnitude; dir /= UnityEngine.Mathf.Max(0.01f, len);
    var edge = to - dir * (UnityEngine.Mathf.Max(b.extents.x, b.extents.z) + 0.8f);
    var bend = UnityEngine.Vector3.Lerp(from, edge, 0.5f) + new UnityEngine.Vector3(dir.z, 0, -dir.x) * 1.2f;
    Road("Path to " + name.Replace("SM_Env_", "").Replace("SM_Prop_", "").Replace("_01", ""), new[] { from, bend, edge }, 2.6f, 1f, 1f);
}
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return sb.ToString() + " gate " + gate.ToString("F0") + " stairs " + foot.ToString("F0");
