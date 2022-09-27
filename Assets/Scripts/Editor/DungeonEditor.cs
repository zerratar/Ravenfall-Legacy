using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonController))]
public class DungeonEditor : Editor
{
    private SerializedProperty nameProp;
    private SerializedProperty spawnRate;
    private SerializedProperty MobsDifficultyScale;
    private SerializedProperty BossCombatScale;
    private SerializedProperty BossHealthScale;
    private SerializedProperty level;
    private SerializedProperty tier;
    private SerializedProperty difficulity;

    private SerializedProperty itemRewardCount;

    private SerializedProperty bossSpawnPoint;
    private SerializedProperty startingPoint;

    void OnEnable()
    {
        spawnRate = serializedObject.FindProperty("SpawnRate");

        MobsDifficultyScale = serializedObject.FindProperty("MobsDifficultyScale");
        BossCombatScale = serializedObject.FindProperty("BossCombatScale");
        BossHealthScale = serializedObject.FindProperty("BossHealthScale");

        nameProp = serializedObject.FindProperty("Name");
        level = serializedObject.FindProperty("Level");
        tier = serializedObject.FindProperty("Tier");
        difficulity = serializedObject.FindProperty("Difficulity");

        itemRewardCount = serializedObject.FindProperty("itemRewardCount");
        bossSpawnPoint = serializedObject.FindProperty("bossSpawnPoint");
        startingPoint = serializedObject.FindProperty("startingPoint");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(spawnRate);
        EditorGUILayout.PropertyField(nameProp);
        EditorGUILayout.PropertyField(level);
        EditorGUILayout.PropertyField(tier);
        EditorGUILayout.PropertyField(difficulity);

        EditorGUILayout.PropertyField(MobsDifficultyScale);
        EditorGUILayout.PropertyField(BossCombatScale);
        EditorGUILayout.PropertyField(BossHealthScale);

        EditorGUILayout.PropertyField(itemRewardCount);
        EditorGUILayout.PropertyField(bossSpawnPoint);
        EditorGUILayout.PropertyField(startingPoint);

        if (target is DungeonController dungeon)
        {
            var noItemDrop = !dungeon.GetComponent<ItemDropHandler>();
            if (noItemDrop)
            {
                var s = new GUIStyle(EditorStyles.label);
                s.normal.textColor = Color.red;
                GUILayout.Label("No ItemDropHandler found!! Make sure to add one.", s);
            }

            if (dungeon.HasPredefinedRooms)
            {
                var s = new GUIStyle(EditorStyles.label);
                if (!dungeon.BossRoom)
                {
                    s.normal.textColor = Color.red;
                    GUILayout.Label("No boss room found. Make sure to add one.", s);
                }
                else
                {
                    s.normal.textColor = Color.cyan;
                    GUILayout.Label("This dungeon has been manually created and is ready to go!", s);
                }
            }
            else
            {
                var s = new GUIStyle(EditorStyles.label);
                s.normal.textColor = Color.green;
                GUILayout.Label("This dungeon will be automatically generated and is ready to go!", s);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}