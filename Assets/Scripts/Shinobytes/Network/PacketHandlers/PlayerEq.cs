using System.Collections.Generic;
using System.Linq;

public class PlayerEq : ChatBotCommandHandler<string>
{
    public PlayerEq(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }
    public override void Handle(string target, GameMessage gm, GameClient client)
    {
        // MSG_EQUIP_STATS
        target = target?.Trim().ToLower();
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        if (!string.IsNullOrEmpty(target))
        {
            if (IsValidTarget(target))
            {
                SendEquipmentDetails(gm, client, target, player);
                return;
            }
            else
            {
                var list = Utility.ReplaceLastOccurrence(string.Join(", ", "weapon", "ranged", "magic", "armor", "amulet", "ring", "pet"), ", ", Localization.MSG_LIST_AND);
                client.SendReply(gm, Localization.MSG_EQ_INVALID_TYPE, target, list);
            }
        }

        var eq = player.EquipmentStats;
        // Armor {armorPower}, Melee Weapon Power {weaponPower}, Melee Weapon Aim {weaponAim}, Magic/Healing Power {magicPower}, Magic Aim {magicAim}, Ranged Weapon Power {rangedPower}, Ranged Weapon Aim {rangedAim}
        client.SendReply(gm, Localization.MSG_EQUIP_STATS, eq.ArmorPower, eq.WeaponPower, eq.WeaponAim, eq.MagicPower, eq.MagicAim, eq.RangedPower, eq.RangedAim);
    }

