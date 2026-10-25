using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Who helps the hero (docs/hero-mode-design.md, 7c), so chat sees itself: per viewer, the damage dealt on Ravebridge
/// while hero mode runs, the healing the hero got, the revives, cheers and taunts. Cleared when hero mode starts.
/// Also a short feed of what chat just did (a cheer, a heal, a taunt, a revive) for the HUD.
/// </summary>
public static class HeroHelpers
{
    public sealed class Helper
    {
        public Guid Id;
        public string Name;
        public long Damage;
        public long Healing;
        public int Revives;
        public int Cheers;
        public int Taunts;
        public double Score => Damage + Healing * 1.5 + Revives * 5000 + Cheers * 250 + Taunts * 500;
    }

    public struct Line
    {
        public string Text;
        public float At;
    }

    private static readonly Dictionary<Guid, Helper> helpers = new Dictionary<Guid, Helper>();
    private static readonly List<Line> feed = new List<Line>();

    public static event Action Changed;

    public static IReadOnlyList<Line> Feed => feed;

    public static void Clear()
    {
        helpers.Clear();
        feed.Clear();
        Changed?.Invoke();
    }

    /// <summary>The best helpers first.</summary>
    public static IEnumerable<Helper> Top(int count) => helpers.Values.OrderByDescending(h => h.Score).Take(count);

    public static int Count => helpers.Count;

    private static Helper Of(PlayerController player)
    {
        if (!helpers.TryGetValue(player.Id, out var h))
        {
            h = new Helper { Id = player.Id, Name = player.Name };
            helpers[player.Id] = h;
        }
        return h;
    }

    /// <summary>A viewer's place in it: a viewer on Ravebridge while someone else is the hero.</summary>
    private static bool Counts(PlayerController player)
    {
        var hero = HeroController.Active;
        return hero && player && player != hero.Player && player.InRavebridge;
    }

    /// <summary>EnemyController.TakeDamage: a viewer's hit on Ravebridge.</summary>
    public static void Damage(PlayerController player, int damage)
    {
        if (damage <= 0 || !Counts(player)) return;
        Of(player).Damage += damage;
    }

    /// <summary>PlayerCombat.HealTarget and !hero heal: healing the hero got from a viewer.</summary>
    public static void Healing(PlayerController healer, PlayerController target, int amount)
    {
        var hero = HeroController.Active;
        if (amount <= 0 || !hero || target != hero.Player || !Counts(healer)) return;
        Of(healer).Healing += amount;
    }

    public static void Revive(PlayerController player)
    {
        if (!Counts(player)) return;
        Of(player).Revives++;
        Changed?.Invoke();
    }

    public static void Cheer(PlayerController player)
    {
        if (!Counts(player)) return;
        Of(player).Cheers++;
        Changed?.Invoke();
    }

    public static void Taunt(PlayerController player)
    {
        if (!Counts(player)) return;
        Of(player).Taunts++;
        Changed?.Invoke();
    }

    /// <summary>A line for the HUD's feed (the last few, for a few seconds).</summary>
    public static void Say(string text)
    {
        feed.Add(new Line { Text = text, At = Time.time });
        while (feed.Count > 5) feed.RemoveAt(0);
        Changed?.Invoke();
    }
}
