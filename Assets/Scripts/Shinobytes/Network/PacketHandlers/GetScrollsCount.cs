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
            client.SendReply(gm, Localization.MSG_SCROLLS_NONE);
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
        // The list is built here, so each entry and the "and" come out in the chosen language.
        var items = "";
        var i = 0;
        foreach (var scroll in scrolls)
        {
            if (i > 0) items += i + 1 == scrolls.Count ? Localization.MSG_LIST_AND : ", ";
            items += Localization.MSG_SCROLL_COUNT_ITEM
                .Replace("{amount}", scroll.Amount.ToString())
                .Replace("{itemName}", scroll.Name);
            ++i;
        }

        client.SendReply(gm, Localization.MSG_SCROLL_COUNT, items);
    }
}
