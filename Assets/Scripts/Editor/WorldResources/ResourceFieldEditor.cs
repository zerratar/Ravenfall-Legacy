using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The inspector of a <see cref="ResourceField"/> and its brush in the Scene view. Pick a mode:
/// <list type="bullet">
/// <item>Paint: click or drag to place the brush type, spread over the brush circle and kept apart by the
/// spacing; hold Shift to erase instead.</item>
/// <item>Erase: click or drag to remove what is inside the circle (only the brush type, if ticked).</item>
/// <item>Pick: click a tree or spot to select it, then move it (it follows the ground), turn it with the ring,
/// size it with the cube, or change its type and look in the inspector; Delete removes it.</item>
/// </list>
/// [ and ] change the brush size, Escape leaves the mode. Everything lands on the ground and is undoable, one
/// undo per stroke. The inspector does not list every instance (a field can hold thousands), only the counts.
/// </summary>
[CustomEditor(typeof(ResourceField))]
public class ResourceFieldEditor : Editor
{
    private enum Mode { Off, Paint, Erase, Pick }

    private const string PrefsPrefix = "Ravenfall.ResourceBrush.";
    private static readonly string[] ModeNames = { "Off", "Paint", "Erase", "Pick" };
    private static readonly Color PaintColour = new Color(0.45f, 0.95f, 0.5f);
    private static readonly Color EraseColour = new Color(1f, 0.4f, 0.35f);
    private static readonly Color PickColour = new Color(1f, 0.82f, 0.38f);

    private static Mode mode;
    private static bool startPainting;
    private static ResourceType[] allTypes;

    // the picked instance in Pick mode, or -1
    private int picked = -1;
    // the stroke under way: its undo group and where it last placed or erased
    private int strokeGroup = -1;
    private Vector3 lastDab;

