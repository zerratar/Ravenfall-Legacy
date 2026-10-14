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
            client.SendReply(gm, Localization.MSG_LOOT_CLEARED);
            plr.Loot.Clear();
            return;
        }

        var loot = plr.Loot.Query(queryFilter);

        if (loot.Count == 0)
        {
            if (string.IsNullOrEmpty(queryFilter))
            {
                client.SendReply(gm, Localization.MSG_LOOT_NONE);
                return;
            }

            client.SendReply(gm, Localization.MSG_LOOT_FILTER_NO_RESULT, queryFilter);
            return;
        }

        var replyBuilder = new StringBuilder();
        foreach (var record in loot.OrderByDescending(x => x.Time))
        {
            var time = Utility.FormatTime(DateTime.UtcNow - record.Time);
            var when = Localization.MSG_LOOT_AGO;

            if (includeOrigin)
            {
                if (record.DungeonIndex != -1) when = Localization.MSG_LOOT_FROM_DUNGEON_AGO;
                else if (record.RaidIndex != -1) when = Localization.MSG_LOOT_FROM_RAID_AGO;
            }

            when = string.Format(when, time);

            if (record.Amount > 1)
            {
                replyBuilder.AppendFormat("{0}x {1} - {2}. ", record.Amount, record.ItemName, when);
            }
            else
            {
                replyBuilder.AppendFormat("{0} - {1}. ", record.ItemName, when);
            }
        }

        client.SendReply(gm, replyBuilder.ToString());
    }
}