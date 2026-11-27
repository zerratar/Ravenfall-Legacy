using System;

[Serializable]
public class EquipmentStats : IComparable
{
    /// <summary>Health from the armour pieces' armour (CombatMath.HealthPerArmor), for the details window.</summary>
    [NonSerialized] public int ArmorHealth;
    /// <summary>Health from the MAX_HEALTH gear stat (jewellery, gems, the trees' flat health).</summary>
    [NonSerialized] public int FlatGearHealth;
    /// <summary>Health from "more health" shares (Hardy, Titan, gems in armour).</summary>
    [NonSerialized] public int BonusHealth;

    public int BaseArmorPower;
    public int BaseWeaponAim;
    public int BaseWeaponPower;
    public int BaseMagicPower;
    public int BaseMagicAim;
    public int BaseRangedPower;
    public int BaseRangedAim;
    public int BaseHealingPower;

    public int ArmorPowerBonus;
    public int WeaponAimBonus;
    public int WeaponPowerBonus;
    public int MagicPowerBonus;
    public int MagicAimBonus;
    public int RangedPowerBonus;
    public int RangedAimBonus;
    public int HealingPowerBonus;

    /// <summary>
    ///     Combat v2's gear stats beyond aim, power and armour (element damage, resistances, evasion, health, crits,
    ///     healing power), summed over the worn gear except the weapon (RavenNest.Combat.GearStats, from Item.Stats).
    ///     Element damage here adds to every hit.
    /// </summary>
    [NonSerialized] public RavenNest.Combat.GearStats Extras;

    /// <summary>
    ///     The weapons' own extras by style (index: RavenNest.Combat.AttackStyle). A player wears a weapon per style
    ///     (a sword, a bow and a staff at once); each weapon's element damage converts the hits of its own style only.
    /// </summary>
    [NonSerialized] public RavenNest.Combat.GearStats[] WeaponExtras;

    /// <summary>
    ///     What the worn gear alone gives a battle pet and taming (the Beastmaster line, docs/taming-gear-design.md):
    ///     PET_POWER, PET_HEALTH, PET_RECOVERY, PET_GATHERING, PET_GUARD and TAME_CHANCE, without the skill tree's
    ///     (Extras has both; the pet reads the tree from the build itself). Null before the first equipment update.
    /// </summary>
    [NonSerialized] public RavenNest.Combat.GearStats PetGear;

    /// <summary>A worn Legendary and its awakening rank (docs/legendary-design.md): the rank is the server's, on the item's tag.</summary>
    public struct WornLegendary
    {
        public RavenNest.Combat.LegendaryInfo Info;
        public int Rank;
        public int Kills;
    }

    /// <summary>The worn Legendaries (a weapon and a shield can both be), null or empty for none.</summary>
    [NonSerialized] public System.Collections.Generic.List<WornLegendary> Legendaries;

    /// <summary>
    ///     Substitution (docs/substitution-escape-idea.md): the best chance and the shortest cooldown of the worn gear's
    ///     attribute and Shadowstones, never added up (RavenNest.Combat.Substitution.Best); empty for most players.
    /// </summary>
    [NonSerialized] public RavenNest.Combat.SubstitutionPower Substitution;

    /// <summary>The stats of <see cref="PetGear"/>.</summary>
    public static readonly RavenNest.Combat.GearStat[] PetGearStats =
    {
        RavenNest.Combat.GearStat.PetPower, RavenNest.Combat.GearStat.PetHealth, RavenNest.Combat.GearStat.PetRecovery,
        RavenNest.Combat.GearStat.PetGathering, RavenNest.Combat.GearStat.PetGuard, RavenNest.Combat.GearStat.TameChance,
    };

    /// <summary>A stat of <see cref="PetGear"/>; 0 without one.</summary>
    public double Pet(RavenNest.Combat.GearStat stat) => PetGear != null ? PetGear[stat] : 0;

    /// <summary>A combat v2 gear stat summed over all worn gear and enchantments (the weapons' too).</summary>
    public double Gear(RavenNest.Combat.GearStat stat)
    {
        var total = Extras != null ? Extras[stat] : 0;
        if (WeaponExtras != null) foreach (var w in WeaponExtras) if (w != null) total += w[stat];
        return total;
    }

    public int WeaponBonus => WeaponAimBonus + WeaponPowerBonus;
    public int RangedBonus => RangedAimBonus + RangedPowerBonus;
    public int MagicBonus => MagicAimBonus + MagicPowerBonus;

    public int ArmorPower => BaseArmorPower + ArmorPowerBonus;
    public int WeaponAim => BaseWeaponAim + WeaponAimBonus;
    public int WeaponPower => BaseWeaponPower + WeaponPowerBonus;
    public int MagicAim => BaseMagicAim + MagicAimBonus;
    public int MagicPower => BaseMagicPower + MagicPowerBonus;
    public int RangedAim => BaseRangedAim + RangedAimBonus;
    public int RangedPower => BaseRangedPower + RangedPowerBonus;
    public int HealingPower => BaseHealingPower + HealingPowerBonus;

