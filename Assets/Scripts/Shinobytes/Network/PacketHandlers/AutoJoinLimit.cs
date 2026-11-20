using System;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// !auto limit [amount | off | status]: the player's own daily limit on what auto join may spend, for their whole
/// account (all characters share the coins), and what it has spent. The server keeps it (RavenNest AutoJoinSpending);
/// the website's dashboard sets the same limit. Past it, paid auto joins stop until midnight UTC.
/// </summary>
public class AutoJoinLimit : ChatBotCommandHandler<string>
{
    private static readonly Regex Amount = new Regex(@"^\d+(\.\d+)?[km]?$", RegexOptions.Compiled);

    public AutoJoinLimit(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player == null)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var text = (data ?? "").Trim().ToLowerInvariant().Replace(",", "").Replace(" ", "");
        var removing = text is "off" or "none" or "remove" or "clear" or "no" or "unlimited";
        var reading = text.Length == 0 || text is "status" or "spent" or "stats" or "left";
        if (!reading && !removing && !Amount.IsMatch(text))
        {
            client.SendReply(gm, Localization.MSG_AUTOJOIN_LIMIT_INVALID);
            return;
        }

        long[] status = null;
        try
        {
            status = await Game.RavenNest.Players.AutoJoinLimit(player.Id, reading ? "" : text);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Auto join limit for " + player.Name + " failed: " + exc.Message);
        }

        // { paid today, paid in 7 days, own limit or -1, free raids left, free dungeons left, free raids a day,
        //   free dungeons a day, discount percent, raid price, dungeon price }
        if (status == null || status.Length < 10)
        {
            client.SendReply(gm, Localization.MSG_AUTOJOIN_LIMIT_FAILED);
            return;
        }

        var paidToday = N(status[0]);
        if (removing)
        {
            client.SendReply(gm, Localization.MSG_AUTOJOIN_LIMIT_REMOVED, paidToday);
            return;
        }

        var limit = status[2] < 0 ? "none" : N(status[2]);
        if (!reading)
        {
            client.SendReply(gm, Localization.MSG_AUTOJOIN_LIMIT_SET, limit, paidToday);
            return;
        }

        var patron = status[5] > 0 || status[6] > 0 || status[7] > 0;
        if (patron)
        {
            client.SendReply(gm, Localization.MSG_AUTOJOIN_LIMIT_STATUS_PATRON, paidToday, limit, status[3].ToString(), status[4].ToString(), status[7].ToString(), N(status[1]));
        }
        else
        {
            client.SendReply(gm, Localization.MSG_AUTOJOIN_LIMIT_STATUS, paidToday, limit, N(status[1]));
        }
    }

    private static string N(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
