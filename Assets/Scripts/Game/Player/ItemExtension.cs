using RavenNest.Models;
using System.Collections;
using System.Collections.Generic;

public static class ItemExtension
{
    public static int GetTotalStats(this ItemController item)
    {
        var invItem = item.Definition;
        if (invItem != null)
        {
            return invItem.GetTotalStats();
        }

        return item.GetTotalStats();
    }

    public static int GetTotalStats(this Item item)
    {
        return //item.Level
            item.WeaponPower
            + item.WeaponAim
            + item.ArmorPower
            + item.MagicAim
            + item.MagicPower
            + item.RangedAim
            + item.RangedPower;
    }

    public static int GetTotalStats(this GameInventoryItem invItem)
    {
        var item = invItem.Item;
        var stats = invItem.GetItemStats();
        return // item.Level
            stats.WeaponPower
            + stats.WeaponAim
            + stats.ArmorPower
            + stats.MagicAim
            + stats.MagicPower
            + stats.RangedAim
            + stats.RangedPower;
    }

    public static IReadOnlyList<SkillBonus> GetSkillBonuses(this GameInventoryItem i)
    {
        List<SkillBonus> result = new List<SkillBonus>();
        var player = i.Player;

        if (i.Enchantments != null)
        {
            foreach (var e in i.Enchantments)
            {
                var value = e.Value;
                var key = e.Name.ToUpper();
                SkillStat targetSkill = null;
                foreach (var skill in player.Stats.SkillList)
                {
                    if (skill.Name.ToUpper() == key)
                    {
                        targetSkill = skill;
                        break;
                    }
                }

                if (targetSkill == null)
                {
                    continue;
                }

                double skillBonus = 0;
                if (e.ValueType == AttributeValueType.Percent)
                {
                    if (value >= 1)
                    {
                        value = value / 100d;
                    }

                    skillBonus = (targetSkill.Level * value);
                }
                else
                {
                    skillBonus = value;
                }

                result.Add(new SkillBonus { Enchantment = e, Bonus = skillBonus, Skill = targetSkill });
            }
        }
        return result;
    }

    public static ItemStatsCollection GetItemStats(this Item i)
    {
        var stats = new List<ItemStat>();
        if (i.WeaponAim > 0) stats.Add(new ItemStat("Weapon Aim", i.WeaponAim, 0, null));
        if (i.WeaponPower > 0) stats.Add(new ItemStat("Weapon Power", i.WeaponPower, 0, null));
        if (i.RangedAim > 0) stats.Add(new ItemStat("Ranged Aim", i.RangedAim, 0, null));
        if (i.RangedPower > 0) stats.Add(new ItemStat("Ranged Power", i.RangedPower, 0, null));
        if (i.MagicAim > 0) stats.Add(new ItemStat("Magic Aim", i.MagicAim, 0, null));
        if (i.MagicPower > 0) stats.Add(new ItemStat("Magic Power", i.MagicPower, 0, null));
        if (i.ArmorPower > 0) stats.Add(new ItemStat("Armor", i.ArmorPower, 0, null));
        return new ItemStatsCollection(stats);
    }

