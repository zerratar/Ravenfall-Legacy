using System.Collections.Generic;
using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     What the stream hears about the Legendaries (docs/legendary-design.md, section 7): one short chat line for a
///     find or a full awakening, at most one Legendary line a minute (lines that pile up are joined into one), and a
///     word over the player. Also follows the awakening the server counts on the item's tag (the inventory feed brings
///     it): the player's numbers and power follow the new rank, and the glow with them. The game only shows these;
///     the server decided them.
/// </summary>
public static class LegendaryNews
{
    /// <summary>At most one Legendary line in chat this often (seconds).</summary>
    public const float Every = 60f;

    private static readonly List<string> pending = new List<string>();
    private static readonly HashSet<PlayerController> refresh = new HashSet<PlayerController>();
    private static float nextLine;

    /// <summary>Lines said this session (for tests: they show what chat got, or would have got).</summary>
    public static readonly List<string> Said = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        pending.Clear();
        refresh.Clear();
        Said.Clear();
        nextLine = 0f;
    }

    /// <summary>The server gave a player a Legendary (a raid, dungeon or tower reward).</summary>
    public static void Found(PlayerController player, string itemName)
    {
        if (!player) return;
        pending.Add(string.Format(Localization.MSG_LEGENDARY_FOUND_PART, player.PlayerName, itemName));
        CombatPaths.Word(player, Localization.UI_FLOAT_LEGENDARY, new Color(1f, 0.78f, 0.3f));
    }

    /// <summary>
    ///     The inventory feed changed a stack's tag (Inventory): for a Legendary that is its awakening count. Its wearer's
    ///     numbers follow on the next frame, and a new rank is shown (rank 5 is said in chat).
    /// </summary>
    public static void TagChanged(PlayerController player, GameInventoryItem stack, string before, string after)
    {
        if (!player || stack?.Item == null) return;
        var legend = Legendary.Get(stack.Item.Id);
        if (legend == null) return;
        refresh.Add(player);
        var from = Legendary.RankOfTag(before);
        var to = Legendary.RankOfTag(after);
        if (to <= from) return;
        CombatPaths.Word(player, string.Format(Localization.UI_FLOAT_LEGENDARY_RANK, to), new Color(1f, 0.85f, 0.45f));
        if (to >= Legendary.MaxRank) pending.Add(string.Format(Localization.MSG_LEGENDARY_AWAKENED_PART, player.PlayerName, legend.Name));
    }

    /// <summary>Once a frame (GameManager).</summary>
    public static void Update()
    {
        if (refresh.Count > 0)
        {
            foreach (var p in refresh)
            {
                if (p && !p.isDestroyed && !p.Removed) p.UpdateEquipmentEffect();
            }
            refresh.Clear();
        }

        if (pending.Count == 0 || Time.unscaledTime < nextLine) return;
        var game = GameManager.Instance;
        if (!game) return;
        var line = string.Join("; ", pending);
        pending.Clear();
        nextLine = Time.unscaledTime + Every;
        Said.Add(line);
        game.RavenBot?.Announce(Localization.MSG_LEGENDARY, line);
    }
}
