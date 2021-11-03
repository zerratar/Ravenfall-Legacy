using RavenNest.Models;

public class UseExpMultiplierScroll : PacketHandler<SetExpMultiplierRequest>
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

        var result = await Game.RavenNest.Game.ActivateExpMultiplierAsync(player, scrollCount);

        if (result > 0)
        {
            for (var i = 0; i < result; ++i) player.Inventory.RemoveScroll(ScrollType.Experience);
            if (result > 1)
            {
                client.SendFormat(data.Player.Username, "You have used {scrollCount} Exp Multiplier Scrolls.", result);
            }
            else
            {
                client.SendFormat(data.Player.Username, "You have used an Exp Multiplier Scroll.");
            }
        }
        else
        {
            if (result == 0 || Game.Boost.Multiplier >= 100 || Game.Boost.Active && Game.Boost.Multiplier >= Game.Twitch.ExpMultiplierLimit)
            {
                client.SendFormat(data.Player.Username, "The maximum multiplier has already been reached. Please try again later.");
                return;
            }

            if (result == -1)
            {
                client.SendFormat(data.Player.Username, "Server was not able to give back a valid response. Uh oh.. BUG! Try again later.");
                return;
            }

            if (result == -2)
            {
                client.SendFormat(data.Player.Username, "You do not have any Exp Multiplier Scrolls! Redeem them under streamer loyalty on the website.");
                return;
            }
        }
    }
}
