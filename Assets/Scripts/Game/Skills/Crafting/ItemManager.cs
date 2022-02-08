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

    // should have been a dictionary, but since we do so many different types of lookups
    // not just by ID, it was more a bother than what it would help. We will sacrifice the performance
    // to make it easier for ourselves here.
    private List<RavenNest.Models.Item> items = new List<Item>();
    private List<RavenNest.Models.RedeemableItem> redeemables = new List<RavenNest.Models.RedeemableItem>();

    private readonly object mutex = new object();
    private LoadingState state = LoadingState.None;

    [Header("Item Material Setup")]
    [SerializeField] private Material[] itemMaterials;
    [SerializeField] private RedeemableItem[] redeemableItems;

    private DateTime redeemablesLastUpdate;
    private TimeSpan redeemablesUpdateInterval = TimeSpan.FromMinutes(0.5);
    private bool updatingRedeemables;

    void Start()
    {
        MaterialProvider.Instance.RegisterBaseMaterials(itemMaterials);

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

        var now = DateTime.UtcNow;
        if (now - this.redeemablesLastUpdate >= redeemablesUpdateInterval)
        {
            UpdateRedeemablesAsync();
        }
    }

    public void SetItems(IEnumerable<RavenNest.Models.Item> items)
    {
        this.items = items.ToList();
        this.state = LoadingState.Loaded;
    }

    public void SetRedeemableItems(IEnumerable<RavenNest.Models.RedeemableItem> redeemables)
    {
        this.redeemables = redeemables.ToList();
        this.redeemableItems = redeemables.Select(MapRedeemable).ToArray();
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

    public IReadOnlyList<RedeemableItem> GetRedeemables()
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

    public Material GetMaterial(int material)
    {
        return material >= 0 && itemMaterials.Length > material ? itemMaterials[material] : null;
    }

    public ItemController Create(Item item, bool useMalePrefab)
    {
        var itemController = Instantiate(baseItemPrefab).GetComponent<ItemController>();
        return itemController.Create(item, useMalePrefab);
    }
    public ItemController Create(GameInventoryItem item, bool useMalePrefab)
    {
        var itemController = Instantiate(baseItemPrefab).GetComponent<ItemController>();
        return itemController.Create(item, useMalePrefab);
    }

    public bool Loaded => state == LoadingState.Loaded;

    public RavenNest.Models.Item GetItem(Guid itemId)
    {
        lock (mutex) return items.FirstOrDefault(x => x.Id == itemId);
    }

    public IReadOnlyList<RavenNest.Models.RedeemableItem> GetRedeemableItems()
    {
        lock (mutex)
        {
            return redeemables;
        }
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

        this.redeemablesLastUpdate = DateTime.UtcNow;
        var loadedItems = await game.RavenNest.Items.GetAsync();
        var redeemableItems = await game.RavenNest.Items.GetRedeemablesAsync();

        lock (mutex)
        {
            items = loadedItems.ToList();
            Shinobytes.Debug.Log(items.Count + " items loaded!");
            game.Overlay.SendItems(items);
            if (redeemableItems != null && redeemableItems.Count > 0)
            {
                redeemables = redeemableItems.ToList();
                this.redeemableItems = redeemables.Select(MapRedeemable).ToArray();
                game.Overlay.SendRedeemables(redeemables);
            }

            Shinobytes.Debug.Log((redeemableItems?.Count ?? 0) + " redeemables loaded!");
        }

        state = LoadingState.Loaded;
        game.SetLoadingState("items", state);
    }


    public async void UpdateRedeemablesAsync()
    {
        if (updatingRedeemables)
        {
            return;
        }

        this.redeemablesLastUpdate = DateTime.UtcNow;
        this.updatingRedeemables = true;

        try
        {
            var items = await game.RavenNest.Items.GetRedeemablesAsync();
            if (items != null && items.Count > 0)
            {
                lock (mutex)
                {
                    this.redeemables = items.ToList();
                    this.redeemableItems = redeemables.Select(MapRedeemable).ToArray();
                    game.Overlay.SendRedeemables(redeemables);
                }
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to update redeemable items. " + exc);
        }

        this.updatingRedeemables = false;
    }


    private RedeemableItem MapRedeemable(RavenNest.Models.RedeemableItem src)
    {
        var startDate = new Date();
        var endDate = new Date();

        if (!string.IsNullOrEmpty(src.AvailableDateRange))
        {
            if (src.AvailableDateRange.Contains("=>"))
            {
                var data = src.AvailableDateRange.Split("=>");
                startDate = Parse(data[0]?.Trim());
                endDate = Parse(data[1]?.Trim());
            }
            else
            {
                startDate = Parse(src.AvailableDateRange.Trim());
            }
        }

        return new RedeemableItem
        {
            ItemId = src.ItemId,
            Cost = src.Cost,
            Name = GetItem(src.ItemId)?.Name,
            YearStart = startDate.Year,
            MonthStart = startDate.Month,
            DayStart = startDate.Day,
            YearEnd = endDate.Year,
            MonthEnd = endDate.Month,
            DayEnd = endDate.Day,
        };
    }

    public Item Get(Guid id)
    {
        return items.FirstOrDefault(x => x.Id == id);
    }

    private Date Parse(string str)
    {
        int year = 0;
        int month = 0;
        int day = 0;

        if (!string.IsNullOrEmpty(str))
        {
            var strData = str.Split('-');
            if (strData.Length > 0) int.TryParse(strData[0], out year);
            if (strData.Length > 1) int.TryParse(strData[1], out month);
            if (strData.Length > 2) int.TryParse(strData[2], out day);
        }

        return new Date
        {
            Year = year,
            Month = month,
            Day = day
        };
    }

    private struct Date
    {
        public int Year;
        public int Month;
        public int Day;
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