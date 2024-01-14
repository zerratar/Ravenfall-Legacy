using Shinobytes.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RavenfallObjectTools
{
    [MenuItem("Ravenfall/Objects/Assert unreachable objects", priority = 0)]
    public static void AssertInvalidPlacement()
    {
        int found = 0;

        EditorUtility.DisplayProgressBar("Looking for unreachable objects (Found: " + found + ")", "Trees...", 0);
        found += PlacementUtility.AssertInvalidPlacement<TreeController>();

        EditorUtility.DisplayProgressBar("Looking for unreachable objects (Found: " + found + ")", "Rocks...", 1f / 6f);
        found += PlacementUtility.AssertInvalidPlacement<RockController>();

        EditorUtility.DisplayProgressBar("Looking for unreachable objects (Found: " + found + ")", "Enemies...", 2f / 6f);
        found += PlacementUtility.AssertInvalidPlacement<EnemyController>();

        EditorUtility.DisplayProgressBar("Looking for unreachable objects (Found: " + found + ")", "Stalls...", 3f / 6f);
        found += PlacementUtility.AssertInvalidPlacement<CraftingStation>();

        EditorUtility.DisplayProgressBar("Looking for unreachable objects (Found: " + found + ")", "Farming...", 4f / 6f);
        found += PlacementUtility.AssertInvalidPlacement<FarmController>();

        EditorUtility.DisplayProgressBar("Looking for unreachable objects (Found: " + found + ")", "Gathering...", 5f / 6f);
        found += PlacementUtility.AssertInvalidPlacement<GatherController>();

        EditorUtility.DisplayProgressBar("Looking for unreachable objects (Found: " + found + ")", "Done", 6f / 6f);

        System.Threading.Thread.Sleep(1000);
        EditorUtility.ClearProgressBar();
    }


    [MenuItem("Ravenfall/Tools/Find Duplicate Objects")]
    public static void FindDuplicateObjects()
    {
        // this is quite simple logic, we wont do anything unless we know for certain. but here it is:
        // 1. find all objects in the scene
        // 2. get names of each individual object, if the name ends with (number), remove that as it indicates a copy
        // 3. group by name, then compare position, rotation and scale
        // 4. log out the number of objects that matched.

        var a = Selection.activeGameObject;
        Transform[] selections = null;
        if (!a)
        {
            selections = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
        else
        {
            selections = a.GetComponentsInChildren<Transform>(true);
        }

        var nameToObject = new Dictionary<string, List<Transform>>();
        foreach (var transform in selections)
        {
            var name = transform.name.Trim();
            if (name.EndsWith(")") && name.Contains(" "))
            {
                var toRemove = name.Split(' ')[^1];
                name = name.Replace(" " + toRemove, "");
            }

            nameToObject.TryGetValue(name, out var list);
            if (list == null)
            {
                list = new List<Transform>();
            }

            list.Add(transform);
            nameToObject[name] = list;
        }


        var itemTypes = 0;
        var duplicates = 0;
        var totalObjects = 0;
        // distinct by will remove duplicates, we will use that to calculate the duplicate in the end.
        foreach (var objGroup in nameToObject)
        {
            itemTypes++;
            var countBeforeDistinct = objGroup.Value.Count;
            var objectsAfterDistinct = objGroup.Value.DistinctBy(x => new { x.position, x.localScale, x.rotation }).AsList();

            var countAfterDistinct = objectsAfterDistinct.Count;
            totalObjects += countBeforeDistinct;
            duplicates += countBeforeDistinct - countAfterDistinct;
        }

        Shinobytes.Debug.Log("A total of " + duplicates + " duplicates was found. Total amount of objects: " + totalObjects + ", of " + itemTypes + " different types");
    }
    // too risky..
    //[MenuItem("Ravenfall/Tools/Find and Destroy Duplicate Objects")]
    //public static void DestroyDuplicateObjects()
    //{
    //    var a = Selection.activeGameObject;
    //    Transform[] selections = null;
    //    if (!a)
    //    {
    //        selections = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    //    }
    //    else
    //    {
    //        selections = a.GetComponentsInChildren<Transform>(true);
    //    }
    //    var nameToObject = new Dictionary<string, List<Transform>>();
    //    foreach (var transform in selections)
    //    {
    //        var name = transform.name.Trim();
    //        if (name.EndsWith(")") && name.Contains(" "))
    //        {
    //            var toRemove = name.Split(' ')[^1];
    //            name = name.Replace(" " + toRemove, "");
    //        }
    //        nameToObject.TryGetValue(name, out var list);
    //        if (list == null)
    //        {
    //            list = new List<Transform>();
    //        }
    //        list.Add(transform);
    //        nameToObject[name] = list;
    //    }
    //    var objectsDestroyed = 0;
    //    // distinct by will remove duplicates, we will use that to calculate the duplicate in the end.
    //    foreach (var objGroup in nameToObject)
    //    {
    //        var countBeforeDistinct = objGroup.Value.Count;
    //        var objectsAfterDistinct =
    //            new HashSet<int>(objGroup.Value.DistinctBy(x => /*new { */ x.position /*, x.localScale, x.rotation }*/ ).Select(x => x.gameObject.GetInstanceID()));
    //        foreach (var obj in objGroup.Value)
    //        {
    //            if (!objectsAfterDistinct.Contains(obj.GetInstanceID()))
    //            {
    //                //GameObject.DestroyImmediate(obj.gameObject);
    //                Shinobytes.Debug.Log(obj.name + " will be destroyed");
    //                objectsDestroyed++;
    //            }
    //        }
    //    }
    //    Shinobytes.Debug.Log("A total of " + objectsDestroyed + " duplicates was destroyed.");
    //}

    private static List<GameObject> tmpDisabledGameObjects = new List<GameObject>();

    //[MenuItem("Ravenfall/Navigation/Generate Temporary Boxes under Docks", priority = 0)]
    //public static void GenerateTempBoxesUnderDock()
    //{
    //    if (tmpDisabledGameObjects.Count != 0)
    //    {
    //        DestroyTempBoxesUnderDock();
    //    }

    //    var tmpNavObj = "Navigation Objects (Temp)";
    //    var temp = GameObject.Find(tmpNavObj);
    //    if (!temp)
    //    {
    //        temp = new GameObject(tmpNavObj);
    //    }

    //    var docks = GameObject.FindObjectsByType<DockController>(FindObjectsSortMode.None);
    //    foreach (var dock in docks)
    //    {
    //        var colliders = dock.transform.GetComponentsInChildren<BoxCollider>();
    //        foreach (var c in colliders)
    //        {
    //            if (c.isTrigger || !c.gameObject.activeInHierarchy)
    //            {
    //                continue;
    //            }

    //            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //            t.transform.SetParent(temp.transform);
    //            t.transform.localScale = Vector3.Scale(c.size, c.transform.lossyScale);
    //            t.transform.rotation = c.transform.rotation;
    //            t.transform.position = (c.transform.position + c.center);

    //            // check if we have a parent with a MeshCollider or MeshRenderer, then we want to disable that one instead.

    //            var parent = c.transform.parent;
    //            var pHasMeshCollider = HasComponent<MeshCollider>(parent);
    //            var pHasMeshRenderer = HasComponent<MeshRenderer>(parent);
    //            var pIsNotDock = parent.name != "Dock";

    //            if (pIsNotDock && (pHasMeshCollider || pHasMeshRenderer))
    //            {
    //                parent.gameObject.SetActive(false);
    //                tmpDisabledGameObjects.Add(parent.gameObject);
    //            }
    //            else
    //            {
    //                c.gameObject.SetActive(false);
    //                tmpDisabledGameObjects.Add(c.gameObject);
    //            }
    //        }
    //    }
    //}


    //[MenuItem("Ravenfall/Navigation/Generate NavMesh (A*)", priority = 0)]
    //public static void GenerateNavMesh()
    //{
    //    var astar = GameObject.FindAnyObjectByType<AstarPath>();
    //    if (!astar)
    //    {
    //        return;
    //    }

    //    Pathfinding.AstarPathEditor.MenuScan();
    //}


    //[MenuItem("Ravenfall/Navigation/Destroy Temporary Boxes under Docks", priority = 0)]
    //public static void DestroyTempBoxesUnderDock()
    //{
    //    var tmpNavObj = "Navigation Objects (Temp)";
    //    var temp = GameObject.Find(tmpNavObj);
    //    if (!temp)
    //    {
    //        return;
    //    }
    //    GameObject.DestroyImmediate(temp);
    //    //var childs = temp.GetComponentsInChildren<Transform>();
    //    //foreach (var c in childs)
    //    //{
    //    //    GameObject.DestroyImmediate(c.gameObject);
    //    //}
    //    try
    //    {
    //        if (tmpDisabledGameObjects.Count != 0)
    //        {
    //            foreach (var c in tmpDisabledGameObjects)
    //            {
    //                c.gameObject.SetActive(true);
    //            }
    //            return;
    //        }

    //        var docks = GameObject.FindObjectsByType<DockController>(FindObjectsSortMode.None);
    //        foreach (var dock in docks)
    //        {
    //            var colliders = dock.transform.GetComponentsInChildren<BoxCollider>(true);
    //            foreach (var c in colliders)
    //            {
    //                if (c.isTrigger || !c.transform.parent.gameObject.activeInHierarchy || c.gameObject.activeInHierarchy)
    //                {
    //                    continue;
    //                }

    //                c.gameObject.SetActive(true);
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        tmpDisabledGameObjects.Clear();
    //    }
    //}
    private static bool HasComponent<T>(Transform obj)
    {
        return obj.GetComponent<T>() != null;
    }


    [MenuItem("Ravenfall/Enemies/Assign Dependencies", priority = 0)]
    public static void AssignEnemyDependencies()
    {
        foreach (var enemy in GameObject.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            enemy.AssignDependencies();
        }
    }

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
        AdjustPlacementsOnIslandSpawnpoints();
    }

    [MenuItem("Ravenfall/Objects/Adjust Placements/Spawnpoints", priority = 10)]
    public static void AdjustPlacementsOnIslandSpawnpoints()
    {
        var islands = GameObject.FindObjectsByType<IslandController>(FindObjectsSortMode.None);
        foreach (var island in islands)
        {
            PlacementUtility.PlaceOnGround(island.SpawnPositionTransform);
        }
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
