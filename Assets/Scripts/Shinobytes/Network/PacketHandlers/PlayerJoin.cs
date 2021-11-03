using System;

public class PlayerJoin : PacketHandler<TwitchPlayerInfo>
{
    public PlayerJoin(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(TwitchPlayerInfo data, GameClient client)
    {
        await Game.Players.JoinAsync(data, client, true);
    }
}