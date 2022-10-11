
public class PlayerLeave : ChatBotCommandHandler<TwitchPlayerInfo>
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
            else if (player.Duel.InDuel||!Game.Arena.CanJoin(player, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
            {                
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
