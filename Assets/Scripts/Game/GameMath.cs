using System;
using UnityEngine;

public static class GameMath
{
    public const int MaxLevel = 999;
    public readonly static double[] ExperienceArray = new double[MaxLevel];

    #region old stuff
    public const int OldMaxLevel = 170;
    public const float MaxExpBonusPerSlot = 50f;
    private static decimal[] OldExperienceArray = new decimal[OldMaxLevel];
    public const decimal ExpScale = 1m;
    #endregion

    static GameMath()
    {
        #region old stuff
        var totalExp = 0L;
        for (var levelIndex = 0; levelIndex < OldMaxLevel; levelIndex++)
        {
            var level = levelIndex + 1M;
            var levelExp = (long)(level + (decimal)(300D * Math.Pow(2D, (double)(level / 7M))));
            totalExp += levelExp;
            OldExperienceArray[levelIndex] = (decimal)((totalExp & 0xffffffffc) / 4d);
        }
        #endregion

        for (var levelIndex = 0; levelIndex < MaxLevel; levelIndex++)
        {
            var level = levelIndex + 1M;
            var expForLevel = Math.Floor(300D * Math.Pow(2D, (double)(level / 7M)));
            ExperienceArray[levelIndex] = Math.Round(expForLevel / 4d, 0, MidpointRounding.ToEven);
        }
    }

    public static float CalculateHealing(IAttackable attacker, IAttackable defender)
    {
        var attackerStats = attacker.GetStats();
        var attackerEq = attacker.GetEquipmentStats();

        var defenderDamageSkill = 1;
        var attackerDamageSkill = attackerStats.Healing.CurrentValue;
        var attackerAimSkill = attackerStats.Healing.CurrentValue;
        var attackerPower = attackerEq.MagicPower;
        var attackerAim = attackerEq.MagicAim;

        return CalculateDamage(attacker, defender, Skills.Zero, EquipmentStats.Zero, defenderDamageSkill, attackerDamageSkill, attackerAimSkill, attackerPower, attackerAim);
    }

    public static float CalculateMagicDamage(IAttackable attacker, IAttackable defender)
    {
        var attackerStats = attacker.GetStats();
        var defenderStats = defender.GetStats();
        var attackerEq = attacker.GetEquipmentStats();
        var defenderEq = defender.GetEquipmentStats();

        var defenderDamageSkill = defenderStats.Magic.CurrentValue;
        var attackerDamageSkill = attackerStats.Magic.CurrentValue;
        var attackerAimSkill = attackerStats.Magic.CurrentValue;
        var attackerPower = attackerEq.MagicPower;
        var attackerAim = attackerEq.MagicAim;

        return CalculateDamage(attacker, defender, defenderStats, defenderEq, defenderDamageSkill, attackerDamageSkill, attackerAimSkill, attackerPower, attackerAim);
    }

    public static float CalculateRangedDamage(IAttackable attacker, IAttackable defender)
    {
        var attackerStats = attacker.GetStats();
        var defenderStats = defender.GetStats();
        var attackerEq = attacker.GetEquipmentStats();
        var defenderEq = defender.GetEquipmentStats();

        var defenderDamageSkill = defenderStats.Ranged.CurrentValue;
        var attackerDamageSkill = attackerStats.Ranged.CurrentValue;
        var attackerAimSkill = attackerStats.Ranged.CurrentValue;
        var attackerPower = attackerEq.RangedPower;
        var attackerAim = attackerEq.RangedAim;

        return CalculateDamage(attacker, defender, defenderStats, defenderEq, defenderDamageSkill, attackerDamageSkill, attackerAimSkill, attackerPower, attackerAim);
    }

