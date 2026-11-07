using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     Combat v2 in the game (docs/combat-v2-design.md): the shared rules in RavenNest.Combat.dll, fed from what the
///     game already has (skills, equipment and the effect modifiers). GameMath's damage and healing functions come
///     here while <see cref="Enabled"/> is on; with it off they run today's formulas, and the health pool is the
///     Health level again, so the two can be compared in the editor.
/// </summary>
public static class CombatRules
{
    /// <summary>The new combat: the health pool, the new hit chance, armour, crits and the times ten scale.</summary>
    public static bool Enabled = false;

    /// <summary>Health points per Health level: ten with the new combat, one with today's.</summary>
    public static int HealthPerLevel => Enabled ? CombatMath.HealthPerLevel : 1;

    /// <summary>The size of the health pool for a Health level (with its bonus).</summary>
    public static int MaxHealth(int healthLevel) => Mathf.Max(1, healthLevel * HealthPerLevel);

    /// <summary>The biggest normal hit or heal, on the scale in use (times ten with the new combat).</summary>
    public static int MaxHit(int level, int power) => Enabled ? (int)CombatMath.MaxHit(level, power) : GameMath.MaxHit(level, power);

    private sealed class UnityRandom : ICombatRandom
    {
        public double Next()
        {
            var value = Random.value;
            return value >= 1f ? 0.999999 : value;
        }
    }

    private static readonly ICombatRandom random = new UnityRandom();

    public static float Melee(IAttackable attacker, IAttackable defender) => Blow(attacker, defender, AttackStyle.Melee);
    public static float Ranged(IAttackable attacker, IAttackable defender) => Blow(attacker, defender, AttackStyle.Ranged);
    public static float Magic(IAttackable attacker, IAttackable defender) => Blow(attacker, defender, AttackStyle.Magic);

    public static float Heal(IAttackable healer)
    {
        var sheet = Sheet(healer);
        return sheet == null ? 0 : CombatMath.Heal(sheet, random).Amount;
    }

    private static float Blow(IAttackable attacker, IAttackable defender, AttackStyle style)
    {
        if (attacker == null || defender == null) return 0;
        var attackerSheet = Sheet(attacker);
        var defenderSheet = Sheet(defender);
        if (attackerSheet == null || defenderSheet == null) return 0;

        // a dodge (from food and potions) still comes first, as it does today
        var defenderModifiers = defender.GetModifiers();
        if (defenderModifiers != null && defenderModifiers.DodgeChance > 0 && Random.value <= defenderModifiers.DodgeChance)
            return 0;

        return CombatMath.Attack(attackerSheet, style, defenderSheet, random).Total;
    }

    /// <summary>
    ///     The sheet for a fighter: the style's skills and gear, with the effect modifiers applied the way their names
    ///     say (a hit chance bonus now raises accuracy; today it did nothing).
    /// </summary>
    public static CombatSheet Sheet(IAttackable fighter)
    {
        var stats = fighter?.GetStats();
        var eq = fighter?.GetEquipmentStats();
        if (stats == null || eq == null) return null;
        var mods = fighter.GetModifiers();

        float Mod(float value) => value <= 0 ? 1f : value;
        var strength = mods != null ? Mod(mods.StrengthMultiplier) : 1f;
        var hitChance = mods != null ? Mod(mods.HitChanceMultiplier) : 1f;

        var sheet = new CombatSheet
        {
            CombatLevel = stats.CombatLevel,
            MaxHealth = stats.Health.MaxValue,
            DefenseLevel = (int)(stats.Defense.MaxLevel * (mods != null ? Mod(mods.DefenseMultiplier) : 1f)),
            Armor = eq.ArmorPower,
            IsPlayer = !fighter.IsEnemy,
            CritChance = CombatMath.BaseCritChance + (mods != null ? mods.CriticalHitChance : 0),
            CritDamage = mods != null && mods.CriticalHitDamage > CombatMath.BaseCritDamage ? mods.CriticalHitDamage : CombatMath.BaseCritDamage,
        };

        sheet.Melee.DamageLevel = (int)(stats.Strength.MaxLevel * strength);
        sheet.Melee.AimLevel = stats.Attack.MaxLevel;
        sheet.Melee.Power = (int)(eq.WeaponPower * (mods != null ? Mod(mods.AttackPowerMultiplier) : 1f));
        sheet.Melee.Accuracy = (int)(eq.WeaponAim * hitChance);

        sheet.Ranged.DamageLevel = (int)(stats.Ranged.MaxLevel * strength);
        sheet.Ranged.AimLevel = stats.Ranged.MaxLevel;
        sheet.Ranged.Power = (int)(eq.RangedPower * (mods != null ? Mod(mods.RangedPowerMultiplier) : 1f));
        sheet.Ranged.Accuracy = (int)(eq.RangedAim * hitChance);

        sheet.Magic.DamageLevel = (int)(stats.Magic.MaxLevel * strength);
        sheet.Magic.AimLevel = stats.Magic.MaxLevel;
        sheet.Magic.Power = (int)(eq.MagicPower * (mods != null ? Mod(mods.MagicPowerMultiplier) : 1f));
        sheet.Magic.Accuracy = (int)(eq.MagicAim * hitChance);

        // healing weapons carry magic power today; healing has its own multiplier
        sheet.Healing.DamageLevel = stats.Healing.MaxLevel;
        sheet.Healing.AimLevel = stats.Healing.MaxLevel;
        sheet.Healing.Power = (int)(eq.MagicPower * (mods != null ? Mod(mods.HealingPowerMultiplier) : 1f));

        // the accuracy bonus applies to the skill side as well, so it helps fighters without gear accuracy too
        if (hitChance != 1f)
        {
            sheet.Melee.AimLevel = (int)(sheet.Melee.AimLevel * hitChance);
            sheet.Ranged.AimLevel = (int)(sheet.Ranged.AimLevel * hitChance);
            sheet.Magic.AimLevel = (int)(sheet.Magic.AimLevel * hitChance);
        }

        // an enemy's element, resistances and boss flag come from its definition
        if (fighter is IHasEnemyDefinition defined && defined.CombatDefinition != null)
            defined.CombatDefinition.ApplyElement(sheet);

        return sheet;
    }
}
