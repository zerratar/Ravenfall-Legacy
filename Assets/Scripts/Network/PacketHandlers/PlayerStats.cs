using System.Linq;

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

        if (!string.IsNullOrEmpty(data.Skill))
        {
            var targetPlayer = PlayerManager.GetPlayerByName(data.Skill);
            if (targetPlayer != null)
            {
                SendPlayerStats(targetPlayer, client);
                return;
            }

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
                    skill = player.GetSkill((Skill)cs);
                }
            }

            if (skill != null)
            {
                client.SendCommand(data.Player.Username, "player_stats", skill.ToString() + ", " + skill.Experience + " EXP.");
            }
            return;
        }

        SendPlayerStats(player, client);
    }

    private void SendPlayerStats(PlayerController player, GameClient client)
    {
        var ps = player.Stats;
        var eq = player.EquipmentStats;
        var combatLevel = ps.CombatLevel;
        var skills = "";
        var total = ps.SkillList
            .Select(x => { skills += x + ", "; return x; })
            .Sum(x => x.Level);

        client.SendCommand(player.PlayerName,
            "player_stats",
            $"Combat level {combatLevel}, " +
            skills +
            $" -- TOTAL {total} --, " +
            $"Eq - power {eq.WeaponPower}, " +
            $"aim {eq.WeaponAim}, " +
            $"armor {eq.ArmorPower}");
    }
}