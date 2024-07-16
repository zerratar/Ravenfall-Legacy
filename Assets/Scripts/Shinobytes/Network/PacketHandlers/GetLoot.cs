using System;
using System.Linq;
using System.Text;

public class GetLoot : ChatBotCommandHandler<string>
{
    public GetLoot(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string queryFilter, GameMessage gm, GameClient client)
    {

        var includeOrigin = PlayerSettings.Instance.Loot.IncludeOrigin;
        var plr = PlayerManager.GetPlayer(gm.Sender);
        if (plr == null)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (!string.IsNullOrEmpty(queryFilter) && queryFilter == "clear")
        {
            client.SendReply(gm, "Your loot records has been cleared.");
            plr.Loot.Clear();
            return;
        }

        var loot = plr.Loot.Query(queryFilter);

        if (loot.Count == 0)
        {
            if (string.IsNullOrEmpty(queryFilter))
            {
                client.SendReply(gm, "You have not looted anything yet.");
                return;
            }

            client.SendReply(gm, "The filter '" + queryFilter + "' did not yield any result.");
            return;
        }

        var replyBuilder = new StringBuilder();
        foreach (var record in loot.OrderByDescending(x => x.Time))
        {
            var timeAgo = Utility.FormatTime(DateTime.UtcNow - record.Time) + " ago";

            if (includeOrigin)
            {
                string origin = record.DungeonIndex != -1 ? "from dungeon " :
                                record.RaidIndex != -1 ? "from raid " : "";
                if (record.Amount > 1)
                {
                    replyBuilder.AppendFormat("{0}x {1} - {2}{3}. ", record.Amount, record.ItemName, origin, timeAgo);
                }
                else
                {
                    replyBuilder.AppendFormat("{0} - {1}{2}. ", record.ItemName, origin, timeAgo);
                }
            }
            else
            {
                if (record.Amount > 1)
                {
                    replyBuilder.AppendFormat("{0}x {1} - {2}. ", record.Amount, record.ItemName, timeAgo);
                }
                else
                {
                    replyBuilder.AppendFormat("{0} - {1}. ", record.ItemName, timeAgo);
                }
            }
        }

        client.SendReply(gm, replyBuilder.ToString());
    }
}