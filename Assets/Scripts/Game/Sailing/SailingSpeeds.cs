using RavenNest.Models;
using UnityEngine;

/// <summary>
/// How fast the ferry and own boats sail (docs/own-boats.md, "Speeds", decided 2026-10-26). The Sailing level fills a
/// curve towards each vessel's cap instead of adding speed without end: the ferry used to sail at 20 plus the captain's
/// level, so on a high level stream a 500 m leg took a second and its passengers barely got any Sailing exp.
/// </summary>
public static class SailingSpeeds
{
    /// <summary>How quickly levels reach the cap: about half way at level 100, 86% at 300, all of it near 999.</summary>
    public const float LevelScale = 150f;

    /// <summary>The ferry's cap, as times its own slowest speed (20, the PathSelector's movement speed).</summary>
    public const float FerryCapFactor = 4f;

    public static float Reach(int level) => 1f - Mathf.Exp(-Mathf.Max(0, level) / LevelScale);

    /// <summary>The ferry's speed with a captain of this Sailing level (0 without one); boost scrolls multiply it.</summary>
    public static float Ferry(float slowest, int captainLevel) => slowest + (slowest * FerryCapFactor - slowest) * Reach(captainLevel);

    /// <summary>An own boat's steady speed in metres a second, by its tier and the owner's Sailing level.</summary>
    public static float Boat(BoatItems.Tier tier, int sailingLevel)
    {
        var (slowest, fastest) = tier switch
        {
            BoatItems.Tier.Sloop => (20f, 50f),
            BoatItems.Tier.FishingBoat => (16f, 36f),
            _ => (12f, 24f),
        };
        return slowest + (fastest - slowest) * Reach(sailingLevel);
    }
}

/// <summary>
/// Fishing at sea from an own boat (docs/own-boats.md, phase 3). A player at anchor fishing is saved as on
/// <see cref="Island"/> 10 (Sea on the server, which the game's older RavenNest.Models does not name), so the server
/// gives them the sea's catch: the fish from land and the fish below, which only bite out here.
/// </summary>
public static class SeaFishing
{
    public const Island Island = (Island)10;

    public static readonly System.Collections.Generic.HashSet<string> SeaOnly = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "Raw Mackerel", "Raw Tuna", "Raw Marlin", "Raw Anglerfish", "Raw Giant Squid", "Raw Sea Serpent", "Message in a Bottle",
    };

    /// <summary>Whether a player is out at anchor on their own boat with their line in the water.</summary>
    public static bool IsFishing(PlayerController player)
    {
        var f = player ? player.ferryHandler : null;
        return f && f.OnOwnBoat && f.OwnBoat.State == OwnBoat.Course.Anchored && player.GetTask() == TaskType.Fishing;
    }
}

/// <summary>
/// Boats as items: item type 37 and category 14 on the server (Boat, Vehicle), which the game's older RavenNest.Models
/// does not name yet. A boat goes in its own slot, changes no stats and is never drawn on the character.
/// </summary>
public static class BoatItems
{
    public const ItemType Type = (ItemType)37;
    public const ItemCategory Category = (ItemCategory)14;

    public enum Tier { Rowboat, FishingBoat, Sloop }

    public static bool Is(Item item) => item != null && (item.Type == Type || item.Category == Category);

    public static Tier TierOf(Item item)
    {
        var name = item?.Name ?? "";
        if (name.Equals("Sloop", System.StringComparison.OrdinalIgnoreCase)) return Tier.Sloop;
        if (name.Equals("Fishing Boat", System.StringComparison.OrdinalIgnoreCase)) return Tier.FishingBoat;
        return Tier.Rowboat;
    }
}
