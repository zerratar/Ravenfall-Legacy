using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class PlayerStats : PacketHandler<PlayerStatsRequest>
{
    public PlayerStats(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(PlayerStatsRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendCommand(data.Player.Username, "player_stats", "You are not currently playing. Use !join to start playing!");
            return;
        }

        var ps = player.Stats;
        var eq = player.EquipmentStats;

        if (!string.IsNullOrEmpty(data.Skill))
        {
            SkillStat skill = null;
            var csi = player.GetCombatTypeFromArgs(data.Skill);
            if (csi != -1)
            {
                skill = player.GetCombatSkill(csi);
            }

            if (skill == null)
            {
                var cs = player.GetSkillTypeFromArgs(data.Skill);
                if (cs != -1)
                {
                    skill = player.GetSkill(cs);
                }
            }

            if (skill != null)
            {
                client.SendCommand(data.Player.Username, "player_stats", skill.ToString());
            }
            return;
        }

        var combatLevel = ps.CombatLevel;
        var skills = "";
        var total = ps
            .GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.FieldType == typeof(SkillStat))
            .Select(x => { skills += (SkillStat)x.GetValue(ps) + ", "; return x; })
            .Sum(x => ((SkillStat)x.GetValue(ps)).Level);

        client.SendCommand(data.Player.Username,
            "player_stats",
            $"Combat level {combatLevel}, " +
            skills +
            $" -- TOTAL {total} --, " +
            $"Eq - power {eq.WeaponPower}, " +
            $"aim {eq.WeaponAim}, " +
            $"armor {eq.ArmorPower}");
    }
}