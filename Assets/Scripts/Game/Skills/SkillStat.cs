using System;
using System.Collections.Generic;
using UnityEngine;
using Skill = RavenNest.Models.Skill;

[Serializable]
public class SkillStat
{
    public string Name;
    public int CurrentValue;
    public int Level;
    public double Experience;
    public Skill Type;
    public float Bonus;

    public int MaxLevel;// => Mathf.FloorToInt(Level + Bonus);

    public int Index;

    private List<ExpGain> expGains = new List<ExpGain>();
    private float windowDuration = 180;//3600; // 1 hour

    public SkillStat() { }

    public SkillStat(float level)
        : this(Mathf.CeilToInt(level))
    {
    }

    public SkillStat(int level)
    {
        this.Level = level;
        this.CurrentValue = level;
        this.MaxLevel = Mathf.FloorToInt(Level + Bonus);
    }

    public SkillStat(
        Skill type,
        string name,
        int level,
        double exp)
    {
        Type = type;
        CurrentValue = level;//GameMath.ExperienceToLevel(exp);
        Level = level;//GameMath.ExperienceToLevel(exp);
        Experience = exp;
        Name = name;
        this.MaxLevel = Mathf.FloorToInt(Level + Bonus);
    }

    public void Set(int newLevel, double newExp, bool updateExpPerHour = true)
    {
        if (newLevel >= GameMath.MaxLevel)
        {
            newLevel = GameMath.MaxLevel;
            var maxExp = GameMath.ExperienceForLevel(GameMath.MaxLevel);
            if (newExp >= maxExp)
                newExp = maxExp;
        }

        if (!updateExpPerHour)
        {
            Level = newLevel;
            CurrentValue = newLevel;
            Experience = newExp;
            this.MaxLevel = Mathf.FloorToInt(Level + Bonus);
            return;
        }

        var expToAdd = 0d;
        var levelDelta = newLevel - Level;
        if (levelDelta > 0)
        {
            // take the remainder for the first level up.
            // then for each level in the delta. we add the additional exp
            // Finally add the exp we have on that level.
            // this whole process will ensure we get the exp/h updated properly.
            var remainder = GameMath.ExperienceForLevel(Level + 1) - Experience;
            expToAdd = remainder;
            --levelDelta;
            for (var i = 1; i <= levelDelta; ++i)
                expToAdd += GameMath.ExperienceForLevel(Level + i + 1);
            expToAdd += newExp;

            this.MaxLevel = Mathf.FloorToInt(Level + Bonus);
        }
        else if (levelDelta == 0 && newExp > Experience)
        {
            // as long as the exp&level is not less than current exp, just add the delta exp.
            expToAdd = newExp - Experience;
        }

        if (expToAdd > 0)
        {
            AddExp(expToAdd);
        }


    }

    public void SetExp(double exp)
    {
        AddExp(exp - Experience, out _);
    }

    public void AddExp(double exp)
    {
        AddExp(exp, out _);
    }

    public bool AddExp(double exp, out int newLevels)
    {
        newLevels = 0;
        Experience += exp;

        var expForNextLevel = GameMath.ExperienceForLevel(this.Level + 1);
        while (Experience >= expForNextLevel)
        {
            ++newLevels;
            ++Level;
            Experience -= expForNextLevel;
            CurrentValue = Level;
            expForNextLevel = GameMath.ExperienceForLevel(Level + 1);
        }

        if (Level >= GameMath.MaxLevel)
        {
            Level = GameMath.MaxLevel;
            var maxExp = GameMath.ExperienceForLevel(GameMath.MaxLevel);
            if (Experience >= maxExp)
                Experience = maxExp;
        }

        expGains.Add(new ExpGain { Exp = exp, Time = UnityEngine.Time.realtimeSinceStartup });

        var windowStart = UnityEngine.Time.realtimeSinceStartup - windowDuration;
        while (expGains.Count > 0 && (expGains.Count > 30 || expGains[0].Time < windowStart))
        {
            expGains.RemoveAt(0);
        }


        if (newLevels > 0)
        {
            this.MaxLevel = Mathf.FloorToInt(Level + Bonus);
            return true;
        }

        return false;
    }
    public void ResetExperiencePerHour()
    {
        expGains.Clear();
    }

    public double GetExperiencePerHour()
    {
        try
        {
            var now = UnityEngine.Time.realtimeSinceStartup;
            var expSum = 0d;
            var windowStart = now - windowDuration;
            for (int i = expGains.Count - 1; i >= 0; i--)
            {
                if (expGains[i].Time < windowStart)
                    break;

                expSum += expGains[i].Exp;
            }

            var gainPerSecond = expSum / windowDuration;
            return gainPerSecond * 3600d;
        }
        catch
        {
            return double.MaxValue;
        }
    }

    public DateTime GetEstimatedTimeToLevelUp()
    {
        var expPerHour = GetExperiencePerHour();
        if (expPerHour <= 0 || this.Level >= GameMath.MaxLevel) return DateTime.MaxValue;
        var expForNextLevel = GameMath.ExperienceForLevel(this.Level + 1) - Experience;
        var now = DateTime.UtcNow;
        var hoursLeft = expForNextLevel / expPerHour;
        if (hoursLeft <= 0) return now;
        try
        {
            return now.AddHours((double)hoursLeft);
        }
        catch
        {
            return DateTime.MaxValue;
        }
    }

    public void Reset()
    {
        CurrentValue = Mathf.FloorToInt(Level + Bonus);
        MaxLevel = Mathf.FloorToInt(Level + Bonus);
    }

    public void Add(int value)
    {
        CurrentValue += value;
        if (CurrentValue > Level)
            CurrentValue = Level;
    }

    public override string ToString()
    {
        var expForNextLevel = GameMath.ExperienceForLevel(Level + 1);
        var proc = Mathf.FloorToInt((float)(Experience / expForNextLevel) * 100);

        if (Bonus > 0)
        {
            return $"{Name} {Level} [+{Bonus:0}] ({proc}%)";
        }

        return $"{Name} {Level} ({proc}%)";
    }


    public override int GetHashCode()
    {
        return HashCode.Combine(Name, CurrentValue, Level, Experience);
    }

    private struct ExpGain
    {
        public double Exp;
        public float Time;
    }
}