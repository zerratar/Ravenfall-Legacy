﻿public class ArenaEnd : PacketHandler<TwitchPlayerInfo>
{
    public ArenaEnd(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
    }
}