    public static ItemStatsCollection GetItemStats(this GameInventoryItem i, bool includeSkillEnchantments = false)
    {
        int aimBonus = 0;
        int armorBonus = 0;
        int powerBonus = 0;

        ItemEnchantment powerEnchantment = null;
        ItemEnchantment aimEnchantment = null;
        ItemEnchantment armorEnchantment = null;

        var stats = new List<ItemStat>();

        if (i.Enchantments != null)
        {
            foreach (var e in i.Enchantments)
            {
                var value = e.Value;
                var key = e.Name.ToUpper();
                if (e.ValueType == AttributeValueType.Percent)
                {
                    if (value >= 1)
                    {
                        value = value / 100d;
                    }

                    if (key == "POWER")
                    {
                        powerEnchantment = e;
                        powerBonus = (int)(i.Item.WeaponPower * value) + (int)(i.Item.MagicPower * value) + (int)(i.Item.RangedPower * value);
                    }
                    if (key == "AIM")
                    {
                        aimEnchantment = e;
                        aimBonus = (int)(i.Item.WeaponAim * value) + (int)(i.Item.MagicAim * value) + (int)(i.Item.RangedAim * value);
                    }
                    if (key == "ARMOUR" || key == "ARMOR")
                    {
                        armorEnchantment = e;
                        armorBonus = (int)(i.Item.ArmorPower * value);
                    }
                }
                else
                {
                    if (key == "POWER")
                    {
                        powerEnchantment = e;
                        powerBonus = (int)value;
                    }
                    if (key == "AIM")
                    {
                        aimEnchantment = e;
                        aimBonus = (int)value;
                    }
                    if (key == "ARMOUR" || key == "ARMOR")
                    {
                        armorEnchantment = e;
                        armorBonus = (int)value;
                    }
                }
            }
        }

        if (i.Item.WeaponAim > 0) stats.Add(new ItemStat("Weapon Aim", i.Item.WeaponAim, aimBonus, aimEnchantment));
        if (i.Item.WeaponPower > 0) stats.Add(new ItemStat("Weapon Power", i.Item.WeaponPower, powerBonus, powerEnchantment));
        if (i.Item.RangedAim > 0) stats.Add(new ItemStat("Ranged Aim", i.Item.RangedAim, aimBonus, aimEnchantment));
        if (i.Item.RangedPower > 0) stats.Add(new ItemStat("Ranged Power", i.Item.RangedPower, powerBonus, powerEnchantment));
        if (i.Item.MagicAim > 0) stats.Add(new ItemStat("Magic Aim", i.Item.MagicAim, aimBonus, aimEnchantment));
        if (i.Item.MagicPower > 0) stats.Add(new ItemStat("Magic Power", i.Item.MagicPower, powerBonus, powerEnchantment));
        if (i.Item.ArmorPower > 0) stats.Add(new ItemStat("Armor", i.Item.ArmorPower, armorBonus, armorEnchantment));

        if (includeSkillEnchantments)
        {
            var bonuses = i.GetSkillBonuses();
            foreach (var b in bonuses)
            {
                stats.Add(new ItemStat(b.Enchantment.Description, 0, 0));//(int)b.Bonus));
            }
        }

        return new ItemStatsCollection(stats);
    }
}

public class SkillBonus
{
    public ItemEnchantment Enchantment { get; set; }
    public SkillStat Skill { get; set; }
    public double Bonus { get; set; }
}

public class ItemStatsCollection : IReadOnlyList<ItemStat>
{
    private readonly List<ItemStat> items;

    public readonly ItemStat ArmorPower = new ItemStat("Armor");
    public readonly ItemStat WeaponAim = new ItemStat("Weapon Aim");
    public readonly ItemStat WeaponPower = new ItemStat("Weapon Power");
    public readonly ItemStat MagicPower = new ItemStat("Magic Power");
    public readonly ItemStat MagicAim = new ItemStat("Magic Aim");
    public readonly ItemStat RangedPower = new ItemStat("Ranged Power");
    public readonly ItemStat RangedAim = new ItemStat("Ranged Aim");

    public ItemStatsCollection()
    {
        items = new List<ItemStat>();
    }

    public ItemStatsCollection(IEnumerable<ItemStat> items)
    {
        this.items = new List<ItemStat>(items);

        foreach (var item in items)
        {
            if (item.Name == "Weapon Aim") WeaponAim = item;
            if (item.Name == "Weapon Power") WeaponPower = item;
            if (item.Name == "Magic Aim") MagicAim = item;
            if (item.Name == "Magic Power") MagicPower = item;
            if (item.Name == "Ranged Aim") RangedAim = item;
            if (item.Name == "Ranged Power") RangedPower = item;
            if (item.Name == "Armor") ArmorPower = item;
        }
    }

    public ItemStat this[int index] => items[index];

    public int Count => items.Count;

    public IEnumerator<ItemStat> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class ItemStat
{
    public ItemStat() { }
    public ItemStat(string name, int value = 0, int bonus = 0, ItemEnchantment enchantment = null)
    {
        this.Name = name;
        this.Value = value;
        this.Bonus = bonus;
        this.Enchantment = enchantment;
    }
    public string Name { get; set; }
    public int Value { get; set; }
    public int Bonus { get; set; }
    public ItemEnchantment Enchantment { get; set; }

    public static implicit operator int(ItemStat enchantment)
    {
        return enchantment.Value + enchantment.Bonus;
    }
}
