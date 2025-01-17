using UnityEngine;

public class ItemDropEvent : ChatBotCommandHandler<string>
{
    public ItemDropEvent(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }
    public override void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            return;
        }

        if (!player.IsGameAdmin)
        {
            return;
        }

        var randomHatDropParty = inputQuery.Contains("random hat") || inputQuery.Contains("hats");
        var randomItem = inputQuery.Contains("random");
        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.Resolve(inputQuery);

        if (item.SuggestedItemNames.Length > 0 && !randomHatDropParty && !randomItem)
        {
            return;
        }

        if (item == null && !randomHatDropParty && !randomItem)
        {
            //client.SendReply(player, "Could not find an item matching the query '{query}'", data.ItemQuery);
            return;
        }

        var dropEventManager = GameObject.FindAnyObjectByType<DropEventManager>();
        if (dropEventManager)
        {
            var itemCount = Game.Players.GetPlayerCount() * 2;
            if (randomHatDropParty)
            {
                dropEventManager.DropRandomHats(itemCount);
            }
            else if (randomItem)
            {
                dropEventManager.DropRandomItems(itemCount);
            }
            else
            {
                dropEventManager.Drop(item.Item, itemCount);
            }
        }
    }
}
