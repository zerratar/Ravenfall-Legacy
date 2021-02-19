using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RavenNest.Models;
using UnityEngine;

public class GameInventoryItem
{
    public RavenNest.Models.Item Item { get; set; }
    public string Tag { get; set; }
    public decimal Amount { get; set; }
}

public class Inventory : MonoBehaviour
{
    private List<GameInventoryItem> backpack = new List<GameInventoryItem>();
    private List<Item> equipped = new List<Item>();
    private PlayerController player;
    private PlayerEquipment equipment;
    private GameManager gameManager;

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        player = GetComponent<PlayerController>();
        equipment = GetComponent<PlayerEquipment>();
    }

    public void Remove(Item item, decimal amount, bool removeEquipped = false)
    {
        if (removeEquipped)
        {
            var eqItem = equipped.FirstOrDefault(x => x.Id == item.Id);
            if (eqItem != null)
            {
                equipped.Remove(eqItem);
                EquipAll();
                return;
            }
        }

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
                    Amount = item.Amount,
                    Tag = item.Tag
                });
            }
        }

        equipment.EquipAll(equipped);
    }

    internal long RemoveScroll(RavenNest.Models.ScrollType scroll)
    {
        var scrolls = GetInventoryItemsOfCategory(ItemCategory.Scroll);
        var s = scrolls.FirstOrDefault(x => x.Item.Name.IndexOf(scroll.ToString(), StringComparison.OrdinalIgnoreCase) >= 0);
        if (s != null)
        {
            if (--s.Amount <= 0)
            {
                backpack.Remove(s);
            }

            return (long)s.Amount;
        }
        return 0L;
    }

    public void Add(RavenNest.Models.Item item, decimal amount = 1)
    {
        if (item == null)
        {
            return;
        }

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
                Amount = amount,
            });
        }
    }

    internal void AddStreamerTokens(int amount)
    {
        var token = gameManager.Items.GetStreamerToken();
        if (token != null)
        {
            Add(token, amount);
        }
    }

    internal void RemoveStreamerTokens(int result)
    {
        var token = GetInventoryItemsOfCategory(ItemCategory.StreamerToken).FirstOrDefault();
        if (token != null)
        {
            Remove(token.Item, result);
        }
    }

    public void EquipAll()
    {
        equipment.EquipAll(equipped);
    }

    public void Unequip(Item item)
    {
        var targetItem = this.equipped.FirstOrDefault(x => x.Id == item.Id);
        if (targetItem != null)
        {
            Add(targetItem);
            equipped.Remove(targetItem);
            equipment.Unequip(targetItem.Id);
        }
    }

    public void Unequip(ItemCategory category, ItemType type)
    {
        var equip = GetEquipmentOfType(category, type);
        if (equip != null)
        {
            Add(equip);
            equipped.Remove(equip);
            equipment.Unequip(equip.Id);
        }
    }

    public void Equip(ItemController i, bool updateAppearance = true)
    {
        var item = gameManager.Items.Get(i.Id);
        Equip(item, updateAppearance);
    }

    public void Equip(Guid itemId, bool updateAppearance = true)
    {
        var item = gameManager.Items.Get(itemId);
        Equip(item, updateAppearance);
    }

    public void Equip(Item item, bool updateAppearance = true)
    {
        if (item == null || !player || player == null || !player.transform || player.transform == null)
            return;

        player.transform.localScale = Vector3.one;

        if (item.Type == ItemType.TwoHandedAxe || item.Type == ItemType.TwoHandedSword)
        {
            var shield = GetEquipmentOfType(ItemCategory.Armor, ItemType.Shield);
            if (shield != null)
            {
                Add(shield);
                equipped.Remove(shield);
                equipment.Unequip(shield.Id);
            }
        }

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
        player.transform.localScale = player.TempScale;
    }

    public void UpdateCombatStats()
    {
        player.UpdateCombatStats(equipped);
    }

    public RavenNest.Models.Item GetEquipmentOfCategory(ItemCategory itemCategory)
    {
        // always default to melee weapon when no type is used.
        if (itemCategory == ItemCategory.Weapon)
        {
            return equipped.FirstOrDefault(IsMeleeWeapon);
        }
        return equipped.FirstOrDefault(x => x.Category == itemCategory);
    }

    public IReadOnlyList<RavenNest.Models.Item> GetEquipmentsOfCategory(ItemCategory itemCategory)
    {
        return equipped.Where(x => x.Category == itemCategory).ToList();
    }

    public IReadOnlyList<GameInventoryItem> GetInventoryItemsOfType(ItemCategory itemCategory, RavenNest.Models.ItemType type)
    {
        return backpack.Where(x => x.Item.Category == itemCategory && x.Item.Type == type).ToList();
    }

    public IReadOnlyList<GameInventoryItem> GetInventoryItemsOfCategory(ItemCategory itemCategory)
    {
        return backpack.Where(x => x.Item.Category == itemCategory).ToList();
    }

    internal IReadOnlyList<InventoryItem> GetInventoryItems()
    {
        var i = new List<InventoryItem>();
        foreach (var eq in equipped)
        {
            i.Add(new InventoryItem
            {
                Amount = 1,
                Equipped = true,
                Id = Guid.NewGuid(),
                ItemId = eq.Id
            });
        }
        foreach (var eq in backpack)
        {
            i.Add(new InventoryItem
            {
                Amount = (long)eq.Amount,
                Equipped = false,
                Id = Guid.NewGuid(),
                ItemId = eq.Item.Id
            });
        }
        return i;
    }
    internal IReadOnlyList<GameInventoryItem> GetInventoryItems(Guid itemId)
    {
        return backpack.Where(x => x.Item.Id == itemId).ToList();
    }

    public Item GetEquippedItem(Guid itemId)
    {
        return equipped.FirstOrDefault(x => x.Id == itemId);
    }

    public RavenNest.Models.Item GetEquipmentOfType(ItemCategory itemCategory, RavenNest.Models.ItemType type)
    {
        if (itemCategory == ItemCategory.Weapon && IsMeleeWeapon(type))
        {
            return equipped.FirstOrDefault(IsMeleeWeapon);
        }

        return equipped.FirstOrDefault(x => x.Category == itemCategory && x.Type == type);
    }

    public RavenNest.Models.Item GetMeleeWeapon()
    {
        return equipped.FirstOrDefault(IsMeleeWeapon);
    }

    public void UnequipArmor()
    {
        Unequip(ItemCategory.Amulet);
        Unequip(ItemCategory.Armor);

        equipment.UpdateAppearance();

        StartCoroutine(DestroyArmorMesh());
    }


    internal void UnequipAll()
    {
        var e = equipped.ToList();
        foreach (var i in e)
        {
            Unequip(i);
        }

        equipment.UpdateAppearance();
        StartCoroutine(DestroyArmorMesh());
    }

    private IEnumerator DestroyArmorMesh()
    {
        // force destroy the combined mesh,
        // since it doesnt get uncombined for some reason.
        for (var i = 0; i < 10; ++i)
        {
            if (equipment.DestroyArmorMesh())
                yield break;

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Unequip(ItemCategory category)
    {
        var equippedItems = GetEquipmentsOfCategory(category);
        foreach (var eq in equippedItems)
        {
            Unequip(eq);
        }
    }

    public void EquipBestItems()
    {
        EquipBestItem(ItemCategory.Amulet);
        EquipBestItem(ItemCategory.Ring);
        EquipBestItem(ItemCategory.Weapon);

        EquipBestItem(ItemCategory.Weapon, ItemType.TwoHandedStaff);
        EquipBestItem(ItemCategory.Weapon, ItemType.TwoHandedBow);
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

        if (player.IsDiaperModeEnabled &&
            category != ItemCategory.Weapon &&
            category != ItemCategory.Pet &&
            category != ItemCategory.Ring)
        {
            return;
        }

        RavenNest.Models.Item bestItem = null;
        foreach (var item in type != null
            ? backpack.Where(x => x.Item.Category == category && x.Item.Type == type)
            : backpack.Where(x => x.Item.Category == category))
        {
            var itemValue = GetItemValue(item);
            var canEquip = CanEquipItem(item);

            if (equippedItem != null && equippedItem.Name == item.Item.Name)
                continue;

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

    public static int GetItemValue(GameInventoryItem item)
    {
        return item.Item.WeaponPower + item.Item.WeaponAim + item.Item.ArmorPower
                        + item.Item.RangedPower + item.Item.RangedAim
                        + item.Item.MagicPower + item.Item.MagicAim;
    }

    public bool CanEquipItem(GameInventoryItem item)
    {
        return player.IsGameAdmin ||
                player.Stats.Defense.Level >= item.Item.RequiredDefenseLevel &&
                player.Stats.Attack.Level >= item.Item.RequiredAttackLevel &&
                player.Stats.Ranged.Level >= item.Item.RequiredRangedLevel &&
                player.Stats.Magic.Level >= item.Item.RequiredMagicLevel &&
                player.Stats.Slayer.Level >= item.Item.RequiredSlayerLevel;
    }

    public void UpdateAppearance()
    {
        equipment.EquipAll(equipped);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsMeleeWeapon(Item item)
    {
        return item.Category == ItemCategory.Weapon && IsMeleeWeapon(item.Type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsMeleeWeapon(ItemType type)
    {
        return type != ItemType.TwoHandedBow && type != ItemType.TwoHandedStaff;
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