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
