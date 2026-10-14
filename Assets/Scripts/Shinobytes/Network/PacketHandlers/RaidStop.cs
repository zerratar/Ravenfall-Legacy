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
            var user = gm.Sender;

            if (Game.Raid && !Game.Raid.Started)
            {
                return;
            }

            if (!user.IsBroadcaster && !user.IsModerator && !user.IsGameModerator && !user.IsGameAdministrator)
            {
                return;
            }

            Game.Raid.EndRaid(false, true);
            client.SendReply(gm, Localization.MSG_RAID_FORCE_STOPPED);
        }
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to force stop raid: " + exc.Message);
        }
    }
}
