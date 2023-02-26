public class ValueItem : ChatBotCommandHandler<TradeItemRequest>
{
    public ValueItem(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player, Localization.MSG_NOT_PLAYING);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery);

        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendMessage(player.PlayerName, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, data.ItemQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendMessage(player, Localization.MSG_VALUE_ITEM_NOT_FOUND, data.ItemQuery);
            return;
        }

        client.SendFormat(player.PlayerName, Localization.MSG_VALUE_ITEM,
            item.Item.Name,
            item.Item.ShopSellPrice);
    }
}
