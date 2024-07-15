using System.Collections.Generic;
using System.Linq;

public class Dps : ChatBotCommandHandler
{
    public Dps(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(gm.Sender);
        if (!plr)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var sessionStats = plr.SessionStats;

        if (sessionStats.HighestDamage <= 0 && sessionStats.HighestHeal <= 0)
        {
            client.SendReply(gm, "You have not dealt any damage or healed anyone yet.");
            return;
        }

        var msg = new ReplyMessageBuilder();

        var dps = sessionStats.DPS;
        var hps = sessionStats.HPS;

        // ⚔️🧙❤️💥💊

        if (dps > 0)
        {
            msg.Add("⚔️ {DPS} DPS", Utility.FormatAmount(dps));
        }

        if (sessionStats.HighestDPS > 0)
        {
            msg.Add("⚔️ {highestDPS} Max DPS", Utility.FormatAmount(sessionStats.HighestDPS));
        }

        if (sessionStats.HighestDamage > 0)
        {
            msg.Add("💥 {highestDamage} Max DMG", Utility.FormatAmount(sessionStats.HighestDamage));
        }

        if (sessionStats.TotalDamageDealt > 0)
        {
            msg.Add("💥 {totalDamage} Total", Utility.FormatAmount(sessionStats.TotalDamageDealt));
        }

        if (hps > 0)
        {
            msg.Add("🧙 {HPS} HPS", Utility.FormatAmount(hps));
        }

        if (sessionStats.HighestHPS > 0)
        {
            msg.Add("🧙 {highestHPS} Max HPS", Utility.FormatAmount(sessionStats.HighestHPS));
        }

        if (sessionStats.HighestHeal > 0)
        {
            msg.Add("❤️ {highestHeal} Max HEAL", Utility.FormatAmount(sessionStats.HighestHeal));
        }

        if (sessionStats.TotalHealthHealed > 0)
        {
            msg.Add("❤️ {totalHealing} Total", Utility.FormatAmount(sessionStats.TotalHealthHealed));
        }

        client.SendReply(gm, msg.ToString(), msg.Arguments);
    }
}

public class ReplyMessageBuilder
{
    private readonly List<string> strings = new List<string>();
    private readonly List<string> arguments = new List<string>();
    public object[] Arguments => arguments.ToArray();
    public ReplyMessageBuilder Add(string value, params object[] args)
    {
        strings.Add(value);
        if (args.Length > 0)
            arguments.AddRange(args.Select(x => x.ToString()));
        return this;
    }

    //public ReplyMessageBuilder AppendLine(string value, params object[] args)
    //{
    //    strings.Add(value + "\n");
    //    if (args.Length > 0)
    //        arguments.AddRange(args.Select(x => x.ToString()));
    //    return this;
    //}

    //public ReplyMessageBuilder AppendLine()
    //{
    //    strings.Add("\n");
    //    return this;
    //}

    public override string ToString()
    {
        return string.Join(" ", strings);
    }
}