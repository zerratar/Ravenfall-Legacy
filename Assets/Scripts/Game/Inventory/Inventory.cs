using System;
using System.Collections.Generic;
using System.Linq;
using RavenNest.Models;
using UnityEngine;

public class GameInventoryItem
{
    public RavenNest.Models.Item Item { get; set; }
    public decimal Amount { get; set; }
}

public class Inventory : MonoBehaviour
{
    private List<GameInventoryItem> backpack;
    private List<Item> equipped;
    private PlayerController player;
    private PlayerEquipment equipment;

    private void Awake()
    {
        backpack = new List<GameInventoryItem>();
        equipped = new List<Item>();
        player = GetComponent<PlayerController>();
        equipment = GetComponent<PlayerEquipment>();
    }

    public void Remove(Item item, decimal amount)
    {
        var target = backpack.FirstOrDefault(x => x.Item.Id == item.Id);
        if (target != null)
        {
            target.Amount -= amount;
            if (target.Amount <= 0)
            {
                backpack.Remove(target);
            }
        }
    }

    public void Create(IReadOnlyList<InventoryItem> inventoryItems, IReadOnlyList<RavenNest.Models.Item> availableItems)
    {
        backpack.Clear();

        foreach (var item in inventoryItems)
        {
            var loadedItem = availableItems.FirstOrDefault(x => x.Id == item.ItemId);
            if (loadedItem == null) continue;
            if (item.Equipped)
            {

                Equip(loadedItem, false);
            }
            else
            {
                backpack.Add(new GameInventoryItem
                {
                    Item = loadedItem,
                    Amount = item.Amount
                });
            }
        }
    }

    public void Add(RavenNest.Models.Item item, decimal amount = 1)
    {
        var existing = backpack.FirstOrDefault(x => x.Item.Id == item.Id);
        if (existing != null)
        {
            existing.Amount += amount;
        }
        else
        {
            backpack.Add(new GameInventoryItem
            {
                Item = item,
                Amount = amount
            });
        }
    }

    public void Equip(Item item, bool updateAppearance = true)
    {
        var equip = GetEquipmentOfType(item.Category, item.Type);
        if (equip != null)
        {
            Add(equip);
            equipped.Remove(equip);
            equipment.Unequip(equip.Id);
        }

        equipped.Add(item);
        Remove(item, 1);

        if (updateAppearance)
        {
            equipment.EquipAll(equipped);
        }
        else
        {
            equipment.Equip(item);
        }

        player.UpdateCombatStats(equipped);
    }

    public RavenNest.Models.Item GetEquipmentOfCategory(ItemCategory itemCategory)
    {
        return equipped.FirstOrDefault(x => x.Category == itemCategory);
    }

    public IReadOnlyList<GameInventoryItem> GetInventoryItemsOfType(ItemCategory itemCategory, RavenNest.Models.ItemType type)
    {
        return backpack.Where(x => x.Item.Category == itemCategory && x.Item.Type == type).ToList();
    }

    internal IReadOnlyList<GameInventoryItem> GetInventoryItems(Guid itemId)
    {
        return backpack.Where(x => x.Item.Id == itemId).ToList();
    }

    public RavenNest.Models.Item GetEquipmentOfType(ItemCategory itemCategory, RavenNest.Models.ItemType type)
    {
        if (itemCategory == ItemCategory.Weapon)
        {
            return GetEquipmentOfCategory(ItemCategory.Weapon);
        }

        return equipped.FirstOrDefault(x => x.Category == itemCategory && x.Type == type);
    }


    public void EquipBestItems()
    {
        EquipBestItem(ItemCategory.Amulet);
        EquipBestItem(ItemCategory.Ring);
        EquipBestItem(ItemCategory.Weapon);

        EquipBestItem(ItemCategory.Armor, ItemType.Helm);
        EquipBestItem(ItemCategory.Armor, ItemType.Leggings);
        EquipBestItem(ItemCategory.Armor, ItemType.Gloves);
        EquipBestItem(ItemCategory.Armor, ItemType.LeftShoulder);
        EquipBestItem(ItemCategory.Armor, ItemType.RightShoulder);
        EquipBestItem(ItemCategory.Armor, ItemType.Boots);
        EquipBestItem(ItemCategory.Armor, ItemType.Chest);
        EquipBestItem(ItemCategory.Armor, ItemType.Shield);

        equipment.EquipAll(equipped);
    }

    private void EquipBestItem(ItemCategory category, ItemType? type = null)
    {
        var equippedItem = type != null
            ? GetEquipmentOfType(category, type.Value)
            : GetEquipmentOfCategory(category);

        var bestValue = equippedItem != null
            ? equippedItem.WeaponPower + equippedItem.WeaponAim + equippedItem.ArmorPower
            : 0;

        RavenNest.Models.Item bestItem = null;
        foreach (var item in type != null
            ? backpack.Where(x => x.Item.Category == category && x.Item.Type == type)
            : backpack.Where(x => x.Item.Category == category))
        {
            var itemValue = item.Item.WeaponPower + item.Item.WeaponAim + item.Item.ArmorPower;
            var canEquip = player.Stats.Defense.CurrentValue >= item.Item.RequiredDefenseLevel &&
                           player.Stats.Attack.CurrentValue >= item.Item.RequiredAttackLevel;

            if (equippedItem != null && equippedItem.Name == item.Item.Name) continue;

            if (bestValue < itemValue && canEquip)
            {
                bestValue = itemValue;
                bestItem = item.Item;
            }
        }

        if (bestItem != null)
        {
            Equip(bestItem, false);
        }
    }

    public void UpdateAppearance()
    {
        equipment.EquipAll(equipped);
    }

    private class ItemComparer : IComparer<RavenNest.Models.Item>
    {
        public int Compare(RavenNest.Models.Item item1, RavenNest.Models.Item item)
        {
            var stats1 = item1.WeaponAim + item1.WeaponPower + item1.ArmorPower;
            var stats2 = item.WeaponAim + item.WeaponPower + item.ArmorPower;
            return stats1 - stats2;
        }
    }
}