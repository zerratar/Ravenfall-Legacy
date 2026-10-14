using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws and edits a <see cref="WorldRoad"/> in the Scene view, much as build mode does in the
/// village: Shift click on the ground adds a point at the end, dragging a point moves it along the
/// ground, Ctrl click on the road inserts a point there and Ctrl click on a point removes it.
/// Points snap onto the points of other roads close by, so roads join; hold Shift while dragging to
/// place freely. Everything is undoable.
/// </summary>
[CustomEditor(typeof(WorldRoad))]
public class WorldRoadEditor : Editor
{
    private const float JoinDistance = 1.2f;
    private static readonly Color PointColour = new Color(1f, 0.82f, 0.38f);
    private static readonly Color LineColour = new Color(1f, 0.82f, 0.38f, 0.6f);

    [MenuItem("GameObject/Ravenfall/Road", false, 10)]
    private static void CreateRoad(MenuCommand command)
    {
        var go = new GameObject("Road");
        var view = SceneView.lastActiveSceneView;
        if (view)
        {
            var pivot = view.pivot;
            pivot.y = WorldRoad.GroundHeight(pivot);
            go.transform.position = pivot;
        }

        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        go.AddComponent<WorldRoad>();
        Undo.RegisterCreatedObjectUndo(go, "Create road");
        Selection.activeGameObject = go;
    }

    public override void OnInspectorGUI()
    {
        var road = (WorldRoad)target;
        serializedObject.Update();

        var surface = serializedObject.FindProperty("surface");
        var width = serializedObject.FindProperty("width");
        var before = surface.enumValueIndex;
        EditorGUILayout.PropertyField(surface);
        EditorGUILayout.PropertyField(width);
        // a new surface brings its usual width, as village roads have
        if (surface.enumValueIndex != before) width.floatValue = WorldRoad.DefaultWidth((WorldRoad.Surface)surface.enumValueIndex);

        EditorGUILayout.LabelField("Points", road.Points.Count.ToString());
        EditorGUILayout.LabelField("Length", RoadMesh.Length(road.Samples).ToString("0.0") + " m");
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.HelpBox(
            "Shift click the ground: add a point at the end\n" +
            "Drag a point: move it (it snaps onto other roads' points; hold Shift to place freely)\n" +
            "Ctrl click the road: insert a point\n" +
            "Ctrl click a point: remove it", MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reverse"))
            {
                Undo.RecordObject(road, "Reverse road");
                road.Points.Reverse();
                Changed(road);
            }

            if (GUILayout.Button("Snap to ground"))
            {
                // heights are not used, but keeping them on the ground keeps the gizmos tidy
                Undo.RecordObject(road, "Snap road to ground");
                for (var i = 0; i < road.Points.Count; ++i)
                {
                    var world = road.WorldPoint(i);
                    road.Points[i] = road.transform.InverseTransformPoint(world);
                }
                Changed(road);
            }

            if (GUILayout.Button("Clear"))
            {
                Undo.RecordObject(road, "Clear road");
                road.Points.Clear();
                Changed(road);
            }
        }
    }

    private void OnSceneGUI()
    {
        var road = (WorldRoad)target;
        var e = Event.current;
        var control = GUIUtility.GetControlID(FocusType.Passive);

        // with Shift or Ctrl held, clicks belong to the road, not to the scene selection
        if (e.type == EventType.Layout && (e.shift || e.control)) HandleUtility.AddDefaultControl(control);

        // the shape
        var world = new List<Vector3>(road.Points.Count);
        for (var i = 0; i < road.Points.Count; ++i) world.Add(road.WorldPoint(i) + Vector3.up * 0.15f);
        if (world.Count >= 2)
        {
            Handles.color = LineColour;
            Handles.DrawAAPolyLine(3f, world.ToArray());
        }

        // Ctrl click on a point removes it
        if (e.type == EventType.MouseDown && e.button == 0 && e.control && !e.alt)
        {
            var hit = PointUnderMouse(world, e.mousePosition);
            if (hit >= 0)
            {
                Undo.RecordObject(road, "Remove road point");
                road.Points.RemoveAt(hit);
                Changed(road);
                e.Use();
                return;
            }

            if (TryGround(e.mousePosition, out var onGround) && road.Samples.Count >= 2
                && RoadMesh.DistanceTo(road.Samples, new Vector2(onGround.x, onGround.z), out _) < road.Width)
            {
                Undo.RecordObject(road, "Insert road point");
                road.Points.Insert(InsertIndex(road, onGround), road.transform.InverseTransformPoint(onGround));
                Changed(road);
                e.Use();
                return;
            }
        }

        // Shift click on the ground adds a point at the end
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift && !e.control && !e.alt && PointUnderMouse(world, e.mousePosition) < 0)
        {
            if (TryGround(e.mousePosition, out var onGround))
            {
                onGround = Join(road, onGround);
                Undo.RecordObject(road, "Add road point");
                road.Points.Add(road.transform.InverseTransformPoint(onGround));
                Changed(road);
                e.Use();
                return;
            }
        }

