using RavenNest.Models;
using Shinobytes.Linq;
using System;

public class DuelCancel : ChatBotCommandHandler<User>
{
    public DuelCancel(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(User data, GameMessage gm, GameClient client)
    {
    }
}
