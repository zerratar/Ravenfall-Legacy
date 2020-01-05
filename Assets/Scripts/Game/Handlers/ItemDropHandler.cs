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

    public void DropItem(PlayerController player, bool guaranteedDrop = false)
    {
        if (items == null || items.Length == 0)
        {
            return;
        }

        do
        {
            var allItems = gameManager.Items.GetItems();
            var droppableItems = items.Select(x =>
                new
                {
                    Item = allItems.FirstOrDefault(y => y.Id == Guid.Parse(x.ItemID)),
                    x.DropChance
                }).OrderByDescending(x => x.DropChance).ToList();

            foreach (var item in droppableItems)
            {
                //if (player.Stats.Attack.Level < item.Item.RequiredAttackLevel ||
                //    player.Stats.Defense.Level < item.Item.RequiredDefenseLevel)
                //    continue;

                if (UnityEngine.Random.value <= item.DropChance)
                {
                    player.PickupItem(item.Item);
                    return;
                }
            }
        } while (guaranteedDrop);
    }
}

[Serializable]
public class ItemDrop
{
    public string ItemID;
    [Range(0.0001f, 1f)]
    public float DropChance;
}
