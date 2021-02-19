using System;
using System.Linq;
using UnityEngine;

public class ItemDropHandler : MonoBehaviour
{
    [SerializeField] private ItemDrop[] items;

    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
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
        do
        {
            var allItems = gameManager.Items.GetItems();

            var droppableItems = items.Select(x => 
            {
                RavenNest.Models.Item item = null;
                if (Guid.TryParse(x.ItemID, out var itemId))
                    item = allItems.FirstOrDefault(y => y.Id == itemId);
                else
                    item = allItems.FirstOrDefault(y => y.Name.IndexOf(x.ItemID, StringComparison.OrdinalIgnoreCase) >= 0);
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
    [Range(0.0001f, 1f)]
    public float DropChance;
}
