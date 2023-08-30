using System;
using System.Collections.Generic;
using System.Linq;
using Skill = RavenNest.Models.Skill;
public class PlayerStats : ChatBotCommandHandler<string>
{
    public PlayerStats(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string skillName, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (!string.IsNullOrEmpty(skillName))
        {
            var targetPlayer = PlayerManager.GetPlayerByName(skillName);
            if (targetPlayer != null)
            {
                SendPlayerStats(gm, targetPlayer, client);
                return;
            }

            var s = SkillUtilities.ParseSkill(skillName.ToLower());
            if (s == Skill.None)
            {
                client.SendReply(gm, "No skill found matching: {skillName}", skillName);
                return;
            }

            var skill = player.GetSkill(s);
            if (skill != null)
            {
                var expRequired = GameMath.ExperienceForLevel(skill.Level + 1);
                var expReq = expRequired < 1000000 ? Utility.FormatValue((long)expRequired) : Utility.FormatExp(expRequired);
                var curExp = skill.Experience < 1000000 ? Utility.FormatValue((long)skill.Experience) : Utility.FormatExp(skill.Experience);
                client.SendReply(gm, Localization.MSG_SKILL, skill.ToString(), curExp, expReq);
            }
            return;
        }

        SendPlayerStats(gm, player, client);
    }

    private void SendPlayerStats(GameMessage gm, PlayerController player, GameClient client)
    {
        var ps = player.Stats;
        var eq = player.EquipmentStats;
        var combatLevel = ps.CombatLevel;
        var skills = "";


        var total = 0;
        foreach (var s in ps.SkillList)
        {
            skills += s + ", ";
            total += s.Level;
        }

        client.SendReply(gm, Localization.MSG_STATS,
            combatLevel.ToString(),
            skills,
            total.ToString(),
            Inspect(player, ps.SkillList)
        );
    }

    private static PlayerInspect Inspect(PlayerController player, SkillStat[] stats)
    {
        var s = player.ActiveSkill;
        var expLeft = 0d;
        var expPerHour = 0d;
        DateTime nextLevel = DateTime.MaxValue;

        if (s != Skill.None)
        {
            var skill = player.GetActiveSkillStat();

            if (skill != null)
            {
                nextLevel = skill.GetEstimatedTimeToLevelUp();
            }
        }

        return new PlayerInspect
        {
            Id = player.Id,
            Name = player.Name,
            Island = player.Island?.name,
            Rested = player.Rested.RestedTime,
            Location = GetLocation(player),
            Skills = GetSkills(stats),
            Training = s,
            ExpLeft = expLeft,
            ExpPerHour = (double)expPerHour,
            NextLevelUtc = nextLevel,
            EquipmentStats = player.EquipmentStats ?? new EquipmentStats()
        };
    }

    private static SkillInfo[] GetSkills(SkillStat[] stats)
    {
        var si = new SkillInfo[stats.Length];
        for (var i = 0; i < stats.Length; i++)
        {
            var stat = stats[i];

            si[i] = new SkillInfo
            {
                Name = stat.Name,
                Level = stat.Level,
                Progress = stat.Experience / GameMath.ExperienceForLevel(stat.Level + 1),
                CurrentValue = stat.CurrentValue,
                MaxLevel = stat.MaxLevel,
                Bonus = stat.Bonus,
            };
        }
        return si;
    }

    private static PlayerLocation GetLocation(PlayerController player)
    {
        if (player.Dungeon.InDungeon) return PlayerLocation.Dungeon;
        if (player.Raid.InRaid) return PlayerLocation.Raid;
        if (player.StreamRaid.InWar) return PlayerLocation.War;
        if (player.Onsen.InOnsen) return PlayerLocation.Resting;
        if (player.Island?.name == null) return PlayerLocation.Ferry;
        return PlayerLocation.Island;
    }

    public class PlayerInspect
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Rested { get; set; }
        public string Island { get; set; }
        public Skill Training { get; set; }
        public SkillInfo[] Skills { get; set; }
        public PlayerLocation Location { get; set; }
        public double ExpLeft { get; set; }
        public double ExpPerHour { get; set; }
        public DateTime NextLevelUtc { get; set; }
        public EquipmentStats EquipmentStats { get; set; }
    }

    public class SkillInfo
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public double Progress { get; set; }
        public int CurrentValue { get; set; }
        public int MaxLevel { get; set; }
        public float Bonus { get; set; }
    }

    public enum PlayerLocation
    {
        Island,
        Ferry,
        Resting,
        Raid,
        Dungeon,
        War
    }
}

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
                client.SendReply(gm, "{target} is not a valid equipment type. These are the available ones: {typeList}", list);
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
        if (target == "weapon" || target == "sword")
        {
            targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.TwoHandedSword);
            if (targetItem == null)
                targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.OneHandedSword);
        }

        if (target == "ranged")
        {
            targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.TwoHandedBow);
        }

        if (target == "magic")
        {
            targetItem = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.TwoHandedStaff);
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
        return target == "shield" || target == "weapon" || target == "sword" || target == "ranged" || target == "magic" || target == "armor" || target == "armour" || target == "amulet" || target == "ring" || target == "pet";
    }
}