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
        if (Game.SessionSettings != null)
        {
            if (Game.SessionSettings.PlayerExpMultiplierLimit > 0)
            {
                expLimit = Game.SessionSettings.PlayerExpMultiplierLimit;
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
                    client.SendReply(gm, Localization.MSG_EXP_SCROLL_MAX_REACHED);
                    return;
                }

                var used = result.Used;
                for (var i = 0; i < used; ++i) player.Inventory.RemoveScroll(ScrollType.Experience);

                if (used > 1)
                {
                    client.SendReply(gm, Localization.MSG_EXP_SCROLL_USED_MANY, result.Used, result.Multiplier.Multiplier);
                }
                else
                {
                    client.SendReply(gm, Localization.MSG_EXP_SCROLL_USED_ONE, result.Multiplier.Multiplier);
                }

                return;
            }

            if (result.Result == ScrollUseResult.InsufficientScrolls)
            {
                client.SendReply(gm, Localization.MSG_EXP_SCROLL_NONE);
                return;
            }

            client.SendReply(gm, Localization.MSG_EXP_SCROLL_OUT_OF_SYNC);
        }
        catch (Exception exc)
        {
            var playerName = player?.Name;
            Shinobytes.Debug.LogError("Exception using multiplier scroll, Player: " + playerName + ", Scrolls: " + scrollCount + ", Error: " + exc);
            client.SendReply(gm, Localization.MSG_EXP_SCROLL_ERROR);
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
