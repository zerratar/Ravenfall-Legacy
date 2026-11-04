public class ArenaBegin : ChatBotCommandHandler<User>
{
    public ArenaBegin(
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

        client.SendReply(gm, Game.Arena.ForceStart() ? Localization.MSG_ARENA_FORCE_STARTED : Localization.MSG_ARENA_FORCE_START_FAILED);
    }
}