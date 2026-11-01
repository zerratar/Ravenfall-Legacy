using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// A boat's look (docs/own-boats.md, "Customizing boats"): the recipe kept in the boat stack's Tag on the server
/// ("look:1;hull=2;front=1;sails=7;flag=3"), read here to assemble the boat (BoatAssembler). Parts left out of the
/// recipe take the boat's defaults, as the server's BoatLooks has them.
/// </summary>
public static class BoatLook
{
    /// <summary>The parts a look is made of, in the order !boat look lists them.</summary>
    public static readonly string[] Slots = { "hull", "front", "middle", "back", "figurehead", "ram", "sails", "flag", "wood" };

    private static readonly Dictionary<string, int> Defaults = new Dictionary<string, int>
    {
        ["hull"] = 1, ["front"] = 1, ["middle"] = 1, ["back"] = 3, ["figurehead"] = 0, ["ram"] = 0,
        ["sails"] = 1, ["flag"] = 0, ["wood"] = 1,
    };

    public static Dictionary<string, int> Parse(string tag)
    {
        var look = new Dictionary<string, int>(Defaults);
        if (string.IsNullOrEmpty(tag) || !tag.StartsWith("look:", StringComparison.Ordinal)) return look;
        foreach (var part in tag.Split(';'))
        {
            var kv = part.Split('=');
            if (kv.Length == 2 && Defaults.ContainsKey(kv[0]) && int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                look[kv[0]] = v;
        }
        return look;
    }

    /// <summary>The look of the boat a player has equipped (its stack's Tag), or the defaults.</summary>
    public static string TagOf(PlayerController player)
    {
        if (!player || player.Inventory == null) return null;
        return player.Inventory.GetEquipmentBySlot(ItemSlot.Boat)?.InventoryItem?.Tag;
    }
}