    private static ResourceType BrushType
    {
        get => AssetDatabase.LoadAssetAtPath<ResourceType>(AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(PrefsPrefix + "Type", "")));
        set => EditorPrefs.SetString(PrefsPrefix + "Type", value ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : "");
    }

    private static float Radius
    {
        get => EditorPrefs.GetFloat(PrefsPrefix + "Radius", 6f);
        set => EditorPrefs.SetFloat(PrefsPrefix + "Radius", Mathf.Clamp(value, 0.5f, 40f));
    }

    private static float Spacing
    {
        get => EditorPrefs.GetFloat(PrefsPrefix + "Spacing", 4f);
        set => EditorPrefs.SetFloat(PrefsPrefix + "Spacing", Mathf.Clamp(value, 0.25f, 30f));
    }

    private static int Amount
    {
        get => EditorPrefs.GetInt(PrefsPrefix + "Amount", 3);
        set => EditorPrefs.SetInt(PrefsPrefix + "Amount", Mathf.Clamp(value, 1, 30));
    }

    private static float MaxSlope
    {
        get => EditorPrefs.GetFloat(PrefsPrefix + "MaxSlope", 40f);
        set => EditorPrefs.SetFloat(PrefsPrefix + "MaxSlope", Mathf.Clamp(value, 0f, 90f));
    }

    // the look to paint, or -1 for a random one by the type's weights
    private static int Variant
    {
        get => EditorPrefs.GetInt(PrefsPrefix + "Variant", -1);
        set => EditorPrefs.SetInt(PrefsPrefix + "Variant", value);
    }

    private static bool EraseOnlyBrushType
    {
        get => EditorPrefs.GetBool(PrefsPrefix + "EraseOnlyType", false);
        set => EditorPrefs.SetBool(PrefsPrefix + "EraseOnlyType", value);
    }

    [MenuItem("GameObject/Ravenfall/Resource Field", false, 11)]
    private static void CreateField(MenuCommand command)
    {
        var parent = command.context as GameObject;
        var go = new GameObject("Resources");
        GameObjectUtility.SetParentAndAlign(go, parent);
        go.AddComponent<ResourceField>();
        Undo.RegisterCreatedObjectUndo(go, "Create resource field");
        startPainting = true;
        Selection.activeGameObject = go;
    }

    private void OnEnable()
    {
        // a field selected later does not start out painting; one just made does
        mode = startPainting ? Mode.Paint : Mode.Off;
        startPainting = false;
        picked = -1;
        EditorApplication.projectChanged += ForgetTypes;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= ForgetTypes;
        EndStroke();
        Tools.hidden = false;
    }

    private static void ForgetTypes() => allTypes = null;

    private static ResourceType[] AllTypes => allTypes ??= AssetDatabase.FindAssets("t:ResourceType")
        .Select(guid => AssetDatabase.LoadAssetAtPath<ResourceType>(AssetDatabase.GUIDToAssetPath(guid)))
        .Where(t => t)
        .OrderBy(t => t.kind).ThenBy(t => t.name)
        .ToArray();

    public override void OnInspectorGUI()
    {
        var field = (ResourceField)target;
        if (picked >= field.Count) picked = -1;

        EditorGUILayout.LabelField("Instances", field.Count.ToString());
        EditorGUILayout.LabelField("Drawn in the scene", ResourceRenderer.InstanceCount + " (all fields)");

        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("treeHealth"), new GUIContent("Tree health", "Hits to cut a tree here down; 0 uses each type's own."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("respawnSeconds"), new GUIContent("Respawn seconds", "Seconds until a tree or spot here is back; 0 uses each type's own."));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        var chosen = (Mode)GUILayout.Toolbar((int)mode, ModeNames);
        if (chosen != mode)
        {
            mode = chosen;
            picked = -1;
            SceneView.RepaintAll();
        }
        EditorGUILayout.HelpBox(ModeHelp(), MessageType.None);

        if (mode == Mode.Paint || mode == Mode.Erase) BrushGUI();
        if (mode == Mode.Pick && picked >= 0) PickedGUI(field);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Types", EditorStyles.boldLabel);
        for (var t = 0; t < field.Types.Count; ++t)
        {
            var type = field.Types[t];
            var used = field.Instances.Count(i => i.type == t);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(type, typeof(ResourceType), false);
                EditorGUILayout.LabelField(used.ToString(), GUILayout.Width(60));
            }
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Snap All To Ground"))
            {
                Undo.RecordObject(field, "Snap resources to ground");
                for (var i = 0; i < field.Count; ++i)
                {
                    var instance = field.Instances[i];
                    instance.position = ResourceGround.Snap(instance.position);
                    field.Set(i, instance);
                }
            }
            if (GUILayout.Button("Clear All Instances") && EditorUtility.DisplayDialog("Clear field", "Remove all " + field.Count + " instances from this field?", "Clear", "Cancel"))
            {
                Undo.RecordObject(field, "Clear resource field");
                field.Clear();
                picked = -1;
            }
        }
    }

    private static string ModeHelp()
    {
        switch (mode)
        {
            case Mode.Paint: return "Click or drag on the ground to place. Shift to erase. [ and ] change the size, Escape stops.";
            case Mode.Erase: return "Click or drag to remove what is inside the circle. [ and ] change the size, Escape stops.";
            case Mode.Pick: return "Click a tree or spot to select it. Drag the arrows to move it, the ring to turn it, the cube to size it. Delete removes it.";
            default: return "Pick a mode to paint, erase or edit this field's trees and spots in the Scene view.";
        }
    }

    private static void BrushGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);

        var brush = BrushType;
        var types = AllTypes;
        if (types.Length == 0) EditorGUILayout.HelpBox("No resource types yet. Make one with Ravenfall > World Resources > Create Tree (or Gather) Type From Selection.", MessageType.Info);
        var names = types.Select(t => t.name).ToArray();
        var at = System.Array.IndexOf(types, brush);
        var pick = GUILayout.SelectionGrid(at, names, 2);
        if (pick != at && pick >= 0)
        {
            brush = BrushType = types[pick];
            Variant = -1;
        }

        if (brush)
        {
            var looks = new[] { "Random" }.Concat(brush.variants.Select((v, i) => string.IsNullOrEmpty(v.name) ? "Look " + (i + 1) : v.name)).ToArray();
            Variant = EditorGUILayout.Popup("Look", Mathf.Clamp(Variant + 1, 0, looks.Length - 1), looks) - 1;
        }

        Radius = EditorGUILayout.Slider("Size", Radius, 0.5f, 40f);
        if (mode == Mode.Paint)
        {
            Amount = EditorGUILayout.IntSlider(new GUIContent("Amount", "How many to try placing per dab. 1 places one exactly under the mouse."), Amount, 1, 30);
            Spacing = EditorGUILayout.Slider(new GUIContent("Spacing", "The least distance between two instances in this field, in metres."), Spacing, 0.25f, 30f);
            MaxSlope = EditorGUILayout.Slider(new GUIContent("Max slope", "Ground steeper than this, in degrees, is skipped."), MaxSlope, 0f, 90f);
            if (brush) EditorGUILayout.LabelField(" ", (brush.randomYaw ? "Random turn" : "No turn") + ", size " + brush.scaleRange.x.ToString("0.00") + " to " + brush.scaleRange.y.ToString("0.00") + " (set on the type)", EditorStyles.miniLabel);
        }
        else
        {
            EraseOnlyBrushType = EditorGUILayout.Toggle("Only the brush type", EraseOnlyBrushType);
        }
    }

    private void PickedGUI(ResourceField field)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selected", EditorStyles.boldLabel);
        var instance = field.Instances[picked];

        EditorGUI.BeginChangeCheck();
        var typeNames = field.Types.Select(t => t ? t.name : "(missing)").ToArray();
        int typeIndex = EditorGUILayout.Popup("Type", instance.type, typeNames);
        var type = typeIndex < field.Types.Count ? field.Types[typeIndex] : null;
        var variant = (int)instance.variant;
        if (type && type.variants.Length > 0)
        {
            var looks = type.variants.Select((v, i) => string.IsNullOrEmpty(v.name) ? "Look " + (i + 1) : v.name).ToArray();
            variant = EditorGUILayout.Popup("Look", Mathf.Clamp(variant, 0, looks.Length - 1), looks);
        }
        var position = EditorGUILayout.Vector3Field("Position", instance.position);
        var yaw = EditorGUILayout.Slider("Turn", Mathf.Repeat(instance.yaw, 360f), 0f, 360f);
        var scale = EditorGUILayout.Slider("Size", instance.scale > 0f ? instance.scale : 1f, 0.1f, 5f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(field, "Edit resource");
            instance.type = (ushort)typeIndex;
            instance.variant = (byte)Mathf.Clamp(variant, 0, 255);
            instance.position = position;
            instance.yaw = yaw;
            instance.scale = scale;
            field.Set(picked, instance);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Snap To Ground"))
            {
                Undo.RecordObject(field, "Snap resource to ground");
                instance.position = ResourceGround.Snap(instance.position);
                field.Set(picked, instance);
            }
            if (GUILayout.Button("Remove"))
            {
                Remove(field);
                GUIUtility.ExitGUI();
            }
        }
    }

    private void OnSceneGUI()
    {
        var field = (ResourceField)target;
        Tools.hidden = mode != Mode.Off;
        if (mode == Mode.Off) return;

        var e = Event.current;
        var control = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(control);
        if (e.type == EventType.MouseMove) HandleUtility.Repaint();

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            mode = Mode.Off;
            picked = -1;
            Tools.hidden = false;
            e.Use();
            Repaint();
            return;
        }

        if (mode == Mode.Pick) PickScene(field, e, control);
        else BrushScene(field, e, control);
    }

    private void BrushScene(ResourceField field, Event e, int control)
    {
        var erasing = mode == Mode.Erase || e.shift;

        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.LeftBracket || e.keyCode == KeyCode.RightBracket))
        {
            Radius *= e.keyCode == KeyCode.RightBracket ? 1.15f : 1f / 1.15f;
            e.Use();
            Repaint();
        }

        if (!ResourceGround.UnderMouse(e.mousePosition, out var ground))
        {
            if (e.type == EventType.MouseUp && GUIUtility.hotControl == control) EndStroke();
            return;
        }

        Handles.color = erasing ? EraseColour : PaintColour;
        Handles.DrawWireDisc(ground.point, ground.normal, Radius, 2f);
        if (!erasing && Amount > 1)
        {
            Handles.color = new Color(PaintColour.r, PaintColour.g, PaintColour.b, 0.35f);
            Handles.DrawWireDisc(ground.point, ground.normal, Mathf.Min(Spacing, Radius) * 0.5f, 1f);
        }

        if (e.alt || e.button != 0) return;

        if (e.type == EventType.MouseDown && HandleUtility.nearestControl == control)
        {
            GUIUtility.hotControl = control;
            Undo.IncrementCurrentGroup();
            strokeGroup = Undo.GetCurrentGroup();
            Dab(field, ground.point, erasing);
            lastDab = ground.point;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == control)
        {
            // a dab every half circle (or every spacing, placing one at a time) along the stroke
            var step = Amount == 1 && !erasing ? Spacing : Radius * 0.5f;
            if (Vector3.Distance(ground.point, lastDab) >= step)
            {
                Dab(field, ground.point, erasing);
                lastDab = ground.point;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseUp && GUIUtility.hotControl == control)
        {
            EndStroke();
            e.Use();
        }
    }

    private void EndStroke()
    {
        if (strokeGroup >= 0) Undo.CollapseUndoOperations(strokeGroup);
        strokeGroup = -1;
        if (GUIUtility.hotControl != 0) GUIUtility.hotControl = 0;
    }

    private void Dab(ResourceField field, Vector3 centre, bool erasing)
    {
        if (erasing)
        {
            Erase(field, centre);
            return;
        }

        var type = BrushType;
        if (!type)
        {
            Debug.LogWarning("Pick a brush type in the resource field's inspector first.");
            return;
        }

        // what is already near, to keep the spacing to
        var reach = Radius + Spacing;
        var near = new List<Vector2>();
        foreach (var instance in field.Instances)
        {
            if (Flat(instance.position - centre).sqrMagnitude <= reach * reach) near.Add(Flat(instance.position));
        }

        var minUp = Mathf.Cos(MaxSlope * Mathf.Deg2Rad);
        var recorded = false;
        for (var n = 0; n < Amount; ++n)
        {
            var offset = Amount == 1 ? Vector2.zero : Random.insideUnitCircle * Radius;
            if (!ResourceGround.Below(centre + new Vector3(offset.x, 0f, offset.y), out var ground)) continue;
            if (ground.normal.y < minUp - 0.0001f) continue;

            var at = Flat(ground.point);
            if (near.Any(p => (p - at).sqrMagnitude < Spacing * Spacing)) continue;

            if (!recorded)
            {
                Undo.RecordObject(field, "Paint resources");
                recorded = true;
            }
            var variant = Variant >= 0 && Variant < type.variants.Length ? Variant : type.PickVariant(Random.value);
            var yaw = type.randomYaw ? Random.Range(0f, 360f) : 0f;
            var scale = Random.Range(Mathf.Min(type.scaleRange.x, type.scaleRange.y), Mathf.Max(type.scaleRange.x, type.scaleRange.y));
            field.Add(type, variant, ground.point, yaw, scale > 0f ? scale : 1f);
            near.Add(at);
        }
    }

    private void Erase(ResourceField field, Vector3 centre)
    {
        var only = EraseOnlyBrushType && mode == Mode.Erase;
        var typeIndex = only ? field.Types.ToList().IndexOf(BrushType) : -1;
        if (only && typeIndex < 0) return;

        var radius = Radius;
        bool Inside(ResourceField.Instance i) => Flat(i.position - centre).sqrMagnitude <= radius * radius && (!only || i.type == typeIndex);
        if (!field.Instances.Any(Inside)) return;

        Undo.RecordObject(field, "Erase resources");
        field.RemoveAll(Inside);
        picked = -1;
    }

    private void PickScene(ResourceField field, Event e, int control)
    {
        if (picked >= field.Count) picked = -1;

        if (picked >= 0)
        {
            var instance = field.Instances[picked];
            var type = field.TypeOf(picked);
            var size = HandleUtility.GetHandleSize(instance.position);

            Handles.color = PickColour;
            Handles.DrawWireDisc(instance.position, Vector3.up, type ? type.actionDistance : 1f, 2f);

            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(instance.position, Quaternion.identity);
            Handles.color = PickColour;
            var turned = Handles.Disc(Quaternion.Euler(0f, instance.yaw, 0f), instance.position, Vector3.up, size * 1.1f, false, 0f);
            var sized = Handles.ScaleValueHandle(instance.scale > 0f ? instance.scale : 1f, instance.position + Vector3.up * size * 1.4f, Quaternion.identity, size * 0.9f, Handles.CubeHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(field, "Edit resource");
                // moved sideways it follows the ground; moved straight up or down it stays where it is put
                if (Flat(moved - instance.position).sqrMagnitude > 0.000001f) moved = ResourceGround.Snap(moved);
                instance.position = moved;
                instance.yaw = Mathf.Repeat(turned.eulerAngles.y, 360f);
                instance.scale = Mathf.Clamp(sized, 0.1f, 5f);
                field.Set(picked, instance);
                Repaint();
            }

            var deleting = (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
                || (e.type == EventType.ExecuteCommand && (e.commandName == "Delete" || e.commandName == "SoftDelete"));
            if (e.type == EventType.ValidateCommand && (e.commandName == "Delete" || e.commandName == "SoftDelete")) e.Use();
            if (deleting)
            {
                Remove(field);
                e.Use();
                return;
            }
        }

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && HandleUtility.nearestControl == control)
        {
            picked = InstanceUnderMouse(field, e.mousePosition);
            e.Use();
            Repaint();
        }
    }

    private void Remove(ResourceField field)
    {
        if (picked < 0 || picked >= field.Count) return;
        Undo.RecordObject(field, "Remove resource");
        field.RemoveAt(picked);
        picked = -1;
        Repaint();
    }

    /// <summary>The instance whose trunk or body is closest to the mouse on screen, within a few pixels.</summary>
    private static int InstanceUnderMouse(ResourceField field, Vector2 mouse)
    {
        var camera = SceneView.currentDrawingSceneView ? SceneView.currentDrawingSceneView.camera : Camera.current;
        var best = -1;
        var bestDistance = 14f;
        for (var i = 0; i < field.Count; ++i)
        {
            var instance = field.Instances[i];
            if (camera && camera.WorldToViewportPoint(instance.position).z <= 0f) continue;

            var type = field.TypeOf(i);
            var look = type && type.variants.Length > 0 ? type.variants[Mathf.Clamp(instance.variant, 0, type.variants.Length - 1)] : null;
            var height = look != null && !look.IsEmpty ? look.lods[0].mesh.bounds.max.y * 0.7f : 1f;
            height *= instance.scale > 0f ? instance.scale : 1f;

            var bottom = HandleUtility.WorldToGUIPoint(instance.position);
            var top = HandleUtility.WorldToGUIPoint(instance.position + Vector3.up * height);
            var d = HandleUtility.DistancePointToLineSegment(mouse, bottom, top);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }

    private static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
}
