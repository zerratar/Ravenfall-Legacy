using System;
using UnityEngine;

[Serializable]
public class Skills : IComparable
{
    public Skills()
    {
        Attack = new SkillStat(nameof(Attack), 0);
        Defense = new SkillStat(nameof(Defense), 0);
        Strength = new SkillStat(nameof(Strength), 0);
        Health = new SkillStat(nameof(Health), 1000);
        Woodcutting = new SkillStat(nameof(Woodcutting), 0);
        Fishing = new SkillStat(nameof(Fishing), 0);
        Mining = new SkillStat(nameof(Mining), 0);
        Crafting = new SkillStat(nameof(Crafting), 0);
        Cooking = new SkillStat(nameof(Cooking), 0);
        Farming = new SkillStat(nameof(Farming), 0);
        Magic = new SkillStat(nameof(Magic), 0);
        Ranged = new SkillStat(nameof(Ranged), 0);
        Slayer = new SkillStat(nameof(Slayer), 0);
        Sailing = new SkillStat(nameof(Sailing), 0);
    }

    public Skills(RavenNest.Models.Skills skills)
    {
        Attack = new SkillStat(nameof(Attack), skills.Attack);
        Defense = new SkillStat(nameof(Defense), skills.Defense);
        Strength = new SkillStat(nameof(Strength), skills.Strength);
        Health = new SkillStat(nameof(Health), skills.Health);
        Woodcutting = new SkillStat(nameof(Woodcutting), skills.Woodcutting);
        Fishing = new SkillStat(nameof(Fishing), skills.Fishing);
        Mining = new SkillStat(nameof(Mining), skills.Mining);
        Crafting = new SkillStat(nameof(Crafting), skills.Crafting);
        Cooking = new SkillStat(nameof(Cooking), skills.Cooking);
        Farming = new SkillStat(nameof(Farming), skills.Farming);
        Magic = new SkillStat(nameof(Magic), skills.Magic);
        Ranged = new SkillStat(nameof(Ranged), skills.Ranged);
        Slayer = new SkillStat(nameof(Slayer), skills.Slayer);
        Sailing = new SkillStat(nameof(Sailing), skills.Sailing);
    }

    public bool IsDead => Health.CurrentValue <= 0;

    public int CombatLevel => (int)((Attack.Level + Defense.Level + Strength.Level + Health.Level) / 4f + Ranged.Level / 8f + Magic.Level / 8f);

    public decimal TotalExperience =>
        Attack.Experience +
        Defense.Experience +
        Strength.Experience +
        Health.Experience +
        Magic.Experience +
        Farming.Experience +
        Cooking.Experience +
        Crafting.Experience +
        Mining.Experience +
        Fishing.Experience +
        Woodcutting.Experience +
        Slayer.Experience +
        Ranged.Experience +
        Sailing.Experience;

    public decimal[] ExperienceList => new decimal[]
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
        Sailing.Experience
    };

    public long[] LevelList => new long[]
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
        Sailing.Level
    };

    public SkillStat[] SkillList => new SkillStat[]
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
            Sailing
    };

    public SkillStat Attack;
    public SkillStat Defense;
    public SkillStat Strength;
    public SkillStat Health;
    public SkillStat Magic;
    public SkillStat Ranged;
    public SkillStat Farming;
    public SkillStat Cooking;
    public SkillStat Crafting;
    public SkillStat Mining;
    public SkillStat Fishing;
    public SkillStat Woodcutting;
    public SkillStat Slayer;
    public SkillStat Sailing;

    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        if (obj is Skills skills) return CombatLevel - skills.CombatLevel;
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
                skill.Experience = tskill.Experience;
            }
        }
    }
}
