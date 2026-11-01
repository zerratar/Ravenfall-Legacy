using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;

/// <summary>
/// What the server paid for something on the Mangrove island (a treasure chest, the pirates' chest, the regatta): the
/// coins and items go to the players here, a line goes in the feed, and only the rare finds (a Golden Banana, the
/// Bananarang, a Monkey Pet) are worth a line in chat.
/// </summary>
public static class MangroveRewards
{
    private static readonly HashSet<string> Rare = new HashSet<string> { "Golden Banana", "Bananarang", "Monkey Pet" };

    /// <summary>Gives out the rewards; the feed line gets the total coins and items ({0}, {1}), the chat line a rare find's player and item.</summary>
    public static void Apply(GameManager game, MangroveRewardData[] rewards, string feedLine, string rareChatLine)
    {
        if (game == null || rewards == null || rewards.Length == 0) return;
        var coins = 0L;
        var items = new List<EventItemReward>();
        foreach (var r in rewards)
        {
            var player = game.Players.GetPlayerById(r.CharacterId);
            if (player && r.Coins > 0)
            {
                player.Resources.Coins += r.Coins;
                coins += r.Coins;
            }
            if (r.Items != null) items.AddRange(r.Items);
        }
        if (items.Count > 0) game.AddItems(items.ToArray());

        // boat looks earned with it: a line each in the feed (the regatta's sails, the treasure's flags, the pirates' colours)
        foreach (var r in rewards)
        {
            if (r.Looks == null || r.Looks.Length == 0) continue;
            var player = game.Players.GetPlayerById(r.CharacterId);
            if (!player) continue;
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_BOAT_LOOK_EARNED, player.Name, string.Join(", ", r.Looks.Select(l => l.Replace('_', ' ')))), "\uf21a");
        }

        if (!string.IsNullOrEmpty(feedLine))
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, feedLine, coins.ToString("N0", CultureInfo.InvariantCulture), items.Sum(i => i.Amount)), "");

        foreach (var item in items)
        {
            var known = game.Items.Get(item.ItemId);
            if (known == null || !Rare.Contains(known.Name)) continue;
            var player = game.Players.GetPlayerById(item.CharacterId);
            if (!player) continue;
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_MANGROVE_RARE, player.Name, known.Name), "");
            if (!string.IsNullOrEmpty(rareChatLine)) game.RavenBot?.Announce(rareChatLine, player.Name, known.Name);
        }
    }
}
