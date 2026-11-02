using System;
using System.Globalization;
using RavenNest.SDK.Endpoints;

/// <summary>
/// The banana island's chat commands (docs/banana-island.md), one packet for all of them ("mangrove_command", the
/// command first): !dig joins a treasure dig; !banana (or !bananas) says how the monkeys' pile stands, !banana feed
/// [amount] hands them the player's own bananas and !banana golden a Golden Banana; !regatta joins (or opens) the
/// regatta, !regatta start starts it now and !regatta leave leaves it; during a race !regatta keg [name] throws a
/// powder keg in front of a racer, !regatta wind [name] fills a racer's sails and !regatta kraken (the broadcaster or a
/// moderator, once a race) wakes the Kraken (RegattaMeddling has the prices); !pirates calls the pirates (the
/// broadcaster or a moderator).
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
                    await Regatta(player, sub, words.Length > 2 ? words[2] : null, gm, client);
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

    private async System.Threading.Tasks.Task Regatta(PlayerController player, string sub, string target, GameMessage gm, GameClient client)
    {
        if (sub == "leave" || sub == "stop")
        {
            if (global::Regatta.Leave(player)) client.SendReply(gm, Localization.MSG_REGATTA_LEFT);
            return;
        }
        switch (sub)
        {
            case "keg":
            case "mine":
            case "bomb":
                await Meddle(player, global::Regatta.Meddling.Keg, target, gm, client);
                return;
            case "wind":
            case "boost":
            case "tailwind":
                await Meddle(player, global::Regatta.Meddling.Wind, target, gm, client);
                return;
            case "kraken":
                await Meddle(player, global::Regatta.Meddling.Kraken, null, gm, client);
                return;
        }

        if (sub == "start" && (player.IsBroadcaster || player.IsModerator || player.IsGameAdmin || player.IsGameModerator))
        {
            // the streamer starts the sign-up now without their own character joining it; with none open, there is
            // nothing to start, and it opens one as !regatta does
            if (global::Regatta.StartNow()) return;
        }
        var result = global::Regatta.Join(player);
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

    /// <summary>
    /// !regatta keg / wind / kraken. A random racer is free now and then; before the free one is back, or aimed at a
    /// racer, it costs coins, and the viewer is told the price first and sends the command again to pay. The coins come
    /// off the game's balance at once (a second command cannot spend them again), it happens, and then the server takes
    /// them; a race that ends meanwhile never charges for nothing, and coins the server did not take are given back.
    /// </summary>
    private async System.Threading.Tasks.Task Meddle(PlayerController player, global::Regatta.Meddling kind, string target, GameMessage gm, GameClient client)
    {
        if (kind == global::Regatta.Meddling.Kraken && !player.IsBroadcaster && !player.IsModerator && !player.IsGameAdmin && !player.IsGameModerator)
        {
            client.SendReply(gm, Localization.MSG_REGATTA_KRAKEN_NO_RIGHTS);
            return;
        }
        var check = global::Regatta.Meddle(player, kind, target, true, out var name, out var wait);
        if (check != global::Regatta.MeddleResult.Done)
        {
            Refuse(check, name, wait, gm, client);
            return;
        }

        var aimed = kind != global::Regatta.Meddling.Kraken && !string.IsNullOrWhiteSpace(target);
        var freeIn = RegattaMeddling.FreeIn(player.Id);
        var price = kind == global::Regatta.Meddling.Kraken ? 0 : aimed ? global::Regatta.AimedPrice(player.Id) : freeIn > 0f ? RegattaMeddling.RandomPrice : 0;
        var confirm = UnityEngine.Mathf.RoundToInt(RegattaMeddling.ConfirmSeconds);
        if (price > 0)
        {
            if (!RegattaMeddling.Confirmed(player.Id, kind + "|" + (aimed ? name.ToLowerInvariant() : "")))
            {
                if (aimed) client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_PRICE_AIMED, name, price, confirm);
                else client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_PRICE, UnityEngine.Mathf.CeilToInt(freeIn), confirm, price);
                return;
            }
            if (player.Resources.Coins < price)
            {
                client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_NO_COINS, price);
                return;
            }
        }

        var result = global::Regatta.Meddle(player, kind, target, false, out name, out wait);
        if (result != global::Regatta.MeddleResult.Done)
        {
            Refuse(result, name, wait, gm, client);
            return;
        }
        if (price > 0) player.Resources.Coins -= price;
        if (kind == global::Regatta.Meddling.Kraken)
        {
            client.SendReply(gm, Localization.MSG_REGATTA_KRAKEN_DONE, name);
            return;
        }
        if (price == 0)
        {
            // no chat reply for a free one: the feed on the stream says it, and a busy chat's replies filled the bot's
            // message budget ahead of the race's own announcements
            RegattaMeddling.UsedFree(player.Id);
            return;
        }
        var taken = false;
        try
        {
            var left = await Game.RavenNest.Mangrove.SpendAsync(player.Id, price);
            taken = left >= 0;
            if (!taken) Shinobytes.Debug.LogWarning(player.Name + " meddled in the regatta for " + price + " coins the server did not find.");
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The regatta could not take " + player.Name + "'s " + price + " coins: " + exc.Message);
        }
        if (!taken)
        {
            if (player) player.Resources.Coins += price;
            client.SendReply(gm, kind == global::Regatta.Meddling.Keg ? Localization.MSG_REGATTA_KEG_NOT_CHARGED : Localization.MSG_REGATTA_WIND_NOT_CHARGED, name);
            return;
        }
        global::Regatta.Paid(player.Id, price, aimed);
        client.SendReply(gm, kind == global::Regatta.Meddling.Keg ? Localization.MSG_REGATTA_KEG_PAID : Localization.MSG_REGATTA_WIND_PAID, name, price);
    }

    private static void Refuse(global::Regatta.MeddleResult result, string name, int wait, GameMessage gm, GameClient client)
    {
        switch (result)
        {
            case global::Regatta.MeddleResult.NoRace: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_NO_RACE); break;
            case global::Regatta.MeddleResult.Racing: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_RACING); break;
            case global::Regatta.MeddleResult.Raced: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_RACED); break;
            case global::Regatta.MeddleResult.NoKegs: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_NO_KEGS); break;
            case global::Regatta.MeddleResult.NoWinds: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_NO_WINDS, name); break;
            case global::Regatta.MeddleResult.WindRest: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_WIND_REST, name, wait); break;
            case global::Regatta.MeddleResult.NoTarget: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_NO_TARGET, name); break;
            case global::Regatta.MeddleResult.Finished: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_FINISHED, name); break;
            case global::Regatta.MeddleResult.Protected: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_PROTECTED, name, wait); break;
            case global::Regatta.MeddleResult.KrakenUsed: client.SendReply(gm, Localization.MSG_REGATTA_KRAKEN_USED); break;
            default: client.SendReply(gm, Localization.MSG_REGATTA_MEDDLE_NOBODY); break;
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
