using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class RedeemStreamerToken : PacketHandler<TradeItemRequest>
{
    public RedeemStreamerToken(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        if (Game.Tavern.MaintenanceMode || !Game.Tavern.CanRedeemItems)
            return;

        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        await HandleRedeemItemAsync(player, client, data.ItemQuery);
    }
    private async Task HandleRedeemItemAsync(PlayerController player, GameClient client, string itemQuery)
    {
        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(itemQuery);

        if (item == null)
        {
            NoSuchRedeemableItem(player, client, itemQuery);
            return;
        }

        var redeemable = Game.Items.Redeemable.FirstOrDefault(x =>
                x.ItemId == item.Item.Id ||
                (x.Name?.Equals(item.Item.Name, StringComparison.OrdinalIgnoreCase)).GetValueOrDefault());

        if (redeemable.ItemId == Guid.Empty && string.IsNullOrEmpty(redeemable.Name))
        {
            NoSuchRedeemableItem(player, client, itemQuery);
            return;
        }

        if (redeemable.Cost <= 0)
        {
            ItemUnavailable(player, client);
            return;
        }

        var result = await Game.RavenNest.Players.RedeemItemAsync(player.Id, item.Item.Id);
        switch (result.Code)
        {
            case RavenNest.Models.RedeemItemResultCode.Success:
                player.Inventory.Remove(result.CurrencyItemId, result.CurrencyCost);
                client.SendFormat(player.PlayerName, "You have successefully redeemed a {itemName} for {amount} {currencyName} and now have {amountLeft} left.",
                    Game.Items.Find(x => x.Id == result.RedeemedItemId)?.Name,
                    result.CurrencyCost,
                    Game.Items.Find(x => x.Id == result.CurrencyItemId)?.Name,
                    result.CurrencyLeft);
                break;

            case RavenNest.Models.RedeemItemResultCode.InsufficientCurrency:
                client.SendFormat(player.PlayerName, "Unable to redeem {itemName}. " + result.ErrorMessage, Game.Items.Find(x => x.Id == result.RedeemedItemId)?.Name);
                break;

            case RavenNest.Models.RedeemItemResultCode.NoSuchItem:
            case RavenNest.Models.RedeemItemResultCode.Error:
                client.SendFormat(player.PlayerName, "Unable to redeem {itemName} right now.", item.Item.Name);
                break;
        }
    }

    private void ItemUnavailable(PlayerController player, GameClient client)
    {
        client.SendMessage(player.PlayerName, Localization.MSG_REDEEM_NOT_REDEEMABLE);
    }

    private static void NoSuchRedeemableItem(PlayerController player, GameClient client, string itemQuery)
    {
        client.SendMessage(player.PlayerName, Localization.MSG_REDEEM_ITEM_NOT_FOUND, itemQuery);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Is(string a, string b)
    {
        return a.Equals(b, StringComparison.OrdinalIgnoreCase);
    }
}