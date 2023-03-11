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

        if (Game.Tavern.MaintenanceMode || !Game.Tavern.CanRedeemItems)
            return;

        var sender = gm.Sender;
        var player = PlayerManager.GetPlayer(sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        await HandleRedeemItemAsync(gm, player, client, query);
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
            switch (result.Code)
            {
                case RavenNest.Models.RedeemItemResultCode.Success:
                    player.Inventory.RemoveByItemId(result.CurrencyItemId, result.CurrencyCost);
                    client.SendReply(gm, "You have successefully redeemed a {itemName} for {amount} {currencyName} and now have {amountLeft} left.",
                        Game.Items.Find(x => x.Id == result.RedeemedItemId)?.Name,
                        result.CurrencyCost,
                        Game.Items.Find(x => x.Id == result.CurrencyItemId)?.Name,
                        result.CurrencyLeft);
                    break;

                case RavenNest.Models.RedeemItemResultCode.InsufficientCurrency:
                    client.SendReply(gm, "Unable to redeem {itemName}. " + result.ErrorMessage, Game.Items.Find(x => x.Id == result.RedeemedItemId)?.Name);
                    break;

                case RavenNest.Models.RedeemItemResultCode.NoSuchItem:
                case RavenNest.Models.RedeemItemResultCode.Error:
                    client.SendReply(gm, "Unable to redeem {itemName} right now.", item.Item.Name);
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