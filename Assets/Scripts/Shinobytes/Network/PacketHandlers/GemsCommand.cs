using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RavenNest.Combat;
using RavenNest.Models;

/// <summary>
///     Gems and sockets from chat (docs/gems-design.md). The bot sends "gems_command" with the action first:
///     !gems (the gems you own and your chance to keep one taken out), !gems enchant ruby 5 (raw gems enchanted by your
///     clan), !gems combine ruby flawed 2 (three into the next tier, for coins), !socket rune sword flawless ruby 2
///     (a gem into an item; the tier and the socket can be left out) and !unsocket rune sword 2. The server does it all;
///     the inventory feed brings the gems and sockets, and the equipment's stats follow.
/// </summary>
public class GemsCommand : ChatBotCommandHandler<string>
{
    public GemsCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var text = (data ?? "").Trim();
        var space = text.IndexOf(' ');
        var action = (space < 0 ? text : text.Substring(0, space)).ToLowerInvariant();
        var rest = space < 0 ? "" : text.Substring(space + 1).Trim();

        try
        {
            switch (action)
            {
                case "":
                case "list":
                    List(player, gm, client);
                    return;
                case "enchant":
                    await Enchant(player, rest, gm, client);
                    return;
                case "combine":
                case "merge":
                    await Combine(player, rest, gm, client);
                    return;
                case "socket":
                    await Socket(player, rest, gm, client);
                    return;
                case "unsocket":
                case "remove":
                    await Unsocket(player, rest, gm, client);
                    return;
                default:
                    // !gems ruby: what a ruby does where
                    if (Gems.TryParseKind(action, out var kind))
                    {
                        client.SendReply(gm, Localization.MSG_GEM_INFO, kind.ToString(),
                            Gems.Describe(kind, GemTier.Chipped, GemSlot.Weapon), Gems.Describe(kind, GemTier.Chipped, GemSlot.Armor),
                            Gems.Describe(kind, GemTier.Chipped, GemSlot.Jewellery), Gems.Describe(kind, GemTier.Perfect, GemSlot.Weapon));
                        return;
                    }
                    client.SendReply(gm, Localization.MSG_GEM_USAGE);
                    return;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("gems_command: " + exc);
            client.SendReply(gm, Localization.MSG_GEM_ERROR);
        }
    }

