using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RavenNest.Models;

/// <summary>
/// The day's challenge (docs/hero-mode-design.md, "Daily challenge"): one run a day that is the same for every streamer.
/// The UTC date seeds which enemies each wave brings, the bosses' order and the boons on offer, and picks two twists
/// that change the rules for the day. Captain Hargrove offers it beside the endless run; the server keeps each
/// streamer's best of the day and the day's leaderboard, apart from the all-time one.
/// </summary>
public partial class WaveRun
{
    public enum Twist { Swarm, ThickHides, BossRush, Hasty, EliteGuard, Hunters, DryFlasks, TinyTerrors, Blessed }

    /// <summary>A day's challenge: the day (yyyyMMdd, UTC), its two twists, and for Blessed the boon it starts with.</summary>
    public sealed class Challenge
    {
        public int Day;
        public Twist[] Twists;
        public string Boon;
        public int BossFrom;

        public bool Has(Twist twist) => Twists != null && Array.IndexOf(Twists, twist) >= 0;
    }

    // twists that undo each other are never drawn together
    private static readonly (Twist, Twist)[] Clashes = { (Twist.Swarm, Twist.ThickHides) };

    private static Challenge today;

    /// <summary>Today's challenge (UTC), the same in every game.</summary>
    public static Challenge Today()
    {
        var now = DateTime.UtcNow;
        var day = now.Year * 10000 + now.Month * 100 + now.Day;
        if (today != null && today.Day == day) return today;

        var rng = new System.Random(day * 7919);
        var all = (Twist[])Enum.GetValues(typeof(Twist));
        var first = all[rng.Next(all.Length)];
        var rest = all.Where(t => t != first && !Clashes.Any(c => (c.Item1 == first && c.Item2 == t) || (c.Item2 == first && c.Item1 == t))).ToArray();
        var second = rest[rng.Next(rest.Length)];
        return today = new Challenge
        {
            Day = day,
            Twists = new[] { first, second },
            Boon = Boons[rng.Next(Boons.Length)].Key,
            BossFrom = rng.Next(Bosses.Length),
        };
    }

    public static string TwistName(Twist twist)
    {
        switch (twist)
        {
            case Twist.Swarm: return Localization.UI_DAILY_SWARM;
            case Twist.ThickHides: return Localization.UI_DAILY_THICK;
            case Twist.BossRush: return Localization.UI_DAILY_BOSSES;
            case Twist.Hasty: return Localization.UI_DAILY_HASTY;
            case Twist.EliteGuard: return Localization.UI_DAILY_ELITES;
            case Twist.Hunters: return Localization.UI_DAILY_HUNTERS;
            case Twist.DryFlasks: return Localization.UI_DAILY_DRY;
            case Twist.TinyTerrors: return Localization.UI_DAILY_TINY;
            case Twist.Blessed: return Localization.UI_DAILY_BLESSED;
            default: return twist.ToString();
        }
    }

    public static string TwistText(Twist twist)
    {
        switch (twist)
        {
            case Twist.Swarm: return Localization.UI_DAILY_SWARM_TEXT;
            case Twist.ThickHides: return Localization.UI_DAILY_THICK_TEXT;
            case Twist.BossRush: return Localization.UI_DAILY_BOSSES_TEXT;
            case Twist.Hasty: return Localization.UI_DAILY_HASTY_TEXT;
            case Twist.EliteGuard: return Localization.UI_DAILY_ELITES_TEXT;
            case Twist.Hunters: return Localization.UI_DAILY_HUNTERS_TEXT;
            case Twist.DryFlasks: return Localization.UI_DAILY_DRY_TEXT;
            case Twist.TinyTerrors: return Localization.UI_DAILY_TINY_TEXT;
            case Twist.Blessed:
                var boon = Boons.FirstOrDefault(b => b.Key == Today().Boon);
                return string.Format(Localization.UI_DAILY_BLESSED_TEXT, boon != null ? boon.Name() : "?");
            default: return string.Empty;
        }
    }

    /// <summary>The twists' names, "Swarm and Boss rush".</summary>
    public static string TwistNames(Challenge challenge) =>
        string.Format(Localization.UI_WAVE_AND, TwistName(challenge.Twists[0]), TwistName(challenge.Twists[1]));

    /// <summary>The twists as the server keeps them for the website's board: "Hunters,Blessed-steel".</summary>
    public static string TwistKeys(Challenge challenge) =>
        string.Join(",", challenge.Twists.Select(t => t == Twist.Blessed && !string.IsNullOrEmpty(challenge.Boon) ? t + "-" + challenge.Boon : t.ToString()));

