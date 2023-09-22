using RavenNest.Models;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GenerateItemRow : OdinEditorWindow
{

    [MenuItem("Ravenfall/Tools/Items/Generate Item Row")]
    public static void ShowWindow()
    {
        var editor = GetWindow<GenerateItemRow>();
        editor.itemManager = new JsonBasedItemRepository();
        editor.loadedItems = editor.itemManager.GetItems();
        editor.Show();
    }

    public GameObject ItemPrefab;

    public string Name;
    public RavenNest.Models.ItemCategory ItemCategory;
    public RavenNest.Models.ItemType ItemType;
    public bool CanCraft;
    public bool Soulbound;

    [ShowIf("CanCraft")]
    public int CraftingLevel = 1000;

    [Button("Generate")]
    public void GenerateItem()
    {
        var item = new RavenNest.Models.Item();

        item.Id = Guid.NewGuid();
        item.Category = ItemCategory;
        item.Type = ItemType;
        item.IsGenericModel = ItemCategory != ItemCategory.Resource;

        if (Soulbound)
        {
            item.Soulbound = Soulbound;
        }

        if (ItemPrefab)
        {
            var path = AssetDatabase.GetAssetPath(ItemPrefab);
            item.GenericPrefab = path.Replace("Assets/Resources/", "").Replace(".prefab", "");
            item.Name = ItemPrefab.name;
        }
        else
        {
            item.Name = Name;
        }

        var str = Newtonsoft.Json.JsonConvert.SerializeObject(item);
        GUIUtility.systemCopyBuffer = str;
        UnityEngine.Debug.Log(str);
    }

    private JsonBasedItemRepository itemManager;
    private List<Item> loadedItems;

    //private string currencyId;
    //private string redeemableId;
    //private string cost;
    //public void OnGUI()
    //{
    //    if (loadedItems == null) return;


    //    GUILayout.Label("Redeemable ID or Name");
    //    redeemableId = GUILayout.TextField(redeemableId);

    //    GUILayout.Label("Currency ID or Name");
    //    currencyId = GUILayout.TextField(currencyId);

    //    GUILayout.Label("Cost");
    //    cost = GUILayout.TextField(cost);

    //    if (GUILayout.Button("Generate"))
    //    {
    //        if (!System.Guid.TryParse(redeemableId, out var redeemableIdGuid))
    //        {
    //            var item = loadedItems.FirstOrDefault(x => x.Name.Equals(redeemableId, StringComparison.OrdinalIgnoreCase));
    //            if (item == null) return;
    //            redeemableIdGuid = item.Id;
    //        }

    //        if (!System.Guid.TryParse(currencyId, out var currencyIdGuid))
    //        {
    //            var item = loadedItems.FirstOrDefault(x => x.Name.Equals(currencyId, StringComparison.OrdinalIgnoreCase));
    //            if (item == null) return;
    //            currencyIdGuid = item.Id;
    //        }

    //        var rowId = System.Guid.NewGuid().ToString();

    //        var str = string.Join("\t", new string[] {
    //                rowId,
    //                redeemableIdGuid.ToString(),
    //                currencyIdGuid.ToString(),
    //                cost,
    //                "1"
    //            });

    //        GUIUtility.systemCopyBuffer = str;
    //        UnityEngine.Debug.Log(str);
    //    }
    //}
}