using RavenNest.Models;
using System.Collections.Generic;

public class GetScrollsCount : PacketHandler<TwitchPlayerInfo>
{
    public GetScrollsCount(
          GameManager game,
          RavenBotConnection server,
          PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override async void Handle(TwitchPlayerInfo data, GameClient client)
    {        //token_count
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
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
            client.SendFormat(data.Username, "You do not have any scrolls. You can redeem them under streamer loyalty on the website");
            return;
        }

        SendScrollCount(data, client, scrolls);
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
    private static void SendScrollCount(TwitchPlayerInfo data, GameClient client, ScrollInfoCollection scrolls)
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
        client.SendFormat(data.Username, message, parameters.ToArray());
    }
}
