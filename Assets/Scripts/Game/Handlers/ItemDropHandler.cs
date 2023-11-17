using System;
using System.Collections.Generic;
using RavenNest.Models;
using Shinobytes.Linq;
using UnityEngine;
using static UnityEngine.Networking.UnityWebRequest;

public class ItemDropHandler : MonoBehaviour
{
    [SerializeField] private ItemDropList dropList;
    [SerializeField] private ItemDrop[] items;
    [SerializeField] private float dropchanceScale = 1f;

    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }

    public void SetDropList(ItemDropList droplist)
    {
        dropList = droplist;
        items = dropList.Items;
    }


    public PlayerItemDropText DropItems(IEnumerable<PlayerController> players, DropType dropType)
    {
        //DropItem(player);
        // Does unity allow us to use C# 10?
        //Action<string, string[]> Announce = gameManager.RavenBot.Announce;
        var droppedItems = new Dictionary<string, List<string>>();
        foreach (var player in players)
        {
            var result = DropItem(player, dropType);
            if (result.Item != null)
            {
                var item = player.PickupItem(result.Item, false);
                var key = result.Item.Name;

                if (!droppedItems.TryGetValue(key, out var items))
                {
                    droppedItems[key] = (items = new List<string>());
                }

                items.Add(player.PlayerName);
            }
        }

        return new PlayerItemDropText(droppedItems, gameManager.ItemDropMessageSettings);
    }

    private AllocatedItemDrop DropItem(
        PlayerController player,
        DropType dropType = DropType.Maybe)
    {

        if (items == null || items.Length == 0)
        {
            return new AllocatedItemDrop
            {
                Player = player
            };
        }

        var dropChance = .25f;
        if (dropType == DropType.Guaranteed)
        {
            dropChance = 1f;
        }

        if (dropType == DropType.Higher)
        {
            dropChance = UnityEngine.Random.value >= 0.75 ? 1f : 0.75f;
        }

        var guaranteedDrop = dropType == DropType.Guaranteed;
        var dropitems = this.items.ToList();
        var invItems = player.Inventory.GetInventoryItems();

        var now = DateTime.UtcNow;
        var santaHat = "Santa Hat";
        var santaHatId = Guid.Parse("cfb510cb-7916-4b2c-a17f-6048f5c6b282");
        var existingSantaHat = invItems.FirstOrDefault(x => x.ItemId == santaHatId) != null;

        if (now.Year == 2023)
        {
            // special xmas extension 2023
            var start = new DateTime(2023, 1, 1);
            var stop = start.AddDays(14);

            AddMonthDrop(dropitems, start, stop, santaHat, santaHatId.ToString(), 0.05f, 0.0175f, existingSantaHat);
            AddMonthDrop(dropitems, start, stop, "Christmas Token", "061edf28-ca3f-4a00-992e-ba8b8a949631", 0.05f, 0.0175f);
        }

        AddMonthDrop(dropitems, 12, 1, santaHat, santaHatId.ToString(), 0.05f, 0.0175f, existingSantaHat);
        AddMonthDrop(dropitems, 12, 1, "Christmas Token", "061edf28-ca3f-4a00-992e-ba8b8a949631", 0.05f, 0.0175f);
        AddMonthDrop(dropitems, 10, 1, "Halloween Token", "91fc824a-0ede-4104-96d1-531cdf8d56a6", 0.05f, 0.0175f);

        var allItems = gameManager.Items.GetItems();
        var droppableItems = BuildDropList(player, allItems, dropitems);
        var item = droppableItems.Weighted(x => x.DropChance);
        if (UnityEngine.Random.value <= dropChance)
        {
            return item;
        }

        return new AllocatedItemDrop
        {
            Player = player
        };
    }

    private IReadOnlyList<AllocatedItemDrop> BuildDropList(PlayerController player, IReadOnlyList<Item> allItems, List<ItemDrop> dropitems)
    {
        // ensure we have the items in place.
        foreach (var x in dropitems)
        {
            if (x.Item == null || x.ItemID != x.Item.Id.ToString())
            {
                x.Item = allItems.FirstOrDefault(y =>
                    y.Name.Equals(x.ItemName ?? "", StringComparison.OrdinalIgnoreCase) ||
                    y.Name.Equals(x.ItemID, StringComparison.OrdinalIgnoreCase) ||
                    y.Id.ToString().ToLower() == x.ItemID.ToLower());

                if (x.Item == null && Guid.TryParse(x.ItemID, out var itemId))
                {
                    x.Item = allItems.FirstOrDefault(y => y.Id == itemId);
                }

                if (x.Item != null)
                {
                    x.ItemID = x.Item.Id.ToString();
                }
            }
        }

        return
            dropitems
            .Where(x => x.Item != null)
            .OrderBy(x => x.Item.ShopSellPrice)
            .Select((x, index) =>
        {
            var item = x.Item;
            // resources may be weighted higher if equipment is lower.

            var attackScale = Mathf.Min(1f, player.Stats.Attack.Level / (float)item.RequiredAttackLevel);
            var defenseScale = Mathf.Min(1f, player.Stats.Defense.Level / (float)item.RequiredDefenseLevel);
            var rangedScale = Mathf.Min(1f, player.Stats.Ranged.Level / (float)item.RequiredRangedLevel);
            var magicScale = Mathf.Min(1f, player.Stats.Magic.Level / (float)item.RequiredMagicLevel);
            var healingScale = Mathf.Min(1f, Mathf.Max(magicScale, (player.Stats.Healing.Level / (float)item.RequiredMagicLevel)));
            dropchanceScale *= Mathf.Clamp01(attackScale * defenseScale * rangedScale * magicScale * healingScale);

            var dropChance = x.DropChance * dropchanceScale;
            var a = Mathf.Min(1f, (dropitems.Count - index) / (float)dropitems.Count);
            var b = Mathf.Min(1f, player.Stats.Slayer.MaxLevel / (float)GameMath.MaxLevel);

            dropChance = Mathf.Max(dropChance * a, dropChance * b);

            return new AllocatedItemDrop
            {
                Item = item,
                Player = player,
                DropChance = dropChance,
                Unique = x.Unique,
            };
        })
        .ToList();
    }

    private void AddMonthDrop(List<ItemDrop> droplist, int monthStart, int monthsLength, string itemName, string itemId, float maxDropRate, float minDropRate, bool dropRateDecreased = false)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, monthStart, 1);
        var end = start.AddMonths(monthsLength);
        AddMonthDrop(droplist, start, end, itemName, itemId, maxDropRate, minDropRate, dropRateDecreased);
    }

    private static void AddMonthDrop(List<ItemDrop> droplist, DateTime start, DateTime end, string itemName, string itemId, float maxDropRate, float minDropRate, bool dropRateDecreased = false)
    {
        DateTime now = DateTime.UtcNow;
        if (now >= start && now < end)
        {
            var dropChance = now.Date == start || now.Date >= end.AddDays(-1)
                    ? maxDropRate
                    : Mathf.Lerp(minDropRate, maxDropRate, (float)((end - now) / (end - start)));

            droplist.Add(new ItemDrop
            {
                ItemName = itemName,
                ItemID = itemId,
                DropChance = dropRateDecreased ? (dropChance * 0.1f) : dropChance,
                Unique = dropRateDecreased,
            });
        }
    }

    struct AllocatedItemDrop
    {
        public PlayerController Player;
        public RavenNest.Models.Item Item;
        public bool Unique;
        public float DropChance;
    }
}