    /// <summary>The day's challenge this run plays, or null for an ordinary run.</summary>
    public Challenge Daily { get; private set; }

    private System.Random dailyRandom;

    private bool Twisted(Twist twist) => Daily != null && Daily.Has(twist);

    /// <summary>The day's seed for what the waves bring, else the usual randomness.</summary>
    private float Roll() => dailyRandom != null ? (float)dailyRandom.NextDouble() : UnityEngine.Random.value;

    /// <summary>A boss every fifth wave, every third on Boss rush.</summary>
    public int BossEvery => Twisted(Twist.BossRush) ? 3 : 5;
    public bool IsBossWave(int wave) => wave > 0 && wave % BossEvery == 0;

    private float CountFactor => Twisted(Twist.Swarm) ? 1.5f : Twisted(Twist.ThickHides) ? 0.75f : 1f;
    private float HitsFactor => Twisted(Twist.Swarm) ? 0.65f : Twisted(Twist.ThickHides) ? 1.4f : 1f;
    private float SpeedFactor => (Twisted(Twist.Hasty) ? 1.25f : 1f) * (Twisted(Twist.TinyTerrors) ? 1.15f : 1f);
    private float ScaleFactor => Twisted(Twist.TinyTerrors) ? 0.6f : 1f;
    private float Hunting => Twisted(Twist.Hunters) ? 0.5f : HunterChance;
    private int MostAlive => Twisted(Twist.Swarm) ? 16 : MaxAlive;

    private bool guardDue;
    private int guardAt;

    /// <summary>A daily run starts: the day's seed, and for Blessed its boon from the first wave.</summary>
    private void StartDaily(Challenge challenge)
    {
        Daily = challenge;
        dailyRandom = new System.Random(challenge.Day);
        if (challenge.Has(Twist.Blessed) && !string.IsNullOrEmpty(challenge.Boon))
        {
            boons.Add(challenge.Boon);
            if (challenge.Boon == "wind" && HeroController.Active) HeroController.Active.RefillPotions();
        }
    }

    /// <summary>Elite guard: from the third wave, every wave but a boss's brings an elite once half of it is in.</summary>
    private void PlanGuard(int count)
    {
        guardDue = Twisted(Twist.EliteGuard) && Wave >= 3 && !IsBossWave(Wave);
        guardAt = count / 2;
    }

    private void SpawnGuard()
    {
        if (!guardDue || toSpawn.Count > guardAt) return;
        guardDue = false;
        var kind = Kinds.Where(k => k.From <= Wave).OrderBy(k => k.From).Last();
        var at = SpawnPoint();
        var enemy = Make(kind, at, true);
        if (!enemy) return;
        alive[enemy] = kind;
        HeroEffects.Burst(HeroEffects.Get?.Arrival, at + Vector3.up * 0.8f, 2f, 2f);
        HeroHelpers.Say(string.Format(Localization.UI_HERO_FEED_GUARD, enemy.name));
        Changed?.Invoke();
    }

    // ---- The day's best, per character ------------------------------------------------------------------------

    private static string DailyKey(PlayerController hero) => "rf_hero_daily_" + hero.Id;

    /// <summary>The character's best of today's challenge (0 for none yet).</summary>
    public static int DailyBestOf(PlayerController hero)
    {
        if (!hero) return 0;
        var saved = PlayerPrefs.GetString(DailyKey(hero), string.Empty).Split(':');
        return saved.Length == 2 && int.TryParse(saved[0], out var day) && day == Today().Day && int.TryParse(saved[1], out var best) ? best : 0;
    }

    private static void SaveDailyBest(PlayerController hero, int day, int best)
    {
        PlayerPrefs.SetString(DailyKey(hero), day + ":" + best);
        PlayerPrefs.Save();
    }

    /// <summary>Today's challenge: every streamer's best run of it (the server's), refreshed with the other board.</summary>
    public static RavenNest.Models.HeroLeaderboard DailyBoard { get; private set; }

    private static async void RefreshDailyBoard()
    {
        var game = GameManager.Instance;
        if (!game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        try
        {
            var board = await game.RavenNest.Quests.HeroDailyLeaderboardAsync();
            if (board != null) DailyBoard = board;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Hero mode's daily leaderboard: " + exc.Message);
        }
    }
}
