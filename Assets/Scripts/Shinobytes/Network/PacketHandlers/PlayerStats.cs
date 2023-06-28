using System;
using System.Linq;

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
                var expReq = Utility.FormatExp(expRequired);
                var curExp = Utility.FormatExp(skill.Experience);
                client.SendReply(gm, Localization.MSG_SKILL,
                    skill.ToString(), curExp, expReq);
            }
            return;
        }

        SendPlayerStats(gm, player, client);
    }
    private static string FormatValue(long num)
    {
        var str = num.ToString();
        if (str.Length <= 3) return str;
        for (var i = str.Length - 3; i >= 0; i -= 3)
            str = str.Insert(i, " ");
        return str;
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
            eq.BaseWeaponPower.ToString(),
            eq.BaseWeaponAim.ToString(),
            eq.BaseArmorPower.ToString(),
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
            var f = player.GetExpFactor();
            var expPerTick = player.GetExperience(s, f);
            var estimatedExpPerHour = expPerTick * GameMath.Exp.GetTicksPerMinute(s) * 60;
            var nextLevelExp = GameMath.ExperienceForLevel(skill.Level + 1);
            expPerHour = Math.Min(estimatedExpPerHour, skill.GetExperiencePerHour());
            expLeft = nextLevelExp - skill.Experience;
            var hours = expLeft / expPerHour;
            if (hours < System.TimeSpan.MaxValue.TotalHours)
            {
                var left = TimeSpan.FromHours(hours);
                if (left.TotalDays < 365)
                {
                    nextLevel = DateTime.UtcNow.Add(left);
                }
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