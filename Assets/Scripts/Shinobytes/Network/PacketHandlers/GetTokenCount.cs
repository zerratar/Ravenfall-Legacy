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
            client.SendReply(gm, "You do not have any seasonal tokens. Join Raids or Dungeons during seasonal events to get some!");
            return;
        }

        var format = "You have ";
        for (var i = 0; i < tokens.Count; ++i)
        {
            format += "{tokenAmount" + i + "} {tokenName" + i + "}(s)";
            if (i + 1 < tokens.Count)
            {
                if (i + 2 == tokens.Count) format += " and ";
                else format += ", ";
            }
        }
        format += ".";

        var parameters = new List<object>();
        foreach (var token in tokens)
        {
            parameters.Add(token.Amount.ToString());
            parameters.Add(token.Name);
        }

        client.SendReply(gm, format, parameters.ToArray());
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