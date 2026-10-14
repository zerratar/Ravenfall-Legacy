using RavenNest.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public class GetTokenCount : ChatBotCommandHandler
{
    public GetTokenCount(
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

        ScrollInfoCollection tokens = null;

        try
        {
            tokens = await Game.RavenNest.Game.GetScrollsAsync(player);
            player.Inventory.UpdateScrolls(tokens);

            if (tokens.Count > 0)
            {
                tokens = new ScrollInfoCollection(tokens.Where(x => x.Name.Contains("token", System.StringComparison.OrdinalIgnoreCase)));
            }
        }
        catch
        {
            tokens = FilterTokens(player);
        }

        if (tokens.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_TOKENS_NONE);
            return;
        }

        // The list is built here, so each entry and the "and" come out in the chosen language.
        var items = "";
        var i = 0;
        foreach (var token in tokens)
        {
            if (i > 0) items += i + 1 == tokens.Count ? Localization.MSG_LIST_AND : ", ";
            items += Localization.MSG_SCROLL_COUNT_ITEM
                .Replace("{amount}", token.Amount.ToString())
                .Replace("{itemName}", token.Name);
            ++i;
        }

        client.SendReply(gm, Localization.MSG_SCROLL_COUNT, items);
    }

    private static ScrollInfoCollection FilterTokens(PlayerController player)
    {
        var scrolls = player.Inventory
            .GetInventoryItemsOfCategory(ItemCategory.Scroll)
            .Where(x => x.Name.Contains("token", System.StringComparison.OrdinalIgnoreCase))
            .ToList();

        var res = new List<ScrollInfo>();
        foreach (var scroll in scrolls)
        {
            res.Add(new ScrollInfo(scroll.Item.Id, scroll.Item.Name, scroll.Amount));
        }
        return new ScrollInfoCollection(res);
    }
}