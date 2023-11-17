using RavenNest.Models;
using System.Threading;

public class RaidForce : ChatBotCommandHandler
{
    private int scrollActive;
    public RaidForce(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {
        try
        {
            var plr = PlayerManager.GetPlayer(gm.Sender);
            if (!plr)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (Game.Raid.IsBusy)
            {
                client.SendReply(gm, "Someone just used a raid scroll.");
                return;
            }

            if (Game.Dungeons.IsBusy)
            {
                client.SendReply(gm, "Someone tried to use a dungeon scroll. Please wait before using a raid scroll.");
                return;
            }

            if (Game.Dungeons.Started)
            {
                client.SendReply(gm, "Unable to start a raid during a dungeon. Please wait for it to be over.");
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendReply(gm, "Unable to start a raid during a war. Please wait for it to be over.");
                return;
            }

            if (Game.Raid && !Game.Raid.Started && !Game.Raid.Boss)
            {
                if (Game.Events.IsActive)
                {
                    client.SendReply(gm, "Raid cannot be started right now. Please try again later");
                    return;
                }

                Game.Raid.IsBusy = true;
                var result = await Game.RavenNest.Game.ActivateRaidAsync(plr);
                if (result == ScrollUseResult.Success)
                {
                    var scrollsLeft = plr.Inventory.RemoveScroll(ScrollType.Raid);

                    if (!await Game.Raid.StartRaid(plr, req =>
                    {
                        if (Game.Raid.CanJoin(plr, string.Empty) == RaidJoinResult.CanJoin)
                        {
                            client.SendReply(gm, "You have used a Raid Scroll and joined the raid. Good luck!");
                            Game.Raid.Join(plr);
                        }
                        else
                        {
                            client.SendReply(gm, "You have used a Raid Scroll.");
                        }

                        Game.Raid.Announce();
                    }))
                    {
                        var raidScroll = Game.Items.Find(x => x.Name.Contains("raid", System.StringComparison.OrdinalIgnoreCase) && x.Type == ItemType.Scroll);
                        if (raidScroll != null)
                        {
                            client.SendReply(gm, "Raid could not be started. Scroll will be refunded.");
                            await Game.RavenNest.Players.AddItemAsync(plr.Id, raidScroll.Id);
                            plr.Inventory.AddToBackpack(raidScroll);
                        }
                    }
                }
                else
                {
                    switch (result)
                    {
                        case ScrollUseResult.InsufficientScrolls:
                            client.SendReply(gm, "You do not have any Raid Scrolls! Redeem them under streamer loyalty on the website.");
                            return;
                        case ScrollUseResult.Error:
                            client.SendReply(gm, "Server was not able to give back a valid response. Uh oh.. BUG!");
                            return;
                    }
                }

            }
        }
        catch { }
        finally
        {
            Game.Raid.IsBusy = false;
        }
    }
}