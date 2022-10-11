public class RaidStop : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public RaidStop(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }
    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        Shinobytes.Debug.Log("Raid Stop Received");
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
        catch (System.Exception exc)
        {
            UnityEngine.Debug.LogError("Unable to force stop raid: " + exc.Message);
        }
    }
}
