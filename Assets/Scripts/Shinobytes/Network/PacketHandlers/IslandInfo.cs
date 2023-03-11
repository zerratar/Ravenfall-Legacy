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

        if (player.Dungeon.InDungeon)
        {
            client.SendReply(gm, $"You're currently in the dungeon.");
            return;
        }

        if (player.StreamRaid.InWar)
        {
            client.SendReply(gm, $"You're currently in a streamer raid war.");
            return;
        }

        var islandName = player.Island?.Identifier;
        if (string.IsNullOrEmpty(islandName))
        {
            if (player.Ferry.OnFerry)
            {
                var dest = player.Ferry.Destination;
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
                var moderatorMessage = "";
                var arg0 = "";
                var arg1 = "";
                if (moderators.Count > 0)
                {
                    var moderator = moderators.Random();
                    arg0 = moderator.PlayerName;
                    arg1 = player.PlayerName;
                    moderatorMessage = " {moderatorName}, could you please use !show @{player} to see if player is stuck?";
                }

                client.SendReply(gm, $"Uh oh. Your character may be stuck." + moderatorMessage, arg0, arg1);
            }
        }
        else
        {
            if (player.Dungeon.InDungeon)
            {
                client.SendReply(gm, $"You are currently inside the dungeon.");
                return;
            }

            client.SendReply(gm, Localization.MSG_ISLAND, islandName);
        }
    }
}
