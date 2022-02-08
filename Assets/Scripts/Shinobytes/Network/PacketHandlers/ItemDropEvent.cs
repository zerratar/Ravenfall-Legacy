using UnityEngine;

public class ItemDropEvent : PacketHandler<TradeItemRequest>
{
    public ItemDropEvent(
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
            return;
        }

        if (!player.IsGameAdmin)
        {
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery);
        if (item == null)
        {
            //client.SendMessage(player.PlayerName, "Could not find an item matching the query '{query}'", data.ItemQuery);
            return;
        }

        var dropEventManager = GameObject.FindObjectOfType<DropEventManager>();
        if (dropEventManager)
        {
            var itemCount = Game.Players.GetPlayerCount() * 2;
            dropEventManager.Drop(item.Item.Item, itemCount);
        }
    }
}
