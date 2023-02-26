using Shinobytes.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public class Skills : IComparable
{
    private readonly ConcurrentDictionary<string, SkillStat> skills
        = new ConcurrentDictionary<string, SkillStat>();

    private SkillStat[] skillList;

    public SkillStat Attack;
    public SkillStat Defense;
    public SkillStat Strength;
    public SkillStat Health;
    public SkillStat Magic;
    public SkillStat Ranged;
    public SkillStat Healing;
    public SkillStat Farming;
    public SkillStat Cooking;
    public SkillStat Crafting;
    public SkillStat Mining;
    public SkillStat Fishing;
    public SkillStat Woodcutting;
    public SkillStat Slayer;
    public SkillStat Sailing;
    public static Skills Zero => new Skills();

    public Skills()
    {
        Attack = new SkillStat(Skill.Attack, nameof(Attack), 1, 0);
        Defense = new SkillStat(Skill.Defense, nameof(Defense), 1, 0);
        Strength = new SkillStat(Skill.Strength, nameof(Strength), 1, 0);
        Health = new SkillStat(Skill.Health, nameof(Health), 10, 1000);
        Woodcutting = new SkillStat(Skill.Woodcutting, nameof(Woodcutting), 1, 0);
        Fishing = new SkillStat(Skill.Fishing, nameof(Fishing), 1, 0);
        Mining = new SkillStat(Skill.Mining, nameof(Mining), 1, 0);
        Crafting = new SkillStat(Skill.Crafting, nameof(Crafting), 1, 0);
        Cooking = new SkillStat(Skill.Cooking, nameof(Cooking), 1, 0);
        Farming = new SkillStat(Skill.Farming, nameof(Farming), 1, 0);
        Magic = new SkillStat(Skill.Magic, nameof(Magic), 1, 0);
        Ranged = new SkillStat(Skill.Ranged, nameof(Ranged), 1, 0);
        Slayer = new SkillStat(Skill.Slayer, nameof(Slayer), 1, 0);
        Sailing = new SkillStat(Skill.Sailing, nameof(Sailing), 1, 0);
        Healing = new SkillStat(Skill.Healing, nameof(Healing), 1, 0);

        this.skills = new ConcurrentDictionary<string, SkillStat>();
        SetupSkillLookup();
    }

    public Skills(RavenNest.Models.Skills skills)
    {
        Attack = new SkillStat(Skill.Attack, nameof(Attack), skills.AttackLevel, skills.Attack);
        Defense = new SkillStat(Skill.Defense, nameof(Defense), skills.DefenseLevel, skills.Defense);
        Strength = new SkillStat(Skill.Strength, nameof(Strength), skills.StrengthLevel, skills.Strength);
        Health = new SkillStat(Skill.Health, nameof(Health), skills.HealthLevel, skills.Health);
        Woodcutting = new SkillStat(Skill.Woodcutting, nameof(Woodcutting), skills.WoodcuttingLevel, skills.Woodcutting);
        Fishing = new SkillStat(Skill.Fishing, nameof(Fishing), skills.FishingLevel, skills.Fishing);
        Mining = new SkillStat(Skill.Mining, nameof(Mining), skills.MiningLevel, skills.Mining);
        Crafting = new SkillStat(Skill.Crafting, nameof(Crafting), skills.CraftingLevel, skills.Crafting);
        Cooking = new SkillStat(Skill.Cooking, nameof(Cooking), skills.CookingLevel, skills.Cooking);
        Farming = new SkillStat(Skill.Farming, nameof(Farming), skills.FarmingLevel, skills.Farming);
        Magic = new SkillStat(Skill.Magic, nameof(Magic), skills.MagicLevel, skills.Magic);
        Ranged = new SkillStat(Skill.Ranged, nameof(Ranged), skills.RangedLevel, skills.Ranged);
        Slayer = new SkillStat(Skill.Slayer, nameof(Slayer), skills.SlayerLevel, skills.Slayer);
        Sailing = new SkillStat(Skill.Sailing, nameof(Sailing), skills.SailingLevel, skills.Sailing);
        Healing = new SkillStat(Skill.Healing, nameof(Healing), skills.HealingLevel, skills.Healing);


        this.skills = new ConcurrentDictionary<string, SkillStat>();
        SetupSkillLookup();
    }

    private void SetupSkillLookup()
    {
        for(var i = 0; i < SkillList.Length; ++i)
        {
            var s = SkillList[i];
            this.skills[s.Name.ToLower()] = s;
        }
    }

    public bool IsDead => Health.CurrentValue <= 0;
    public int CombatLevel => (int)(
        (Attack.Level + Defense.Level + Strength.Level + Health.Level) / 4f 
        + (Ranged.Level + Magic.Level + Healing.Level) / 8f);

    public double TotalExperience => SkillList.Sum(x => x.Experience);
    public double[] GetExperienceList()
    {
        //SkillList.Select(x => x.Experience).ToArray();
        return new double[]
        {
            Attack.Experience,
            Defense.Experience,
            Strength.Experience,
            Health.Experience,
            Woodcutting.Experience,
            Fishing.Experience,
            Mining.Experience,
            Crafting.Experience,
            Cooking.Experience,
            Farming.Experience,
            Slayer.Experience,
            Magic.Experience,
            Ranged.Experience,
            Sailing.Experience,
            Healing.Experience
        };
    }

    public int[] GetLevelList()
    {
        //SkillList.Select(x => x.Level).ToArray();
        return new int[]
        {
            Attack.Level,
            Defense.Level,
            Strength.Level,
            Health.Level,
            Woodcutting.Level,
            Fishing.Level,
            Mining.Level,
            Crafting.Level,
            Cooking.Level,
            Farming.Level,
            Slayer.Level,
            Magic.Level,
            Ranged.Level,
            Sailing.Level,
            Healing.Level
        };
    }
    public float HealthPercent => Health.CurrentValue / (float)Health.MaxLevel;
    public SkillStat[] SkillList => skillList ??
        (skillList = new SkillStat[]
        {
            Attack,
            Defense,
            Strength,
            Health,
            Woodcutting,
            Fishing,
            Mining,
            Crafting,
            Cooking,
            Farming,
            Slayer,
            Magic,
            Ranged,
            Sailing,
            Healing
        });

    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        if (obj is Skills skills)
            return CombatLevel - skills.CombatLevel;
        return 0;
    }

    internal void TakeBestOf(Skills target)
    {
        for (int i = 0; i < SkillList.Length; i++)
        {
            SkillStat skill = SkillList[i];
            SkillStat tskill = target.SkillList[i];
            if (tskill.Experience > skill.Experience)
            {
                skill.Set(tskill.Level, tskill.Experience);
                //skill.SetExp(tskill.Experience);
            }
        }
    }

    internal void CopyTo(Skills target)
    {
        for (int i = 0; i < SkillList.Length; i++)
        {
            SkillStat skill = SkillList[i];
            SkillStat tskill = target.SkillList[i];
            tskill.Set(skill.Level, skill.Experience);
        }
    }

    public static implicit operator RavenNest.Models.Skills(Skills skills)
    {
        return new RavenNest.Models.Skills
        {
            Attack = skills.Attack.Experience,
            AttackLevel = skills.Attack.Level,

            Cooking = skills.Cooking.Experience,
            CookingLevel = skills.Cooking.Level,

            Crafting = skills.Crafting.Experience,
            CraftingLevel = skills.Crafting.Level,

            Defense = skills.Defense.Experience,
            DefenseLevel = skills.Defense.Level,

            Farming = skills.Farming.Experience,
            FarmingLevel = skills.Farming.Level,

            Fishing = skills.Fishing.Experience,
            FishingLevel = skills.Fishing.Level,

            Health = skills.Health.Experience,
            HealthLevel = skills.Health.Level,

            Magic = skills.Magic.Experience,
            MagicLevel = skills.Magic.Level,

            Healing = skills.Healing.Experience,
            HealingLevel = skills.Healing.Level,

            Mining = skills.Mining.Experience,
            MiningLevel = skills.Mining.Level,

            Ranged = skills.Ranged.Experience,
            RangedLevel = skills.Ranged.Level,

            Sailing = skills.Sailing.Experience,
            SailingLevel = skills.Sailing.Level,

            Slayer = skills.Slayer.Experience,
            SlayerLevel = skills.Slayer.Level,

            Strength = skills.Strength.Experience,
            StrengthLevel = skills.Strength.Level,

            Woodcutting = skills.Woodcutting.Experience,
            WoodcuttingLevel = skills.Woodcutting.Level
        };
    }

    internal static int IndexOf(Skills skills, SkillStat activeSkill)
    {
        return Array.IndexOf(skills.SkillList, activeSkill);
    }

    //public SkillStat this[int index]
    //{
    //    get => this.skillList[index];
    //}

    public SkillStat this[Skill skill]
    {
        get
        {
            var index = (int)skill;
            if (this.SkillList.Length < index)
            {
                throw new Exception("Trying to get skill (index: " + index + ", " + skill + ")");
            }
            return this.skillList[index];
        }
    }

    internal SkillStat GetCombatSkill(CombatSkill skill)
    {
        switch (skill)
        {
            case CombatSkill.Attack: return Attack;
            case CombatSkill.Defense: return Defense;
            case CombatSkill.Strength: return Strength;
            case CombatSkill.Health: return Health;
            case CombatSkill.Magic: return Magic;
            case CombatSkill.Ranged: return Ranged;
            case CombatSkill.Healing: return Healing;
        }
        return null;
    }
    public SkillStat GetSkill(Skill skill)
    {
        return this.skillList[(int)skill];
    }

    [Obsolete("Please use GetSkill instead.")]
    public SkillStat GetSkill(TaskSkill skill)
    {
        switch (skill)
        {
            case TaskSkill.Woodcutting: return Woodcutting;
            case TaskSkill.Fishing: return Fishing;
            case TaskSkill.Crafting: return Crafting;
            case TaskSkill.Cooking: return Cooking;
            case TaskSkill.Mining: return Mining;
            case TaskSkill.Farming: return Farming;
            case TaskSkill.Slayer: return Slayer;
            case TaskSkill.Sailing: return Sailing;
        }
        return null;
    }

    public SkillStat GetSkillByName(string skillName)
    {
        if (skills.TryGetValue(skillName.ToLower(), out var skill))
        {
            return skill;
        }

        return null;
    }

    public RavenNest.Models.Skills ToServerModel()
    {
        var output = new RavenNest.Models.Skills();
        foreach (var prop in output
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.Name.Contains("Id"))
            {
                continue;
            }

            if (prop.Name.Contains("Level"))
            {
                var skill = GetSkillByName(prop.Name.Replace("Level", ""));
                if (skill != null)
                {
                    prop.SetValue(output, skill.Level);
                }
            }
            else
            {
                var skill = GetSkillByName(prop.Name);
                if (skill != null)
                {
                    prop.SetValue(output, skill.Experience);
                }
            }
        }

        return output;
    }

    public static Skills operator *(Skills srcSkills, float num)
    {
        var newSkills = new Skills();
        if (srcSkills == null) return newSkills;
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var outSkill = newSkills.SkillList[i];
            var skill = srcSkills.SkillList[i];

            var high = (skill.Level * num);
            var low = (int)high;
            var progress = high - low;
            var additionalExp = GameMath.ExperienceForLevel(low + 1) * progress;

            if (low >= GameMath.MaxLevel)
            {
                outSkill.Set(low, 0, false);
                continue;
            }

            outSkill.Set(low, additionalExp, false);
            outSkill.AddExp(skill.Experience * num);
        }

        return newSkills;
    }

    public static Skills operator +(Skills valueA, Skills valueB)
    {
        var newSkills = new Skills();
        if (valueA == null || valueB == null) return newSkills;
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var newSkill = newSkills.SkillList[i];
            var a = valueA.SkillList[i];
            var b = valueB.SkillList[i];
            var level = (a.Level + b.Level);
            newSkill.Set(level, 0, false);

            if (level < GameMath.MaxLevel)
            {
                newSkill.AddExp(a.Experience + b.Experience);
            }
        }

        return newSkills;
    }

    /// <summary>
    /// Gets the highest value out of the two given stats.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Skills Max(Skills a, Skills b)
    {
        var newSkills = new Skills();
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var newSkill = newSkills.SkillList[i];
            var skillA = a.SkillList[i];
            var skillB = b.SkillList[i];
            newSkill.Set(Math.Max(skillA.Level, skillB.Level), 0, false);
        }
        return newSkills;
    }

    /// <summary>
    /// Lerps between the two given stats
    /// </summary>
    /// <param name="valueFrom"></param>
    /// <param name="valueTo"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public static Skills Lerp(Skills valueFrom, Skills valueTo, float amount)
    {
        var newSkills = new Skills();
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var newSkill = newSkills.SkillList[i];
            var lowSkill = valueFrom.SkillList[i];
            var higSkill = valueTo.SkillList[i];
            var newLevel = (int)(Mathf.Lerp(lowSkill.Level, higSkill.Level, amount));
            newSkill.Set(Math.Max(1, newLevel), 0, false);
        }
        return newSkills;
    }

    /// <summary>
    /// Gets random skills given the lower and upper range
    /// </summary>
    /// <param name="rngLowStats"></param>
    /// <param name="rngHighStats"></param>
    /// <returns></returns>
    public static Skills Random(Skills rngLowStats, Skills rngHighStats)
    {
        var newSkills = new Skills();
        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            var newSkill = newSkills.SkillList[i];
            var lowSkill = rngLowStats.SkillList[i];
            var higSkill = rngHighStats.SkillList[i];
            var newLevel = (int)(UnityEngine.Random.Range(lowSkill.Level, higSkill.Level));
            newSkill.Set(Math.Max(1, newLevel), 0, false);
        }
        return newSkills;
    }
}

