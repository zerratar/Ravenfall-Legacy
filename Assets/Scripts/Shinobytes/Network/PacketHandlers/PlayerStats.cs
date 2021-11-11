﻿using System.Linq;
using System.Text;
using UnityEngine;

public class PlayerStats : PacketHandler<PlayerStatsRequest>
{
    public PlayerStats(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(PlayerStatsRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
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
                    skill = player.GetSkill((TaskSkill)cs);
                }
            }

            if (skill != null)
            {
                var expRequired = GameMath.ExperienceForLevel(skill.Level + 1);
                var expReq = (long)expRequired;
                var curExp = (long)skill.Experience;
                client.SendMessage(data.Player.Username, Localization.MSG_SKILL,
                    skill.ToString(),
                    FormatValue(curExp),
                    FormatValue(expReq));
            }
            return;
        }

        SendPlayerStats(player, client);
    }
    private static string FormatValue(long num)
    {
        var str = num.ToString();
        if (str.Length <= 3) return str;
        for (var i = str.Length - 3; i >= 0; i -= 3)
            str = str.Insert(i, " ");
        return str;
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

        client.SendMessage(player.PlayerName, Localization.MSG_STATS,
            combatLevel.ToString(),
            skills,
            total.ToString(),
            eq.WeaponPower.ToString(),
            eq.WeaponAim.ToString(),
            eq.ArmorPower.ToString());
    }
}