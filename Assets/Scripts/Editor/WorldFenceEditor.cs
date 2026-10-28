using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws and edits a <see cref="WorldFence"/> in the Scene view: drag a point's handle to move it along the ground,
/// Shift click the ground to add a point at the end, Ctrl click a point to remove it. The fence is laid on the ground
/// again after every change (and with the Rebuild button, after the ground under it changed). Undoable.
/// </summary>
[CustomEditor(typeof(WorldFence))]
public class WorldFenceEditor : Editor
{
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

    public override void OnInspectorGUI()
    {
        var fence = (WorldFence)target;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) fence.Rebuild();
        EditorGUILayout.HelpBox("Drag a point: move it\nShift click the ground: add a point at the end\nCtrl click a point: remove it", MessageType.None);
        if (GUILayout.Button("Rebuild (lay it on the ground again)")) { Undo.RecordObject(fence.GetComponent<MeshFilter>(), "Rebuild fence"); fence.Rebuild(); }
    }

    private void OnSceneGUI()
    {
        var fence = (WorldFence)target;
        var e = Event.current;
        for (var i = 0; i < fence.Points.Count; i++)
        {
            var world = fence.transform.TransformPoint(fence.Points[i]);
            world.y = WorldRoad.GroundHeight(world);
            var size = HandleUtility.GetHandleSize(world) * 0.08f;
            if (e.control)
            {
                if (Handles.Button(world, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    Undo.RecordObject(fence, "Remove fence point");
                    fence.Points.RemoveAt(i);
                    fence.Rebuild();
                    return;
                }
                continue;
            }
            EditorGUI.BeginChangeCheck();
            var moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(fence, "Move fence point");
                moved.y = WorldRoad.GroundHeight(moved);
                fence.Points[i] = fence.transform.InverseTransformPoint(moved);
                fence.Rebuild();
            }
        }
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
        {
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out var hit, 5000f))
            {
                Undo.RecordObject(fence, "Add fence point");
                fence.AddPoint(fence.transform.InverseTransformPoint(hit.point));
                fence.Rebuild();
                e.Use();
            }
        }
    }
}
