public class ValueItem : PacketHandler<TradeItemRequest>
{
    public ValueItem(
       GameManager game,
       GameServer server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override async void Handle(TradeItemRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(
                data.Player, "You have to play the game to valuate items. Use !join to play.");
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendMessage(
                data.Player, "Unable to valuate the item right now.");
            return;
        }

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery);
        if (item == null)
        {
            client.SendMessage(player, "Could not find an item matching the query '" + data.ItemQuery + "'");
            return;
        }

        client.SendMessage(player, $"{item.Item.Name} can be sold for {item.Item.ShopSellPrice} in the !vendor");
    }
}
