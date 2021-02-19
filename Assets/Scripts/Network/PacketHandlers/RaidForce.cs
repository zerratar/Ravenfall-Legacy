using RavenNest.Models;
using System.Threading;

public class RaidForce : PacketHandler<Player>
{
    private int scrollActive;
    public RaidForce(
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
                Game.Raid.IsBusy = true;
                client.SendMessage(data.Username, "Someone just used a raid scroll.");
                return;
            }

            if (Game.Dungeons.IsBusy)
            {
                client.SendMessage(data.Username, "Someone tried to use a dungeon scroll. Please wait before using a raid scroll.");
                return;
            }

            if (Game.Dungeons.Started)
            {
                client.SendMessage(data.Username, "Unable to start a raid during a dungeon. Please wait for it to be over.");
                return;
            }

            if (Game.StreamRaid.IsWar)
            {
                client.SendMessage(data.Username, "Unable to start a raid during a war. Please wait for it to be over.");
                return;
            }

            if (Game.Raid && !Game.Raid.Started && !Game.Raid.Boss)
            {
                if (Game.Events.IsActive)
                {
                    client.SendFormat(data.Username, "Raid cannot be started right now. Please try again later");
                    return;
                }

                var result = await Game.RavenNest.Game.ActivateRaidAsync(plr);
                if (result == ScrollUseResult.Success)
                {
                    var scrollsLeft = plr.Inventory.RemoveScroll(ScrollType.Raid);
                    client.SendFormat(data.Username, "You have used a Raid Scroll.");
                    Game.Raid.StartRaid(data.Username);
                }
                else
                {
                    switch (result)
                    {
                        case ScrollUseResult.InsufficientScrolls:
                            client.SendFormat(data.Username, "You do not have any Raid Scrolls! Redeem them under streamer loyalty on the website.");
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
            Game.Raid.IsBusy = false;
            Interlocked.Exchange(ref scrollActive, 0);
        }
    }
}