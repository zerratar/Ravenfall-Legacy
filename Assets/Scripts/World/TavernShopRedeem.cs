using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RavenNest.SDK.Endpoints;

/// <summary>
/// !redeem for the tavern shelves (see <see cref="TavernShop"/>): "!redeem 3" buys from shelf 3,
/// "!redeem rune ore" buys the rune ore on a shelf, and a number at the end is how many
/// ("!redeem 3 50", "!redeem rune ore 50"), one when there is none.
/// </summary>
/// <remarks>
/// The seasonal token redeemables keep their names: a name that is one of them is redeemed with tokens
/// as before, and the reply says when the same item is also on a shelf, with its number. A name on
/// several shelves gets the shelves listed. Anything that is neither falls through to the token
/// redeem, which answers as it always has.
/// </remarks>
public static class TavernShopRedeem
{
    private static readonly Regex AmountToken = new Regex(@"^x?(\d+)x?$", RegexOptions.IgnoreCase);
    private static readonly Regex ShelfToken = new Regex(@"^#?(\d+)$");

    /// <summary>
    /// What a !redeem asked for: a shelf number or a name, and how many. The text is the query without
    /// the amount, for the token redeem to resolve.
    /// </summary>
    public static (int? Shelf, string Text, long Amount) Parse(string query)
    {
        var tokens = (query ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).ToList();
        long amount = 1;
        if (tokens.Count > 1)
        {
            var last = AmountToken.Match(tokens[tokens.Count - 1]);
            if (last.Success && long.TryParse(last.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0)
            {
                amount = n;
                tokens.RemoveAt(tokens.Count - 1);
            }
        }

        if (tokens.Count == 1)
        {
            var shelf = ShelfToken.Match(tokens[0]);
            if (shelf.Success && int.TryParse(shelf.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
            {
                return (s, tokens[0], amount);
            }
        }

        return (null, string.Join(" ", tokens), amount);
    }

    /// <summary>
    /// Handles the !redeem when it is for a shelf, and says whether it did. When it was not (a token
    /// redeemable, or nothing on the shelves by that name), the caller redeems with tokens as before;
    /// <paramref name="alsoOnShelf"/> is then the note to send after, when the item is on a shelf too.
    /// </summary>
    public static async Task<(bool Handled, string AlsoOnShelf, string Text)> TryHandleAsync(
        GameManager game, GameClient client, GameMessage gm, PlayerController player, string query)
    {
        var (shelf, text, amount) = Parse(query);
        var shop = await GetShopAsync(game);
        var listings = shop?.Listings ?? new List<StreamerShopListingInfo>();

        if (shelf.HasValue)
        {
            var onShelf = listings.FirstOrDefault(x => x.Shelf == shelf.Value - 1);
            // a number that is no shelf in this tavern (no shop, or beyond its shelves) is left to the token redeem
            if (shop == null || shelf.Value < 1 || shelf.Value > shop.Shelves) return (false, null, text);
            if (onShelf == null)
            {
                client.SendReply(gm, Localization.MSG_SHOP_SHELF_EMPTY, shelf.Value);
                return (true, null, text);
            }

            await BuyAsync(game, client, gm, player, onShelf, amount);
            return (true, null, text);
        }

        if (listings.Count == 0 || string.IsNullOrWhiteSpace(text)) return (false, null, text);

        // the item the name means, the same way the token redeem reads it
        var ioc = game.gameObject.GetComponent<IoCContainer>();
        var resolved = ioc != null ? ioc.Resolve<IItemResolver>().ResolveAny(text, text + " pet") : null;
        var itemId = resolved?.Item != null ? resolved.Id : Guid.Empty;
        var matching = listings
            .Where(x => (itemId != Guid.Empty && x.ItemId == itemId) || NameOf(game, x).Equals(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Shelf)
            .ToList();
        if (matching.Count == 0) return (false, null, text);

        if (itemId != Guid.Empty && IsTokenRedeemable(game, itemId))
        {
            var first = matching[0];
            var note = string.Format(Localization.MSG_SHOP_ALSO_ON_SHELF_TEXT, NameOf(game, first), first.Shelf + 1, first.PricePerItem);
            return (false, note, text);
        }

        if (matching.Count > 1)
        {
            var shelves = string.Join(", ", matching.Select(x => string.Format(Localization.MSG_SHOP_SHELF_ENTRY, x.Shelf + 1, x.PricePerItem)));
            client.SendReply(gm, Localization.MSG_SHOP_ON_SHELVES, NameOf(game, matching[0]), shelves);
            return (true, null, text);
        }

        await BuyAsync(game, client, gm, player, matching[0], amount);
        return (true, null, text);
    }

    private static async Task BuyAsync(GameManager game, GameClient client, GameMessage gm, PlayerController player, StreamerShopListingInfo listing, long amount)
    {
        var name = NameOf(game, listing);
        var shelf = listing.Shelf + 1;
        StreamerShopBuyResult result;
        try
        {
            result = await game.RavenNest.Shop.BuyAsync(listing.Id, player.Id, amount);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Buying from a tavern shelf failed: " + exc);
            client.SendReply(gm, Localization.MSG_SHOP_FAILED, exc.Message);
            return;
        }

        if (result == null)
        {
            client.SendReply(gm, Localization.MSG_SHOP_FAILED, "");
            return;
        }

        switch (result.Code)
        {
            case StreamerShopBuyResultCode.Success:
                if (result.LoyaltyPoints > 0)
                    client.SendReply(gm, Localization.MSG_SHOP_BOUGHT_LOYALTY, result.Amount, name, shelf, result.Price, result.LoyaltyPoints);
                else
                    client.SendReply(gm, Localization.MSG_SHOP_BOUGHT, result.Amount, name, shelf, result.Price);
                break;
            case StreamerShopBuyResultCode.InsufficientCoins:
                if (result.MaxAmount > 0)
                    client.SendReply(gm, Localization.MSG_SHOP_NOT_ENOUGH_COINS_SOME, result.Price, result.CoinsLeft, result.MaxAmount);
                else
                    client.SendReply(gm, Localization.MSG_SHOP_NOT_ENOUGH_COINS, result.Price, result.CoinsLeft);
                break;
            case StreamerShopBuyResultCode.NotEnoughLeft:
                client.SendReply(gm, Localization.MSG_SHOP_ONLY_LEFT, result.MaxAmount, shelf);
                break;
            case StreamerShopBuyResultCode.LimitReached:
                if (result.MaxAmount > 0)
                    client.SendReply(gm, Localization.MSG_SHOP_LIMIT_SOME, result.MaxAmount);
                else
                    client.SendReply(gm, Localization.MSG_SHOP_LIMIT, listing.MaxPerViewer);
                break;
            case StreamerShopBuyResultCode.NotAllowed:
                client.SendReply(gm, Localization.MSG_SHOP_NOT_ALLOWED);
                break;
            case StreamerShopBuyResultCode.OwnListing:
                client.SendReply(gm, Localization.MSG_SHOP_OWN);
                break;
            case StreamerShopBuyResultCode.NotFound:
            case StreamerShopBuyResultCode.SoldOut:
                client.SendReply(gm, Localization.MSG_SHOP_SHELF_EMPTY, shelf);
                break;
            case StreamerShopBuyResultCode.NotInSession:
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                break;
            default:
                client.SendReply(gm, Localization.MSG_SHOP_FAILED, result.Error);
                break;
        }
    }

    /// <summary>The shop as last seen, or fetched when the tavern has not been opened this session.</summary>
    private static async Task<StreamerShopInfo> GetShopAsync(GameManager game)
    {
        if (TavernShop.Latest != null) return TavernShop.Latest;
        try
        {
            var shop = await game.RavenNest.Shop.GetAsync();
            TavernShop.Apply(shop);
            return shop;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The tavern shelves could not be loaded for a redeem: " + exc.Message);
            return null;
        }
    }

    private static bool IsTokenRedeemable(GameManager game, Guid itemId)
    {
        if (!game.Tavern || !game.Tavern.CanRedeemItems) return false;
        var redeemable = game.Items.GetRedeemables().FirstOrDefault(x => x.ItemId == itemId);
        return redeemable.ItemId != Guid.Empty && redeemable.IsRedeemable() && redeemable.Cost > 0;
    }

    private static string NameOf(GameManager game, StreamerShopListingInfo listing) =>
        !string.IsNullOrEmpty(listing.Name) ? listing.Name : game.Items.Get(listing.ItemId)?.Name ?? "?";
}
