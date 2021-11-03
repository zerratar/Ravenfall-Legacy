using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RavenfallDungeonTools
{
    [MenuItem("Ravenfall/Dungeon/Create Prefab")]
    static void CreateDungeonPrefab()
    {
        // Keep track of the currently selected GameObject(s)
        GameObject[] objectArray = Selection.gameObjects;

        // Loop through every GameObject in the array above
        foreach (GameObject gameObject in objectArray)
        {
            // Set the path as within the Assets folder,
            // and name it as the GameObject's name with the .Prefab format
            string localPath = "Assets/Prefabs/Dungeon/" + gameObject.name + ".prefab";

            // Make sure the file name is unique, in case an existing Prefab has the same name.
            localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

            // Create the new Prefab.
            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, localPath, InteractionMode.UserAction);
        }
    }

    // Disable the menu item if no selection is in place.
    [MenuItem("Ravenfall/Dungeon/Create Prefab", true)]
    static bool ValidateCreateDungeonPrefab()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene == null)
        {
            return false;
        }

        if (activeScene.name.IndexOf("dungeon", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        return Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject);
    }
}
