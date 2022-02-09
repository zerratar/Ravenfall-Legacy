using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemDropHandler : MonoBehaviour
{
    [SerializeField] private ItemDropList dropList;
    [SerializeField] private ItemDrop[] items;

    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
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

        var guaranteedDrop = dropType == DropType.Guaranteed;

        var dropitems = this.items.ToList();
        var now = DateTime.UtcNow;

        var invItems = player.Inventory.GetInventoryItems();

        var santaHat = "Santa Hat";
        var santaHatId = Guid.Parse("cfb510cb-7916-4b2c-a17f-6048f5c6b282");
        var existingSantaHat = invItems.FirstOrDefault(x => x.ItemId == santaHatId) != null;
        AddMonthDrop(dropitems, now.Year == 2022 ? 1 : 12, 1, santaHat, santaHatId.ToString(), 0.05f, 0.0175f, existingSantaHat);
        AddMonthDrop(dropitems, now.Year == 2022 ? 1 : 12, 1, "Christmas Token", "061edf28-ca3f-4a00-992e-ba8b8a949631", 0.05f, 0.0175f);
        AddMonthDrop(dropitems, 10, now.Year == 2021 ? 2 : 1, "Halloween Token", "91fc824a-0ede-4104-96d1-531cdf8d56a6", 0.05f, 0.0175f);

        do
        {
            var allItems = gameManager.Items.GetItems();
            var droppableItems = dropitems.Select(x =>
            {
                RavenNest.Models.Item item = allItems.FirstOrDefault(y =>
                    y.Name.StartsWith(x.ItemName ?? "", StringComparison.OrdinalIgnoreCase) ||
                    y.Name.StartsWith(x.ItemID, StringComparison.OrdinalIgnoreCase) ||
                    y.Id.ToString().ToLower() == x.ItemID.ToLower());

                if (item == null && Guid.TryParse(x.ItemID, out var itemId))
                    item = allItems.FirstOrDefault(y => y.Id == itemId);

                return new
                {
                    Item = item,
                    x.DropChance,
                    x.Unique,
                };
            })
                .Where(x => x.Item != null)
                .OrderByDescending(x => x.DropChance).ToList();

            foreach (var item in droppableItems)
            {
                //if (player.Stats.Attack.Level < item.Item.RequiredAttackLevel ||
                //    player.Stats.Defense.Level < item.Item.RequiredDefenseLevel)
                //    continue;

                if (UnityEngine.Random.value <= item.DropChance)
                {
                    return new AllocatedItemDrop
                    {
                        Player = player,
                        Item = item.Item
                    };
                }
            }
        } while (guaranteedDrop);

        return new AllocatedItemDrop
        {
            Player = player
        };
    }

    private void AddMonthDrop(List<ItemDrop> droplist, int monthStart, int monthsLength, string itemName, string itemId, float maxDropRate, float minDropRate, bool dropRateDecreased = false)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, monthStart, 1);
        var end = start.AddMonths(monthsLength);
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
    }
}
