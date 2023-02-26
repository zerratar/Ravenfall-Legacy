using System;
using System.Collections;
using System.Collections.Generic;
using Shinobytes.Linq;
using System.Runtime.CompilerServices;
using RavenNest.Models;
using UnityEngine;

public class GameInventoryItem
{
    public RavenNest.Models.Item Item { get; }
    public IReadOnlyList<ItemEnchantment> Enchantments { get; set; }
    public InventoryItem InventoryItem { get; set; }
    public PlayerController Player { get; }
    public GameInventoryItem(PlayerController owner, InventoryItem instance, Item item)
    {
        this.Player = owner;
        this.InventoryItem = instance;
        this.Item = item;
        this.Enchantments = Inventory.GetItemEnchantments(instance.Enchantment);

        Soulbound = instance.Soulbound ?? item.Soulbound ?? false;
        RequiredDefenseLevel = item.RequiredDefenseLevel;
        RequiredAttackLevel = item.RequiredAttackLevel;
        RequiredSlayerLevel = item.RequiredSlayerLevel;
        RequiredMagicLevel = item.RequiredMagicLevel;
        RequiredRangedLevel = item.RequiredRangedLevel;
        Name = instance.Name ?? item.Name;
        ItemId = item.Id;
        Type = item.Type;
        Category = item.Category;
    }
    public bool Soulbound { get; set; }
    public int RequiredDefenseLevel { get; set; }
    public int RequiredAttackLevel { get; set; }
    public int RequiredSlayerLevel { get; set; }
    public int RequiredMagicLevel { get; set; }
    public int RequiredRangedLevel { get; set; }
    public string Name { get; set; }
    public Guid InstanceId { get => InventoryItem.Id; set => InventoryItem.Id = value; }
    public Guid ItemId { get; set; }
    public ItemCategory Category { get; set; }

    public ItemType Type { get; set; }

    public long Amount
    {
        get => InventoryItem.Amount;
        set => InventoryItem.Amount = value;
    }
    //public string Tag { get; set; }
    //public double Amount { get; set; }
}

public class Inventory : MonoBehaviour
{
    private List<GameInventoryItem> backpack = new List<GameInventoryItem>();
    private List<GameInventoryItem> equipped = new List<GameInventoryItem>();
    private PlayerController player;
    private PlayerEquipment equipment;
    private GameManager gameManager;
    private GameInventoryItem equippedMeleeWeapon;

    private readonly object mutex = new object();