    public static float CalculateMeleeDamage(IAttackable attacker, IAttackable defender)
    {
        var attackerStats = attacker.GetStats();
        var defenderStats = defender.GetStats();
        var attackerEq = attacker.GetEquipmentStats();
        var defenderEq = defender.GetEquipmentStats();

        var defenderDamageSkill = defenderStats.Strength.CurrentValue;
        var attackerDamageSkill = attackerStats.Strength.CurrentValue;
        var attackerAimSkill = attackerStats.Attack.CurrentValue;
        var attackerPower = attackerEq.WeaponPower;
        var attackerAim = attackerEq.WeaponAim;

        return CalculateDamage(attacker, defender, defenderStats, defenderEq, defenderDamageSkill, attackerDamageSkill, attackerAimSkill, attackerPower, attackerAim);
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
        var newDef = (int)(((UnityEngine.Random.Range(0, 100) <= 5 ? 0 : defenderStats.Defense.CurrentValue) * 1.1D)
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

    public static float CalculateHouseExpBonus(SkillStat skill)
    {
        // up to 50% exp bonus
        return (skill.Level / (float)OldMaxLevel) * MaxExpBonusPerSlot;
    }

    public static SkillStat GetSkillByHouseType(Skills stats, TownHouseSlotType type)
    {
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

    public static TownHouseSlotType GetHouseTypeBySkill(Skill skill)
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
            default: return TownHouseSlotType.Empty;
        }
    }

    public static TownHouseSlotType GetHouseTypeBySkill(CombatSkill skill)
    {
        switch (skill)
        {
            case CombatSkill.Healing: return TownHouseSlotType.Healing;
            case CombatSkill.Ranged: return TownHouseSlotType.Ranged;
            case CombatSkill.Magic: return TownHouseSlotType.Magic;
            default: return TownHouseSlotType.Melee;
        }
    }
    internal static float CalculateExplosionDamage(IAttackable enemy, IAttackable player, float scale = 0.75f)
    {
        return CalculateMeleeDamage(enemy, player) * scale;
    }

    private static float CalculateCastDamage(IAttackable attacker, IAttackable defender, int level, double power)
    {
        var rangeLvl = level;
        var armour = defender.GetEquipmentStats().ArmorPower;
        var rangeEquip = 15f;
        int armourRatio = (int)(60D + ((double)((rangeEquip * 3D) - armour) / 300D) * 40D);

        if (UnityEngine.Random.value * 100f > armourRatio
                && UnityEngine.Random.value <= 0.5)
        {
            return 0;
        }

        int max = (int)((rangeLvl * 0.15D) + 0.85D + power);
        int peak = (int)(max / 100D * armourRatio);
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
        var levelDiff = skillStat.CurrentValue - targetLevel;
        var hit = Mathf.Max(1f, Math.Abs(levelDiff));
        if (levelDiff >= 5 || UnityEngine.Random.value <= 0.5)
        {
            return hit;
        }

        return 0;
    }

    public static int MaxHit(int strength, int weaponPower)
    {
        var w1 = weaponPower * 0.00175D;
        var w2 = w1 + 0.1d;
        var w3 = (strength + 3) * w2 + 1.05D;
        return (int)(w3 * 0.95d);
    }

    public static double AddPrayers(bool first, bool second, bool third)
    {
        if (third) return 1.15d;
        if (second) return 1.1d;
        if (first) return 1.05d;
        return 1.0d;
    }

    public static int CombatExperience(Skills mob)
    {
        return (int)((mob.CombatLevel * 10 + 10) * 1.5D);
    }

    public static int CombatExperience(int level)
    {
        return (int)((level * 10 + 10) * 1.5D);
    }

    public static decimal ExperienceForLevel(int level)
    {
        return (decimal)(level - 2 < 0 ? 0 : ExperienceArray[level - 2]);
    }

    [Obsolete]
    public static int OLD_ExperienceToLevel(decimal exp)
    {
        for (int level = 0; level < OldMaxLevel - 1; level++)
        {
            if (exp >= OldExperienceArray[level])
                continue;
            return (level + 1);
        }
        return OldMaxLevel;
    }

    [Obsolete]
    public static decimal OLD_LevelToExperience(int level)
    {
        return level - 2 < 0 ? 0 : OldExperienceArray[level - 2];
    }

    public static decimal GetFishingExperience(int level)
    {
        return (level * 0.66m) + (level * (level / 40m)) + (level * level * 0.005m) + level * 0.5m;
    }

    public static decimal GetFarmingExperience(int level)
    {
        /*
            Following formula will generate:
            Level   Exp
            15		24,15
            30		61,8
            45		112,95
            60		177,6
            75		255,75
            90		347,4
            110		490,6
         */
        return (level * 0.66m) + (level * (level / 40m)) + (level * level * 0.005m) + level * 0.5m;
    }
    public static decimal GetWoodcuttingExperience(int level)
    {
        return (level * 0.66m) + (level * (level / 40m)) + (level * level * 0.005m) + level * 0.5m;
    }

    public static decimal GetMiningExperienceFromType(RockType type)
    {
        switch (type)
        {
            case RockType.Rune: return 100;
            default: return 5m;
        }
    }
}
