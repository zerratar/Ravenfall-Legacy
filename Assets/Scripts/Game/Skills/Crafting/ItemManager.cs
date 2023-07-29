using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    private Dictionary<Guid, RavenNest.Models.Item> itemLookup = new Dictionary<Guid, Item>();

    private Dictionary<Guid, (int, int[])> femaleModelId = new Dictionary<Guid, (int, int[])>();
    private Dictionary<Guid, (int, int[])> maleModelId = new Dictionary<Guid, (int, int[])>();

    private Dictionary<string, GameObject> loadedItemPrefabs = new Dictionary<string, GameObject>();

    //private readonly object mutex = new object();
    private LoadingState state = LoadingState.None;

    [Header("Item Material Setup")]
    [SerializeField] private Material[] itemMaterials;
    [SerializeField] private RedeemableItem[] redeemableItems;

    private DateTime redeemablesLastUpdate;
    private TimeSpan redeemablesUpdateInterval = TimeSpan.FromMinutes(0.5);
    private bool updatingRedeemables;

    public bool TryGetPrefab(string path, out GameObject prefab)
    {
        return loadedItemPrefabs.TryGetValue(path, out prefab);
    }

    public ((int, int[]), (int, int[])) GetModelIndices(Guid itemId)
    {
        return (maleModelId[itemId], femaleModelId[itemId]);
    }

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
        this.itemLookup = items.ToDictionary(x => x.Id, x => x);
        this.state = LoadingState.Loaded;
    }

    public void SetRedeemableItems(IEnumerable<RavenNest.Models.RedeemableItem> redeemables)
    {
        this.redeemables = redeemables.ToList();
        this.redeemableItems = redeemables.Select(MapRedeemable).ToArray();
    }

    public Item Find(Func<Item, bool> predicate)
    {
        return items.FirstOrDefault(predicate);

    }
    public Item GetStreamerToken()
    {
        return items.FirstOrDefault(x => x.Category == ItemCategory.StreamerToken);
    }

    public IReadOnlyList<RedeemableItem> GetRedeemables()
    {
        var items = new List<RedeemableItem>();
        var date = DateTime.UtcNow;
        if (redeemableItems == null)
            return items;

        return redeemableItems;
    }

    public Material GetMaterial(int material)
    {
        return material >= 0 && itemMaterials.Length > material ? itemMaterials[material] : null;
    }

    public ItemController Create(Item item, bool useMalePrefab)
    {
        var itemController = Instantiate(baseItemPrefab).GetComponent<ItemController>();
        return itemController.Create(this, item, useMalePrefab);
    }
    public ItemController Create(GameInventoryItem item, bool useMalePrefab)
    {
        var itemController = Instantiate(baseItemPrefab).GetComponent<ItemController>();
        return itemController.Create(this, item, useMalePrefab);
    }

    public bool Loaded => state == LoadingState.Loaded;

    public IReadOnlyList<RavenNest.Models.RedeemableItem> GetRedeemableItems()
    {
        return redeemables;
    }

    public IReadOnlyList<RavenNest.Models.Item> GetItems()
    {
        return items;
    }

    private async void LoadItemsAsync()
    {
        state = LoadingState.Loading;

        this.redeemablesLastUpdate = DateTime.UtcNow;
        var loadedItems = await game.RavenNest.Items.GetAsync();
        var redeemableItems = await game.RavenNest.Items.GetRedeemablesAsync();

        items = loadedItems.ToList();
        itemLookup = items.ToDictionary(x => x.Id, x => x);
        Shinobytes.Debug.Log(items.Count + " items loaded!");
        game.Overlay.SendItems(items);
        if (redeemableItems != null && redeemableItems.Count > 0)
        {
            redeemables = redeemableItems.ToList();
            this.redeemableItems = redeemables.Select(MapRedeemable).ToArray();
            game.Overlay.SendRedeemables(redeemables);
        }

        Shinobytes.Debug.Log((redeemableItems?.Count ?? 0) + " redeemables loaded!");

        foreach (var item in items)
        {
            var FemaleModelID = -1;
            var MaleModelID = -1;
            int[] MaleAdditionalIndex = new int[0];
            int[] FemaleAdditionalIndex = new int[0];

            if (!string.IsNullOrEmpty(item.FemaleModelId))
            {
                if (item.FemaleModelId.Contains(","))
                {
                    var indices = item.FemaleModelId.Split(',');
                    FemaleModelID = int.Parse(indices[0]);
                    FemaleAdditionalIndex = indices.Skip(1).Select(int.Parse).ToArray();
                }
                else
                {
                    FemaleModelID = int.Parse(item.FemaleModelId);
                }
            }

            if (!string.IsNullOrEmpty(item.MaleModelId))
            {
                if (item.MaleModelId.Contains(","))
                {
                    var indices = item.MaleModelId.Split(',');
                    MaleModelID = int.Parse(indices[0]);
                    MaleAdditionalIndex = indices.Skip(1).Select(int.Parse).ToArray();
                }
                else
                {
                    MaleModelID = int.Parse(item.MaleModelId);
                }
            }

            femaleModelId[item.Id] = (FemaleModelID, FemaleAdditionalIndex);
            maleModelId[item.Id] = (MaleModelID, MaleAdditionalIndex);

            var Category = item.Category;
            var GenericPrefabPath = item.GenericPrefab;
            var MalePrefabPath = item.MalePrefab;
            var FemalePrefabPath = item.FemalePrefab;
            var IsGenericModel = item.IsGenericModel || Category == ItemCategory.Pet || !string.IsNullOrEmpty(GenericPrefabPath);

            if (!IsGenericModel && string.IsNullOrEmpty(FemalePrefabPath) && string.IsNullOrEmpty(MalePrefabPath))
            {
                continue;
            }

            if (IsGenericModel)
            {
                if (!loadedItemPrefabs.TryGetValue(GenericPrefabPath, out var prefab) || !prefab)
                {
                    prefab = UnityEngine.Resources.Load<GameObject>(GenericPrefabPath);
                    loadedItemPrefabs[GenericPrefabPath] = prefab;
                }
            }
            else
            {

                if (!string.IsNullOrEmpty(FemalePrefabPath) && (!loadedItemPrefabs.TryGetValue(FemalePrefabPath, out var fe) || !fe))
                {
                    loadedItemPrefabs[FemalePrefabPath] = UnityEngine.Resources.Load<GameObject>(FemalePrefabPath);
                }

                if (!string.IsNullOrEmpty(MalePrefabPath) && (!loadedItemPrefabs.TryGetValue(MalePrefabPath, out var ma) || !ma))
                {
                    loadedItemPrefabs[MalePrefabPath] = UnityEngine.Resources.Load<GameObject>(MalePrefabPath);
                }
            }
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
                this.redeemables = items.ToList();
                this.redeemableItems = redeemables.Select(MapRedeemable).ToArray();
                game.Overlay.SendRedeemables(redeemables);
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
            Name = Get(src.ItemId)?.Name,
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
        if (itemLookup.TryGetValue(id, out var value))
        {
            return value;
        }

        return null;
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

public struct DateRange
{
    public DateTime Start;
    public DateTime End;
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

    public DateRange GetRedeemableDateRange()
    {
        var min = DateTime.MinValue;
        var max = DateTime.MaxValue;

        var yStart = YearStart > 0 ? YearStart : min.Year;
        var yEnd = YearEnd > 0 ? YearEnd : max.Year;
        var mStart = MonthStart > 0 ? MonthStart : min.Month;
        var mEnd = MonthEnd > 0 ? MonthEnd : max.Month;
        var dStart = DayStart > 0 ? DayStart : min.Day;
        var dEnd = DayEnd > 0 ? DayEnd : max.Day;

        return new DateRange
        {
            Start = new DateTime(yStart, mStart, dStart),
            End = new DateTime(yEnd, mEnd, dEnd)
        };
    }

    public bool IsRedeemable()
    {
        var date = DateTime.UtcNow;
        if (YearStart > 0 || YearEnd > 0)
        {
            if ((YearEnd > 0 && date.Year > YearEnd) || date.Year < YearStart)
            {
                return false;
            }
        }

        if (MonthStart > 0 || MonthEnd > 0)
        {
            if ((MonthEnd > 0 && date.Month > MonthEnd) || date.Month < MonthStart)
            {
                return false;
            }
        }

        if (DayStart > 0 || DayEnd > 0)
        {
            if ((DayEnd > 0 && date.Day > DayEnd) || date.Day < DayStart)
            {
                return false;
            }
        }

        return true;
    }
}
public class MaterialProvider
{
    public static readonly MaterialProvider Instance = new MaterialProvider();
    private readonly Dictionary<ItemMaterial, Material> baseMaterials = new Dictionary<ItemMaterial, Material>();

    // make sure we can only use the Instance
    private MaterialProvider()
    {
    }

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