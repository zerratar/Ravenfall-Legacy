using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class RedeemStreamerToken : ChatBotCommandHandler<string>
{
    public RedeemStreamerToken(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override async void Handle(string query, GameMessage gm, GameClient client)
    {

        if (Game.Tavern.MaintenanceMode)
            return;

        var sender = gm.Sender;
        var player = PlayerManager.GetPlayer(sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        // the streamer's tavern shelves first: a shelf number, or a name only the shelves have
        var shop = await TavernShopRedeem.TryHandleAsync(Game, client, gm, player, query);
        if (shop.Handled)
            return;

        if (!Game.Tavern.CanRedeemItems)
            return;

        // without the amount, which only the shelves use: token redeemables are one at a time
        await HandleRedeemItemAsync(gm, player, client, string.IsNullOrWhiteSpace(shop.Text) ? query : shop.Text);
        if (!string.IsNullOrEmpty(shop.AlsoOnShelf))
            client.SendReply(gm, shop.AlsoOnShelf);
    }
    private async Task HandleRedeemItemAsync(GameMessage gm, PlayerController player, GameClient client, string itemQuery)
    {
        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.ResolveAny(itemQuery, itemQuery + " pet");

        if (item.Item == null)
        {
            if (item.SuggestedItemNames.Length > 0)
            {
                client.SendReply(gm, Localization.MSG_REDEEM_ITEM_NOT_FOUND_SUGGEST, string.Join(", ", item.SuggestedItemNames));
                return;
            }

            client.SendReply(gm, Localization.MSG_REDEEM_ITEM_NOT_FOUND, itemQuery);
            return;
        }

        try
        {
            var redeemables = Game.Items.GetRedeemables();
            var redeemable = redeemables.FirstOrDefault(x =>
                        x.ItemId == item.Id ||
                        (x.Name?.Equals(item.Item.Name, StringComparison.OrdinalIgnoreCase)).GetValueOrDefault()
            );

            if (redeemable.ItemId == Guid.Empty && string.IsNullOrEmpty(redeemable.Name))
            {
                client.SendReply(gm, Localization.MSG_REDEEM_ITEM_NOT_FOUND, itemQuery);
                return;
            }

            if (!redeemable.IsRedeemable() || redeemable.Cost <= 0)
            {
                //NotRedeemableRightNow(redeemable, player, client);
                client.SendReply(gm, Localization.MSG_REDEEM_NOT_REDEEMABLE);
                return;
            }

            var result = await Game.RavenNest.Players.RedeemItemAsync(player.Id, item.Id);
            var targetItem = Game.Items.Find(x => x.Id == result.RedeemedItemId);
            var targetCurrency = Game.Items.Find(x => x.Id == result.CurrencyItemId);
            switch (result.Code)
            {
                case RavenNest.Models.RedeemItemResultCode.Success:
                    //player.Inventory.RemoveByItemId(result.CurrencyItemId, result.CurrencyCost);
                    client.SendReply(gm, Localization.MSG_REDEEM_TOKEN_SUCCESS,
                        targetItem?.Name,
                        result.CurrencyCost,
                        targetCurrency?.Name,
                        result.CurrencyLeft);
                    break;

                case RavenNest.Models.RedeemItemResultCode.InsufficientCurrency:
                    client.SendReply(gm, Localization.MSG_REDEEM_TOKEN_FAILED_REASON, targetItem?.Name, result.ErrorMessage);
                    break;

                case RavenNest.Models.RedeemItemResultCode.NoSuchItem:
                case RavenNest.Models.RedeemItemResultCode.Error:
                    client.SendReply(gm, Localization.MSG_REDEEM_TOKEN_FAILED, item.Item.Name);
                    break;
            }
        }
        catch (Exception exc)
        {
            client.SendReply(gm, Localization.MSG_REDEEM_ITEM_NOT_FOUND, itemQuery);
            Shinobytes.Debug.LogError("Failed to redeem tokens: " + exc);
        }
    }
}