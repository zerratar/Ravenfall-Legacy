using RavenNest.Models;
using System.Collections.Generic;

public class GetScrollsCount : ChatBotCommandHandler
{
    public GetScrollsCount(
          GameManager game,
          RavenBotConnection server,
          PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {        //token_count
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        ScrollInfoCollection scrolls = null;

        try
        {
            scrolls = await Game.RavenNest.Game.GetScrollsAsync(player);
            player.Inventory.UpdateScrolls(scrolls);
        }
        catch
        {
            scrolls = GetScrollInfoCollection(player);
        }

        if (scrolls.Count == 0)
        {
            client.SendReply(gm, "You do not have any scrolls. You can redeem them under streamer loyalty on the website");
            return;
        }

        SendScrollCount(gm, client, scrolls);
    }
    private static ScrollInfoCollection GetScrollInfoCollection(PlayerController player)
    {
        var scrolls = player.Inventory.GetInventoryItemsOfCategory(ItemCategory.Scroll);
        var res = new List<ScrollInfo>();
        foreach (var scroll in scrolls)
        {
            res.Add(new ScrollInfo(scroll.Item.Id, scroll.Item.Name, scroll.Amount));
        }
        return new ScrollInfoCollection(res);
    }
    private static void SendScrollCount(
        GameMessage gm, 
        GameClient client, 
        ScrollInfoCollection scrolls)
    {
        var parameters = new List<object>();
        var messages = new List<string>();
        var message = "You have ";
        var i = 0;
        foreach (var s in scrolls)
        {
            messages.Add("{amount" + i + "} {itemName" + i + "}");
            parameters.Add(((long)s.Amount).ToString());
            parameters.Add(s.Name);
            ++i;
        }
        message += string.Join(", ", messages);
        client.SendReply(gm, message, parameters.ToArray());
    }
}
