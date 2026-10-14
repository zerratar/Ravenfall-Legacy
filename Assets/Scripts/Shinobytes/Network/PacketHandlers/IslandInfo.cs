using Shinobytes.Linq;
public class IslandInfo : ChatBotCommandHandler
{
    public IslandInfo(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (player.dungeonHandler.InDungeon)
        {
            client.SendReply(gm, Localization.MSG_IN_DUNGEON);
            return;
        }

        if (player.streamRaidHandler.InWar)
        {
            client.SendReply(gm, Localization.MSG_ISLAND_IN_WAR);
            return;
        }

        var islandName = player.Island?.Identifier;
        if (string.IsNullOrEmpty(islandName))
        {
            if (player.ferryHandler.OnFerry)
            {
                var dest = player.ferryHandler.Destination;
                if (dest != null)
                {
                    client.SendReply(gm, Localization.MSG_ISLAND_ON_FERRY_DEST, dest.Identifier);
                }
                else
                {
                    client.SendReply(gm, Localization.MSG_ISLAND_ON_FERRY);
                }
            }
            else
            {

                var moderators = PlayerManager.GetAllModerators();
                if (moderators.Count > 0)
                {
                    var moderator = moderators.Random();
                    client.SendReply(gm, Localization.MSG_ISLAND_STUCK_ASK_MODERATOR, moderator.PlayerName, player.PlayerName);
                }
                else
                {
                    client.SendReply(gm, Localization.MSG_ISLAND_STUCK);
                }
            }
        }
        else
        {
            if (player.dungeonHandler.InDungeon)
            {
                client.SendReply(gm, Localization.MSG_ISLAND_INSIDE_DUNGEON);
                return;
            }

            client.SendReply(gm, Localization.MSG_ISLAND, islandName);
        }
    }
}
