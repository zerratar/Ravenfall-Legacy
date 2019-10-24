using UnityEngine;

public class ItemDropEvent : PacketHandler<TradeItemRequest>
{
    public ItemDropEvent(
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
            client.SendCommand(
                data.Player.Username, "message",
                "You're not playing, Mr. Broadcaster.");
            return;
        }

        if (!data.Player.IsBroadcaster)
        {
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        if (!ioc)
        {
            client.SendCommand(player.PlayerName, "message", "Crap. Error occured. Dunno why.");
            return;
        }

        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(data.ItemQuery);
        if (item == null)
        {
            client.SendCommand(player.PlayerName, "message", "Could not find an item matching the query '" + data.ItemQuery + "'");
            return;
        }

        var dropEventManager = GameObject.FindObjectOfType<DropEventManager>();
        if (dropEventManager)
        {
            var itemCount = Game.Players.GetPlayerCount() * 2;
            dropEventManager.Drop(item.Item, itemCount);
        }
    }
}
