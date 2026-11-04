public class ArenaAdd : ChatBotCommandHandler<User>
{
    public ArenaAdd(
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

        if (target.Island != Game.Arena.Island || target.ferryHandler.OnFerry || target.InRavebridge || target.raidHandler.InRaid
            || target.dungeonHandler.InDungeon || target.duelHandler.InDuel || !Game.Arena.CanJoin(target, out _, out _))
        {
            client.SendReply(gm, Localization.MSG_ARENA_ADD_FAILED, target.PlayerName);
            return;
        }

        Game.Arena.Join(target);
        client.SendReply(gm, Game.Arena.HasJoined(target) ? Localization.MSG_ARENA_ADDED : Localization.MSG_ARENA_ADD_FAILED, target.PlayerName);
    }
}