
public class UseVendor : ChatBotCommandHandler<string>
{
    public UseVendor(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
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
        if (string.IsNullOrWhiteSpace(query))
        {
            client.SendReply(gm, Localization.MSG_VENDOR_MISSING_ARGS);
            return;
        }

        // !vendor sell and !vendor value were empty stubs that answered nothing, though the game's own help named them:
        // they are the plain !vendor <item> and !value <item>. The vendor sells nothing, so buy points at the market.
        switch (parts[0].ToLower())
        {
            case "value":
            case "stock":
                new ValueItem(Game, Server, PlayerManager).Handle(query, gm, client);
                break;
            case "buy":
                client.SendReply(gm, Localization.MSG_VENDOR_NO_BUY, query);
                break;
            case "sell":
                new SellItemToVendorVendor(Game, Server, PlayerManager).Handle(query, gm, client);
                break;
            default:
                client.SendReply(gm, Localization.MSG_VENDOR_MISSING_ACTION, query);
                break;
        }
    }
}