    private static void List(PlayerController player, GameMessage gm, GameClient client)
    {
        var owned = new List<string>();
        foreach (var item in player.Inventory.GetAllItems())
        {
            if (item?.Item == null || !Gems.TryGetGem(item.ItemId, out var kind, out var tier)) continue;
            owned.Add(item.Amount + "x " + Gems.Name(kind, tier));
        }

        if (owned.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_GEM_NONE);
            return;
        }
        client.SendReply(gm, Localization.MSG_GEM_LIST, string.Join(", ", owned));
    }

    private async Task Enchant(PlayerController player, string rest, GameMessage gm, GameClient client)
    {
        var words = Words(rest);
        var amount = TakeNumber(words, 1);
        if (words.Count == 0 || !Gems.TryParseKind(words[0], out var kind))
        {
            client.SendReply(gm, Localization.MSG_GEM_ENCHANT_USAGE);
            return;
        }

        var result = await Game.RavenNest.Players.GemEnchantAsync(player.Id, (int)kind, amount);
        if (!Ok(result, gm, client)) return;
        var made = new List<string>();
        for (var t = Gems.TierCount - 1; t >= 0; --t)
        {
            if (result.Counts != null && t < result.Counts.Length && result.Counts[t] > 0)
                made.Add(result.Counts[t] + "x " + Gems.Name(kind, (GemTier)t));
        }
        client.SendReply(gm, Localization.MSG_GEM_ENCHANTED, string.Join(", ", made));
    }

    private async Task Combine(PlayerController player, string rest, GameMessage gm, GameClient client)
    {
        var words = Words(rest);
        var times = TakeNumber(words, 1);
        if (!TryKindAndTier(words, out var kind, out var tier) || tier < 0)
        {
            client.SendReply(gm, Localization.MSG_GEM_COMBINE_USAGE);
            return;
        }

        var result = await Game.RavenNest.Players.GemCombineAsync(player.Id, (int)kind, tier, times);
        if (!Ok(result, gm, client)) return;
        var made = result.Counts != null && result.Counts.Length > 0 ? result.Counts[0] : 0;
        client.SendReply(gm, Localization.MSG_GEM_COMBINED, made + "x " + Gems.Name(kind, (GemTier)result.Tier), result.Coins.ToString("N0", CultureInfo.InvariantCulture));
    }

    private async Task Socket(PlayerController player, string rest, GameMessage gm, GameClient client)
    {
        // the item's name, then the gem (a tier before it if wanted), then a socket number if wanted
        var words = Words(rest);
        var socket = TakeNumber(words, 0);
        var at = words.FindLastIndex(w => Gems.TryParseKind(w, out _) && w.Length >= 3);
        if (at <= 0)
        {
            client.SendReply(gm, Localization.MSG_GEM_SOCKET_USAGE);
            return;
        }

        Gems.TryParseKind(words[at], out var kind);
        var tier = -1;
        var itemEnd = at;
        if (at > 1 && Gems.TryParseTier(words[at - 1], out var t) && words[at - 1].Length >= 4)
        {
            tier = (int)t;
            itemEnd = at - 1;
        }

        var item = FindItem(player, string.Join(" ", words.Take(itemEnd)), gm, client);
        if (item == null) return;
        if (!item.Item.TryGetGemSlot(out _) || item.Item.GetSocketCount() == 0)
        {
            client.SendReply(gm, Localization.MSG_GEM_NO_SOCKETS, item.Name);
            return;
        }

        var result = await Game.RavenNest.Players.GemSocketAsync(player.Id, item.InstanceId, (int)kind, tier, socket);
        if (!Ok(result, gm, client)) return;
        var gem = Gems.Name((GemKind)result.Kind, (GemTier)result.Tier);
        var message = string.Format(CultureInfo.InvariantCulture, "{0} -> {1} ({2}/{3})", gem, result.ItemName, result.Socket, result.SocketCount);
        client.SendReply(gm, Localization.MSG_GEM_SOCKETED, message, OldGem(result));
    }

    private async Task Unsocket(PlayerController player, string rest, GameMessage gm, GameClient client)
    {
        var words = Words(rest);
        var socket = TakeNumber(words, 0);
        if (words.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_GEM_UNSOCKET_USAGE);
            return;
        }

        var item = FindItem(player, string.Join(" ", words), gm, client);
        if (item == null) return;
        var result = await Game.RavenNest.Players.GemUnsocketAsync(player.Id, item.InstanceId, socket);
        if (!Ok(result, gm, client)) return;
        client.SendReply(gm, Localization.MSG_GEM_UNSOCKETED, result.ItemName, OldGem(result));
    }

    // "Your Chipped Ruby came back (12% chance)." or "... crumbled (12% chance to keep it)."
    private static string OldGem(GemResult result)
    {
        if (!result.OldKind.HasValue) return "";
        var name = Gems.Name((GemKind)result.OldKind.Value, (GemTier)result.OldTier.GetValueOrDefault());
        var chance = Math.Round(result.KeepChance * 100).ToString(CultureInfo.InvariantCulture) + "%";
        return string.Format(result.Kept ? Localization.MSG_GEM_KEPT : Localization.MSG_GEM_LOST, name, chance);
    }

    private bool Ok(GemResult result, GameMessage gm, GameClient client)
    {
        if (result == null)
        {
            client.SendReply(gm, Localization.MSG_GEM_ERROR);
            return false;
        }

        var kind = Gems.KindCount > result.Kind ? ((GemKind)result.Kind).ToString() : "";
        switch (result.Status)
        {
            case GemResultStatus.Ok: return true;
            case GemResultStatus.NotInClan: client.SendReply(gm, Localization.MSG_GEM_NOT_IN_CLAN); break;
            case GemResultStatus.NoEnchanting: client.SendReply(gm, Localization.MSG_GEM_NO_ENCHANTING); break;
            case GemResultStatus.NoRawGem: client.SendReply(gm, Localization.MSG_GEM_NO_RAW, kind); break;
            case GemResultStatus.NoGem: client.SendReply(gm, Localization.MSG_GEM_NOT_OWNED, kind); break;
            case GemResultStatus.NotEnoughGems: client.SendReply(gm, Localization.MSG_GEM_NOT_ENOUGH, Gems.Name((GemKind)result.Kind, (GemTier)result.Tier)); break;
            case GemResultStatus.NotEnoughCoins: client.SendReply(gm, Localization.MSG_GEM_NO_COINS, result.Coins.ToString("N0", CultureInfo.InvariantCulture)); break;
            case GemResultStatus.HighestTier: client.SendReply(gm, Localization.MSG_GEM_HIGHEST); break;
            case GemResultStatus.NotSocketable:
            case GemResultStatus.NoSockets: client.SendReply(gm, Localization.MSG_GEM_NO_SOCKETS, result.ItemName); break;
            case GemResultStatus.SocketsFull: client.SendReply(gm, Localization.MSG_GEM_FULL, result.ItemName, result.SocketCount); break;
            case GemResultStatus.NoSuchSocket: client.SendReply(gm, Localization.MSG_GEM_NO_SUCH_SOCKET, result.ItemName, result.SocketCount); break;
            case GemResultStatus.EmptySocket: client.SendReply(gm, Localization.MSG_GEM_EMPTY_SOCKET, result.Socket, result.ItemName); break;
            case GemResultStatus.NoSuchItem: client.SendReply(gm, Localization.MSG_GEM_ERROR); break;
            case GemResultStatus.Busy: client.SendReply(gm, Localization.MSG_GEM_BUSY); break;
            default: client.SendReply(gm, Localization.MSG_GEM_ERROR); break;
        }
        return false;
    }

    private GameInventoryItem FindItem(PlayerController player, string query, GameMessage gm, GameClient client)
    {
        query = query.Trim();
        var items = player.Inventory.GetAllItems();
        // equipped first: that is usually the one meant
        var item = items.Where(x => x?.Item != null && x.Name.Equals(query, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.InventoryItem.Equipped).FirstOrDefault()
            ?? items.Where(x => x?.Item != null && x.Item.Name.Equals(query, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.InventoryItem.Equipped).FirstOrDefault();
        if (item != null) return item;

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var resolver = ioc.Resolve<IItemResolver>();
        var queried = resolver.ResolveTradeQuery(query, parsePrice: false, parseAmount: false, playerToSearch: player);
        if (queried.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, query, string.Join(", ", queried.SuggestedItemNames));
            return null;
        }
        if (queried.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, query);
            return null;
        }

        item = items.Where(x => x?.Item != null && x.ItemId == queried.Item.Id).OrderByDescending(x => x.InventoryItem.Equipped).FirstOrDefault();
        if (item == null) client.SendReply(gm, Localization.MSG_ITEM_NOT_OWNED, queried.Item.Name);
        return item;
    }

    private static bool TryKindAndTier(List<string> words, out GemKind kind, out int tier)
    {
        kind = default;
        tier = -1;
        foreach (var w in words)
        {
            if (Gems.TryParseKind(w, out var k) && w.Length >= 3) kind = k;
            else if (Gems.TryParseTier(w, out var t) && w.Length >= 4) tier = (int)t;
            else return false;
        }
        return words.Count > 0;
    }

    private static List<string> Words(string text) => (text ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

    // a number at the end (an amount or a socket), taken off the words
    private static int TakeNumber(List<string> words, int fallback)
    {
        if (words.Count == 0) return fallback;
        var last = words[words.Count - 1].TrimStart('#', 'x');
        if (!int.TryParse(last, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return fallback;
        words.RemoveAt(words.Count - 1);
        return n;
    }
}
