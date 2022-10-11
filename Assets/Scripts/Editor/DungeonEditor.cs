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
            var style = new GUIStyle(EditorStyles.label);
            if (noItemDrop)
            {
                style.normal.textColor = Color.red;
                GUILayout.Label("No ItemDropHandler found!! Make sure to add one.", style);
            }

            if (!dungeon.BossRoom)
            {
                style.normal.textColor = Color.red;
                GUILayout.Label("No boss room found. Make sure to add one.", style);
            }
            else
            {
                style.normal.textColor = Color.cyan;
                GUILayout.Label("This dungeon has been manually created and is ready to go!", style);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}