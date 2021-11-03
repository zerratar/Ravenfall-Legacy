using UnityEditor;
using UnityEngine;




[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var ag = (DungeonGenerator)target;

        if (GUILayout.Button("Generate"))
        {

            ag.GenerateDungeon();

            EditorApplication.update -= ag.Step;
            EditorApplication.update += ag.Step;

            //if (!Application.isPlaying)
            //{
            //    EditorApplication.update += ag.Step;
            //}
        }

        if (GUILayout.Button("Interrupt"))
        {

            EditorApplication.update -= ag.Step;
            EditorApplication.update += ag.Step;

            ag.Interrupt();
            //if (!Application.isPlaying)
            //{
            //    EditorApplication.update += ag.Step;
            //}
        }

        if (GUILayout.Button("Clear"))
        {

            EditorApplication.update -= ag.Step;
            EditorApplication.update += ag.Step;

            ag.ClearChildren();

            //if (!Application.isPlaying)
            //{
            //    EditorApplication.update += ag.Step;
            //}
        }


    }
}
