using System.Collections.Generic;
using System.Linq;
using RavenNest.Models;

/// <summary>
/// The kind of items a "!gift player ether armor set" or "!send 2 rune weapons set" asks for: the word before "set"
/// (armor or armour, weapon or weapons, ring, amulet). Without one the whole set goes, as before.
/// </summary>
public static class ItemSetFilter
{
    public static ItemCategory? From(IReadOnlyList<string> words)
    {
        foreach (var word in words)
        {
            switch (word.ToLowerInvariant())
            {
                case "armor":
                case "armour":
                    return ItemCategory.Armor;
                case "weapon":
                case "weapons":
                    return ItemCategory.Weapon;
                case "ring":
                case "rings":
                    return ItemCategory.Ring;
                case "amulet":
                case "amulets":
                    return ItemCategory.Amulet;
            }
        }
        return null;
    }

    public static IReadOnlyList<Item> Apply(IReadOnlyList<Item> set, ItemCategory? only)
        => only == null ? set : set.Where(i => i.Category == only.Value).ToList();
}

/// <summary>
/// A "!gift" or "!send" asking for a whole set: who it is for, which set, which part of it, and how
/// many. "zerratar abraxas armor set 5", "zerratar abraxas armor sets x5", "zerratar 5 abraxas sets"
/// and "zerratar 5 sets of abraxas armor" all mean five of every Abraxas armour piece. The count
/// used to be read as part of the set's name, so asking for five of anything matched no set at all
/// and people gave the pieces away one by one.
/// </summary>
public sealed class ItemSetRequest
{
    public string Target { get; private set; }
    public string SetType { get; private set; }
    public ItemCategory? Only { get; private set; }
    public long Count { get; private set; } = 1;

    /// <summary>Whether the query names a set at all: the word set or sets after the player.</summary>
    public static bool IsSetQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;
        return Words(query).Skip(1).Any(w => w == "set" || w == "sets");
    }

    /// <summary>The request, or null when there is no player and set name in it.</summary>
    public static ItemSetRequest Parse(string query)
    {
        var words = Words(query);
        if (words.Count < 3) return null;

        var request = new ItemSetRequest { Target = words[0] };
        var rest = new List<string>();
        foreach (var word in words.Skip(1))
        {
            if (word == "set" || word == "sets" || word == "of") continue;
            if (TryCount(word, out var count))
            {
                request.Count = count;
                continue;
            }
            rest.Add(word);
        }

        if (rest.Count == 0) return null;
        // "elder" alone would match every elder item, so it takes the next word with it.
        request.SetType = rest[0] == "elder" && rest.Count > 1 ? rest[0] + " " + rest[1] : rest[0];
        request.Only = ItemSetFilter.From(rest);
        return request;
    }

    /// <summary>"Abraxas Helmet, Abraxas Chest and Abraxas Boots", or "5x Abraxas Helmet, ..." when more than one was asked for.</summary>
    public static string ListOf(IReadOnlyList<(string Name, long Amount)> items, long asked)
    {
        var names = items.Select(x => asked > 1 || x.Amount > 1 ? x.Amount + "x " + x.Name : x.Name).ToArray();
        return Utility.ReplaceLastOccurrence(string.Join(", ", names), ", ", Localization.MSG_LIST_AND);
    }

    private static List<string> Words(string query)
        => query.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLowerInvariant()).ToList();

    /// <summary>"5", "x5" or "5x". Nothing in a set's name is a number, so a number is the count.</summary>
    private static bool TryCount(string word, out long count)
    {
        var digits = word.Trim('x');
        if (digits.Length > 0 && digits.Length >= word.Length - 1 && long.TryParse(digits, out count) && count > 0)
        {
            return true;
        }
        count = 0;
        return false;
    }
}
