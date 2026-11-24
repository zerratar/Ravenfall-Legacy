using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Damage done, healing done and damage taken per player over one fight, for the meters window.
/// </summary>
/// <remarks>
/// <c>PlayerSessionStats</c> already counts damage and healing, but per session and with a rolling
/// ten second window, which cannot answer "who did what in this dungeon". This is the per run
/// version: started when the run begins, ended when it finishes, and kept until the next one so the
/// result can be shown afterwards.
///
/// <para>
/// Recording is three additions on a dictionary lookup, because it sits on the hit path of every
/// player in the fight. Sorting and percentages are left to whoever reads it, on their own interval.
/// </para>
/// </remarks>
public sealed class CombatMeter
{
    /// <summary>The meter for the current or most recent dungeon run.</summary>
    public static readonly CombatMeter Dungeon = new CombatMeter();

    /// <summary>
    /// The meter for the current or most recent tower boss fight, a mini boss or a floor boss (TowerManager
    /// begins and ends it; each loaded part of the tower has one of its own for the time it is trained on).
    /// </summary>
    public static readonly CombatMeter Tower = new CombatMeter();

    /// <summary>What the run is, for the window's title ("Floor 1, part 7 - Big Gorp"); null for a dungeon.</summary>
    public string Title;

    /// <summary>A short line for the window while the run goes on ("MINI BOSS"); null for a dungeon, which says its room.</summary>
    public string Caption;

    /// <summary>How the run ended ("VICTORY"), for the result line; null: the plain "RESULT".</summary>
    public string Outcome;

    /// <summary>Players pointed out in the result (who landed the last blow on a floor boss), with <see cref="MarkText"/> by their names.</summary>
    public readonly HashSet<Guid> Marked = new HashSet<Guid>();

    public string MarkText;

    /// <summary>The part of <see cref="TotalDamage"/> done to the run's main target (a tower boss; the rest went to its helpers).</summary>
    public long TargetDamage;

    public sealed class Entry
    {
        public Guid PlayerId;
        public PlayerController Player;
        public string Name;
        public string ColorHex;
        public long Damage;
        public long Healing;
        public long Taken;
        /// <summary>The style the player last attacked or healed with, for the row icon.</summary>
        public CombatStyle Style;
    }

    public enum CombatStyle { Melee, Ranged, Magic, Healing }

    private readonly Dictionary<Guid, Entry> byPlayer = new Dictionary<Guid, Entry>();
    private readonly List<Entry> entries = new List<Entry>();

    private float startTime;
    private float endTime;

    /// <summary>True between <see cref="Begin"/> and <see cref="End"/>.</summary>
    public bool Running { get; private set; }

    /// <summary>Bumped on every Begin, so a reader can tell a new run from the old one.</summary>
    public int RunId { get; private set; }

    /// <summary>Real time when the last run ended, or 0 if one is running or none has run.</summary>
    public float EndedAt => Running ? 0f : endTime;

    public IReadOnlyList<Entry> Entries => entries;

    /// <summary>Seconds the fight has lasted, frozen once it ends.</summary>
    public float Duration
    {
        get
        {
            if (RunId == 0) return 0f;
            return Mathf.Max(0f, (Running ? Time.unscaledTime : endTime) - startTime);
        }
    }

    public long TotalDamage { get; private set; }
    public long TotalHealing { get; private set; }
    public long TotalTaken { get; private set; }

    public void Begin()
    {
        byPlayer.Clear();
        entries.Clear();
        TotalDamage = 0;
        TotalHealing = 0;
        TotalTaken = 0;
        TargetDamage = 0;
        Outcome = null;
        Marked.Clear();
        startTime = Time.unscaledTime;
        endTime = 0f;
        Running = true;
        RunId++;
    }

    public void End()
    {
        if (!Running)
        {
            return;
        }

        Running = false;
        endTime = Time.unscaledTime;
    }

    public void AddDamage(PlayerController player, int amount)
    {
        if (!Running || amount <= 0) return;
        var e = Get(player);
        if (e == null) return;
        e.Damage += amount;
        e.Style = player.TrainingRanged ? CombatStyle.Ranged : player.TrainingMagic ? CombatStyle.Magic : CombatStyle.Melee;
        TotalDamage += amount;
    }

    public void AddHealing(PlayerController player, int amount)
    {
        if (!Running || amount <= 0) return;
        var e = Get(player);
        if (e == null) return;
        e.Healing += amount;
        e.Style = CombatStyle.Healing;
        TotalHealing += amount;
    }

    public void AddTaken(PlayerController player, int amount)
    {
        if (!Running || amount <= 0) return;
        var e = Get(player);
        if (e == null) return;
        e.Taken += amount;
        TotalTaken += amount;
    }

    /// <summary>A player's entry in this run, or null when they did nothing in it.</summary>
    public Entry Find(Guid playerId) => byPlayer.TryGetValue(playerId, out var e) ? e : null;

    private Entry Get(PlayerController player)
    {
        if (!player)
        {
            return null;
        }

        var id = player.Id;
        if (!byPlayer.TryGetValue(id, out var e))
        {
            e = new Entry
            {
                PlayerId = id,
                Name = player.PlayerName,
                Style = player.TrainingHealing ? CombatStyle.Healing
                    : player.TrainingRanged ? CombatStyle.Ranged
                    : player.TrainingMagic ? CombatStyle.Magic
                    : CombatStyle.Melee,
            };
            byPlayer[id] = e;
            entries.Add(e);
        }

        // Refreshed on every hit: a player who leaves and rejoins gets a new controller.
        e.Player = player;
        e.ColorHex = player.PlayerNameHexColor;
        return e;
    }
}
