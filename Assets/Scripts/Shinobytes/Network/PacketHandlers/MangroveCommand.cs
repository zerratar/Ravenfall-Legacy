using System;
using System.Globalization;
using RavenNest.SDK.Endpoints;

/// <summary>
/// The banana island's chat commands (docs/banana-island.md), one packet for all of them ("mangrove_command", the
/// command first): !dig joins a treasure dig; !banana (or !bananas) says how the monkeys' pile stands, !banana feed
/// [amount] hands them the player's own bananas and !banana golden a Golden Banana; !regatta joins (or opens) the
/// regatta, !regatta start starts it now and !regatta leave leaves it; !pirates calls the pirates (the broadcaster or
/// a moderator).
/// </summary>
public class MangroveCommand : ChatBotCommandHandler<string>
{
    public MangroveCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string arguments, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player)) return;

        var words = (arguments ?? "").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var command = words.Length > 0 ? words[0].ToLowerInvariant().TrimStart('!') : "";
        var sub = words.Length > 1 ? words[1].ToLowerInvariant() : "";

        try
        {
            switch (command)
            {
                case "dig":
                    Dig(player, gm, client);
                    return;
                case "banana":
                case "bananas":
                    await Bananas(player, sub, words.Length > 2 ? words[2] : sub, gm, client);
                    return;
                case "regatta":
                    Regatta(player, sub, gm, client);
                    return;
                case "pirates":
                case "pirate":
                    Pirates(player, gm, client);
                    return;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("The banana island's !" + command + " failed: " + exc);
        }
    }

    private static void Dig(PlayerController player, GameMessage gm, GameClient client)
    {
        switch (TreasureHunt.JoinCurrent(player))
        {
            case TreasureHunt.JoinResult.Joined: client.SendReply(gm, Localization.MSG_DIG_JOINED); break;
            case TreasureHunt.JoinResult.Already: client.SendReply(gm, Localization.MSG_DIG_ALREADY); break;
            case TreasureHunt.JoinResult.NoHunt: client.SendReply(gm, Localization.MSG_DIG_NONE); break;
            case TreasureHunt.JoinResult.NotOnIsland: client.SendReply(gm, Localization.MSG_DIG_NOT_ON_ISLAND); break;
            default: client.SendReply(gm, Localization.MSG_DIG_BUSY); break;
        }
    }

    private async System.Threading.Tasks.Task Bananas(PlayerController player, string sub, string amountText, GameMessage gm, GameClient client)
    {
        var island = Game.Islands.Get(IslandController.MangroveIsland);
        if (!island || island.Locked)
        {
            client.SendReply(gm, Localization.MSG_BANANAS_NOT_OPEN);
            return;
        }

        if (sub == "golden" || sub == "gold")
        {
            var golden = await Game.RavenNest.Mangrove.FeedAsync(player.Id, "golden", 1);
            if (golden <= 0) { client.SendReply(gm, Localization.MSG_BANANAS_NONE); return; }
            BananaPile.Add(BananaPile.PerGoldenBanana * golden);
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_BANANAS_FED_GOLDEN, player.Name), "");
            client.SendReply(gm, Localization.MSG_BANANAS_FED_GOLDEN);
            return;
        }

        if (sub == "feed" || sub == "give")
        {
            if (!long.TryParse(amountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount) || amount <= 0) amount = 10;
            var taken = await Game.RavenNest.Mangrove.FeedAsync(player.Id, "banana", amount);
            if (taken <= 0) { client.SendReply(gm, Localization.MSG_BANANAS_NONE); return; }
            BananaPile.Add(BananaPile.PerBanana * taken);
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_BANANAS_FED, player.Name, taken, BananaPile.AmountText), "");
            client.SendReply(gm, Localization.MSG_BANANAS_FED, taken, BananaPile.AmountText);
            return;
        }

        client.SendReply(gm, Localization.MSG_BANANAS_STATUS, BananaPile.AmountText, BananaPile.MoodName);
    }

    private static void Regatta(PlayerController player, string sub, GameMessage gm, GameClient client)
    {
        if (sub == "leave" || sub == "stop")
        {
            if (global::Regatta.Leave(player)) client.SendReply(gm, Localization.MSG_REGATTA_LEFT);
            return;
        }

        var result = global::Regatta.Join(player);
        if (sub == "start" && (player.IsBroadcaster || player.IsModerator || player.IsGameAdmin || player.IsGameModerator))
        {
            // the streamer starts it now, whether or not their own character races
            if (global::Regatta.StartNow()) return;
        }
        switch (result)
        {
            case global::Regatta.JoinResult.Joined: client.SendReply(gm, Localization.MSG_REGATTA_JOINED); break;
            case global::Regatta.JoinResult.Already: client.SendReply(gm, Localization.MSG_REGATTA_ALREADY); break;
            case global::Regatta.JoinResult.NotOpen: client.SendReply(gm, Localization.MSG_REGATTA_NOT_OPEN); break;
            case global::Regatta.JoinResult.NoBoat: client.SendReply(gm, Localization.MSG_REGATTA_NO_BOAT); break;
            case global::Regatta.JoinResult.Full: client.SendReply(gm, Localization.MSG_REGATTA_FULL); break;
            case global::Regatta.JoinResult.Underway: client.SendReply(gm, Localization.MSG_REGATTA_UNDERWAY); break;
            case global::Regatta.JoinResult.Cooldown: client.SendReply(gm, Localization.MSG_REGATTA_COOLDOWN, UnityEngine.Mathf.CeilToInt(global::Regatta.CooldownLeft)); break;
            default: client.SendReply(gm, Localization.MSG_REGATTA_BUSY); break;
        }
    }

    private static void Pirates(PlayerController player, GameMessage gm, GameClient client)
    {
        if (!player.IsBroadcaster && !player.IsModerator && !player.IsGameAdmin && !player.IsGameModerator)
        {
            client.SendReply(gm, Localization.MSG_PIRATES_NO_RIGHTS);
            return;
        }
        switch (PirateRaid.Begin())
        {
            case PirateRaid.StartResult.Started: client.SendReply(gm, Localization.MSG_PIRATES_STARTED); break;
            case PirateRaid.StartResult.Running: client.SendReply(gm, Localization.MSG_PIRATES_RUNNING); break;
            case PirateRaid.StartResult.Busy: client.SendReply(gm, Localization.MSG_PIRATES_BUSY); break;
            default: client.SendReply(gm, Localization.MSG_PIRATES_NOT_OPEN); break;
        }
    }
}
