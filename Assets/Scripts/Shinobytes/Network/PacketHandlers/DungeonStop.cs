﻿public class DungeonStop : ChatBotCommandHandler
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
            var user = gm.Sender;

            if (Game.Dungeons && !Game.Dungeons.Started)
            {
                return;
            }

            if (!user.IsBroadcaster && !user.IsModerator)
            {
                return;
            }

            Game.Dungeons.EndDungeonFailed(false);
            client.SendReply(gm, "Dungeon has been forcibly stopped.");
        }
        catch { }
    }
}
