public class ArenaKick : ChatBotCommandHandler<User>
{
    public ArenaKick(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(User data, GameMessage gm, GameClient client)
    {
        // the arena's streamer and moderator actions were empty and answered nothing
        var user = gm.Sender;
        var self = PlayerManager.GetPlayer(user);
        if (!user.IsBroadcaster && !user.IsModerator && !(self && (self.IsGameAdmin || self.IsGameModerator)))
        {
            return;
        }

        var target = data != null ? PlayerManager.GetPlayer(data) : null;
        if (!target)
        {
            client.SendReply(gm, Localization.MSG_ARENA_PLAYER_NOT_FOUND, data?.Username ?? "");
            return;
        }

        if (Game.Arena.Kick(target)) client.SendReply(gm, Localization.MSG_ARENA_KICKED, target.PlayerName);
        else client.SendReply(gm, Localization.MSG_ARENA_NOT_JOINED_PLAYER, target.PlayerName);
    }
}