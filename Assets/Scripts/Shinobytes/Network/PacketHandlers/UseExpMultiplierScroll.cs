using RavenNest.Models;
using System;

public class UseExpMultiplierScroll : ChatBotCommandHandler<int>
{
    public UseExpMultiplierScroll(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override async void Handle(int data, GameMessage gm, GameClient client)
    {
        UseExpScrollResult result = null;
        PlayerController player = null;
        
        var expLimit = 100;
        if (Game.Permissions != null)
        {
            if (Game.Permissions.PlayerExpMultiplierLimit > 0)
            {
                expLimit = Game.Permissions.PlayerExpMultiplierLimit;
            }
        }

        var scrollCount = data;
        if (scrollCount < 0) scrollCount = 1;
        if (scrollCount > expLimit) scrollCount = expLimit;

        try
        {

            player = PlayerManager.GetPlayer(gm.Sender);
            if (player == null || !player)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }

            result = await Game.RavenNest.Game.UseExpScrollAsync(player, scrollCount);

            if (result.Result == ScrollUseResult.Success)
            {
                if (result.Used == 0 || Game.Boost.Multiplier >= expLimit || Game.Boost.Active && Game.Boost.Multiplier >= Game.Twitch.ExpMultiplierLimit)
                {
                    client.SendReply(gm, "The maximum multiplier has already been reached.");
                    return;
                }

                var used = result.Used;
                for (var i = 0; i < used; ++i) player.Inventory.RemoveScroll(ScrollType.Experience);

                if (used > 1)
                {
                    client.SendReply(gm, "You have used {scrollCount} Exp Multiplier Scrolls. The multiplier is now at {multiplier}.", result.Used, result.Multiplier.Multiplier);
                }
                else
                {
                    client.SendReply(gm, "You have used an Exp Multiplier Scroll. The multiplier is now at {multiplier}.", result.Multiplier.Multiplier);
                }

                return;
            }

            if (result.Result == ScrollUseResult.InsufficientScrolls)
            {
                client.SendReply(gm, "You do not have any Exp Multiplier Scrolls! Redeem them under streamer loyalty on the website.");
                return;
            }

            client.SendReply(gm, "Unable to use any scrolls at this time, game may be out of sync. Try again later.");
        }
        catch (Exception exc)
        {
            var playerName = player?.Name;
            Shinobytes.Debug.LogError("Exception using multiplier scroll, Player: " + playerName + ", Scrolls: " + scrollCount + ", Error: " + exc);
            client.SendReply(gm, "Unable to use any scrolls at this time, game may be out of sync. Try again later. Error has been logged.");
        }
        finally
        {
            if (result != null)
            {
                // regardless of error, update exp multiplier
                Game.HandleGameEvent(result.Multiplier);
            }
        }
    }
}