public static class SkillUtilities
{
    static SkillUtilities()
    {
        if (Skills == null)
        {
            Skills = Enums.GetValues<Skill>().ToArray();
        }

        if (SkillLookup == null)
        {
            SkillLookup = new Dictionary<string, Skill>();
            foreach (var s in Skills)
            {
                var name = s.ToString();
                var lower = name.ToLower();
                SkillLookup[name] = s;
                SkillLookup[lower] = s;

                if (s != Skill.Health && s != Skill.Healing)
                    SkillLookup[lower.Remove(3)] = s;
            }

            SkillLookup["hp"] = Skill.Health;
            SkillLookup["all"] = Skill.Health;
            SkillLookup["health"] = Skill.Health;
            SkillLookup["heal"] = Skill.Healing;
            SkillLookup["mage"] = Skill.Magic;
            SkillLookup["atk"] = Skill.Attack;
            SkillLookup["mine"] = Skill.Mining;
        }
    }

    public static Skill ParseSkill(string val)
    {
        if (string.IsNullOrEmpty(val)) return Skill.None;
        if (SkillLookup.TryGetValue(val, out var skill))
            return skill;

        if (Enum.TryParse<Skill>(val, true, out var s))
        {
            return SkillLookup[val] = s;
        }


        return Skill.None;
    }

    public static readonly Skill[] Skills;

    private static readonly Dictionary<string, Skill> SkillLookup;
}
