using RavenNest.Models;
using System;
using System.Linq;

public class UseFerryBoostScroll : ChatBotCommandHandler
{
    public UseFerryBoostScroll(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        try
        {
            var ferryScroll = player.Inventory
                .GetInventoryItemsOfCategory(ItemCategory.Scroll)
                .FirstOrDefault(x => x.Name.Contains("ferry", StringComparison.OrdinalIgnoreCase));

            if (ferryScroll == null)
            {
                client.SendReply(gm, "You don't have any ferry boost scrolls.");
                return;
            }

            var result = await Game.RavenNest.Players.UseItemAsync(player.Id, ferryScroll.InventoryItem.Id);
            if (result == null || result.InventoryItemId == Guid.Empty || result.Effects == null || result.Effects.Count == 0)
            {
                client.SendReply(gm, "{itemName} can not be used right now.", ferryScroll.InventoryItem.Name);
                return;
            }

            player.Inventory.UpdateInventoryItem(result.InventoryItemId, result.NewStackAmount);
            Game.Ferry.ApplyFerryBoost(result.Effects.FirstOrDefault());
            client.SendReply(gm, $"The ferry will now move faster for {Game.Ferry.GetRemainingBoostTime()}. ");
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error using ferry boost scroll: " + exc);
            client.SendReply(gm, "Error using ferry boost scroll");
            return;
        }
    }
}
