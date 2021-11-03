public class DungeonStop : PacketHandler<TwitchPlayerInfo>
{
    public DungeonStop(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }
    public override async void Handle(TwitchPlayerInfo data, GameClient client)
    {
        GameManager.Log("Dungeon Stop Received");
        try
        {
            var plr = PlayerManager.GetPlayer(data);
            if (!plr)
            {
                client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (Game.Dungeons && !Game.Dungeons.Started)
            {
                return;
            }

            if (!plr.IsBroadcaster && !plr.IsModerator)
            {
                return;
            }

            Game.Dungeons.EndDungeonFailed();
            client.SendMessage(data.Username, "Dungeon has been forcibly stopped.");
        }
        catch { }
    }
}
