using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws and edits a <see cref="WorldFence"/> in the Scene view: click a point to select it and move it with its
/// gizmo (or type its position in the inspector), drag any point along the ground, Shift click the ground to add a
/// point at the end, Ctrl click a point to remove it. The fence is laid on the ground again when a move ends (and with
/// the Rebuild button, after the ground under it changed); while dragging, its line is drawn on the ground. Undoable.
/// </summary>
/// <remarks>
/// A fence that blocks walking needs the island's navmesh baked again after it moves (the Bake button on the island's
/// NavMeshSurface).
/// </remarks>
[CustomEditor(typeof(WorldFence))]
public class WorldFenceEditor : Editor
{
    private static readonly Color LineColour = new Color(0.95f, 0.75f, 0.35f, 0.9f);
    private static readonly Color PointColour = new Color(0.95f, 0.75f, 0.35f, 1f);
    private static readonly Color SelectedColour = new Color(0.3f, 0.85f, 1f, 1f);

    // the point being edited, and whether a move is waiting for the fence to be laid again
    private int selected = -1;
    private bool pending;

    [MenuItem("GameObject/Ravenfall/Fence", false, 11)]
    private static void CreateFence(MenuCommand command)
    {
        var go = new GameObject("Fence");
        var view = SceneView.lastActiveSceneView;
        if (view)
        {
            var pivot = view.pivot;
            pivot.y = WorldRoad.GroundHeight(pivot);
            go.transform.position = pivot;
        }
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        var fence = go.AddComponent<WorldFence>();
        fence.Piece = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Fence_03.prefab");
        Undo.RegisterCreatedObjectUndo(go, "Create fence");
        Selection.activeGameObject = go;
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += AfterUndo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= AfterUndo;
        // a move still waiting when the fence is deselected
        if (pending && target) ((WorldFence)target).Rebuild();
        pending = false;
    }

    // undo brings the points back, not the bent mesh: it is laid again
    private void AfterUndo()
    {
        if (target) ((WorldFence)target).Rebuild();
    }

    public override void OnInspectorGUI()
    {
        var fence = (WorldFence)target;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) fence.Rebuild();

        if (selected >= fence.Points.Count) selected = -1;
        if (selected >= 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Point " + selected, EditorStyles.boldLabel);
            var world = fence.transform.TransformPoint(fence.Points[selected]);
            EditorGUI.BeginChangeCheck();
            var x = EditorGUILayout.FloatField("X", world.x);
            var z = EditorGUILayout.FloatField("Z", world.z);
            if (EditorGUI.EndChangeCheck()) MovePoint(fence, selected, new Vector3(x, world.y, z), true);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Insert after"))
                {
                    var next = selected + 1 < fence.Points.Count ? fence.transform.TransformPoint(fence.Points[selected + 1]) : world + (world - fence.transform.TransformPoint(fence.Points[Mathf.Max(0, selected - 1)])).normalized * 2f;
                    Undo.RecordObject(fence, "Insert fence point");
                    fence.Points.Insert(selected + 1, fence.transform.InverseTransformPoint((world + next) * 0.5f));
                    selected++;
                    fence.Rebuild();
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Remove") && fence.Points.Count > 2)
                {
                    Undo.RecordObject(fence, "Remove fence point");
                    fence.Points.RemoveAt(selected);
                    selected = Mathf.Min(selected, fence.Points.Count - 1);
                    fence.Rebuild();
                    SceneView.RepaintAll();
                }
            }
        }

