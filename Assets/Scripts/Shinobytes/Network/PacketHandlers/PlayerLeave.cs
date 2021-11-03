
public class PlayerLeave : PacketHandler<TwitchPlayerInfo>
{
    public PlayerLeave(
    GameManager game,
    RavenBotConnection server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (player)
        {

            if (Game.Dungeons.JoinedDungeon(player))
            {
                Game.Dungeons.Remove(player);
            }

            if (!Game.Arena.Started && Game.Arena.Activated && player.Arena.InArena)
            {
                Game.Arena.Leave(player);
            }
            else if (!Game.Arena.CanJoin(player, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
            {
                client.SendMessage(data.Username, "You cannot leave as you are participating in the arena that has already started and may break it. You have been queued up to leave after the arena has been finished.");
                Game.QueueRemovePlayer(player);
                return;
            }

            if (player.Duel.InDuel)
            {
                client.SendMessage(data.Username, "You cannot leave while fighting a duel. You have been queued up to leave after the duel has ended.");
                Game.QueueRemovePlayer(player);
                return;
            }

            Game.RemovePlayer(player);
            client.SendMessage(data.Username, "You have left the game.");
        }
        else
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
        }
    }
}
