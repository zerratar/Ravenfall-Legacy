using System;

public class PlayerJoin : ChatBotCommandHandler
{
    public PlayerJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {
        await Game.Players.JoinAsync(gm, gm.Sender, client, true, false, null);
    }
}