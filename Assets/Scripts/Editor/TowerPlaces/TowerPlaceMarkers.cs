using System;
using System.Collections.Generic;
using System.Linq;
using RavenNest.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///     The Scene view markers of Ravenfall > Tower Places while the window is open: where players arrive, the monster
///     spots, the boss, the gates, the town's people, work stands, rest spots and strolls of every tower place that is
///     open. Each kind can be switched off in the window. Big things (arrival, boss, gates, people) always have their
///     label; the many small ones only when the camera is near, and a label that would lie over another is left out.
///     Clicking a label selects its object.
/// </summary>
internal static class TowerPlaceMarkers
{
    internal enum Kind { Arrival, Monster, Boss, Gate, Person, Work, Rest, Stroll }

    internal static readonly string[] KindLabels = { "Arrival", "Monster spots", "Boss", "Gates", "Town people", "Work stands", "Rest spots", "Strolls" };

    internal static readonly Color[] KindColors =
    {
        new Color(0.30f, 0.95f, 0.40f), // arrival
        new Color(1.00f, 0.32f, 0.28f), // monster
        new Color(1.00f, 0.55f, 0.10f), // boss
        new Color(1.00f, 0.86f, 0.20f), // gate
        new Color(0.40f, 0.75f, 1.00f), // person
        new Color(0.25f, 0.95f, 0.90f), // work
        new Color(0.78f, 0.52f, 1.00f), // rest
        new Color(0.85f, 0.92f, 0.55f), // stroll
    };

    private static readonly float[] KindRadius = { 1.6f, 1.2f, 2.6f, 1.8f, 1.2f, 0.7f, 0.6f, 0.6f };
    private static readonly bool[] KindMajor = { true, false, true, true, true, false, false, false };

    internal struct Marker
    {
        public Kind Kind;
        public Vector3 Position;
        public Vector3 Forward;
        public string Label;
        public Transform Target;
    }

    private const string PrefPrefix = "Ravenfall.TowerPlaces.Marker.";
    internal const string PrefAll = "Ravenfall.TowerPlaces.Markers";

    /// <summary>Small labels show within this distance of the camera.</summary>
    private const float NearLabels = 70f;

