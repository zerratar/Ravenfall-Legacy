using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Skill = RavenNest.Models.Skill;
public class PlayerStats : ChatBotCommandHandler<string>
{
    protected IItemResolver itemResolver;
    public PlayerStats(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
        var ioc = game.gameObject.GetComponent<IoCContainer>();
        this.itemResolver = ioc.Resolve<IItemResolver>();
    }

    public override void Handle(string skillName, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        // originally for stats (skills) of a player or specific skill
        // but now it should also support giving  details about a target item.

        if (!string.IsNullOrEmpty(skillName))
        {
            var invItem = itemResolver.ResolveInventoryItem(player, skillName);
            if (invItem != null && invItem.InventoryItem != null)
            {
                SendInventoryItemStats(invItem.InventoryItem, gm, client);
                return;
            }

            var item = itemResolver.ResolveAny(skillName);
            if (item != null && item.Item != null)
            {
                SendItemStats(item.Item, gm, client);
                return;
            }

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

    private void SendInventoryItemStats(GameInventoryItem inventoryItem, GameMessage gm, GameClient client)
    {
        var stats = inventoryItem.GetItemStats(true);
        var statsString = string.Join(", ", stats.Select(FormatStat));
        if (string.IsNullOrEmpty(statsString))
        {
            client.SendReply(gm, "Oh dear! {itemName} seem to be missing stats!", inventoryItem.Name);
            return;
        }
        client.SendReply(gm, "{itemName}: {stats}", inventoryItem.Name, statsString);
    }

    private void SendItemStats(Item item, GameMessage gm, GameClient client)
    {
        var stats = item.GetItemStats();
        var statsString = string.Join(", ", stats.Select(FormatStat));
        if (string.IsNullOrEmpty(statsString))
        {
            client.SendReply(gm, "Oh dear! {itemName} seem to be missing stats!", item.Name);
            return;
        }
        client.SendReply(gm, "{itemName}: {stats}", item.Name, statsString);
    }

    private string FormatStat(ItemStat stat)
    {
        if (stat.Enchantment != null)
        {
            var e = stat.Enchantment;
            var value = (e.ValueType == AttributeValueType.Percent ? ((int)(e.Value * 100)) + "%" : e.Value + "");
            return stat.Name + ": " + (int)stat + " (+" + value + ")";
        }

        // Enchantments will not have a value, only use the description.
        var s = (int)stat;
        if (s > 0)
        {
            return stat.Name + ": " + (int)stat;
        }

        if (stat.Name.Contains("%"))
        {
            // make sure we parse these not to give a huge amount of decimals.
            var v = stat.Name.Split(' ');
            var num = v[^1].Split('%')[0];
            if (float.TryParse(num.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return stat.Name.Replace(num, Math.Round(value, 2).ToString());
            }
        }

        return stat.Name;
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
        if (player.dungeonHandler.InDungeon) return PlayerLocation.Dungeon;
        if (player.raidHandler.InRaid) return PlayerLocation.Raid;
        if (player.streamRaidHandler.InWar) return PlayerLocation.War;
        if (player.onsenHandler.InOnsen) return PlayerLocation.Resting;
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
