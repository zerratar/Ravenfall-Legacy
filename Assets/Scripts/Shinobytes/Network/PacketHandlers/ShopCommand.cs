using System;
using System.Globalization;
using System.Linq;
using RavenNest.SDK.Endpoints;

/// <summary>
/// !shop: anyone can list what is on the streamer's tavern shelves; the streamer alone can take a shelf
/// down (!shop remove 3), change a price (!shop price 3 2500) and see what the shelves have made for the
/// village (!shop report). Putting new items up is done in the game, in the shop window.
/// </summary>
public class ShopCommand : ChatBotCommandHandler<string>
{
    public ShopCommand(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override async void Handle(string arguments, GameMessage gm, GameClient client)
    {
        var parts = (arguments ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";

        try
        {
            if (sub == "" || sub == "list")
            {
                await List(gm, client);
                return;
            }

            if (gm.Sender == null || !gm.Sender.IsBroadcaster)
            {
                client.SendReply(gm, Localization.MSG_SHOP_STREAMER_ONLY);
                return;
            }

            switch (sub)
            {
                case "remove":
                case "down":
                case "takedown":
                    await Remove(gm, client, parts);
                    break;
                case "price":
                    await Price(gm, client, parts);
                    break;
                case "report":
                case "sales":
                    await Report(gm, client);
                    break;
                default:
                    client.SendReply(gm, Localization.MSG_SHOP_USAGE);
                    break;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("!shop failed: " + exc);
            client.SendReply(gm, Localization.MSG_SHOP_FAILED, exc.Message);
        }
    }

    private async System.Threading.Tasks.Task List(GameMessage gm, GameClient client)
    {
        var shop = await Game.RavenNest.Shop.GetAsync();
        TavernShop.Apply(shop);
        var listings = shop?.Listings?.OrderBy(x => x.Shelf).ToList();
        if (listings == null || listings.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_SHOP_LIST_EMPTY);
            return;
        }

        var entries = string.Join(", ", listings.Select(x => string.Format(Localization.MSG_SHOP_LIST_ENTRY,
            x.Shelf + 1, NameOf(x), x.PricePerItem.ToString("N0", CultureInfo.InvariantCulture), x.Amount)));
        client.SendReply(gm, Localization.MSG_SHOP_LIST, entries);
    }

    private async System.Threading.Tasks.Task Remove(GameMessage gm, GameClient client, string[] parts)
    {
        var listing = await ListingOn(gm, client, parts);
        if (listing == null) return;
        var result = await Game.RavenNest.Shop.RemoveAsync(listing.Id);
        if (result == null || !result.Ok)
        {
            client.SendReply(gm, Localization.MSG_SHOP_FAILED, result?.Error);
            return;
        }

        TavernShop.Apply(result.Shop);
        client.SendReply(gm, Localization.MSG_SHOP_REMOVED, NameOf(listing), listing.Shelf + 1);
    }

    private async System.Threading.Tasks.Task Price(GameMessage gm, GameClient client, string[] parts)
    {
        if (parts.Length < 3 || !long.TryParse(parts[2].Replace(",", "").Replace(".", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) || price < 1)
        {
            client.SendReply(gm, Localization.MSG_SHOP_USAGE);
            return;
        }

        var listing = await ListingOn(gm, client, parts);
        if (listing == null) return;

        // only the price changes; the rest of the listing stays as it is
        var result = await Game.RavenNest.Shop.EditAsync(listing.Id, new StreamerShopListingRequest
        {
            PricePerItem = price,
            DurationHours = 0,
            Access = listing.Access,
            AllowedClanRoleIds = listing.AllowedClanRoleIds,
            MaxPerViewer = listing.MaxPerViewer,
        });
        if (result == null || !result.Ok)
        {
            client.SendReply(gm, Localization.MSG_SHOP_FAILED, result?.Error);
            return;
        }

        TavernShop.Apply(result.Shop);
        client.SendReply(gm, Localization.MSG_SHOP_PRICE_SET, listing.Shelf + 1, NameOf(listing), price.ToString("N0", CultureInfo.InvariantCulture));
    }

    private async System.Threading.Tasks.Task Report(GameMessage gm, GameClient client)
    {
        var report = await Game.RavenNest.Shop.GetReportAsync();
        if (report == null)
        {
            client.SendReply(gm, Localization.MSG_SHOP_FAILED, "");
            return;
        }

        client.SendReply(gm, Localization.MSG_SHOP_REPORT,
            report.TotalCoins.ToString("N0", CultureInfo.InvariantCulture),
            report.TotalItems.ToString("N0", CultureInfo.InvariantCulture),
            report.CoinsLast24Hours.ToString("N0", CultureInfo.InvariantCulture));
    }

    /// <summary>The listing on the shelf named in the command (the second word), or a reply saying why not.</summary>
    private async System.Threading.Tasks.Task<StreamerShopListingInfo> ListingOn(GameMessage gm, GameClient client, string[] parts)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1].TrimStart('#'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var shelf))
        {
            client.SendReply(gm, Localization.MSG_SHOP_USAGE);
            return null;
        }

        var shop = await Game.RavenNest.Shop.GetAsync();
        TavernShop.Apply(shop);
        var listing = shop?.Listings?.FirstOrDefault(x => x.Shelf == shelf - 1);
        if (listing == null) client.SendReply(gm, Localization.MSG_SHOP_SHELF_EMPTY, shelf);
        return listing;
    }

    private string NameOf(StreamerShopListingInfo listing) =>
        !string.IsNullOrEmpty(listing.Name) ? listing.Name : Game.Items.Get(listing.ItemId)?.Name ?? "?";
}
