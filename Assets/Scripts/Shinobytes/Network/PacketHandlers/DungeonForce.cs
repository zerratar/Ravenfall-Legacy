using RavenNest.Models;
using System.Threading;
public class DungeonForce : PacketHandler<TwitchPlayerInfo>
{
    private int scrollActive;
    public DungeonForce(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override async void Handle(TwitchPlayerInfo data, GameClient client)
    {
        try
        {
            var plr = PlayerManager.GetPlayer(data);
            if (!plr)
            {
                client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (Game.Dungeons.IsBusy)
            {
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

                Game.Dungeons.IsBusy = true;
                var result = await Game.RavenNest.Game.ActivateDungeonAsync(plr);
                if (result == ScrollUseResult.Success)
                {
                    if (Game.Dungeons.ActivateDungeon())
                    {
                        var scrollsLeft = plr.Inventory.RemoveScroll(ScrollType.Dungeon);
                        client.SendFormat(data.Username, "You have used a Dungeon Scroll.");
                    }
                    else
                    {
                        client.SendFormat(data.Username, "Dungeon could not be started. Try again later");
                        var dungeonScroll = Game.Items.Find(x => x.Name.ToLower().Contains("dungeon") && x.Category == ItemCategory.Scroll);
                        if (dungeonScroll != null)
                        {
                            await Game.RavenNest.Players.AddItemAsync(plr.UserId, dungeonScroll.Id);
                        }
                    }
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
        catch (System.Exception exc)
        {
            GameManager.LogError(exc.ToString());
        }
        finally
        {
            Game.Dungeons.IsBusy = false;
        }
    }
}
