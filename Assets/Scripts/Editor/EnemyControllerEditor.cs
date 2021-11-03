using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyController))]
public class EnemyControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {

        var ag = (EnemyController)target;

        EditorGUILayout.LabelField("Combat Level: " + ag.Stats.CombatLevel);

        DrawDefaultInspector();


    }

}
