public class ArenaLeave : ChatBotCommandHandler<User>
{
    public ArenaLeave(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(User data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (Game.Arena.Leave(player))
        {
            player.arenaHandler.WalkAwayFromArena();
            client.SendReply(gm, Localization.MSG_ARENA_LEFT);
            return;
        }

        client.SendReply(gm, Game.Arena.HasJoined(player) ? Localization.MSG_ARENA_LEAVE_STARTED : Localization.MSG_ARENA_NOT_JOINED);
    }
}