using System;
using System.Linq;
public class Highscore : ChatBotCommandHandler<string>
{
    public Highscore(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        var skillName = data;
        var player = PlayerManager.GetPlayer(gm.Sender);
        int result;

        if (player == null)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        //    if (player.CharacterIndex > 0)
        //{
        //    client.SendMessage(data.Player.Username, Localization.MSG_HIGHSCORE_MAIN_ONLY);
        //    return;
        //}

        skillName =
            string.IsNullOrEmpty(skillName)
            || skillName.Equals("all", StringComparison.OrdinalIgnoreCase)
            || skillName.Equals("overall", StringComparison.OrdinalIgnoreCase) ?
            "all" : skillName;

        result = await Game.RavenNest.Players.GetHighscoreAsync(player.Id, skillName);
        if (result == -2)
        {
            // Admin or moderators dont get to use this.
            Shinobytes.Debug.Log("Game Admin or Game Moderators cant use !hs command.");
            return;
        }

        if (result <= 0)
        {
            client.SendReply(gm, Localization.MSG_HIGHSCORE_BAD_SKILL, skillName);
            return;
        }

        client.SendReply(gm, Localization.MSG_HIGHSCORE_RANK, result);
    }
}
