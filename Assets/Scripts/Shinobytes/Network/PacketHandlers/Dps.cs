using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class Dps : ChatBotCommandHandler
{
    /// <summary>Seconds after a tower boss fight that !dps still says what the player did in it.</summary>
    private const float TowerFightSeconds = 300f;

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

        // a tower boss fight under way or just over (the meters window's): what they did in it and where they stand
        var fight = CombatMeter.Tower;
        var inFight = fight.RunId > 0 && (fight.Running || UnityEngine.Time.unscaledTime - fight.EndedAt < TowerFightSeconds) ? fight.Find(plr.Id) : null;
        if (inFight != null && inFight.Damage + inFight.Healing <= 0) inFight = null;

        if (sessionStats.HighestDamage <= 0 && sessionStats.HighestHeal <= 0 && inFight == null)
        {
            client.SendReply(gm, Localization.MSG_DPS_NONE);
            return;
        }

        var msg = new ReplyMessageBuilder();

        if (inFight != null)
        {
            var rank = 1;
            var fighters = 0;
            foreach (var e in fight.Entries)
            {
                if (e.Damage + e.Healing > 0) fighters++;
                if (e.Damage > inFight.Damage) rank++;
            }
            msg.Add(Localization.MSG_DPS_TOWER, fight.Title ?? "Tower", Utility.FormatAmount(inFight.Damage), Utility.FormatAmount(inFight.Healing), rank, fighters);
        }

        var dps = sessionStats.DPS;
        var hps = sessionStats.HPS;

        // ⚔️🧙❤️💥💊

        if (dps > 0)
        {
            msg.Add(Localization.MSG_DPS_DPS, Utility.FormatAmount(dps));
        }

        if (sessionStats.HighestDPS > 0)
        {
            msg.Add(Localization.MSG_DPS_MAX_DPS, Utility.FormatAmount(sessionStats.HighestDPS));
        }

        if (sessionStats.HighestDamage > 0)
        {
            msg.Add(Localization.MSG_DPS_MAX_DAMAGE, Utility.FormatAmount(sessionStats.HighestDamage));
        }

        if (sessionStats.TotalDamageDealt > 0)
        {
            msg.Add(Localization.MSG_DPS_TOTAL_DAMAGE, Utility.FormatAmount(sessionStats.TotalDamageDealt));
        }

        if (hps > 0)
        {
            msg.Add(Localization.MSG_DPS_HPS, Utility.FormatAmount(hps));
        }

        if (sessionStats.HighestHPS > 0)
        {
            msg.Add(Localization.MSG_DPS_MAX_HPS, Utility.FormatAmount(sessionStats.HighestHPS));
        }

        if (sessionStats.HighestHeal > 0)
        {
            msg.Add(Localization.MSG_DPS_MAX_HEAL, Utility.FormatAmount(sessionStats.HighestHeal));
        }

        if (sessionStats.TotalHealthHealed > 0)
        {
            msg.Add(Localization.MSG_DPS_TOTAL_HEALING, Utility.FormatAmount(sessionStats.TotalHealthHealed));
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