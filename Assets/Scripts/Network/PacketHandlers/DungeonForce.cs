using RavenNest.Models;
using System.Threading;

public class DungeonForce : PacketHandler<Player>
{
    private int scrollActive;
    public DungeonForce(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override async void Handle(Player data, GameClient client)
    {
        try
        {
            var plr = PlayerManager.GetPlayer(data);
            if (!plr)
            {
                client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (Interlocked.CompareExchange(ref scrollActive, 1, 0) == 1)
            {
                Game.Dungeons.IsBusy = true;
                client.SendMessage(data.Username, "Someone just used a dungeon scroll.");
                return;
            }

            if (Game.Raid.IsBusy)
            {
                client.SendMessage(data.Username, "Someone tried to use a raid scroll. Please wait before using a dungeon scroll.");
                return;
            }

            if (Game.Raid.Started)
            {
                client.SendMessage(data.Username, Localization.MSG_DUNGEON_START_FAILED_RAID);
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendMessage(data.Username, Localization.MSG_DUNGEON_START_FAILED_WAR);
                return;
            }

            if (Game.Dungeons && !Game.Dungeons.Started)
            {
                if (Game.Events.IsActive)
                {
                    client.SendFormat(data.Username, "Dungeon cannot be started right now. Please try again later");
                    return;
                }

                var result = await Game.RavenNest.Game.ActivateDungeonAsync(plr);
                if (result == ScrollUseResult.Success)
                {
                    var scrollsLeft = plr.Inventory.RemoveScroll(ScrollType.Dungeon);
                    client.SendFormat(data.Username, "You have used a Dungeon Scroll.");
                    Game.Dungeons.ActivateDungeon();
                }
                else
                {
                    switch (result)
                    {
                        case ScrollUseResult.InsufficientScrolls:
                            client.SendFormat(data.Username, "You do not have any Dungeon Scrolls! Redeem them under streamer loyalty on the website.");
                            return;
                        case ScrollUseResult.Error:
                            client.SendFormat(data.Username, "Server was not able to give back a valid response. Uh oh.. BUG!");
                            return;
                    }
                }
            }
        }
        finally
        {
            Game.Dungeons.IsBusy = false;
            Interlocked.Exchange(ref scrollActive, 0);
        }
    }
}
