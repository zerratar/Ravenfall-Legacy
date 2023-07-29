using System.Threading.Tasks;

public class UseMarketplace : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;

    public UseMarketplace(
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
            client.SendReply(gm, Localization.MSG_MARKET_MISSING_ARGS);
            return;
        }

        var d = data.Trim();
        var parts = d.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
        // we expect first part to be an action.

        if (parts.Length == 0)
        {
            client.SendReply(gm, Localization.MSG_MARKET_MISSING_ARGS);
            return;
        }

        var query = string.Join(" ", parts[1..]);
        switch (parts[0].ToLower())
        {
            case "value":
            case "stock":
                // show market value, lowest selling one, highest selling one, and average selling price.
                // and the total amount on the market. if amount is supplied, the price for buying said amount
                // with the lowest price will be calculated
                await GetMarketValueAsync(query, gm, client);
                break;

            case "buy":
                await BuyFromMarketAsync(query, gm, client);
                break;

            case "sell":
                await SellToMarketAsync(query, gm, client);
                break;

            default:
                client.SendReply(gm, Localization.MSG_VENDOR_MISSING_ACTION, query);
                break;
        }
    }

    private Task BuyFromMarketAsync(string query, GameMessage gm, GameClient client)
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

    private Task SellToMarketAsync(string query, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return Task.CompletedTask;
        }

        var item = itemResolver.ResolveTradeQuery(query, playerToSearch: player);

        // 1. check if its a valid item
        // 2. check if we have the item
        // 3. if the item is equipped, unequip it first
        // 4. put the item in the market

        return Task.CompletedTask;
    }

    private Task GetMarketValueAsync(string query, GameMessage gm, GameClient client)
    {
        var item = itemResolver.ResolveTradeQuery(query, parsePrice: false);


        // 1. check if its a valid item
        // 2. show market value, lowest selling one, highest selling one, and average selling price.
        // and the total amount on the market. if amount is supplied, the price for buying said amount
        // with the lowest price will be calculated

        return Task.CompletedTask;
    }
}
