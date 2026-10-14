using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The inspector of a <see cref="ResourceField"/>: what it holds, per type, without listing every instance
/// (a field can hold thousands). Painting and erasing come with the brush.
/// </summary>
[CustomEditor(typeof(ResourceField))]
public class ResourceFieldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var field = (ResourceField)target;
        EditorGUILayout.LabelField("Instances", field.Count.ToString());
        EditorGUILayout.LabelField("Drawn in the scene", ResourceRenderer.InstanceCount + " (all fields)");

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
        if (GUILayout.Button("Clear All Instances") && EditorUtility.DisplayDialog("Clear field", "Remove all " + field.Count + " instances from this field?", "Clear", "Cancel"))
        {
            Undo.RecordObject(field, "Clear resource field");
            field.Clear();
            EditorUtility.SetDirty(field);
        }
    }
}