    public static readonly EquipmentStats Zero = new EquipmentStats();

    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        if (obj is EquipmentStats eq)
        {
            return (ArmorPower + WeaponAim + WeaponPower + MagicPower + MagicAim + RangedPower + RangedAim + HealingPower) -
                   (eq.ArmorPower + eq.WeaponAim + eq.WeaponPower + eq.MagicPower + eq.MagicAim + eq.RangedPower + eq.RangedAim + eq.HealingPower);
        }
        return 0;
    }

    public static EquipmentStats operator *(EquipmentStats stats, float num)
    {
        if (stats == null) return new EquipmentStats();
        return new EquipmentStats
        {
            BaseArmorPower = (int)(stats.BaseArmorPower * num),
            BaseWeaponAim = (int)(stats.BaseWeaponAim * num),
            BaseWeaponPower = (int)(stats.BaseWeaponPower * num),
            BaseMagicPower = (int)(stats.BaseMagicPower * num),
            BaseMagicAim = (int)(stats.BaseMagicAim * num),
            BaseRangedPower = (int)(stats.BaseRangedPower * num),
            BaseRangedAim = (int)(stats.BaseRangedAim * num),
            BaseHealingPower = (int)(stats.BaseHealingPower * num),
        };
    }
    public static EquipmentStats operator +(EquipmentStats stats, EquipmentStats other)
    {
        if (stats == null) return new EquipmentStats();
        return new EquipmentStats
        {
            BaseArmorPower = (int)(stats.BaseArmorPower + other.BaseArmorPower),
            BaseWeaponAim = (int)(stats.BaseWeaponAim + other.BaseWeaponAim),
            BaseWeaponPower = (int)(stats.BaseWeaponPower + other.BaseWeaponPower),
            BaseMagicPower = (int)(stats.BaseMagicPower + other.BaseMagicPower),
            BaseMagicAim = (int)(stats.BaseMagicAim + other.BaseMagicAim),
            BaseRangedPower = (int)(stats.BaseRangedPower + other.BaseRangedPower),
            BaseRangedAim = (int)(stats.BaseRangedAim + other.BaseRangedAim),
            BaseHealingPower = (int)(stats.BaseHealingPower + other.BaseHealingPower),
        };
    }

    public static EquipmentStats operator -(EquipmentStats stats, EquipmentStats other)
    {
        if (stats == null) return new EquipmentStats();
        return new EquipmentStats
        {
            BaseArmorPower = (int)(stats.BaseArmorPower - other.BaseArmorPower),
            BaseWeaponAim = (int)(stats.BaseWeaponAim - other.BaseWeaponAim),
            BaseWeaponPower = (int)(stats.BaseWeaponPower - other.BaseWeaponPower),
            BaseMagicPower = (int)(stats.BaseMagicPower - other.BaseMagicPower),
            BaseMagicAim = (int)(stats.BaseMagicAim - other.BaseMagicAim),
            BaseRangedPower = (int)(stats.BaseRangedPower - other.BaseRangedPower),
            BaseRangedAim = (int)(stats.BaseRangedAim - other.BaseRangedAim),
            BaseHealingPower = (int)(stats.BaseHealingPower - other.BaseHealingPower),
        };
    }

    public static EquipmentStats operator /(EquipmentStats stats, float num)
    {
        if (stats == null) return new EquipmentStats();
        return new EquipmentStats
        {
            BaseArmorPower = (int)(stats.BaseArmorPower / num),
            BaseWeaponAim = (int)(stats.BaseWeaponAim / num),
            BaseWeaponPower = (int)(stats.BaseWeaponPower / num),
            BaseMagicPower = (int)(stats.BaseMagicPower / num),
            BaseMagicAim = (int)(stats.BaseMagicAim / num),
            BaseRangedPower = (int)(stats.BaseRangedPower / num),
            BaseRangedAim = (int)(stats.BaseRangedAim / num),
            BaseHealingPower = (int)(stats.BaseHealingPower / num),
        };
    }
    public static EquipmentStats operator +(EquipmentStats stats, float num)
    {
        if (stats == null) return new EquipmentStats();
        return new EquipmentStats
        {
            BaseArmorPower = (int)(stats.BaseArmorPower + num),
            BaseWeaponAim = (int)(stats.BaseWeaponAim + num),
            BaseWeaponPower = (int)(stats.BaseWeaponPower + num),
            BaseMagicPower = (int)(stats.BaseMagicPower + num),
            BaseMagicAim = (int)(stats.BaseMagicAim + num),
            BaseRangedPower = (int)(stats.BaseRangedPower + num),
            BaseRangedAim = (int)(stats.BaseRangedAim + num),
            BaseHealingPower = (int)(stats.BaseHealingPower + num),
        };
    }
}