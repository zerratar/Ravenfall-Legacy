using RavenNest.Models;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;



public class ToggleTMProTypeWindow : OdinEditorWindow
{
    [MenuItem("Ravenfall/Tools/Toggle TMPro")]
    public static void ShowWindow()
    {
        var editor = GetWindow<ToggleTMProTypeWindow>();
        editor.Show();
    }

    public GameObject Source;
    public class PropertyInstance
    {
        public System.Reflection.PropertyInfo Property;
        public object PropertyValue;
    }

    private Dictionary<string, PropertyInstance> GetProperties(Type type, bool includeParent, params string[] propertiesToIgnore)
    {
        var props = type
            .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
            .ToList();

        var names = props.Select(x => new { Name = x.Name, Property = x });
        props = names.GroupBy(x => x.Name).Select(x => x.FirstOrDefault().Property).ToList();

        var thisDictionary = props.Where(x => !propertiesToIgnore.Contains(x.Name)).ToDictionary(p => p.Name, x => new PropertyInstance { Property = x });
        if (includeParent)
        {
            var parentDictionary = GetProperties(type.BaseType, false, propertiesToIgnore);

            foreach (var prop in parentDictionary)
            {
                thisDictionary.TryAdd(prop.Key, prop.Value);
            }
        }

        return thisDictionary;
    }

    private Dictionary<string, PropertyInstance> GetProperties<T>(bool includeParent, params string[] propertiesToIgnore)
    {
        return GetProperties(typeof(T), includeParent, propertiesToIgnore);
    }

    public void ReplaceComponents<TOld, TNew>(GameObject hierarchy, bool includeParent, params string[] propertiesToIgnore)
        where TOld : UnityEngine.MonoBehaviour
        where TNew : UnityEngine.MonoBehaviour
    {
        var components = hierarchy.transform.GetComponentsInChildren<TOld>();
        var srcFields = GetProperties<TOld>(includeParent, propertiesToIgnore);
        var dstFields = GetProperties<TNew>(includeParent, propertiesToIgnore);

        var objects = components.Select(x => x.transform.gameObject).ToList();

        // get property values
        foreach (var c in components)
        {
            foreach (var f in srcFields)
            {
                // some properties will throw an exception when accessed.
                try
                {
                    if (f.Value.Property.CanRead)
                        f.Value.PropertyValue = f.Value.Property.GetValue(c);
                }
                catch
                {
                    // ignored
                }
            }

            GameObject.DestroyImmediate(c);
        }

        // set property values
        foreach (var c in objects)
        {
            var newObj = c.AddComponent<TNew>();

            foreach (var f in srcFields)
            {
                if (dstFields.TryGetValue(f.Key, out var dst))
                {
                    try
                    {
                        if (dst.Property.CanWrite && f.Value.PropertyValue != null)
                            dst.Property.SetValue(newObj, dst.PropertyValue);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            //if (newObj is TMPro.TextMeshProUGUI ugui)
            //{
            //    if (srcFields.TryGetValue("text", out var txt))
            //    {
            //        ugui.text = txt.PropertyValue?.ToString();
            //    }
            //}

            //if (newObj is TMPro.TextMeshPro pro)
            //{
            //    if (srcFields.TryGetValue("text", out var txt))
            //    {
            //        pro.text = txt.PropertyValue?.ToString();
            //    }
            //}
        }
    }

    public void AddToComponents<TFind, TAdd>(GameObject hierarchy)
      where TFind : UnityEngine.Component
      where TAdd : UnityEngine.Component
    {
        var components = hierarchy.transform.GetComponentsInChildren<TFind>();
        foreach (var c in components)
        {
            var existing = c.transform.gameObject.GetComponent<TAdd>();
            if (existing)
            {
                continue;
            }

            c.transform.gameObject.AddComponent<TAdd>();
        }
    }
    public void DestroyComponents<TFind, TDestroy>(GameObject hierarchy)
      where TFind : UnityEngine.Component
      where TDestroy : UnityEngine.Component
    {
        var components = hierarchy.transform.GetComponentsInChildren<TFind>();
        foreach (var c in components)
        {
            var existing = c.transform.gameObject.GetComponent<TDestroy>();
            if (existing)
            {
                GameObject.DestroyImmediate(existing);
            }
        }
    }


    [ShowIf("Source")]
    [Button("Make into TMPro Text Mesh GUI (UI)")]
    public void ChangeToUI()
    {

        var meshRenderer = Source.GetComponent<MeshRenderer>();
        var old = Source.GetComponent<TMPro.TextMeshPro>();

        var text = old.text;
        var style = old.textStyle;
        var size = old.fontSize;
        var color = old.color;
        var alignment = old.alignment;
        var horAlign = old.horizontalAlignment;
        var vertAlign = old.verticalAlignment;

        var wrap = old.enableWordWrapping;

        GameObject.DestroyImmediate(old);
        GameObject.DestroyImmediate(meshRenderer);

        //var canvasRenderer = Source.AddComponent<CanvasRenderer>();
        var newText = Source.AddComponent<TMPro.TextMeshProUGUI>();

        newText.text = text;
        newText.textStyle = style;
        newText.fontSize = size / 10f;
        newText.color = color;
        newText.alignment = alignment;
        newText.horizontalAlignment = horAlign;
        newText.verticalAlignment = vertAlign;
        newText.enableWordWrapping = wrap;

        //AddToComponents<TMPro.TextMeshPro, CanvasRenderer>(Source);
        //ReplaceComponents<TMPro.TextMeshPro, TMPro.TextMeshProUGUI>(Source, true, "renderer", "mesh", "meshfilter", "name", "tag", "layer");
        //DestroyComponents<TMPro.TextMeshProUGUI, MeshRenderer>(Source);
    }

    //[ShowIf("Source")]
    //[Button("Make into TMPro Text Mesh (3D)")]
    //public void ChangeTo3D()
    //{
    //    ReplaceComponents<TMPro.TextMeshProUGUI, TMPro.TextMeshPro>(Source, "renderer");
    //}
}

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

        if (CanCraft)
        {
            item.Craftable = CanCraft;
            item.RequiredCraftingLevel = CraftingLevel;
        }
        else
        {
            item.RequiredCraftingLevel = 1000;
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