using System;
using UnityEngine;

public static class GameMath
{
    public const int MaxLevel = 170;

    private static decimal[] ExperienceArray = new decimal[MaxLevel];

    static GameMath()
    {
        var totalExp = 0L;
        for (var levelIndex = 0; levelIndex < MaxLevel; levelIndex++)
        {
            var level = levelIndex + 1M;
            var levelExp = (long)(level + (decimal)(300D * Math.Pow(2D, (double)(level / 7M))));
            totalExp += levelExp;
            ExperienceArray[levelIndex] = (decimal)((totalExp & 0xffffffffc) / 4d);
        }
    }

    public static float CalculateMagicDamage(IAttackable attacker, IAttackable defender)
    {
        var lvl = attacker.GetStats().Magic.Level;
        return CalculateCastDamage(attacker, defender, lvl, MagicPower(lvl / 10));
    }

    public static float CalculateRangedDamage(IAttackable attacker, IAttackable defender)
    {
        var rangeLvl = attacker.GetStats().Ranged.Level;
        return CalculateCastDamage(attacker, defender, rangeLvl, ArrowPower(rangeLvl / 10));
    }

    public static float CalculateDamage(IAttackable attacker, IAttackable defender)
    {
        var attackerStats = attacker.GetStats();
        var defenderStats = defender.GetStats();
        var attackerEq = attacker.GetEquipmentStats();
        var defenderEq = defender.GetEquipmentStats();

        var burst = false;
        var superhuman = false;
        var ultimate = false;
        var bonus = StyleBonus(attacker, 2);
        var max = MaxHit(attackerStats.Strength.CurrentValue, attackerEq.WeaponPower, burst, superhuman, ultimate, bonus);

        var attackPrayers = AddPrayers(burst, superhuman, ultimate);

        var newAtt = (int)(attackPrayers * (attackerStats.Attack.CurrentValue / 0.8D)
                     + (UnityEngine.Random.Range(0, 4) == 0
                         ? attackerEq.WeaponPower
                         : attackerEq.WeaponAim / 2.5d)
                     + (attacker.GetCombatStyle() == 1 && UnityEngine.Random.Range(0, 2) == 0 ? 4 : 0)
                     + (UnityEngine.Random.Range(0, 100) <= 10 ? (attackerStats.Strength.CurrentValue / 5D) : 0)
                     + (StyleBonus(attacker, 0) * 2));

        var defensePrayers = AddPrayers(burst, superhuman, ultimate);

        var newDef = (int)(defensePrayers
                     * ((UnityEngine.Random.Range(0, 100) <= 5 ? 0 : defenderStats.Defense.CurrentValue) * 1.1D)
                     + ((UnityEngine.Random.Range(0, 100) <= 5 ? 0 : defenderEq.ArmorPower) / 2.75D)
                     + (defenderStats.Strength.CurrentValue / 4D) + (StyleBonus(defender, 1) * 2));

        var hitChance = UnityEngine.Random.Range(0, 100) + (newAtt - newDef);

        if (attacker is EnemyController)
        {
            hitChance -= 5;
        }

        if (UnityEngine.Random.Range(0, 100) <= 10)
        {
            hitChance += 20;
        }

        if (hitChance > (defender is EnemyController ? 40 : 50))
        {
            var newMax = 0;
            var maxProb = 5;
            var nearMaxProp = 7;
            var avProb = 73;
            var lowHit = 10;

            var shiftValue = (int)Math.Round(defenderEq.ArmorPower * 0.02d, MidpointRounding.AwayFromZero);

            maxProb -= shiftValue;
            nearMaxProp -= (int)Math.Round(shiftValue * 1.5, MidpointRounding.AwayFromZero);
            avProb -= (int)Math.Round(shiftValue * 2.0, MidpointRounding.AwayFromZero);
            lowHit += (int)Math.Round(shiftValue * 3.5, MidpointRounding.AwayFromZero);

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


            //return (int)Math.Round(Math.Abs(newMax - newMax * (UnityEngine.Random.Range(0, 95) * 0.01D)), MidpointRounding.AwayFromZero);
        }

        return 0;
    }

    public static float CalculateHouseExpBonus(SkillStat skill)
    {
        // up to 50% exp bonus
        return (skill.CurrentValue / (float)MaxLevel) * 50f;
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

#warning Use combat level instead of health for combat based town house slots
            case TownHouseSlotType.Melee: return stats.Health;

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
            case CombatSkill.Ranged: return TownHouseSlotType.Ranged;
            case CombatSkill.Magic: return TownHouseSlotType.Magic;
            default: return TownHouseSlotType.Melee;
        }
    }
    internal static float CalculateExplosionDamage(IAttackable enemy, IAttackable player, float scale = 0.75f)
    {
        return CalculateDamage(enemy, player) * scale;
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

        int max = (int)(((double)rangeLvl * 0.15D) + 0.85D + power);
        int peak = (int)(((double)max / 100D) * (double)armourRatio);
        int dip = (int)(((double)peak / 3D) * 2D);
        return RandomWeighted(0, dip, peak, max);
    }

    private static double ArrowPower(int arrowId)
    {
        return arrowId * 0.5f;
    }

    private static double MagicPower(int arrowId)
    {
        return arrowId * 0.5f;
    }

    public static int RandomWeighted(int low, int dip, int peak, int max)
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

    public static int MaxHit(
        int strength, int weaponPower,
        bool burst, bool superhuman, bool ultimate, int bonus)
    {
        var prayer = AddPrayers(burst, superhuman, ultimate);
        var newStrength = strength * prayer + bonus;

        var w1 = weaponPower * 0.00175D;
        var w2 = w1 + 0.1d;
        var w3 = newStrength * w2 + 1.05D;
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

    public static int ExperienceToLevel(decimal exp)
    {
        for (int level = 0; level < MaxLevel - 1; level++)
        {
            if (exp >= ExperienceArray[level])
                continue;
            return (level + 1);
        }
        return MaxLevel;
    }

    public static decimal LevelToExperience(int level)
    {
        return level - 2 < 0 ? 0 : ExperienceArray[level - 2];
    }

    public static decimal GetFishingExperience(int level)
    {
        if (level < 15) return 25;
        if (level < 30) return 37.5m;
        if (level < 45) return 100;
        if (level < 60) return 175;
        if (level < 75) return 250;

        return 10;
    }

    public static decimal GetFarmingExperience(int level)
    {
        if (level < 15) return 25;
        if (level < 30) return 37.5m;
        if (level < 45) return 100;
        if (level < 60) return 175;
        if (level < 75) return 250;

        return 10;
    }
    public static decimal GetWoodcuttingExperience(int level)
    {
        if (level >= 90) return 300;
        if (level < 75) return 250;
        if (level < 60) return 175;
        if (level < 45) return 100;
        if (level < 30) return 37.5m;
        if (level < 15) return 25;
        return 25;
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
