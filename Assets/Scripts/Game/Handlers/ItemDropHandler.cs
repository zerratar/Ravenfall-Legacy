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

    public void DropItem(PlayerController player,
        DropType dropType = DropType.Standard,
        string messageStart = "You found")
    {
        if (items == null || items.Length == 0)
        {
            return;
        }
        var guaranteedDrop = dropType == DropType.MagicRewardGuaranteed || dropType == DropType.StandardGuaranteed;

        var dropitems = this.items.ToList();
        var now = DateTime.UtcNow;
        //AddTimelyMonthDrop(10, 1, "Halloween Token");
        AddMonthDrop(dropitems, 12, 1, "Christmas Token", "061edf28-ca3f-4a00-992e-ba8b8a949631", 0.05f, 0.0175f);
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
                    x.DropChance
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
                    player.PickupItem(item.Item, messageStart);
                    return;
                }
            }
        } while (guaranteedDrop);
    }

    private void AddMonthDrop(List<ItemDrop> droplist, int monthStart, int monthsLength, string itemName, string itemId, float maxDropRate, float minDropRate)
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
                DropChance = dropChance,
            });
        }
    }
}

public enum DropType
{
    Standard,
    StandardGuaranteed,
    MagicReward,
    MagicRewardGuaranteed
}

[Serializable]
public class ItemDrop
{
    public string ItemID;
    public string ItemName;

    [Range(0.0001f, 1f)]
    public float DropChance;
}
