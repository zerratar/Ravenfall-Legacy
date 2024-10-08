﻿
using RavenNest.Models;

public class ReloadGame : ChatBotCommandHandler
{
    public ReloadGame(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var user = gm.Sender;

        if (user.IsGameAdministrator || user.IsGameModerator)
        {
            Game.SaveStateAndLoadScene();
            return;
        }

        if (!user.IsModerator && !user.IsBroadcaster)
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (!player)
            {
                return;
            }

            if (!player.IsGameAdmin && !player.IsGameModerator)
            {
                return;
            }
        }

        Game.SaveStateAndLoadScene();
    }
}