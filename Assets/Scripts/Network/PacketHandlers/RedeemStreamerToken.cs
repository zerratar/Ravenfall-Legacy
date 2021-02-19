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

        if (await HandleRedeemExpMultiplierAsync(player, client, data.ItemQuery))
            return;

        await HandleRedeemItemAsync(player, client, data.ItemQuery);
    }

    private async Task<bool> HandleRedeemExpMultiplierAsync(PlayerController player, GameClient client, string itemQuery)
    {
        var sections = itemQuery.Split(' ');
        if (!sections.Any(x => Is(x, "exp") || Is(x, "xp") || Is(x, "multiplier") || Is(x, "multi")))
            return false;

        var amount = sections.Last();
        int increaseMultiplierBy;
        int.TryParse(amount, out increaseMultiplierBy);

        increaseMultiplierBy = Math.Max(1, increaseMultiplierBy);

        var limit = Game.Twitch.ExpMultiplierLimit;
        var left = limit - Game.Twitch.CurrentBoost.Multiplier;

        if (left > 0)
        {
            increaseMultiplierBy = Math.Min((int)left, increaseMultiplierBy);
        }

        if (increaseMultiplierBy == 0)
        {
            client.SendMessage(player, "You cannot redeem 0 tokens.");
            return true;
        }

        var result = await Game.RavenNest.Players.RedeemTokensAsync(player.UserId, increaseMultiplierBy, false);
        if (result == 0)
        {
            client.SendMessage(player, "You dont have enough tokens to activate a multiplier. Sub, Gift sub, Cheer bits or play Tavern games to gain more tokens.");
            return true;
        }

        client.SendMessage(player, "You redeemed {increaseMultiplierBy} tokens towards the exp multiplier.", increaseMultiplierBy.ToString());
        player.Inventory.RemoveStreamerTokens(result);
        Game.Twitch.IncreaseExpMultiplier(player.Name, result);
        return true;
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

        var tokens = GetTokenCount(player);
        if (redeemable.Cost > tokens)
        {
            client.SendFormat(player.PlayerName, Localization.MSG_REDEEM_INSUFFICIENT_TOKENS,
                (int)tokens,
                redeemable.Cost);
            return;
        }

        var result = await Game.RavenNest.Players.RedeemTokensAsync(player.UserId, redeemable.Cost, true);
        if (result == 0)
        {
            client.SendFormat(player.PlayerName, Localization.MSG_REDEEM_INSUFFICIENT_TOKENS,
                (int)tokens,
                redeemable.Cost);

            return;
        }

        var addItemResult = await Game.RavenNest.Players.AddItemAsync(player.UserId, item.Item.Id);
        if (addItemResult == RavenNest.Models.AddItemResult.AddedAndEquipped)
        {
            player.Inventory.RemoveStreamerTokens(result);
            player.Inventory.Add(item.Item);
            player.Inventory.Equip(item.Item);
            client.SendFormat(player.PlayerName, Localization.MSG_REDEEM_EQUIP,
                item.Item.Name,
                redeemable.Cost,
                tokens - redeemable.Cost);
        }
        else if (addItemResult == RavenNest.Models.AddItemResult.Added)
        {
            player.Inventory.RemoveStreamerTokens(result);
            player.Inventory.Add(item.Item);
            client.SendFormat(player.PlayerName, Localization.MSG_REDEEM,
                item.Item.Name,
                redeemable.Cost,
                tokens - redeemable.Cost);
        }
        else
        {
            client.SendMessage(player, Localization.MSG_REDEEM_FAILED, item.Item.Name);
            await Game.RavenNest.Players.AddTokensAsync(player.UserId, result);
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

    private decimal GetTokenCount(PlayerController player)
    {
        var streamerTokens = player.Inventory
            .GetInventoryItemsOfCategory(RavenNest.Models.ItemCategory.StreamerToken)
            .Where(x => x.Tag == null || x.Tag == Game.RavenNest.TwitchUserId)
            .ToList();

        var streamerTokenCount = 0m;
        if (streamerTokens.Count > 0)
        {
            streamerTokenCount = streamerTokens.Sum(x => x.Amount);
        }

        return streamerTokenCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Is(string a, string b)
    {
        return a.Equals(b, StringComparison.OrdinalIgnoreCase);
    }
}