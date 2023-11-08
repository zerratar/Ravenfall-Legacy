using RavenNest.Models;
using System.Collections.Generic;
using System.Linq;

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


            // filter out tokens
            if (scrolls.Count > 0)
            {
                scrolls = new ScrollInfoCollection(scrolls.Where(x => !x.Name.Contains("token", System.StringComparison.OrdinalIgnoreCase)));
            }
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
        var scrolls = player.Inventory
            .GetInventoryItemsOfCategory(ItemCategory.Scroll)
            .Where(x => !x.Name.Contains("token", System.StringComparison.OrdinalIgnoreCase))
            .ToList();

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
        var format = "You have ";
        for (var i = 0; i < scrolls.Count; ++i)
        {
            format += "{tokenAmount" + i + "} {tokenName" + i + "}(s)";
            if (i + 1 < scrolls.Count)
            {
                if (i + 2 == scrolls.Count) format += " and ";
                else format += ", ";
            }
        }
        format += ".";
        var parameters = new List<object>();
        foreach (var token in scrolls)
        {
            parameters.Add(token.Amount.ToString());
            parameters.Add(token.Name);
        }

        client.SendReply(gm, format, parameters.ToArray());
    }
}
