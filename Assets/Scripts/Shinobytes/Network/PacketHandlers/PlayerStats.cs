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
                var expReq = (long)expRequired;
                var curExp = (long)skill.Experience;
                client.SendReply(gm, Localization.MSG_SKILL,
                    skill.ToString(),
                    FormatValue(curExp),
                    FormatValue(expReq));
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
            eq.WeaponPower.ToString(),
            eq.WeaponAim.ToString(),
            eq.ArmorPower.ToString());
    }
}