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
                if (redeemable.Year != 0 && redeemable.Year != date.Year)
                    continue;
                if (redeemable.Month != 0 && redeemable.Month != date.Month)
                    continue;
                if (redeemable.Day != 0 && redeemable.Day != date.Day)
                    continue;

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

        Debug.Log(items.Count + " items loaded!");
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
    public int Year;
    public int Month;
    public int Day;
}