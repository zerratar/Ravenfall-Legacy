using RavenNest.Models;

public class UseExpMultiplierScroll : ChatBotCommandHandler<SetExpMultiplierRequest>
{
    public UseExpMultiplierScroll(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override async void Handle(SetExpMultiplierRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (player == null || !player)
        {
            client.SendFormat(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var scrollCount = data.ExpMultiplier;
        if (scrollCount < 0) scrollCount = 1;
        if (scrollCount > 100) scrollCount = 100;

        var result = await Game.RavenNest.Game.UseExpScrollAsync(player, scrollCount);
        try
        {
            if (result.Result == ScrollUseResult.Success)
            {
                if (result.Used == 0 || Game.Boost.Multiplier >= 100 || Game.Boost.Active && Game.Boost.Multiplier >= Game.Twitch.ExpMultiplierLimit)
                {
                    client.SendFormat(data.Player.Username, "The maximum multiplier has already been reached. Please try again later.");
                    return;
                }

                var used = result.Used;
                for (var i = 0; i < used; ++i) player.Inventory.RemoveScroll(ScrollType.Experience);

                if (used > 1)
                {
                    client.SendFormat(data.Player.Username, "You have used {scrollCount} Exp Multiplier Scrolls. The multiplier is now at {multiplier}.", result.Used, result.Multiplier.Multiplier);
                }
                else
                {
                    client.SendFormat(data.Player.Username, "You have used an Exp Multiplier Scroll. The multiplier is now at {multiplier}.", result.Multiplier.Multiplier);
                }

                return;
            }

            if (result.Result == ScrollUseResult.InsufficientScrolls)
            {
                client.SendFormat(data.Player.Username, "You do not have any Exp Multiplier Scrolls! Redeem them under streamer loyalty on the website.");
                return;
            }

            client.SendFormat(data.Player.Username, "Unable to use any scrolls at this time, game may be out of sync. Try again later.");
        }
        finally
        {
            // regardless of error, update exp multiplier
            Game.HandleGameEvent(result.Multiplier);
        }
    }
}