    private void Awake()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        player = GetComponent<PlayerController>();
        equipment = GetComponent<PlayerEquipment>();
    }

    public void RemoveByItemId(Guid itemId, long amount)
    {
        lock (mutex)
        {
            var target = FromBackpack(itemId);
            if (target == null)
            {
                return;
            }

            target.InventoryItem.Amount -= amount;
            if (target.InventoryItem.Amount <= 0)
            {
                backpack.Remove(target);
            }
        }
    }

    public void Remove(GameInventoryItem item, double amount, bool removeEquipped = false)
    {
        lock (mutex)
        {
            if (removeEquipped)
            {
                if (IsEquipped(item))
                {
                    equipped.Remove(item);
                    EquipAll();
                    return;
                }
            }

            item.Amount -= (long)amount;
            if (item.Amount <= 0)
            {
                backpack.Remove(item);
            }
        }
    }

    internal void RemoveByInventoryId(Guid inventoryItemId, long amount)
    {
        lock (mutex)
        {
            var equippedItem = this.equipped.FirstOrDefault(x => x.InstanceId == inventoryItemId);
            if (equippedItem != null)
            {
                this.equipped.Remove(equippedItem);
                EquipAll();
            }
            var backpackItem = this.backpack.FirstOrDefault(x => x.InstanceId == inventoryItemId);
            if (backpackItem != null)
            {

                if (amount >= backpackItem.Amount)
                {
                    this.backpack.Remove(backpackItem);
                }
                else
                {
                    backpackItem.Amount -= amount;
                }
            }
            //this.equipped
        }
    }

    public void Create(IReadOnlyList<InventoryItem> inventoryItems)
    {
        lock (mutex)
        {
            backpack.Clear();
            var itemManager = gameManager.Items;
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                InventoryItem item = inventoryItems[i];
                var loadedItem = itemManager.Get(item.ItemId);
                if (loadedItem == null) continue;

                var gameItem = new GameInventoryItem(this.player, item, loadedItem);

                if (item.Equipped)
                {
                    Equip(gameItem, false);
                }
                else
                {
                    backpack.Add(gameItem);
                }
            }

            if (equipped.Count > 0)
            {
                equipment.EquipAll(equipped);
            }
            else
            {
                var appearance = player.Appearance;
                appearance.UpdateAppearance();
                appearance.Optimize();
            }
        }
    }

    public void UpdateScrolls(ScrollInfoCollection scrolls)
    {
        lock (mutex)
        {
            var existingScrolls = GetInventoryItemsOfCategory(ItemCategory.Scroll);
            foreach (var scroll in existingScrolls)
            {
                backpack.Remove(scroll);
            }

            foreach (var s in scrolls)
            {
                var scrollItem = gameManager.Items.Get(s.ItemId);
                if (scrollItem != null)
                {
                    AddToBackpack(scrollItem, s.Amount);
                }
            }
        }
    }

    internal long RemoveScroll(RavenNest.Models.ScrollType scroll)
    {
        lock (mutex)
        {
            var scrolls = GetInventoryItemsOfCategory(ItemCategory.Scroll);
            var s = scrolls.FirstOrDefault(x => x.Item.Name.IndexOf(scroll.ToString(), StringComparison.OrdinalIgnoreCase) >= 0);
            if (s != null)
            {
                if (--s.InventoryItem.Amount <= 0)
                {
                    backpack.Remove(s);
                }

                return s.InventoryItem.Amount;
            }
            return 0L;
        }
    }


    public static IReadOnlyList<ItemEnchantment> GetItemEnchantments(InventoryItem item)
    {
        return GetItemEnchantments(item.Enchantment);
    }

    public static IReadOnlyList<ItemEnchantment> GetItemEnchantments(string enchantmentString)
    {
        List<ItemEnchantment> enchantments = null;

        if (enchantmentString == null)
        {
            return enchantments;
        }

        if (!string.IsNullOrEmpty(enchantmentString))
        {
            enchantments = new List<ItemEnchantment>();
            var en = enchantmentString.Split(';');
            foreach (var e in en)
            {
                var data = e.Split(':');
                var key = data[0];
                var value = GetValue(data[1], out var type);

                //var attr = availableAttributes.FirstOrDefault(x => x.Name == key);

                var description = "";

                if (type == AttributeValueType.Percent)
                {
                    //if (attr != null)
                    //{
                    //    description = attr.Description.Replace(attr.MaxValue, value + "%");
                    //}

                    description = "Increases " + key.ToLower() + " by " + value + "%";
                    value = value / 100d;
                }
                else
                {
                    description = "Increases " + key.ToLower() + " by " + value;
                    //if (attr != null)
                    //{
                    //    description = attr.Description.Replace(attr.MaxValue, value.ToString());
                    //}
                }

                enchantments.Add(new ItemEnchantment
                {
                    Name = (key[0] + key.ToLower().Substring(1)),
                    Value = value,
                    ValueType = type,
                    Description = description,
                });
            }
        }
        return enchantments;
    }

    public static double GetValue(string val, out AttributeValueType valueType)
    {
        valueType = AttributeValueType.Number;
        val = val.Replace(',', '.');
        if (string.IsNullOrEmpty(val))
        {
            return 0d;
        }
        else
        {
            if (val.EndsWith("%"))
            {
                if (TryParse(val.Replace("%", ""), out var value))
                {
                    valueType = AttributeValueType.Percent;
                    //return value / 100d;
                    return value;
                }
            }

            TryParse(val, out var number);
            return number;
        }
    }

    private static bool TryParse(string val, out double value)
    {
        if (double.TryParse(val, out value))
            return true;

        if (double.TryParse(val.Replace(',', '.'), out value))
            return true;

        return double.TryParse(val.Replace('.', ','), out value);
    }

    internal GameInventoryItem ApplyEnchantment(InventoryItem enchantedItem)
    {
        lock (mutex)
        {
            var existing = backpack.FirstOrDefault(x => x.InventoryItem.Id == enchantedItem.Id);
            if (existing == null)
            {
                existing = equipped.FirstOrDefault(x => x.InventoryItem.Id == enchantedItem.Id);
            }

            if (existing != null)
            {
                existing.Name = enchantedItem.Name;
                existing.InventoryItem = enchantedItem;
                existing.Enchantments = Inventory.GetItemEnchantments(enchantedItem.Enchantment);
                existing.Soulbound = enchantedItem.Soulbound.GetValueOrDefault();
                return existing;
            }

            return null;
        }
    }

    public GameInventoryItem AddToBackpack(RavenNest.Models.InventoryItem item, long amount = 1)
    {
        lock (mutex)
        {
            var existing = backpack.FirstOrDefault(x => x.InventoryItem.Id == item.Id);
            if (existing != null)
            {
                if (item.Soulbound.GetValueOrDefault() || !string.IsNullOrEmpty(item.Enchantment) || item.TransmogrificationId != null)
                {
                    existing.Amount -= amount;
                    return CreateInstance(item, amount);
                }

                existing.InventoryItem.Amount += amount;
                return existing;
            }

            return CreateInstance(item, amount);
        }
    }

    private GameInventoryItem CreateInstance(InventoryItem item, long amount)
    {
        var instance = new GameInventoryItem(this.player, new InventoryItem
        {
            Amount = amount,
            ItemId = item.ItemId,
            Id = item.Id,
            Tag = item.Tag,
            TransmogrificationId = item.TransmogrificationId,
            Enchantment = item.Enchantment,
            Name = item.Name,
            Flags = item.Flags,
            Soulbound = item.Soulbound,
        }, gameManager.Items.Get(item.ItemId));

        backpack.Add(instance);
        return instance;
    }

    public GameInventoryItem AddToBackpack(RavenNest.Models.CraftItemResult item)
    {
        lock (mutex)
        {
            if (item == null)
            {
                return null;
            }

            var existing = backpack.FirstOrDefault(x => x.InventoryItem != null && x.InventoryItem.Id == item.InventoryItemId);
            if (existing != null && existing.InventoryItem != null)
            {
                existing.InventoryItem.Amount += item.Value;
                return existing;
            }

            var i = gameManager.Items.Get(item.ItemId);
            var instance = new GameInventoryItem(this.player, new InventoryItem
            {
                Amount = item.Value,
                ItemId = item.ItemId,
                Id = item.InventoryItemId,
                Name = i.Name,
                Soulbound = i.Soulbound,
            }, i);

            backpack.Add(instance);
            return instance;
        }
    }
    public GameInventoryItem AddToBackpack(RavenNest.Models.ItemAdd item)
    {
        lock (mutex)
        {
            var existing = backpack.FirstOrDefault(x => x.InventoryItem.Id == item.InventoryItemId);
            if (existing != null)
            {
                existing.InventoryItem.Amount += (long)item.Amount;
                return existing;
            }

            var instance = new GameInventoryItem(this.player, new InventoryItem
            {
                Amount = item.Amount,
                ItemId = item.ItemId,
                Id = item.InventoryItemId,
                Tag = item.Tag,
                TransmogrificationId = item.TransmogrificationId,
                Enchantment = item.Enchantment,
                Name = item.Name,
                Flags = item.Flags,
                Soulbound = item.Soulbound,
            }, gameManager.Items.Get(item.ItemId));

            backpack.Add(instance);
            return instance;
        }
    }

    public void AddToBackpack(GameInventoryItem item, double amount = 1)
    {
        if (item == null)
        {
            return;
        }

        lock (mutex)
        {
            var existing = backpack.FirstOrDefault(x => x.InventoryItem.Id == item.InventoryItem.Id);
            if (existing != null)
            {
                existing.InventoryItem.Amount += (long)amount;
            }
            else
            {
                var ii = item.InventoryItem;
                var instance = new GameInventoryItem(this.player, new InventoryItem
                {
                    Amount = (long)amount,
                    ItemId = item.Item.Id,
                    Id = ii.Id,
                    Tag = ii.Tag,
                    TransmogrificationId = ii.TransmogrificationId,
                    Enchantment = ii.Enchantment,
                    Name = ii.Name,
                    Flags = ii.Flags,
                    Soulbound = ii.Soulbound,
                }, item.Item);

                backpack.Add(instance);
            }
        }
    }

    public GameInventoryItem AddToBackpack(RavenNest.Models.Item item, double amount = 1)
    {
        if (item == null)
        {
            return null;
        }

        lock (mutex)
        {
            GameInventoryItem result = null;
            var existing = backpack.FirstOrDefault(x => x.Item.Id == item.Id && (x.Enchantments == null || x.Enchantments.Count == 0));
            if (existing != null)
            {
                existing.Amount += (long)amount;
                result = existing;
            }
            else
            {
                result = new GameInventoryItem(this.player, new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    Amount = (long)amount,
                    ItemId = item.Id,
                }, item);
                backpack.Add(result);
            }

            return result;
        }
    }

    internal void AddStreamerTokens(int amount)
    {
        var token = gameManager.Items.GetStreamerToken();
        if (token != null)
        {
            AddToBackpack(token, amount);
        }
    }

    internal void RemoveStreamerTokens(int result)
    {
        var token = GetInventoryItemsOfCategory(ItemCategory.StreamerToken).FirstOrDefault();
        if (token != null)
        {
            Remove(token, result);
        }
    }

    public void EquipAll()
    {
        equipment.EquipAll(equipped);
    }

    public void Unequip(GameInventoryItem item)
    {
        lock (mutex)
        {
            var targetItem = this.equipped.FirstOrDefault(x => x.InventoryItem.Id == item.InventoryItem.Id);
            if (targetItem != null)
            {
                AddToBackpack(targetItem);
                equipped.Remove(targetItem);
                equipment.Unequip(targetItem);
            }
        }
    }

    public void Unequip(ItemCategory category, ItemType type)
    {
        lock (mutex)
        {
            var equip = GetEquipmentOfType(category, type);
            if (equip != null)
            {
                AddToBackpack(equip);
                equipped.Remove(equip);
                equipment.Unequip(equip);
            }
        }
    }

    public bool Equip(ItemController i, bool updateAppearance = true)
    {
        //var item = gameManager.Items.Get(i.Id);
        //return Equip(item, updateAppearance);
        return Equip(i.Definition, updateAppearance);
    }

    public bool Equip(Guid itemId, bool updateAppearance = true)
    {
        var item = this.GetInventoryItems(itemId).FirstOrDefault();
        return Equip(item, updateAppearance);
    }

    public bool Equip(GameInventoryItem item, bool updateAppearance = true, bool updateEquipmentEffect = true)
    {
        if (item == null || !player || player == null || !player.transform || player.transform == null || !CanEquipItem(item))
            return false;

        player.transform.localScale = Vector3.one;


        lock (mutex)
        {

            if (item.Item.Type == ItemType.TwoHandedAxe || item.Item.Type == ItemType.TwoHandedSword)
            {
                var shield = GetEquipmentOfType(ItemCategory.Armor, ItemType.Shield);
                if (shield != null)
                {
                    AddToBackpack(shield);
                    equipped.Remove(shield);
                    equipment.Unequip(shield);
                }
            }

            if (IsMeleeWeapon(item))
            {
                this.equippedMeleeWeapon = item;
            }


            var equip = GetEquipmentOfType(item.Item.Category, item.Item.Type);
            if (equip != null)
            {
                AddToBackpack(equip);
                equipped.Remove(equip);
                equipment.Unequip(equip);
            }

            equipped.Add(item);

            if (item.Amount == 1)
            {
                backpack.Remove(item);
            }
            else
            {
                RemoveByItemId(item.Item.Id, 1);
            }

            if (updateAppearance)
            {
                equipment.EquipAll(equipped);
            }
            else
            {
                equipment.Equip(item, false);
            }

            if (updateEquipmentEffect)
            {
                player.UpdateEquipmentEffect(equipped);
            }

            player.transform.localScale = player.TempScale;
            return true;
        }
    }

    public void UpdateEquipmentEffect()
    {
        player.UpdateEquipmentEffect(equipped);
    }

    public GameInventoryItem GetEquipmentOfCategory(ItemCategory itemCategory)
    {
        lock (mutex)
        {
            // always default to melee weapon when no type is used.
            if (itemCategory == ItemCategory.Weapon)
            {
                for (var i = 0; i < equipped.Count; ++i)
                {
                    var item = equipped[i];
                    if (IsMeleeWeapon(item)) return item;
                }
                return null;
            }

            for (var i = 0; i < equipped.Count; ++i)
            {
                var item = equipped[i];
                if (item.Item.Category == itemCategory) return item;
            }
            return null;
        }
    }

    public IReadOnlyList<GameInventoryItem> GetEquipmentsOfCategory(ItemCategory itemCategory)
    {
        lock (mutex)
        {
            return equipped.AsList(x => x.Item.Category == itemCategory);
        }
    }

    public IReadOnlyList<GameInventoryItem> GetInventoryItemsOfType(ItemCategory itemCategory, RavenNest.Models.ItemType type)
    {
        lock (mutex)
            return backpack.AsList(x => x.Item.Category == itemCategory && x.Item.Type == type);
    }

    public IReadOnlyList<GameInventoryItem> GetInventoryItemsOfCategory(ItemCategory itemCategory)
    {
        lock (mutex)
            return backpack.AsList(x => x.Item.Category == itemCategory);
    }

    public IReadOnlyList<GameInventoryItem> GetAllItems()
    {
        lock (mutex)
            return backpack.Concat(equipped);
    }
    public List<GameInventoryItem> GetBackpackItems() { lock (mutex) return backpack; }
    public List<GameInventoryItem> GetEquippedItems() { lock (mutex) return equipped; }

    internal IReadOnlyList<InventoryItem> GetInventoryItems()
    {
        lock (mutex)
        {
            var i = new List<InventoryItem>();
            foreach (var eq in equipped)
            {
                i.Add(new InventoryItem
                {
                    Amount = 1,
                    Equipped = true,
                    Id = eq.InventoryItem.Id,
                    ItemId = eq.Item.Id,
                    Soulbound = eq.Item.Soulbound,
                    Enchantment = eq.InventoryItem.Enchantment,
                    Flags = eq.InventoryItem.Flags,
                });
            }
            foreach (var eq in backpack)
            {
                i.Add(new InventoryItem
                {
                    Amount = (long)eq.InventoryItem.Amount,
                    Equipped = false,
                    Id = eq.InventoryItem.Id,
                    ItemId = eq.Item.Id,
                    Soulbound = eq.Item.Soulbound,
                    Enchantment = eq.InventoryItem.Enchantment,
                    Flags = eq.InventoryItem.Flags,

                });
            }
            return i;
        }
    }
    internal IReadOnlyList<GameInventoryItem> GetInventoryItems(Guid itemId)
    {
        lock (mutex)
        {
            return backpack.AsList(x => x.Item.Id == itemId);
        }
    }

    public bool IsEquipped(GameInventoryItem item)
    {
        lock (mutex)
        {
            return equipped.Any(x => x.InstanceId == item.InstanceId);
        }
    }

    public GameInventoryItem GetEquippedItem(Guid itemId)
    {
        lock (mutex)
        {
            return equipped.FirstOrDefault(x => x.Item.Id == itemId);
        }
    }

    public GameInventoryItem GetEquipmentOfType(ItemCategory itemCategory, RavenNest.Models.ItemType type)
    {
        lock (mutex)
        {
            if (itemCategory == ItemCategory.Weapon && IsMeleeWeapon(type))
            {


                return equipped.FirstOrDefault(IsMeleeWeapon);
            }

            return equipped.FirstOrDefault(x => x.Item.Category == itemCategory && x.Item.Type == type);
        }
    }

    public GameInventoryItem GetMeleeWeapon()
    {
        lock (mutex)
        {
            if (equippedMeleeWeapon == null)
            {
                equippedMeleeWeapon = equipped.FirstOrDefault(IsMeleeWeapon);
            }

            return equippedMeleeWeapon;
        }
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
        lock (mutex)
        {
            try
            {
                var equipments = this.equipped.ToList();
                for (int i = 0; i < equipments.Count; i++)
                {
                    Unequip(equipments[i]);
                }
            }
            catch { }
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
        EquipBestItem(ItemCategory.Weapon, ItemType.TwoHandedStaff);
        EquipBestItem(ItemCategory.Weapon, ItemType.TwoHandedBow);
        EquipBestItem(ItemCategory.Armor, ItemType.Helmet);

        // TODO: Equip best suitable out of the Masks, Hat, HeadCoverings and Helmets

        EquipBestItem(ItemCategory.Armor, ItemType.Leggings);
        EquipBestItem(ItemCategory.Armor, ItemType.Gloves);
        EquipBestItem(ItemCategory.Armor, ItemType.LeftShoulder);
        EquipBestItem(ItemCategory.Armor, ItemType.RightShoulder);
        EquipBestItem(ItemCategory.Armor, ItemType.Boots);
        EquipBestItem(ItemCategory.Armor, ItemType.Chest);
        EquipBestItem(ItemCategory.Armor, ItemType.Shield);

        EquipBestItem(ItemCategory.Weapon);

        player.UpdateEquipmentEffect(equipped);

        equipment.EquipAll(equipped);
    }


    private void EquipBestItem(ItemCategory category, ItemType? type = null)
    {
        var equippedItem = type != null
            ? GetEquipmentOfType(category, type.Value)
            : GetEquipmentOfCategory(category);

        var bestValue = equippedItem != null
            ? equippedItem.GetTotalStats()
            : 0;

        if (player.IsDiaperModeEnabled &&
            category != ItemCategory.Weapon &&
            category != ItemCategory.Pet &&
            category != ItemCategory.Ring)
        {
            return;
        }

        lock (mutex)
        {
            GameInventoryItem bestItem = null;

            for (var i = 0; i < backpack.Count; ++i)
            {
                var item = backpack[i];
                if (item.Item.Category == category && (type == null || item.Item.Type == type))
                {
                    var itemValue = GetItemValue(item);
                    var canEquip = CanEquipItem(item);

                    //if (equippedItem != null && equippedItem.Item.Name == item.Item.Name)
                    //    continue;

                    if (bestValue < itemValue && canEquip)
                    {
                        bestValue = itemValue;
                        bestItem = item;
                    }
                }
            }

            if (bestItem != null)
            {
                Equip(bestItem, false, false);
            }
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GameInventoryItem FromBackpack(Guid itemId)
    {
        var s = backpack.Count;
        for (var i = 0; i < s; ++i)
        {
            var tmp = backpack[i];
            if (tmp.Enchantments?.Count == 0 && tmp.ItemId == itemId)
            {
                return tmp;
            }
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GameInventoryItem FromEquipped(Guid itemId)
    {
        var s = equipped.Count;
        for (var i = 0; i < s; ++i)
        {
            var tmp = equipped[i];
            if (tmp.Enchantments?.Count == 0 && tmp.ItemId == itemId)
            {
                return tmp;
            }
        }
        return null;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanEquipItem(Item item)
    {
        return player.IsGameAdmin ||
                player.Stats.Defense.Level >= item.RequiredDefenseLevel &&
                player.Stats.Attack.Level >= item.RequiredAttackLevel &&
                player.Stats.Ranged.Level >= item.RequiredRangedLevel &&
                (player.Stats.Magic.Level >= item.RequiredMagicLevel || player.Stats.Healing.Level >= item.RequiredMagicLevel) &&
                player.Stats.Slayer.Level >= item.RequiredSlayerLevel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanEquipItem(GameInventoryItem item)
    {
        return CanEquipItem(item.Item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetItemValue(GameInventoryItem item)
    {
        return item.Item.GetTotalStats();
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
    private bool IsMeleeWeapon(GameInventoryItem item)
    {
        return item.Item.Category == ItemCategory.Weapon && IsMeleeWeapon(item.Item.Type);
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
            var stats1 = item1.GetTotalStats();
            var stats2 = item.GetTotalStats();
            return stats1 - stats2;
        }
    }
    //public class ItemEnchantment
    //{
    //    public string Name { get; set; }
    //    public string Description { get; set; }
    //    public AttributeValueType ValueType { get; set; }
    //    public double Value { get; set; }
    //}
}