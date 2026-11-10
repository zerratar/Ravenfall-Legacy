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
        public readonly List<Modifier> Modifiers = new List<Modifier>();
        public readonly List<Hot> Hots = new List<Hot>();
        public bool Empty => Set.Active.Count == 0 && Modifiers.Count == 0 && Hots.Count == 0;
    }

    /// <summary>A skill's timed change to a fighter (Shield Wall, Expose Weakness, Disarm, Rally, Rapid Fire).</summary>
    public enum ModifierKind { DamageTaken, DamageDealt, AttackSpeed }

    private sealed class Modifier { public ModifierKind Kind; public double Amount; public float Until; }

    private sealed class Hot { public IAttackable Healer; public double PerSecond; public float Until; }

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
        CombatRules.SheetModifiers = ApplyModifiers;
    }

    // the skills' timed buffs and debuffs on a fighter's sheet
    private static void ApplyModifiers(IAttackable fighter, CombatSheet sheet)
    {
        if (fighter == null || !entries.TryGetValue(fighter, out var entry) || entry.Modifiers.Count == 0) return;
        var now = Time.time;
        foreach (var m in entry.Modifiers)
        {
            if (m.Until < now) continue;
            switch (m.Kind)
            {
                case ModifierKind.DamageTaken: sheet.DamageTakenMultiplier *= System.Math.Max(0.05, 1 + m.Amount); break;
                case ModifierKind.DamageDealt: sheet.DamageMultiplier *= System.Math.Max(0.05, 1 + m.Amount); break;
                case ModifierKind.AttackSpeed: sheet.AttackSpeed *= 1 + m.Amount; break;
            }
        }
    }

    /// <summary>A skill's timed change on a fighter; the same kind from the same skill again refreshes it.</summary>
    public static void AddModifier(IAttackable fighter, ModifierKind kind, double amount, float seconds)
    {
        if (!CombatRules.Enabled || fighter == null) return;
        var entry = GetOrAdd(fighter, null, null);
        var until = Time.time + seconds;
        var same = entry.Modifiers.Find(m => m.Kind == kind && System.Math.Abs(m.Amount - amount) < 1e-6);
        if (same != null) same.Until = Mathf.Max(same.Until, until);
        else entry.Modifiers.Add(new Modifier { Kind = kind, Amount = amount, Until = until });
    }

    /// <summary>Damage over time from a skill (Arrow Rain, Toxic Cloud): fire burns, poison stacks, the rest bleeds.</summary>
    public static void AddDamageOverTime(IAttackable source, IAttackable target, Element? element, double total, float seconds)
    {
        if (!CombatRules.Enabled || target == null || total < 1 || seconds <= 0) return;
        var entry = GetOrAdd(target, source, CombatRules.Sheet(target));
        var perSecond = total / seconds;
        switch (element)
        {
            case Element.Fire: entry.Set.Burn(perSecond, seconds); break;
            case Element.Poison: entry.Set.Poison(perSecond); break;
            default: entry.Set.Bleed(perSecond, seconds); break;
        }
    }

    /// <summary>Healing over time from a skill (Renew, Sanctuary), once a second.</summary>
    public static void AddHealOverTime(IAttackable healer, IAttackable target, double total, float seconds)
    {
        if (!CombatRules.Enabled || target == null || total < 1 || seconds <= 0) return;
        var entry = GetOrAdd(target, null, null);
        entry.Hots.Add(new Hot { Healer = healer, PerSecond = total / seconds, Until = Time.time + seconds });
    }

    /// <summary>A stun, freeze or root from a skill; false when it did not land (a boss shrugging it off, Iron Skin).</summary>
    public static bool CrowdControl(IAttackable source, IAttackable target, StatusEffectKind kind, float seconds)
    {
        if (!CombatRules.Enabled || target == null || seconds <= 0) return false;
        var entry = GetOrAdd(target, source, CombatRules.Sheet(target));
        return entry.Set.CrowdControl(kind, seconds);
    }

    /// <summary>Iron Skin.</summary>
    public static void ImmuneToCrowdControl(IAttackable fighter, float seconds)
    {
        if (!CombatRules.Enabled || fighter == null) return;
        GetOrAdd(fighter, null, null).Set.ImmuneToCrowdControl(seconds);
    }

    /// <summary>Cleanse: burns, poison, chills and bleeding end.</summary>
    public static void Cleanse(IAttackable fighter)
    {
        if (fighter != null && entries.TryGetValue(fighter, out var entry)) entry.Set.Cleanse();
    }

    /// <summary>Burning, poisoned, chilled or bleeding (for Cleanse's use rule).</summary>
    public static bool HasHarmful(IAttackable fighter)
    {
        var set = Get(fighter);
        return set != null && (set.Has(StatusEffectKind.Burn) || set.Has(StatusEffectKind.Poison) || set.Has(StatusEffectKind.Chill) || set.Has(StatusEffectKind.Bleed));
    }

    /// <summary>Rooted, stunned or frozen: stays where it is.</summary>
    public static bool CannotMove(IAttackable fighter)
    {
        var set = Get(fighter);
        return set != null && set.CannotMove;
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

    /// <summary>Attack intervals are divided by this: below 1 while chilled, above it with Rapid Fire.</summary>
    public static float AttackSpeedFactor(IAttackable fighter)
    {
        if (fighter == null || !entries.TryGetValue(fighter, out var entry)) return 1f;
        var factor = entry.Set.AttackSpeedFactor;
        var now = Time.time;
        foreach (var m in entry.Modifiers) if (m.Kind == ModifierKind.AttackSpeed && m.Until >= now) factor *= 1 + m.Amount;
        return (float)factor;
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

        // Searing and fire gems: the attacker's burns hurt more; emeralds: poison; topaz: lightning's jumps
        var eq = attacker != null ? attacker.GetEquipmentStats() : null;
        var burnBonus = eq?.Gear(GearStat.BurnBonus) ?? 0;
        var poisonBonus = eq?.Gear(GearStat.PoisonBonus) ?? 0;
        var jumpBonus = eq?.Gear(GearStat.LightningJumpBonus) ?? 0;
        if (burnBonus != 0 || poisonBonus != 0 || jumpBonus != 0)
        {
            for (var i = 0; i < result.Procs.Count; ++i)
            {
                var proc = result.Procs[i];
                if (proc.Element == Element.Fire) proc.Damage *= 1 + burnBonus;
                else if (proc.Element == Element.Poison) proc.Damage *= 1 + poisonBonus;
                else if (proc.Element == Element.Lightning) proc.Damage *= 1 + jumpBonus;
                result.Procs[i] = proc;
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
        else if (chained >= 1 && defender is PlayerController player && attacker is EnemyController)
        {
            ChainToPlayer(attacker, player, Mathf.RoundToInt((float)chained)); // an enemy's lightning jumps to a player nearby
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

            var now = Time.time;
            entry.Modifiers.RemoveAll(m => m.Until < now);
            for (var i = entry.Hots.Count - 1; i >= 0; --i)
            {
                var hot = entry.Hots[i];
                var healed = Mathf.RoundToInt((float)(hot.PerSecond * seconds));
                if (healed >= 1) fighter.Heal(healed);
                if (hot.Until <= now) entry.Hots.RemoveAt(i);
            }

            var dot = entry.Set.Tick(seconds);
            if (entry.Empty) gone.Add(fighter);
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

    private static void ChainToPlayer(IAttackable attacker, PlayerController from, int amount)
    {
        var game = GameManager.Instance;
        if (!game || game.Players == null) return;
        PlayerController nearest = null;
        var best = ChainRange * ChainRange;
        foreach (var other in game.Players.GetAllPlayers())
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