        EditorGUILayout.HelpBox("Click a point: select it, then move it with its gizmo or type its position above\nDrag a point: move it along the ground\nShift click the ground: add a point at the end\nCtrl click a point: remove it\nThe fence is laid again when you let go. Bake the island's navmesh after moving a fence that blocks walking.", MessageType.None);
        if (GUILayout.Button("Rebuild (lay it on the ground again)")) { Undo.RecordObject(fence.GetComponent<MeshFilter>(), "Rebuild fence"); fence.Rebuild(); }
    }

    private void OnSceneGUI()
    {
        var fence = (WorldFence)target;
        var e = Event.current;
        var count = fence.Points.Count;
        var world = new Vector3[count];
        for (var i = 0; i < count; i++)
        {
            world[i] = fence.transform.TransformPoint(fence.Points[i]);
            world[i].y = WorldRoad.GroundHeight(world[i]) + 0.05f;
        }

        // the line along the ground, between the points
        Handles.color = LineColour;
        for (var i = 0; i + 1 < count; i++)
        {
            var steps = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(world[i], world[i + 1]) / 1.5f), 1, 64);
            var line = new Vector3[steps + 1];
            for (var s = 0; s <= steps; s++)
            {
                var p = Vector3.Lerp(world[i], world[i + 1], s / (float)steps);
                p.y = WorldRoad.GroundHeight(p) + 0.1f;
                line[s] = p;
            }
            Handles.DrawAAPolyLine(4f, line);
        }

        for (var i = 0; i < count; i++)
        {
            var size = HandleUtility.GetHandleSize(world[i]) * 0.1f;
            Handles.color = i == selected ? SelectedColour : PointColour;
            Handles.Label(world[i] + Vector3.up * size * 3f, i.ToString(), EditorStyles.whiteMiniLabel);
            if (e.control)
            {
                if (Handles.Button(world[i], Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    Undo.RecordObject(fence, "Remove fence point");
                    fence.Points.RemoveAt(i);
                    selected = -1;
                    fence.Rebuild();
                    return;
                }
                continue;
            }
            // dragged along the ground under the cursor; a click without a drag selects it
            var id = GUIUtility.GetControlID(FocusType.Passive);
            Handles.SphereHandleCap(id, world[i], Quaternion.identity, size * 2f, e.type);
            switch (e.GetTypeForControl(id))
            {
                case EventType.Layout:
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(world[i], size));
                    break;
                case EventType.MouseDown:
                    if (e.button == 0 && HandleUtility.nearestControl == id && !e.shift && !e.alt)
                    {
                        GUIUtility.hotControl = id;
                        selected = i;
                        Undo.RecordObject(fence, "Move fence point");
                        e.Use();
                        Repaint();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id && GroundUnder(e.mousePosition, out var hit))
                    {
                        MovePoint(fence, i, hit, false);
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                        LayAgain(fence);
                    }
                    break;
            }
        }

        // the selected point's gizmo, for small moves along x and z
        if (selected >= 0 && selected < count && !e.control)
        {
            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(world[selected], Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(fence, "Move fence point");
                MovePoint(fence, selected, moved, false);
            }
            // the gizmo's drag has ended
            if (pending && e.type == EventType.MouseUp) LayAgain(fence);
        }

        if (e.type == EventType.MouseDown && e.button == 0 && e.shift && GroundUnder(e.mousePosition, out var at))
        {
            Undo.RecordObject(fence, "Add fence point");
            fence.AddPoint(fence.transform.InverseTransformPoint(at));
            selected = fence.Points.Count - 1;
            fence.Rebuild();
            e.Use();
        }
    }

    // a point to a place in the world (its height is the ground's); the fence is laid again now or when the move ends
    private void MovePoint(WorldFence fence, int index, Vector3 to, bool layNow)
    {
        to.y = WorldRoad.GroundHeight(to);
        fence.Points[index] = fence.transform.InverseTransformPoint(to);
        if (layNow) fence.Rebuild();
        else pending = true;
        Repaint();
        SceneView.RepaintAll();
    }

    private void LayAgain(WorldFence fence)
    {
        if (!pending) return;
        pending = false;
        fence.Rebuild();
        EditorUtility.SetDirty(fence);
    }

    // where the cursor meets the ground: the terrains first (the fence lies on them), else any collider
    private static bool GroundUnder(Vector2 mouse, out Vector3 point)
    {
        var ray = HandleUtility.GUIPointToWorldRay(mouse);
        var best = float.MaxValue;
        point = Vector3.zero;
        foreach (var terrain in Terrain.activeTerrains)
        {
            var collider = terrain ? terrain.GetComponent<TerrainCollider>() : null;
            if (collider && collider.Raycast(ray, out var hit, 5000f) && hit.distance < best) { best = hit.distance; point = hit.point; }
        }
        if (best < float.MaxValue) return true;
        if (Physics.Raycast(ray, out var any, 5000f)) { point = any.point; return true; }
        return false;
    }
}
