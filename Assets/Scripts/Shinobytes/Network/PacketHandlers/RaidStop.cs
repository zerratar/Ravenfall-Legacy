public class RaidStop : ChatBotCommandHandler
{
    public RaidStop(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }
    public override void Handle(GameMessage gm, GameClient client)
    {
        Shinobytes.Debug.Log("Raid Stop Received");
        try
        {
            var plr = PlayerManager.GetPlayer(gm.Sender);
            if (!plr)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
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
            client.SendReply(gm, "Raid has been forcibly stopped.");
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to force stop raid: " + exc.Message);
        }
    }
}
