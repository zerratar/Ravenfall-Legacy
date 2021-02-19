using System.Collections.Generic;
using System.Linq;

public class GetScrollsCount : PacketHandler<Player>
{
    public GetScrollsCount(
          GameManager game,
          RavenBotConnection server,
          PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {        //token_count
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var scrolls = player.Inventory
          .GetInventoryItemsOfCategory(RavenNest.Models.ItemCategory.Scroll)
          .ToList();

        if (scrolls.Count == 0)
        {
            client.SendFormat(data.Username, "You do not have any scrolls. You can redeem them under streamer loyalty on the website");
            return;
        }
        var parameters = new List<object>();
        var messages = new List<string>();
        var message = "You have ";
        var i = 0;
        foreach (var s in scrolls)
        {
            messages.Add("{amount" + i + "} {itemName" + i + "}");
            parameters.Add(((long)s.Amount).ToString());
            parameters.Add(s.Item.Name);
            ++i;
        }
        message += string.Join(", ", messages);
        client.SendFormat(data.Username, message, parameters.ToArray());
    }
}
