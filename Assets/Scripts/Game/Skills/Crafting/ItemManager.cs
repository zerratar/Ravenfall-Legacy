using System;
using System.Collections.Generic;
using System.Linq;
using RavenNest.Models;
using UnityEngine;

public enum LoadingState
{
    None,
    Loading,
    Loaded,
}

public class ItemManager : MonoBehaviour
{
    [SerializeField] private GameManager game;
    [SerializeField] private GameObject baseItemPrefab;

    private List<RavenNest.Models.Item> items = new List<Item>();
    private readonly object mutex = new object();
    private LoadingState state = LoadingState.None;

    [Header("Item Material Setup")]
    [SerializeField] private Material[] itemMaterials;

    [SerializeField] private RedeemableItem[] redeemableItems;

    void Start()
    {
        if (!game) game = GetComponent<GameManager>();
        if (!game) return;
        game.SetLoadingState("items", state);

        MaterialProvider.Instance.RegisterBaseMaterials(itemMaterials);
    }

    void Update()
    {
        if (game == null || game.RavenNest == null)
        {
            return;
        }

        if (state == LoadingState.None && game && game.RavenNest.SessionStarted)
        {
            LoadItemsAsync();
        }
    }


    public Item Find(Func<Item, bool> predicate)
    {
        lock (mutex)
        {
            return items.FirstOrDefault(predicate);
        }
    }
    public Item GetStreamerToken()
    {
        lock (mutex)
        {
            return items.FirstOrDefault(x => x.Category == ItemCategory.StreamerToken);
        }
    }

    public IReadOnlyList<RedeemableItem> Redeemable
    {
        get
        {
            var items = new List<RedeemableItem>();
            var date = DateTime.UtcNow;
            if (redeemableItems == null)
                return items;

            foreach (var redeemable in redeemableItems)
            {
                if (redeemable.YearStart > 0 || redeemable.YearEnd > 0)
                {
                    if ((redeemable.YearEnd > 0 && date.Year > redeemable.YearEnd) || date.Year < redeemable.YearStart)
                    {
                        continue;
                    }
                }

                if (redeemable.MonthStart > 0 || redeemable.MonthEnd > 0)
                {
                    if ((redeemable.MonthEnd > 0 && date.Month > redeemable.MonthEnd) || date.Month < redeemable.MonthStart)
                    {
                        continue;
                    }
                }

                if (redeemable.DayStart > 0 || redeemable.DayEnd > 0)
                {
                    if ((redeemable.DayEnd > 0 && date.Day > redeemable.DayEnd) || date.Day < redeemable.DayStart)
                    {
                        continue;
                    }
                }

                items.Add(redeemable);
            }
            return items;
        }
    }

    public Material GetMaterial(int material)
    {
        return material >= 0 && itemMaterials.Length > material ? itemMaterials[material] : null;
    }

    public ItemController Create(Item item, bool useMalePrefab)
    {
        var itemController = Instantiate(baseItemPrefab).GetComponent<ItemController>();
        return itemController.Create(item, useMalePrefab);
    }

    public bool Loaded => state == LoadingState.Loaded;

    public RavenNest.Models.Item GetItem(Guid itemId)
    {
        lock (mutex) return items.FirstOrDefault(x => x.Id == itemId);
    }

    public IReadOnlyList<RavenNest.Models.Item> GetItems()
    {
        lock (mutex)
        {
            return items;
        }
    }

    private async void LoadItemsAsync()
    {
        state = LoadingState.Loading;

        var loadedItems = await game.RavenNest.Items.GetAsync();

        lock (mutex)
        {
            items = loadedItems.ToList();
        }

        state = LoadingState.Loaded;
        game.SetLoadingState("items", state);

        Shinobytes.Debug.Log(items.Count + " items loaded!");
    }
    public Item Get(Guid id)
    {
        return items.FirstOrDefault(x => x.Id == id);
    }
}

[Serializable]
public struct RedeemableItem
{
    public Guid ItemId;
    public string Name;
    public int Cost;
    public int YearStart;
    public int YearEnd;
    public int MonthStart;
    public int MonthEnd;
    public int DayStart;
    public int DayEnd;
}
public class MaterialProvider
{
    public static readonly MaterialProvider Instance = new MaterialProvider();
    private readonly Dictionary<ItemMaterial, Material> baseMaterials = new Dictionary<ItemMaterial, Material>();

    internal void RegisterBaseMaterials(Material[] itemMaterials)
    {
        var materialTypes = Enum
            .GetValues(typeof(RavenNest.Models.ItemMaterial))
            .Cast<RavenNest.Models.ItemMaterial>()
            .ToArray();

        for (var i = 0; i < materialTypes.Length; ++i)
        {
            if (i < itemMaterials.Length)
            {
                RegisterMaterial(materialTypes[i], itemMaterials[i]);
            }
        }
    }
    private void RegisterMaterial(ItemMaterial itemMaterial, Material material)
    {
        baseMaterials[itemMaterial] = material;
    }
}