
public class PlayerLeave : ChatBotCommandHandler
{
    public PlayerLeave(
    GameManager game,
    RavenBotConnection server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player)
        {

            if (Game.Dungeons.JoinedDungeon(player))
            {
                Game.Dungeons.Remove(player);
            }

            if (!Game.Arena.Started && Game.Arena.Activated && player.arenaHandler.InArena)
            {
                Game.Arena.Leave(player);
            }
            else if (player.duelHandler.InDuel || !Game.Arena.CanJoin(player, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
            {
                Game.QueueRemovePlayer(player);
                return;
            }

            Game.RemovePlayer(player);
            client.SendReply(gm, "You have left the game.");
        }
        else
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
        }
    }
}