    private void SendEquipmentDetails(GameMessage gm, GameClient client, string target, PlayerController player)
    {
        if (target == "armor" || target == "armour")
        {
            SendEquipmentList(gm, client, target, player.Inventory.GetEquipmentsOfCategory(RavenNest.Models.ItemCategory.Armor), player);
            return;
        }

        GameInventoryItem targetItem = null;
        if (target == "weapon" || target == "sword" || target == "spear" || target == "axe" || target == "katana")
        {
            targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.TwoHandedSword);
            if (targetItem == null)
                targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.OneHandedSword);
            if (targetItem == null)
                targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.TwoHandedSpear);
            if (targetItem == null)
                targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.TwoHandedAxe);
            if (targetItem == null)
                targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.OneHandedAxe);
        }

        if (target == "ranged" || target == "bow")
        {
            targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.TwoHandedBow);
        }

        if (target == "magic" || target == "staff")
        {
            targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.TwoHandedStaff);
        }

        if (targetItem == null)
        {
            client.SendReply(gm, Localization.MSG_EQ_NONE_OF_TYPE, target);
            return;
        }

        SendEquipmentDetails(gm, client, targetItem, player);
    }

    private void SendEquipmentList(GameMessage gm, GameClient client, string target, IReadOnlyList<GameInventoryItem> items, PlayerController player)
    {
        if (items.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_EQ_NONE_OF_TYPE, target);
            return;
        }

        var totalArmor = 0;
        var totalWeaponPower = 0;
        var totalWeaponAim = 0;
        var totalMagicPower = 0;
        var totalMagicAim = 0;
        var totalRangedPower = 0;
        var totalRangedAim = 0;

        foreach (var item in items)
        {
            var stats = item.GetItemStats();
            foreach (var s in stats)
            {
                if (s.Name == "Armor") totalArmor += s;
                if (s.Name == "Weapon Aim") totalWeaponAim += s;
                if (s.Name == "Weapon Power") totalWeaponPower += s;
                if (s.Name == "Ranged Aim") totalRangedAim += s;
                if (s.Name == "Ranged Power") totalRangedPower += s;
                if (s.Name == "Magic Aim") totalMagicAim += s;
                if (s.Name == "Magic Power") totalMagicPower += s;
            }
        }

        var statsString = BuildEqStatsString(totalArmor, totalWeaponPower, totalWeaponAim, totalMagicPower, totalMagicAim, totalRangedPower, totalRangedAim);

        if (items.Count == 1)
        {
            var itemName = items[0].Name;
            var format = Utility.IsVocal(itemName[0]) ? Localization.MSG_EQ_ITEM_STATS_AN : Localization.MSG_EQ_ITEM_STATS_A;
            client.SendReply(gm, format, itemName, statsString);
            return;
        }
        else
        {
            var itemList = Utility.ReplaceLastOccurrence(string.Join(", ", items.Select(x => x.Name)), ", ", Localization.MSG_LIST_AND);
            client.SendReply(gm, Localization.MSG_EQ_ITEMS_STATS, itemList, statsString);
        }
    }

    /// <summary>
    /// Builds the stat list, for example "12 Armor, 3 Magic Aim", from the stat fields in Localization.
    /// Each field has one named placeholder, filled here so the whole list goes to the bot as one argument.
    /// </summary>
    private string BuildEqStatsString(
        int totalArmor, int totalWeaponPower, int totalWeaponAim, int totalMagicPower, int totalMagicAim, int totalRangedPower, int totalRangedAim)
    {
        var values = new List<string>();

        if (totalArmor > 0) values.Add(Localization.MSG_EQ_STAT_ARMOR.Replace("{armorPower}", totalArmor.ToString()));
        if (totalWeaponPower > 0) values.Add(Localization.MSG_EQ_STAT_WEAPON_POWER.Replace("{weaponPower}", totalWeaponPower.ToString()));
        if (totalWeaponAim > 0) values.Add(Localization.MSG_EQ_STAT_WEAPON_AIM.Replace("{weaponAim}", totalWeaponAim.ToString()));
        if (totalMagicPower > 0) values.Add(Localization.MSG_EQ_STAT_MAGIC_POWER.Replace("{magicPower}", totalMagicPower.ToString()));
        if (totalMagicAim > 0) values.Add(Localization.MSG_EQ_STAT_MAGIC_AIM.Replace("{magicAim}", totalMagicAim.ToString()));
        if (totalRangedPower > 0) values.Add(Localization.MSG_EQ_STAT_RANGED_POWER.Replace("{rangedPower}", totalRangedPower.ToString()));
        if (totalRangedAim > 0) values.Add(Localization.MSG_EQ_STAT_RANGED_AIM.Replace("{rangedAim}", totalRangedAim.ToString()));

        if (values.Count == 0)
        {
            return "";
        }

        return string.Join(", ", values.ToArray());
    }

    private void SendEquipmentDetails(GameMessage gm, GameClient client, GameInventoryItem item, PlayerController player)
    {
        var totalArmor = 0;
        var totalWeaponPower = 0;
        var totalWeaponAim = 0;
        var totalMagicPower = 0;
        var totalMagicAim = 0;
        var totalRangedPower = 0;
        var totalRangedAim = 0;

        var stats = item.GetItemStats();
        foreach (var s in stats)
        {
            if (s.Name == "Armor") totalArmor += s;
            if (s.Name == "Weapon Aim") totalWeaponAim += s;
            if (s.Name == "Weapon Power") totalWeaponPower += s;
            if (s.Name == "Ranged Aim") totalRangedAim += s;
            if (s.Name == "Ranged Power") totalRangedPower += s;
            if (s.Name == "Magic Aim") totalMagicAim += s;
            if (s.Name == "Magic Power") totalMagicPower += s;
        }

        var itemName = item.Name;
        var statsString = BuildEqStatsString(totalArmor, totalWeaponPower, totalWeaponAim, totalMagicPower, totalMagicAim, totalRangedPower, totalRangedAim);
        var format = Utility.IsVocal(itemName[0]) ? Localization.MSG_EQ_ITEM_STATS_AN : Localization.MSG_EQ_ITEM_STATS_A;
        client.SendReply(gm, format, itemName, statsString);
    }

    private bool IsValidTarget(string target)
    {
        return target == "shield" || target == "weapon"
            || target == "sword" || target == "ranged"
            || target == "magic" || target == "armor"
            || target == "bow" || target == "staff"
            || target == "spear" || target == "axe"
            || target == "katana"
            || target == "armour" || target == "amulet"
            || target == "ring" || target == "pet";
    }
}