        // drag the points
        Handles.color = PointColour;
        for (var i = 0; i < world.Count; ++i)
        {
            var size = HandleUtility.GetHandleSize(world[i]) * 0.09f;
            EditorGUI.BeginChangeCheck();
            var moved = Handles.FreeMoveHandle(world[i], size, Vector3.zero, Handles.SphereHandleCap);
            if (!EditorGUI.EndChangeCheck()) continue;

            // a free move handle moves in the view's plane; put the point back down on the ground
            // under the mouse, where it is meant to be
            var target = TryGround(e.mousePosition, out var onGround) ? onGround : moved;
            if (!e.shift) target = Join(road, target);
            target.y = WorldRoad.GroundHeight(target);
            Undo.RecordObject(road, "Move road point");
            road.Points[i] = road.transform.InverseTransformPoint(target);
            Changed(road);
        }
    }

    private static void Changed(WorldRoad road)
    {
        road.Rebuild();
        EditorUtility.SetDirty(road);
    }

    private static int PointUnderMouse(List<Vector3> world, Vector2 mouse)
    {
        var best = -1;
        var bestDistance = 12f;
        for (var i = 0; i < world.Count; ++i)
        {
            var d = Vector2.Distance(HandleUtility.WorldToGUIPoint(world[i]), mouse);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }

    /// <summary>Where the mouse points on the ground: the terrain first, else any collider.</summary>
    private static bool TryGround(Vector2 mouse, out Vector3 point)
    {
        var ray = HandleUtility.GUIPointToWorldRay(mouse);
        var best = float.MaxValue;
        point = default;
        foreach (var terrain in Terrain.activeTerrains)
        {
            var collider = terrain ? terrain.GetComponent<TerrainCollider>() : null;
            if (collider && collider.Raycast(ray, out var hit, 10000f) && hit.distance < best)
            {
                best = hit.distance;
                point = hit.point;
            }
        }
        if (best < float.MaxValue) return true;

        if (Physics.Raycast(ray, out var any, 10000f, ~0, QueryTriggerInteraction.Ignore))
        {
            point = any.point;
            return true;
        }
        return false;
    }

    /// <summary>Onto a point of another road, when one is close, so roads meet end to end.</summary>
    private static Vector3 Join(WorldRoad road, Vector3 point)
    {
        var best = JoinDistance;
        var joined = point;
        foreach (var other in Object.FindObjectsByType<WorldRoad>(FindObjectsSortMode.None))
        {
            if (other == road) continue;
            for (var i = 0; i < other.Points.Count; ++i)
            {
                var p = other.WorldPoint(i);
                var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(point.x, point.z));
                if (d < best)
                {
                    best = d;
                    joined = p;
                }
            }
        }
        return joined;
    }

    /// <summary>Where a new point goes: into the stretch between points it lies closest to.</summary>
    private static int InsertIndex(WorldRoad road, Vector3 point)
    {
        var flat = new Vector2(point.x, point.z);
        var best = 1;
        var bestDistance = float.MaxValue;
        for (var i = 1; i < road.Points.Count; ++i)
        {
            var a = road.WorldPoint(i - 1);
            var b = road.WorldPoint(i);
            var d = RoadMesh.DistanceToSegment(flat, new Vector2(a.x, a.z), new Vector2(b.x, b.z));
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }
}