    internal static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefAll, true);
        set { EditorPrefs.SetBool(PrefAll, value); SceneView.RepaintAll(); }
    }

    internal static bool Shown(Kind k) => EditorPrefs.GetBool(PrefPrefix + k, true);

    internal static void Show(Kind k, bool on)
    {
        EditorPrefs.SetBool(PrefPrefix + k, on);
        SceneView.RepaintAll();
    }

    private static readonly List<Marker> markers = new List<Marker>();
    private static bool stale = true;
    private static int users;

    internal static void Attach()
    {
        if (users++ > 0) return;
        SceneView.duringSceneGui += Draw;
        EditorApplication.hierarchyChanged += MarkStale;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += OnSceneClosed;
        stale = true;
        SceneView.RepaintAll();
    }

    internal static void Detach()
    {
        if (--users > 0) return;
        users = 0;
        SceneView.duringSceneGui -= Draw;
        EditorApplication.hierarchyChanged -= MarkStale;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= OnSceneClosed;
        SceneView.RepaintAll();
    }

    private static void OnSceneOpened(Scene s, UnityEditor.SceneManagement.OpenSceneMode m) => MarkStale();
    private static void OnSceneClosed(Scene s) => MarkStale();

    internal static void MarkStale()
    {
        stale = true;
        SceneView.RepaintAll();
    }

    /// <summary>The tower places in the open scenes.</summary>
    internal static IEnumerable<TowerPlace> OpenPlaces()
    {
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            foreach (var root in s.GetRootGameObjects())
            {
                var p = root.GetComponent<TowerPlace>();
                if (p) yield return p;
            }
        }
    }

    internal static List<Marker> Collect(TowerPlace place)
    {
        var list = new List<Marker>();
        void Add(Kind k, Transform t, string label, Vector3? at = null)
        {
            if (!t && at == null) return;
            list.Add(new Marker { Kind = k, Position = at ?? t.position, Forward = t ? t.forward : Vector3.forward, Label = label, Target = t });
        }
        if (place.SpawnPoint) Add(Kind.Arrival, place.SpawnPoint, "Arrival");
        else Add(Kind.Arrival, place.transform, "Arrival (the root: no spawn point)");

        if (place is TowerFloor floor)
        {
            var n = 0;
            if (floor.Spawns) foreach (Transform s in floor.Spawns) Add(Kind.Monster, s, "Monster " + (++n));
            if (floor.BossPoint) Add(Kind.Boss, floor.BossPoint, "Boss");
            if (floor.Route && floor.Route.Zones != null)
            {
                for (var z = 0; z < floor.Route.Zones.Length; z++)
                {
                    var zone = floor.Route.Zones[z];
                    if (!zone) continue;
                    var title = string.IsNullOrEmpty(zone.Title) ? zone.name : zone.Title;
                    if (zone.Entry) Add(Kind.Arrival, zone.Entry, "Room " + (z + 1) + " arrival: " + title);
                    var m = 0;
                    if (zone.Spawns) foreach (Transform s in zone.Spawns) Add(Kind.Monster, s, title + " monster " + (++m));
                }
            }
            foreach (var gate in floor.GetComponentsInChildren<TowerGate>(true)) Add(Kind.Gate, gate.transform, "Gate: " + gate.name);
        }
        if (place is TowerTown town)
        {
            var definition = Tower.Town(town.Key);
            foreach (var npc in town.GetComponentsInChildren<IslandNpc>(true))
            {
                var name = definition?.People.FirstOrDefault(p => p.Key == npc.Key)?.Name;
                Add(Kind.Person, npc.transform, string.IsNullOrEmpty(name) ? npc.name : name);
            }
            if (town.Work)
                foreach (Transform work in town.Work)
                    foreach (Transform stand in work)
                        if (stand.name.StartsWith("Stand")) Add(Kind.Work, stand, work.name + " stand");
            if (town.RestSpots) foreach (Transform s in town.RestSpots) Add(Kind.Rest, s, "Rest: " + s.name);
            if (town.Strolls) foreach (Transform s in town.Strolls) Add(Kind.Stroll, s, "Stroll");
            foreach (var gate in town.GetComponentsInChildren<TowerGate>(true)) Add(Kind.Gate, gate.transform, "Gate: " + gate.name);
        }
        return list;
    }

    private static void Refresh()
    {
        stale = false;
        markers.Clear();
        foreach (var place in OpenPlaces()) markers.AddRange(Collect(place));
    }

    private static GUIStyle labelStyle;
    private static Texture2D labelBack;

    private static GUIStyle LabelStyle
    {
        get
        {
            if (labelStyle != null && labelBack) return labelStyle;
            labelBack = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            labelBack.SetPixel(0, 0, new Color(0.06f, 0.06f, 0.07f, 0.88f));
            labelBack.Apply();
            labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 3, 3),
                normal = { background = labelBack, textColor = Color.white },
            };
            return labelStyle;
        }
    }

    private static void Draw(SceneView view)
    {
        if (!Enabled) return;
        if (stale) Refresh();
        if (markers.Count == 0) return;
        var cam = view.camera;
        if (!cam) return;
        var camPos = cam.transform.position;

        var oldZ = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        foreach (var m in markers)
        {
            if (!Shown(m.Kind)) continue;
            var c = KindColors[(int)m.Kind];
            var r = KindRadius[(int)m.Kind];
            var p = m.Target ? m.Target.position : m.Position;
            Handles.color = new Color(c.r, c.g, c.b, 0.28f);
            Handles.DrawSolidDisc(p + Vector3.up * 0.05f, Vector3.up, r);
            Handles.color = c;
            Handles.DrawWireDisc(p + Vector3.up * 0.05f, Vector3.up, r, 2f);
            Handles.DrawLine(p, p + Vector3.up * (KindMajor[(int)m.Kind] ? 3f : 1.6f), 2f);
            if (m.Kind != Kind.Monster && m.Kind != Kind.Stroll && m.Target)
            {
                var f = m.Target.forward; f.y = 0f;
                if (f.sqrMagnitude > 0.01f) Handles.DrawLine(p + Vector3.up * 0.1f, p + Vector3.up * 0.1f + f.normalized * (r + 1.2f), 3f);
            }
        }
        Handles.zTest = oldZ;

        // labels: big things first, then the nearest; one that would lie over another is left out
        var placed = new List<Rect>();
        var style = LabelStyle;
        var order = markers.Where(m => Shown(m.Kind))
            .Select(m => (m, p: m.Target ? m.Target.position : m.Position))
            .Select(x => (x.m, x.p, d: Vector3.Distance(camPos, x.p)))
            .Where(x => KindMajor[(int)x.m.Kind] || x.d < NearLabels)
            .OrderByDescending(x => KindMajor[(int)x.m.Kind]).ThenBy(x => x.d).ToList();
        Handles.BeginGUI();
        var e = Event.current;
        foreach (var (m, p, d) in order)
        {
            var top = p + Vector3.up * (KindMajor[(int)m.Kind] ? 3.4f : 2f);
            var vp = cam.WorldToViewportPoint(top);
            if (vp.z <= 0f || vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f) continue;
            var gp = HandleUtility.WorldToGUIPoint(top);
            var content = new GUIContent(m.Label);
            var size = style.CalcSize(content);
            var rect = new Rect(gp.x - size.x / 2f, gp.y - size.y, size.x, size.y);
            var tries = 0;
            while (placed.Any(o => o.Overlaps(rect)) && KindMajor[(int)m.Kind] && tries++ < 3) rect.y -= size.y + 1f;
            if (placed.Any(o => o.Overlaps(rect))) continue;
            placed.Add(rect);
            var c = KindColors[(int)m.Kind];
            var bar = new Rect(rect.x, rect.y, 4f, rect.height);
            GUI.Label(rect, content, style);
            EditorGUI.DrawRect(bar, c);
            if (m.Target && e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                Selection.activeTransform = m.Target;
                EditorGUIUtility.PingObject(m.Target);
                e.Use();
            }
        }
        Handles.EndGUI();
    }
}
