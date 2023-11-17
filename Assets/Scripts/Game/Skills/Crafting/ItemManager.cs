using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

    private DateTime itemsLastUpdate;
    private TimeSpan itemsUpdateInterval = TimeSpan.FromMinutes(20);
    private List<ItemRecipe> recipes;
    private List<ResourceItemDrop> resourceDrops;

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
        //if (now - this.redeemablesLastUpdate >= redeemablesUpdateInterval)
        //{
        //    UpdateRedeemablesAsync();
        //}
        if (state != LoadingState.Loading && now - this.itemsLastUpdate >= itemsUpdateInterval)
        {
            //UpdateItemsAsync();
            LoadItemsAsync();
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

    public void SetItemRecipes(IEnumerable<RavenNest.Models.ItemRecipe> recipes)
    {
        this.recipes = recipes.ToList();
    }

    public ItemRecipe GetItemRecipe(Guid itemId)
    {
        return recipes.FirstOrDefault(x => x.ItemId == itemId);
    }

    public ItemRecipe GetItemRecipe(Item item)
    {
        return recipes.FirstOrDefault(x => x.ItemId == item.Id);
    }

    public Item Find(Func<Item, bool> predicate)
    {
        return items.FirstOrDefault(predicate);

    }
    public Item GetStreamerToken()
    {
        return items.FirstOrDefault(x => x.Category == ItemCategory.StreamerToken);
    }

    /// <summary>
    ///     Gets wether or not the target item is dropped by training a skill.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanBeDropped(Item item)
    {
        return resourceDrops.Any(x => x.ItemId == item.Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetRequiredLevelForDrop(Item item)
    {
        return resourceDrops.FirstOrDefault(x => x.ItemId == item.Id)?.LevelRequirement ?? -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResourceItemDrop GetResourceDrop(Guid itemId)
    {
        return resourceDrops.FirstOrDefault(x => x.ItemId == itemId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResourceItemDrop GetResourceDrop(Item item)
    {
        return GetResourceDrop(item.Id);
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
        if (state != LoadingState.Loaded)
            state = LoadingState.Loading;

        try
        {
            await DownloadItemsAsync();
            await DownloadItemRecipesAsync();
            await DownloadRedeemableItemsAsync();
            await DownloadItemResourceDropsAsync();

            // no need to set this status multiple times, after first time we load this data, should be ok.
            if (state != LoadingState.Loaded)
            {
                state = LoadingState.Loaded;
                game.SetLoadingState("items", state);
            }
        }
        catch (Exception exc)
        {
            // ignore, this only happens when connectio nto server fails.
        }
    }
    private async Task DownloadItemResourceDropsAsync()
    {
        ResourceItemDropCollection resourceDrops = await game.RavenNest.Items.GetResourceDropsAsync();
        if (resourceDrops != null && resourceDrops.Count > 0)
        {
            this.resourceDrops = resourceDrops.ToList();
        }
    }
    private async Task DownloadRedeemableItemsAsync()
    {
        var redeemableItems = await game.RavenNest.Items.GetRedeemablesAsync();
        if (redeemableItems != null && redeemableItems.Count > 0)
        {
            redeemables = redeemableItems.ToList();
            this.redeemableItems = redeemables.Select(MapRedeemable).ToArray();
            game.Overlay.SendRedeemables(redeemables);
        }
    }

    private async Task DownloadItemRecipesAsync()
    {
        var recipes = await game.RavenNest.Items.GetRecipesAsync();
        if (recipes != null && recipes.Count > 0)
        {
            this.recipes = recipes.ToList();
        }
    }

    private async Task DownloadItemsAsync()
    {
        this.itemsLastUpdate = DateTime.UtcNow;

        if (items != null && items.Count > 0)
        {
            var lastModified = items.Max(x => x.Modified.GetValueOrDefault());
            var deltas = await game.RavenNest.Items.GetDeltaAsync(lastModified);
            if (deltas == null || deltas.Count == 0)
            {
                this.itemsLastUpdate = this.itemsLastUpdate.AddMinutes(-(itemsUpdateInterval.TotalMinutes * 0.5));
                return;
            }
            foreach (var d in deltas)
            {
                var existing = items.FirstOrDefault(x => x.Id == d.Id);
                if (existing != null)
                {
                    UpdateItem(existing, d);
                }
                else
                {
                    items.Add(d);
                }
            }
        }
        else
        {
            var loadedItems = await game.RavenNest.Items.GetAsync();
            items = loadedItems.ToList();
        }

        // rebuild lookups
        itemLookup = items.ToDictionary(x => x.Id, x => x);
        game.Overlay.SendItems(items);

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
    }

    private void UpdateItem(Item existing, Item updatedItem)
    {
        //existing.Id = updatedItem.Id;
        existing.Name = updatedItem.Name;
        existing.Description = updatedItem.Description;
        existing.Level = updatedItem.Level;
        existing.WeaponAim = updatedItem.WeaponAim;
        existing.WeaponPower = updatedItem.WeaponPower;
        existing.MagicAim = updatedItem.MagicAim;
        existing.MagicPower = updatedItem.MagicPower;
        existing.RangedAim = updatedItem.RangedAim;
        existing.RangedPower = updatedItem.RangedPower;
        existing.ArmorPower = updatedItem.ArmorPower;
        existing.RequiredAttackLevel = updatedItem.RequiredAttackLevel;
        existing.RequiredDefenseLevel = updatedItem.RequiredDefenseLevel;
        existing.RequiredMagicLevel = updatedItem.RequiredMagicLevel;
        existing.RequiredRangedLevel = updatedItem.RequiredRangedLevel;
        existing.RequiredSlayerLevel = updatedItem.RequiredSlayerLevel;
        existing.Category = updatedItem.Category;
        existing.Type = updatedItem.Type;
        existing.Material = updatedItem.Material;
        existing.MaleModelId = updatedItem.MaleModelId;
        existing.FemaleModelId = updatedItem.FemaleModelId;
        existing.GenericPrefab = updatedItem.GenericPrefab;
        existing.MalePrefab = updatedItem.MalePrefab;
        existing.FemalePrefab = updatedItem.FemalePrefab;
        existing.IsGenericModel = updatedItem.IsGenericModel;
        existing.ShopBuyPrice = updatedItem.ShopBuyPrice;
        existing.ShopSellPrice = updatedItem.ShopSellPrice;
        existing.Soulbound = updatedItem.Soulbound;
        existing.Modified = updatedItem.Modified;
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
            Currency = Get(src.CurrencyItemId)?.Name,
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

    internal IReadOnlyList<ItemRecipe> GetItemRecipes()
    {
        return this.recipes;
    }

    internal ItemRecipe GetRecipeWithSingleIngredient(Item item)
    {
        return this.recipes.FirstOrDefault(x => x.Ingredients.Count == 1 && x.Ingredients[0].ItemId == item.Id && x.Ingredients[0].Amount <= 1);
    }

    private struct Date
    {
        public int Year;
        public int Month;
        public int Day;
    }
}

[Serializable]
public struct DateRange
{
    public DateTime Start;
    public DateTime End;

    /// <summary>
    ///     Gets a collection of the dates in the date range.
    /// </summary>
    public IList<DateTime> Dates
    {
        get
        {
            var startDate = Start;

            return Enumerable.Range(0, Days)
                .Select(offset => startDate.AddDays(offset))
                .ToList();
        }
    }

    /// <summary>
    ///     Gets the number of whole days in the date range.
    /// </summary>
    public int Days
    {
        get { return (int)((End - Start).TotalDays + 1); }
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
    public string Currency;

    public DateRange GetRedeemableDateRange()
    {
        var min = DateTime.UnixEpoch;
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