using System.Collections.Generic;
using System.Linq;
public class GetTokenCount : ChatBotCommandHandler
{
    public GetTokenCount(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }
    public override void Handle(GameMessage gm, GameClient client)
    {        //token_count
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var tokens = player.Inventory
            .GetInventoryItemsOfCategory(RavenNest.Models.ItemCategory.Scroll)
            .Where(x => x.Item.Name.Equals("Halloween Token") ||
                        x.Item.Name.Equals("Christmas Token") ||
                        x.Item.Name.Equals("Easter Token") ||
                        x.Item.Name.Equals("Birthday Token") ||
                        x.Item.Name.Equals("New Year Token"))
            .ToList();

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
            parameters.Add(token.Item.Name);
        }

        client.SendReply(gm, format, parameters.ToArray());
    }
}