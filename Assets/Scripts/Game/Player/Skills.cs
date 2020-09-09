using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    public SkillStat Farming;
    public SkillStat Cooking;
    public SkillStat Crafting;
    public SkillStat Mining;
    public SkillStat Fishing;
    public SkillStat Woodcutting;
    public SkillStat Slayer;
    public SkillStat Sailing;

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

        skills = new ConcurrentDictionary<string, SkillStat>(
            SkillList.ToDictionary(x => x.Name.ToLower(), x => x));
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
    public decimal TotalExperience => SkillList.Sum(x => x.Experience);
    public decimal[] ExperienceList => SkillList.Select(x => x.Experience).ToArray();
    public int[] LevelList => SkillList.Select(x => x.Level).ToArray();

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
                Sailing
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
                skill.SetExp(tskill.Experience);
            }
        }
    }

    public static implicit operator RavenNest.Models.Skills(Skills skills)
    {
        return new RavenNest.Models.Skills
        {
            Attack = skills.Attack.Experience,
            Cooking = skills.Cooking.Experience,
            Crafting = skills.Crafting.Experience,
            Defense = skills.Defense.Experience,
            Farming = skills.Farming.Experience,
            Fishing = skills.Fishing.Experience,
            Health = skills.Health.Experience,
            Magic = skills.Magic.Experience,
            Mining = skills.Mining.Experience,
            Ranged = skills.Ranged.Experience,
            Sailing = skills.Sailing.Experience,
            Slayer = skills.Slayer.Experience,
            Strength = skills.Strength.Experience,
            Woodcutting = skills.Woodcutting.Experience
        };
    }
    public static Skills operator *(Skills skill, float num)
    {
        var newSkills = new Skills();
        newSkills.TakeBestOf(skill);

        for (int i = 0; i < newSkills.SkillList.Length; i++)
        {
            newSkills.SkillList[i].SetExp((decimal)num * newSkills.SkillList[i].Experience);
        }

        return newSkills;
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
        }
        return null;
    }

    public SkillStat GetSkill(Skill skill)
    {
        switch (skill)
        {
            case Skill.Woodcutting: return Woodcutting;
            case Skill.Fishing: return Fishing;
            case Skill.Crafting: return Crafting;
            case Skill.Cooking: return Cooking;
            case Skill.Mining: return Mining;
            case Skill.Farming: return Farming;
            case Skill.Slayer: return Slayer;
            case Skill.Sailing: return Sailing;
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
}
