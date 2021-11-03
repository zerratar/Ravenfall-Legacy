public class RaidStop : PacketHandler<TwitchPlayerInfo>
{
    public RaidStop(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }
    public override async void Handle(TwitchPlayerInfo data, GameClient client)
    {
        GameManager.Log("Raid Stop Received");
        try
        {
            var plr = PlayerManager.GetPlayer(data);
            if (!plr)
            {
                client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (Game.Raid && !Game.Raid.Started)
            {
                return;
            }

            if (!plr.IsBroadcaster && !plr.IsModerator)
            {
                return;
            }

            Game.Raid.EndRaid(false, true);
            client.SendMessage(data.Username, "Raid has been forcibly stopped.");
        }
        catch { }
    }
}
