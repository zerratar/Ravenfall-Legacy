using System.Collections.Generic;
using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     Combat v2's status effects in the game's fights (docs/combat-v2-design.md, section 5): what an element blow
///     leaves on its target, from the shared library's <see cref="StatusEffectSet"/>. Fire burns and poison stacks
///     (damage once a second, credited to whoever caused it), ice chills (slower attacks) and five chills freeze,
///     lightning jumps to an enemy nearby and now and then stuns. Frozen and stunned fighters do not attack. Bosses take
///     shorter crowd control, then ignore it for a while. Only with <see cref="CombatRules.Enabled"/>.
/// </summary>
public static class CombatEffects
{
    private const float ChainRange = 6f;

    private sealed class Entry
    {
        public readonly StatusEffectSet Set = new StatusEffectSet();
        public IAttackable Source;
    }

    private static readonly Dictionary<IAttackable, Entry> entries = new Dictionary<IAttackable, Entry>();
    private static readonly List<IAttackable> gone = new List<IAttackable>();
    private static readonly ICombatRandom random = new UnityCombatRandom();
    private static float tickTimer;

    private sealed class UnityCombatRandom : ICombatRandom
    {
        public double Next()
        {
            var value = Random.value;
            return value >= 1f ? 0.999999 : value;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Hook()
    {
        entries.Clear();
        CombatRules.BlowLanded = OnBlow;
    }

    /// <summary>The effects on a fighter, null when it has none.</summary>
    public static StatusEffectSet Get(IAttackable fighter)
    {
        return fighter != null && entries.TryGetValue(fighter, out var entry) ? entry.Set : null;
    }

    /// <summary>Stunned or frozen: no attacks until it passes.</summary>
    public static bool CannotAct(IAttackable fighter)
    {
        var set = Get(fighter);
        return set != null && set.CannotAct;
    }

    /// <summary>Attack intervals are divided by this: below 1 while chilled.</summary>
    public static float AttackSpeedFactor(IAttackable fighter)
    {
        var set = Get(fighter);
        return set == null ? 1f : (float)set.AttackSpeedFactor;
    }

    /// <summary>A blow landed (CombatRules): its element effects go on the defender, lightning jumps on.</summary>
    public static void OnBlow(IAttackable attacker, IAttackable defender, AttackResult result, CombatSheet defenderSheet)
    {
        if (!CombatRules.Enabled || result == null || defender == null)
        {
            return;
        }

        // thorns hurt the attacker back, life steal heals it
        if (result.Reflected > 0 && attacker != null && Alive(attacker)) Deal(defender, attacker, result.Reflected);
        if (result.Healed > 0 && attacker != null && Alive(attacker)) attacker.Heal(result.Healed);

        // Rend: a crit bleeds over 4 seconds
        if (result.Bleed >= 1 && defender != null && Alive(defender))
        {
            var bleeding = GetOrAdd(defender, attacker, defenderSheet);
            bleeding.Set.Bleed(result.Bleed / 4.0, 4);
        }

        if (result.Procs == null || result.Procs.Count == 0)
        {
            return;
        }

        // Searing: the attacker's burns hurt more
        var burnBonus = attacker != null && CombatRules.Enabled ? attacker.GetEquipmentStats()?.Gear(GearStat.BurnBonus) ?? 0 : 0;
        if (burnBonus != 0)
        {
            for (var i = 0; i < result.Procs.Count; ++i)
            {
                var proc = result.Procs[i];
                if (proc.Element == Element.Fire) { proc.Damage *= 1 + burnBonus; result.Procs[i] = proc; }
            }
        }

        if (!entries.TryGetValue(defender, out var entry))
        {
            entries[defender] = entry = new Entry();
        }

        entry.Set.IsBoss = defenderSheet != null && defenderSheet.IsBoss;
        entry.Source = attacker;
        var chained = entry.Set.ApplyProcs(result.Procs, random);
        if (chained >= 1 && defender is EnemyController enemy)
        {
            Chain(attacker, enemy, Mathf.RoundToInt((float)chained));
        }
    }

    private static Entry GetOrAdd(IAttackable fighter, IAttackable source, CombatSheet sheet)
    {
        if (!entries.TryGetValue(fighter, out var entry)) entries[fighter] = entry = new Entry();
        if (sheet != null) entry.Set.IsBoss = sheet.IsBoss;
        if (source != null) entry.Source = source;
        return entry;
    }

    /// <summary>A fighter died, left or came back: its effects end.</summary>
    public static void Clear(IAttackable fighter)
    {
        if (fighter != null) entries.Remove(fighter);
    }

    /// <summary>Once a frame (GameManager): damage over time once a second, effects run out.</summary>
    public static void Update(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer < 1f)
        {
            return;
        }

        var seconds = tickTimer;
        tickTimer = 0f;
        Regenerate(seconds);
        if (entries.Count == 0)
        {
            return;
        }

        gone.Clear();
        foreach (var pair in entries)
        {
            var fighter = pair.Key;
            var entry = pair.Value;
            if (!Alive(fighter))
            {
                gone.Add(fighter);
                continue;
            }

            var dot = entry.Set.Tick(seconds);
            if (entry.Set.Active.Count == 0) gone.Add(fighter);
            if (dot.Total <= 0) continue;

            // burn by fire resistance, poison by poison resistance (it ignores armour), bleed by armour
            var sheet = CombatRules.Sheet(fighter);
            var damage = dot.Physical * (1 - CombatMath.ArmorReduction(sheet != null ? sheet.Armor : 0))
                + dot.Fire * CombatMath.ResistanceMultiplier(sheet != null ? sheet.Resistance.Fire : 0, sheet != null && sheet.IsPlayer)
                + dot.Poison * CombatMath.ResistanceMultiplier(sheet != null ? sheet.Resistance.Poison : 0, sheet != null && sheet.IsPlayer);
            if (sheet != null) damage *= sheet.DamageTakenMultiplier; // damage reduction
            var amount = Mathf.RoundToInt((float)damage);
            if (amount >= 1) Deal(entry.Source, fighter, amount);
        }

        foreach (var fighter in gone) entries.Remove(fighter);
    }

