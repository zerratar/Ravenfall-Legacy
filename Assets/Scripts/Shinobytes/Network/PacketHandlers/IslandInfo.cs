using Shinobytes.Linq;
public class IslandInfo : PacketHandler<TwitchPlayerInfo>
{
    public IslandInfo(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (player.Dungeon.InDungeon)
        {
            client.SendMessage(data.Username, $"You're currently in the dungeon.");
            return;
        }

        if (player.StreamRaid.InWar)
        {
            client.SendMessage(data.Username, $"You're currently in a streamer raid war.");
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
                    client.SendMessage(data.Username, Localization.MSG_ISLAND_ON_FERRY_DEST, dest.Identifier);
                }
                else
                {
                    client.SendMessage(data.Username, Localization.MSG_ISLAND_ON_FERRY);
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

                client.SendMessage(data.Username, $"Uh oh. Your character may be stuck." + moderatorMessage, arg0, arg1);
            }
        }
        else
        {
            if (player.Dungeon.InDungeon)
            {
                client.SendMessage(data.Username, $"You are currently inside the dungeon.");
                return;
            }

            client.SendMessage(data.Username, Localization.MSG_ISLAND, islandName);
        }
    }
}
