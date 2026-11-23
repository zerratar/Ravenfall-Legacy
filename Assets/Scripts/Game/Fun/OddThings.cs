using System;
using System.Collections.Generic;
using System.Reflection;
using RavenNest.Models;

/// <summary>
/// The words for the silly things (docs/easter-eggs.md): the odd finds the server turns up while gathering (an Old
/// Boot, Quackers, a Pet Rock, a Birds Nest, a Lookalike Potato, a Very Good Stick), the tower towns' keepsakes (the
/// Rubber Duck, the Holy Hand Grenade, Woolly's Bell) and a few words !use answers although they are no item.
/// </summary>
/// <remarks>
/// Using one of these changes nothing: the item stays, no server call is made, a line is picked here and said in
/// chat. The lines are Localization fields named UI_ODD_KEY_n and UI_KEEPSAKE_KEY_n (n from 1), so a translation can
/// have as many as the English, and {player} is the user's name.
/// </remarks>
public static class OddThings
{
    private const string PlayerPlaceholder = "{player}";

    /// <summary>The server's odd finds (RavenNest OddFinds) by item name, and the key of their lines.</summary>
    private static readonly Dictionary<string, string> Finds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Old Boot", "OLD_BOOT" },
        { "Quackers", "QUACKERS" },
        { "Pet Rock", "PET_ROCK" },
        { "Birds Nest", "BIRDS_NEST" },
        { "Lookalike Potato", "LOOKALIKE_POTATO" },
        { "Very Good Stick", "VERY_GOOD_STICK" },
    };

    /// <summary>The tower towns' keepsakes (quest chain rewards), which said they could not be used before.</summary>
    private static readonly Dictionary<string, string> Keepsakes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Rubber Duck", "RUBBER_DUCK" },
        { "Holy Hand Grenade", "HOLY_HAND_GRENADE" },
        { "Woollys Bell", "WOOLLYS_BELL" },
    };

    /// <summary>Words !use answers that are no item at all.</summary>
    private static readonly Dictionary<string, Func<string>> Secrets = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
    {
        { "xyzzy", () => Localization.UI_SECRET_USE_XYZZY },
        { "plugh", () => Localization.UI_SECRET_USE_PLUGH },
        { "the force", () => Localization.UI_SECRET_USE_FORCE },
        { "force", () => Localization.UI_SECRET_USE_FORCE },
        { "head", () => Localization.UI_SECRET_USE_HEAD },
        { "your head", () => Localization.UI_SECRET_USE_HEAD },
        { "my head", () => Localization.UI_SECRET_USE_HEAD },
        { "brain", () => Localization.UI_SECRET_USE_HEAD },
        { "your brain", () => Localization.UI_SECRET_USE_HEAD },
        { "cheats", () => Localization.UI_SECRET_USE_CHEATS },
        { "cheat", () => Localization.UI_SECRET_USE_CHEATS },
        { "hacks", () => Localization.UI_SECRET_USE_CHEATS },
    };

    private static readonly Random random = new Random();

    /// <summary>Whether !use has a line for this item.</summary>
    public static bool Handles(Item item) => item != null && (Finds.ContainsKey(item.Name ?? "") || Keepsakes.ContainsKey(item.Name ?? ""));

    /// <summary>Whether the item is one of the server's odd finds.</summary>
    public static bool IsOddFind(string itemName) => !string.IsNullOrEmpty(itemName) && Finds.ContainsKey(itemName);

    /// <summary>A random line for using the item, with the user's name in it; null when it has none.</summary>
    public static string UseLine(Item item, string player)
    {
        if (item == null) return null;
        string key;
        var prefix = Finds.TryGetValue(item.Name ?? "", out key) ? "UI_ODD_" : Keepsakes.TryGetValue(item.Name ?? "", out key) ? "UI_KEEPSAKE_" : null;
        if (prefix == null) return null;
        var lines = Lines(prefix + key + "_");
        return lines.Count == 0 ? null : lines[random.Next(lines.Count)].Replace(PlayerPlaceholder, player ?? "");
    }

    /// <summary>The stream's line for an odd find, with the finder's name in it; null for an item this game does not know.</summary>
    public static string FoundLine(string itemName, string player)
    {
        if (!Finds.TryGetValue(itemName ?? "", out var key)) return null;
        var field = typeof(Localization).GetField("UI_ODD_FOUND_" + key, BindingFlags.Public | BindingFlags.Static);
        return (field?.GetValue(null) as string)?.Replace(PlayerPlaceholder, player ?? "");
    }

    /// <summary>What !use says to a word that is no item ("xyzzy"); null for every other word.</summary>
    public static string SecretUse(string query, string player)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        var q = string.Join(" ", query.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        return Secrets.TryGetValue(q, out var line) ? line().Replace(PlayerPlaceholder, player ?? "") : null;
    }

    private static List<string> Lines(string prefix)
    {
        var lines = new List<string>();
        for (var n = 1; n < 50; ++n)
        {
            var field = typeof(Localization).GetField(prefix + n, BindingFlags.Public | BindingFlags.Static);
            if (field == null) break;
            if (field.GetValue(null) is string s && !string.IsNullOrEmpty(s)) lines.Add(s);
        }
        return lines;
    }
}
