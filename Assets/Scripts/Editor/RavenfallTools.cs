using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RavenfallDungeonTools
{

    [MenuItem("Ravenfall/Objects/Adjust Placements/All", priority = 0)]
    public static void AdjustPlacementsOnAllOobjects()
    {
        PlacementUtility.PlaceOnGround<TreeController>();
        PlacementUtility.PlaceOnGround<RockController>();
        PlacementUtility.PlaceOnGround<EnemyController>();
        PlacementUtility.PlaceOnGround<Torch>();
        PlacementUtility.PlaceOnGround<CraftingStation>();
        PlacementUtility.PlaceOnGround<FarmController>();
        PlacementUtility.PlaceOnGround<TownHouseSlot>();
    }

    [MenuItem("Ravenfall/Objects/Adjust Placements/Trees", priority = 10)]
    public static void AdjustPlacementsOnTreebjects() => PlacementUtility.PlaceOnGround<TreeController>();
    [MenuItem("Ravenfall/Objects/Adjust Placements/Town House Slot", priority = 10)]
    public static void AdjustPlacementsOnTownHouseSlot() => PlacementUtility.PlaceOnGround<TownHouseSlot>();
    [MenuItem("Ravenfall/Objects/Adjust Placements/Rocks", priority = 10)]
    public static void AdjustPlacementsOnRocks() => PlacementUtility.PlaceOnGround<RockController>();

    [MenuItem("Ravenfall/Objects/Adjust Placements/Enemies", priority = 10)]
    public static void AdjustPlacementsOnEnemyController() => PlacementUtility.PlaceOnGround<EnemyController>();

    [MenuItem("Ravenfall/Objects/Adjust Placements/Crafting Stations", priority = 10)]
    public static void AdjustPlacementsOnCraftingStation() => PlacementUtility.PlaceOnGround<CraftingStation>();
    [MenuItem("Ravenfall/Objects/Adjust Placements/Farming", priority = 10)]
    public static void AdjustPlacementsOnFarming() => PlacementUtility.PlaceOnGround<FarmController>();
    [MenuItem("Ravenfall/Objects/Adjust Placements/Torches", priority = 10)]
    public static void AdjustPlacementsOnTorch() => PlacementUtility.PlaceOnGround<Torch>();
    [MenuItem("Ravenfall/Objects/Adjust Placements/Selections", priority = 20)]
    public static void AdjustPlacementsOnSelection() => PlacementUtility.PlaceSelectionOnGround();
    [MenuItem("Ravenfall/Objects/Adjust Placements/Children of selection", priority = 20)]
    public static void AdjustPlacementsOnChildrenOfSelection() => PlacementUtility.PlaceChildrenOnGround(Selection.gameObjects);


    [MenuItem("Ravenfall/Dungeon/Create Prefab")]
    public static void CreateDungeonPrefab()
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
    public static bool ValidateCreateDungeonPrefab()
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
