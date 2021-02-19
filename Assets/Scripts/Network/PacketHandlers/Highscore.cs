using System;
using System.Linq;
public class Highscore : PacketHandler<HighestSkillRequest>
{
    public Highscore(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(HighestSkillRequest data, GameClient client)
    {
        var skillName = data.Skill;
        var player = PlayerManager.GetPlayer(data.Player);
        int result;

        if (player == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (player.CharacterIndex > 0)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_HIGHSCORE_MAIN_ONLY);
            return;
        }

        skillName =
            string.IsNullOrEmpty(skillName)
            || skillName.Equals("all", StringComparison.OrdinalIgnoreCase)
            || skillName.Equals("overall", StringComparison.OrdinalIgnoreCase) ?
            "all" : skillName;

        result = await Game.RavenNest.Players.GetHighscoreAsync(player.Id, skillName);

        if (result <= 0)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_HIGHSCORE_BAD_SKILL, skillName);
            return;
        }

        client.SendFormat(data.Player.Username, Localization.MSG_HIGHSCORE_RANK, result);
    }
}
