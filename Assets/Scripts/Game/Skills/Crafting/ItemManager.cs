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