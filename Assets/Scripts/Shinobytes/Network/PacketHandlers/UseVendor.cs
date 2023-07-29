using System.Threading.Tasks;

public class UseVendor : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;

    public UseVendor(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
        var ioc = game.gameObject.GetComponent<IoCContainer>();
        this.itemResolver = ioc.Resolve<IItemResolver>();
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        if (string.IsNullOrEmpty(data))
        {
            client.SendReply(gm, Localization.MSG_VENDOR_MISSING_ARGS);
            return;
        }

        var d = data.Trim();
        var parts = d.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
        // we expect first part to be an action.

        if (parts.Length == 0)
        {
            client.SendReply(gm, Localization.MSG_VENDOR_MISSING_ARGS);
            return;
        }

        var query = string.Join(" ", parts[1..]);
        switch (parts[0].ToLower())
        {
            case "value":
            case "stock":
                // Always let the players know that the value may change at any second.
                // this should also mention current stock, last stock change, price per unit, and price it will be selling queried amount.
                // also show when next re-stock will happen and thats when prices gets reset. (Every 12 hours)
                await GetVendorValueAsync(query, gm, client);
                break;
            case "buy":
                await BuyFromVendorAsync(query, gm, client);
                break;
            case "sell":
                await SellToVendorAsync(query, gm, client);
                break;
            default:
                client.SendReply(gm, Localization.MSG_VENDOR_MISSING_ACTION, query);
                break;
        }
    }

    private Task BuyFromVendorAsync(string query, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return Task.CompletedTask;
        }

        var item = itemResolver.ResolveTradeQuery(query, parsePrice: false);

        // 1. check if its a valid item
        // 2. try to buy the item

        return Task.CompletedTask;
    }

    private Task SellToVendorAsync(string query, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return Task.CompletedTask;
        }

        // 1. check if its a valid item
        // 2. check if we have the item
        // 3. if the item is equipped, unequip it first
        // 4. put the item in the market

        return Task.CompletedTask;
    }

    private Task GetVendorValueAsync(string query, GameMessage gm, GameClient client)
    {
        var item = itemResolver.ResolveTradeQuery(query, parsePrice: false);

        // 1. check if its a valid item
        // 2. Always let the players know that the value may change at any second.
        // this should also mention current stock, last stock change, price per unit, and price it will be selling queried amount.
        // also show when next re-stock will happen and thats when prices gets reset. (Every 12 hours)

        return Task.CompletedTask;
    }
}