    // Regeneration (Vitality): a share of max health each second while in combat
    private static void Regenerate(float seconds)
    {
        if (!CombatRules.Enabled) return;
        var game = GameManager.Instance;
        if (!game || game.Players == null) return;
        foreach (var player in game.Players.GetAllPlayers())
        {
            if (!player || !player.InCombat || player.Stats == null || player.Stats.IsDead) continue;
            var regen = player.EquipmentStats.Gear(GearStat.CombatRegen);
            if (regen <= 0) continue;
            var amount = Mathf.RoundToInt((float)(player.Stats.Health.MaxValue * regen * seconds));
            if (amount >= 1 && player.Stats.Health.CurrentValue < player.Stats.Health.MaxValue) player.Stats.Health.Add(amount);
        }
    }

    private static bool Alive(IAttackable fighter)
    {
        if (fighter is Object unityObject && !unityObject) return false;
        var stats = fighter.GetStats();
        return stats != null && !stats.IsDead;
    }

    // a hit that is not a swing: the kill counts as the source's, as a swing's would (experience, quests)
    private static void Deal(IAttackable source, IAttackable target, int amount)
    {
        if (source is Object sourceObject && !sourceObject) source = null;
        if (source == null)
        {
            return; // enemies take damage only from players, and the one who caused it has gone
        }

        if (target.TakeDamage(source, amount) && source is PlayerController player && player.Combat != null && target is EnemyController)
        {
            player.Combat.Defeated(target);
        }
    }

    private static void Chain(IAttackable attacker, EnemyController from, int amount)
    {
        EnemyController nearest = null;
        var best = ChainRange * ChainRange;
        foreach (var other in EnemyController.Active)
        {
            if (!other || other == from || other.Island != from.Island || other.Stats == null || other.Stats.IsDead) continue;
            var d = (other.Position - from.Position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = other;
            }
        }

        if (nearest) Deal(attacker, nearest, amount);
    }
}
