using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RavenNest.Models;
using System;
public class HatMaker : MonoBehaviour
{
#if UNITY_EDITOR
    [Button("Generate")]
    private void GenerateItemJson()
    {
        var path = UnityEditor.AssetDatabase.GetAssetPath(this).Replace("Assets\\Resources\\", "").Replace("Assets/Resources/", "").Replace(".prefab", "");
        var targetDirectory = @"C:\Ravenfall\generated-hat-jsons";

        var itemName = this.name;

        var item = new Item
        {
            Id = Guid.NewGuid(),
            Name = itemName,
            Type = ItemType.Helmet,
            GenericPrefab = path,
            IsGenericModel = true,
            ShopSellPrice = 1,
            ShopBuyPrice = 1,
            Category = ItemCategory.Armor,
        };

        System.IO.File.WriteAllText(System.IO.Path.Combine(targetDirectory, itemName + ".json"), Newtonsoft.Json.JsonConvert.SerializeObject(item));

        Shinobytes.Debug.Log(path);
    }
#endif

}
