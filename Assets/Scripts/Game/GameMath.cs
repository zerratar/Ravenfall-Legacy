using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class GameMath
{
    public const int MaxLevel = 999;
    public const int MaxVillageLevel = 300;

    public readonly static double[] ExperienceArray = new double[MaxLevel];

    [Obsolete]
    public readonly static double[] OldExperienceArray = new double[MaxLevel];

    public const float MaxExpBonusPerSlot = 200f;

    static GameMath()
    {
        var expForLevel = 100d;
        for (var levelIndex = 0; levelIndex < MaxLevel; levelIndex++)
        {
            var level = levelIndex + 1;

            // new
            var tenth = Math.Truncate(level / 10d) + 1;
            var incrementor = tenth * 100d + Math.Pow(tenth, 3d);
            expForLevel += Math.Truncate(incrementor);
            ExperienceArray[levelIndex] = expForLevel;
        }

        // Old formula
        for (var levelIndex = 0; levelIndex < MaxLevel; levelIndex++)
        {
            var level = levelIndex + 1M;
            expForLevel = Math.Floor(300D * Math.Pow(2D, (double)(level / 7M)));
            OldExperienceArray[levelIndex] = Math.Round(expForLevel / 4d, 0, MidpointRounding.ToEven);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Lerp(double v1, double v2, double t)
    {
        return v1 + (v2 - v1) * t;
    }

    public static float CalculateHealing(IAttackable attacker, IAttackable defender)
    {
        try
        {
            if (attacker == null || defender == null) return 0;
            var attackerStats = attacker.GetStats();
            var attackerEq = attacker.GetEquipmentStats();
            if (attackerStats == null || attackerEq == null) return 0;

            var defenderDamageSkill = 1;
            var attackerDamageSkill = attackerStats.Healing.MaxLevel;
            var attackerAimSkill = attackerStats.Healing.MaxLevel;
            var attackerPower = attackerEq.MagicPower;
            var attackerAim = attackerEq.MagicAim;

            return CalculateDamage(attacker, defender, Skills.Zero, EquipmentStats.Zero, defenderDamageSkill, attackerDamageSkill, attackerAimSkill, attackerPower, attackerAim);
        }
        catch
        {
            return 0;
        }
    }

    public static float CalculateMagicDamage(IAttackable attacker, IAttackable defender)
    {
        try
        {
            if (attacker == null || defender == null) return 0;
            var attackerStats = attacker.GetStats();
            var defenderStats = defender.GetStats();
            if (attackerStats == null || defenderStats == null) return 0;
            var attackerEq = attacker.GetEquipmentStats();
            var defenderEq = defender.GetEquipmentStats();
            if (attackerEq == null || defenderEq == null) return 0;

            var defenderDamageSkill = defenderStats.Magic.MaxLevel;
            var attackerDamageSkill = attackerStats.Magic.MaxLevel;
            var attackerAimSkill = attackerStats.Magic.MaxLevel;
            var attackerPower = attackerEq.MagicPower;
            var attackerAim = attackerEq.MagicAim;

            return CalculateDamage(attacker, defender, defenderStats, defenderEq, defenderDamageSkill, attackerDamageSkill, attackerAimSkill, attackerPower, attackerAim);
        }
        catch
        {
            return 0;
        }
    }

    public static float CalculateRangedDamage(IAttackable attacker, IAttackable defender)
    {
        try
        {
            if (attacker == null || defender == null) return 0;
            var attackerStats = attacker.GetStats();
            var defenderStats = defender.GetStats();
            if (attackerStats == null || defenderStats == null) return 0;
            var attackerEq = attacker.GetEquipmentStats();
            var defenderEq = defender.GetEquipmentStats();
            if (attackerEq == null || defenderEq == null) return 0;

            var defenderDamageSkill = defenderStats.Ranged.MaxLevel;
            var attackerDamageSkill = attackerStats.Ranged.MaxLevel;
            var attackerAimSkill = attackerStats.Ranged.MaxLevel;
            var attackerPower = attackerEq.RangedPower;
            var attackerAim = attackerEq.RangedAim;

            return CalculateDamage(attacker, defender, defenderStats, defenderEq, defenderDamageSkill, attackerDamageSkill, attackerAimSkill, attackerPower, attackerAim);
        }
        catch
        {
            return 0;
        }
    }

    public static float CalculateMeleeDamage(IAttackable attacker, IAttackable defender)
    {
        try
        {
            if (attacker == null || defender == null) return 0;
            var attackerStats = attacker.GetStats();
            var defenderStats = defender.GetStats();
            if (attackerStats == null || defenderStats == null) return 0;
            var attackerEq = attacker.GetEquipmentStats();
            var defenderEq = defender.GetEquipmentStats();
            if (attackerEq == null || defenderEq == null) return 0;

            var defenderDamageSkill = defenderStats.Strength.MaxLevel;
            var attackerDamageSkill = attackerStats.Strength.MaxLevel;
            var attackerAimSkill = attackerStats.Attack.MaxLevel;
            var attackerPower = attackerEq.WeaponPower;
            var attackerAim = attackerEq.WeaponAim;

            return CalculateDamage(attacker, defender, defenderStats, defenderEq, defenderDamageSkill, attackerDamageSkill, attackerAimSkill, attackerPower, attackerAim);
        }
        catch
        {
            return 0;
        }
    }

    private static float CalculateDamage(
        IAttackable attacker,
        IAttackable defender,
        Skills defenderStats,
        EquipmentStats defenderEq,
        int defenderDamageSkill,
        int attackerDamageSkill,
        int attackerAimSkill,
        int attackerPower,
        int attackerAim,
        int minHitChance = 40)
    {
        var max = MaxHit(attackerDamageSkill, attackerPower);
        var newAtt = (int)((attackerAimSkill / 0.8D) + attackerAim + (attackerDamageSkill / 5D) + 10);
        var newDef = (int)(((UnityEngine.Random.Range(0, 100) <= 5 ? 0 : defenderStats.Defense.MaxLevel) * 1.1D)
                     + ((UnityEngine.Random.Range(0, 100) <= 5 ? 0 : defenderEq.ArmorPower) / 2.75D)
                     + (defenderDamageSkill / 4D) + (StyleBonus(defender, 1) * 2));

        var hitChance = UnityEngine.Random.Range(0, 100) + (newAtt - newDef);
        if (attacker is EnemyController)
        {
            hitChance -= 5;
        }

        if (UnityEngine.Random.Range(0, 100) <= 10)
        {
            hitChance += 20;
        }

        var reqHitChance = Mathf.Min(minHitChance, (defender is EnemyController ? 40 : 50));
        if (hitChance > reqHitChance)
        {
            var newMax = 0;
            var maxProb = 5;
            var nearMaxProp = 7;
            var avProb = 73;
            var defenseRange = (int)Math.Round(defenderEq.ArmorPower * 0.02d, MidpointRounding.AwayFromZero);

            maxProb -= defenseRange;
            nearMaxProp -= (int)Math.Round(defenseRange * 1.5, MidpointRounding.AwayFromZero);
            avProb -= (int)Math.Round(defenseRange * 2.0, MidpointRounding.AwayFromZero);

            var hitRange = UnityEngine.Random.Range(0, 100);
            if (hitRange >= (100 - maxProb))
            {
                return max;
            }

            if (hitRange >= (100 - nearMaxProp))
            {
                return (int)Math.Round(Math.Abs(max - max * (UnityEngine.Random.Range(0, 10) * 0.01D)), MidpointRounding.AwayFromZero);
            }

            if (hitRange >= (100 - avProb))
            {
                newMax = (int)Math.Round(max - (max * 0.1D));
                return (int)Math.Round(Math.Abs(newMax - newMax * (UnityEngine.Random.Range(0, 50) * 0.01D)), MidpointRounding.AwayFromZero);
            }


            newMax = (int)Math.Round(max - max * 0.5D);
            return (int)Math.Round(Math.Abs((newMax - (newMax * (UnityEngine.Random.Range(0, 50) * 0.01D)))), MidpointRounding.AwayFromZero);
        }

        return 0;
    }

    public static float CalculateHouseExpBonus(this SkillStat skill)
    {
        // up to 50% exp bonus
        return (skill.Level / (float)MaxLevel) * MaxExpBonusPerSlot;
    }
    public static SkillStat GetSkillByHouseType(this Skills stats, TownHouseSlotType type)
    {
        if (type == TownHouseSlotType.Melee)
        {
            if (stats.Attack.Level > stats.Defense.Level && stats.Attack.Level > stats.Strength.Level)
            {
                return stats.Attack;
            }
            else if (stats.Defense.Level > stats.Attack.Level && stats.Defense.Level > stats.Strength.Level)
            {
                return stats.Defense;
            }
            else if (stats.Strength.Level >= stats.Attack.Level && stats.Strength.Level >= stats.Defense.Level)
            {
                return stats.Strength;
            }
        }
        switch (type)
        {
            case TownHouseSlotType.Woodcutting: return stats.Woodcutting;
            case TownHouseSlotType.Mining: return stats.Mining;
            case TownHouseSlotType.Farming: return stats.Farming;
            case TownHouseSlotType.Crafting: return stats.Crafting;
            case TownHouseSlotType.Cooking: return stats.Cooking;
            case TownHouseSlotType.Slayer: return stats.Slayer;
            case TownHouseSlotType.Sailing: return stats.Sailing;
            case TownHouseSlotType.Fishing: return stats.Fishing;
            case TownHouseSlotType.Melee: return stats.Health;
            case TownHouseSlotType.Healing: return stats.Healing;
            case TownHouseSlotType.Magic: return stats.Magic;
            case TownHouseSlotType.Ranged: return stats.Ranged;
            default: return stats.Mining;
        }
    }

    public static TownHouseSlotType GetHouseTypeBySkill(this Skill skill)
    {
        switch (skill)
        {
            case Skill.Cooking: return TownHouseSlotType.Cooking;
            case Skill.Crafting: return TownHouseSlotType.Crafting;
            case Skill.Farming: return TownHouseSlotType.Farming;
            case Skill.Mining: return TownHouseSlotType.Mining;
            case Skill.Sailing: return TownHouseSlotType.Sailing;
            case Skill.Slayer: return TownHouseSlotType.Slayer;
            case Skill.Woodcutting: return TownHouseSlotType.Woodcutting;
            case Skill.Fishing: return TownHouseSlotType.Fishing;
            case Skill.Healing: return TownHouseSlotType.Healing;
            case Skill.Ranged: return TownHouseSlotType.Ranged;
            case Skill.Magic: return TownHouseSlotType.Magic;
            default: return TownHouseSlotType.Melee;
        }
    }

    public static bool IsCombatSkill(this Skill skill)
    {
        switch (skill)
        {
            //case Skill.All:
            case Skill.Attack:
            case Skill.Defense:
            case Skill.Strength:
            case Skill.Health:
            case Skill.Ranged:
            case Skill.Magic:
            case Skill.Healing:
                return true;
        }
        return false;
    }

    internal static float CalculateExplosionDamage(IAttackable enemy, IAttackable player, float scale = 0.75f)
    {
        return CalculateMeleeDamage(enemy, player) * scale;
    }

    private static float CalculateCastDamage(IAttackable attacker, IAttackable defender, int level, double power)
    {
        var rangeLvl = level;
        var armor = defender.GetEquipmentStats().ArmorPower;
        var rangeEquip = 15f;
        int armorRatio = (int)(60D + ((double)((rangeEquip * 3D) - armor) / 300D) * 40D);

        if (UnityEngine.Random.value * 100f > armorRatio
                && UnityEngine.Random.value <= 0.5)
        {
            return 0;
        }

        int max = (int)((rangeLvl * 0.15D) + 0.85D + power);
        int peak = (int)(max / 100D * armorRatio);
        int dip = (int)(peak / 3D * 2D);
        return RandomWeighted(dip, peak, max);
    }

    public static int RandomWeighted(int dip, int peak, int max)
    {
        int total = 0;
        int probability = 100;
        int[] probArray = new int[max + 1];
        for (int x = 0; x < probArray.Length; x++)
        {
            total += probArray[x] = probability;
            if (x < dip || x > peak)
            {
                probability -= 3;
            }
            else
            {
                probability += 3;
            }
        }
        int hit = UnityEngine.Random.Range(0, total);
        total = 0;
        for (int x = 0; x < probArray.Length; x++)
        {
            if (hit >= total && hit < (total + probArray[x]))
            {
                return x;
            }
            total += probArray[x];
        }
        return 0;
    }
    private static int StyleBonus(IAttackable attacker, int skill)
    {
        var style = attacker.GetCombatStyle();
        if (style == 0) return 1;
        return (skill == 0 && style == 2) || (skill == 1 && style == 3) || (skill == 2 && style == 1) ? 3 : 0;
    }

    public static float CalculateSkillDamage(SkillStat skillStat, int targetLevel)
    {
        var levelDiff = skillStat.MaxLevel - targetLevel;
        var hit = Mathf.Max(1f, Math.Abs(levelDiff));
        if (levelDiff >= 5 || UnityEngine.Random.value <= 0.5)
        {
            return hit;
        }

        return 0;
    }

    public static int MaxHit(int level, int power)
    {
        var w1 = power * 0.00175D;
        var w2 = w1 + 0.1d;
        var w3 = (level + 3) * w2 + 1.05D;
        return (int)(w3 * 0.95d);
    }

    public static double ExperienceForLevel(int level)
    {
        if (level - 2 >= ExperienceArray.Length)
        {
            return ExperienceArray[ExperienceArray.Length - 1];
        }

        return (level - 2 < 0 ? 0 : ExperienceArray[level - 2]);
    }


    [Obsolete]
    public static double OldExperienceForLevel(int level)
    {
        if (level - 2 >= OldExperienceArray.Length)
        {
            return OldExperienceArray[OldExperienceArray.Length - 1];
        }

        return (level - 2 < 0 ? 0 : OldExperienceArray[level - 2]);
    }

    public static class Exp
    {
        /// <summary>
        /// The level where time between level has peaked at <see cref="IncrementMins"/>.
        /// </summary>
        public const double EasyLevel = 70.0;

        public const double IncrementMins = 14.0;
        public const double IncrementHours = IncrementMins / 60.0;
        public const double IncrementDays = IncrementHours / 24.0;
        public const double MaxLevelDays = IncrementDays * MaxLevel;
        public const double MultiEffectiveness = 1.375d;

        public const double MaxExpFactorFromIsland = 1d;

        /// <summary>
        /// Calculates the amount of exp that should be yielded given the current skill and level.
        /// </summary>
        /// <param name="nextLevel"></param>
        /// <param name="skill"></param>
        /// <param name="factor"></param>
        /// <param name="boost"></param>
        /// <param name="multiplierFactor"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CalculateExperience(int nextLevel, Skill skill, double factor = 1, double boost = 1, double multiplierFactor = 1)
        {
            var bTicksForLevel = GetTotalTicksForLevel(nextLevel, skill, boost);
            var expForNextLevel = ExperienceForLevel(nextLevel);
            var maxExpGain = expForNextLevel / bTicksForLevel;
            var minExpGainPercent = GetMinExpGainPercent(nextLevel, skill);
            var minExpGain = ExperienceForLevel(nextLevel) * minExpGainPercent;
            return Lerp(0, Lerp(minExpGain, maxExpGain, multiplierFactor), factor);
        }

        /// <summary>
        /// Gets the total amount of "Ticks" to level up to the given target level after applying the exp boost.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="skill"></param>
        /// <param name="multiplier"></param>
        /// <param name="playersInArea"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetTotalTicksForLevel(int level, Skill skill, double multiplier = 1, int playersInArea = 100)
        {
            return GetTotalTicksForLevel(level, skill, playersInArea) / GetEffectiveExpMultiplier(level, multiplier);
        }

        /// <summary>
        /// Gets the total amount of "Ticks" to level up to the given target level. Without applying any exp boost.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="skill"></param>
        /// <param name="playersInArea"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetTotalTicksForLevel(int level, Skill skill, int playersInArea = 100)
        {
            return GetMaxMinutesForLevel(level) * GetTicksPerMinute(skill, playersInArea);
        }

        /// <summary>
        /// Gets the effective exp multiplier given the current multiplier and player level; 
        /// This is multiplied by the exp given by one "Tick"
        /// </summary>
        /// <param name="multiplier">Expected to be in full form (100 and not 1.0)</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetEffectiveExpMultiplier(int level, double multiplier = 1)
        {
            return Math.Max(Math.Min((((MaxLevel * MultiEffectiveness) - (level - 1)) / (MaxLevel * MultiEffectiveness)) * multiplier, multiplier), 1.0);
        }

        /// <summary>
        /// Gets the minimum exp gain in percent towards the next skill level. Is to boost up exp gains for higher levels.
        /// </summary>
        /// <param name="nextLevel"></param>
        /// <param name="skill"></param>
        /// <param name="playersInArea"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetMinExpGainPercent(int nextLevel, Skill skill, int playersInArea = 100)
        {
            return 1d / (GetTicksPerMinute(skill, playersInArea) * GetMaxMinutesForLevel(nextLevel));
        }

        /// <summary>
        /// Gets the maximum possible time needed to level up from level-1 to target level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetMaxMinutesForLevel(int level)
        {
            if (level <= EasyLevel)
            {
                return (level - 1) * GameMath.Lerp(IncrementMins / 8.0d, IncrementMins, level / EasyLevel);
            }

            return (level - 1) * IncrementMins;
        }

        /// <summary>
        /// Gets the expected exp ticks per minutes the target skill and players training the same thing in the area.
        /// These values are taken from real world cases and used as an estimate.
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="playersInArea"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetTicksPerMinute(Skill skill, int playersInArea = 100)
        {
            return GetTicksPerSeconds(skill, playersInArea) * 60;
        }

        /// <summary>
        /// Get the expected exp ticks per seconds given the target skill and players training the same thing in the area.
        /// These values are taken from real world cases and used as an estimate.
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="playersInArea"></param>
        /// <returns></returns>
        public static double GetTicksPerSeconds(Skill skill, int playersInArea = 100)
        {
            switch (skill)
            {
                case Skill.Woodcutting when playersInArea < 100: return 0.15;
                case Skill.Woodcutting when playersInArea >= 100: return 0.33;
                case Skill.Farming:
                case Skill.Crafting:
                case Skill.Cooking:
                case Skill.Fishing:
                    return 1d / 3d;

                case Skill.Mining:
                    return 0.5;

                case (Skill.Health or Skill.Attack or Skill.Defense or Skill.Strength or Skill.Magic or Skill.Ranged) when playersInArea < 10:
                    return 0.25;

                case (Skill.Health or Skill.Attack or Skill.Defense or Skill.Strength or Skill.Magic or Skill.Ranged) when playersInArea < 100:
                    return 0.75;

                case (Skill.Health or Skill.Attack or Skill.Defense or Skill.Strength or Skill.Magic or Skill.Ranged) when playersInArea >= 100:
                    return 1.25;

                case Skill.Healing: return 0.5d;
                case Skill.Sailing: return 0.4d;
                default: return 0.5;
            }
        }
    }
}
