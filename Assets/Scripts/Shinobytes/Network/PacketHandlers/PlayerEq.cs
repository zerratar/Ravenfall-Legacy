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
                var list = Utility.ReplaceLastOccurrence(string.Join(", ", "weapon", "ranged", "magic", "armor", "amulet", "ring", "pet"), ", ", " and ");
                client.SendReply(gm, "{target} is not a valid equipment type. These are the available ones: {typeList}", target, list);
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
            client.SendReply(gm, "You don't seem to have any {type} equipped.", target);
            return;
        }

        SendEquipmentDetails(gm, client, targetItem, player);
    }

    private void SendEquipmentList(GameMessage gm, GameClient client, string target, IReadOnlyList<GameInventoryItem> items, PlayerController player)
    {
        if (items.Count == 0)
        {
            client.SendReply(gm, "You don't seem to have any {type} equipped.", target);
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

        if (items.Count == 1)
        {
            var itemName = items[0].Name;
            var a = Utility.IsVocal(itemName[0]) ? "an" : "a";

            var args = new List<object>();
            args.Add(items[0].Name);
            args.AddRange(GetNonZero(totalArmor, totalWeaponPower, totalWeaponAim, totalMagicPower, totalMagicAim, totalRangedPower, totalRangedAim));

            client.SendReply(gm, "You have " + a + " {itemName} equipped with the following stats: " +
                BuildEqFormatString(totalArmor, totalWeaponPower, totalWeaponAim, totalMagicPower, totalMagicAim, totalRangedPower, totalRangedAim),
                args.ToArray()
            );
            return;
        }
        else
        {
            var itemList = Utility.ReplaceLastOccurrence(string.Join(", ", items.Select(x => x.Name)), ", ", " and ");
            var args = new List<object>();
            args.Add(itemList);
            args.AddRange(GetNonZero(totalArmor, totalWeaponPower, totalWeaponAim, totalMagicPower, totalMagicAim, totalRangedPower, totalRangedAim));

            client.SendReply(gm, "You have the following items equipped: {itemList}. These items gives the total of " +
                BuildEqFormatString(totalArmor, totalWeaponPower, totalWeaponAim, totalMagicPower, totalMagicAim, totalRangedPower, totalRangedAim),
                args.ToArray()
            );
        }
    }

    private object[] GetNonZero(params int[] values)
    {
        var result = new List<object>();
        for (var i = 0; i < values.Length; ++i)
        {
            if (values[i] > 0) result.Add(values[i]);
        }
        return result.ToArray();
    }

    private string BuildEqFormatString(
        int totalArmor, int totalWeaponPower, int totalWeaponAim, int totalMagicPower, int totalMagicAim, int totalRangedPower, int totalRangedAim)
    {
        var values = new List<string>();

        if (totalArmor > 0) values.Add("{armorPower} Armor");
        if (totalWeaponPower > 0) values.Add("{weaponPower} Melee Weapon Power");
        if (totalWeaponAim > 0) values.Add("{weaponAim} Melee Weapon Aim");
        if (totalMagicPower > 0) values.Add("{magicPower} Magic/Healing Power");
        if (totalMagicAim > 0) values.Add("{magicAim} Magic Aim");
        if (totalRangedPower > 0) values.Add("{rangedPower} Ranged Weapon Power");
        if (totalRangedAim > 0) values.Add("{rangedAim} Ranged Weapon Aim");

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

        var args = new List<object>();
        args.Add(itemName);
        args.AddRange(GetNonZero(totalArmor, totalWeaponPower, totalWeaponAim, totalMagicPower, totalMagicAim, totalRangedPower, totalRangedAim));

        var a = Utility.IsVocal(itemName[0]) ? "an" : "a";
        client.SendReply(gm, "You have " + a + " {itemName} equipped with the following stats: " +
            BuildEqFormatString(totalArmor, totalWeaponPower, totalWeaponAim, totalMagicPower, totalMagicAim, totalRangedPower, totalRangedAim),
            args.ToArray()
        );
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