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

        var result = await Game.RavenNest.Game.ActivateExpMultiplierAsync(player);
        if (result == ScrollUseResult.Success)
        {
            var scrollsLeft = player.Inventory.RemoveScroll(ScrollType.Experience);
            client.SendFormat(data.Player.Username, "You have used an Exp Multiplier Scroll.");
        }
        else
        {
            switch (result)
            {
                case ScrollUseResult.InsufficientScrolls:
                    client.SendFormat(data.Player.Username, "You do not have any Exp Multiplier Scrolls! Redeem them under streamer loyalty on the website.");
                    return;
                case ScrollUseResult.Error:
                    client.SendFormat(data.Player.Username, "Server was not able to give back a valid response. Uh oh.. BUG! Try again later.");
                    return;
            }
        }
    }
}
