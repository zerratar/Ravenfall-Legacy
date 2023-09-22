using Shinobytes.Linq;

public class ExamineItem : ChatBotCommandHandler<string>
{
    public ExamineItem(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }


        if (string.IsNullOrEmpty(inputQuery) || inputQuery.ToLower() == "last" || inputQuery.ToLower() == "drop")
        {
            if (player.Inventory.LastAddedItem != null)
            {
                var desc = player.Inventory.LastAddedItem.Item.Description;
                if (string.IsNullOrEmpty(desc))
                {
                    return;
                }

                client.SendReply(gm, desc);
                return;
            }
            else
            {
                client.SendReply(gm, Localization.MSG_ITEM_EXAMINE_MISSING_ARGS);
                return;
            }
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveTradeQuery(inputQuery, parsePrice: false, parseUsername: false, parseAmount: false);

        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (string.IsNullOrEmpty(queriedItem.Item.Description))
        {
            client.SendReply(gm, "There is not much to say about {itemName}", inputQuery);
            return;
        }

        client.SendReply(gm, queriedItem.Item.Description);
    }
}
