public class DungeonStop : ChatBotCommandHandler
{
    public DungeonStop(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }
    public override void Handle(GameMessage gm, GameClient client)
    {
        Shinobytes.Debug.Log("Dungeon Stop Received");
        try
        {
            var plr = PlayerManager.GetPlayer(gm.Sender);
            if (!plr)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
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

            Game.Dungeons.EndDungeonFailed(false);
            client.SendReply(gm, "Dungeon has been forcibly stopped.");
        }
        catch { }
    }
}
