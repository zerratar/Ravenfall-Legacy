using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillStat
{
    private static readonly Dictionary<int, double> expScaleCache = new Dictionary<int, double>();

    public string Name;
    public int CurrentValue;
    public int Level;
    public double Experience;
    public Skill Type;
    public float Bonus;

    public int MaxLevel => Mathf.FloorToInt(Level + Bonus);

    private float refreshRate = 60f;
    private double totalEarnedExperience;
    private float earnedExperienceStart;
    private float lastExperienceGain;

    public SkillStat() { }
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

    internal void ResetExpPerHour()
    {
        lastExperienceGain = 0;
    }

    public bool AddExp(double exp, out int newLevels)
    {
        var now = UnityEngine.Time.realtimeSinceStartup;
        if (earnedExperienceStart == 0 || now - lastExperienceGain/*earnedExperienceStart */>= refreshRate)
        {
            earnedExperienceStart = now;
            totalEarnedExperience = 0d;
        }

        lastExperienceGain = now;
        totalEarnedExperience += exp;

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

        return newLevels > 0;
    }

    public double GetExperiencePerHour()
    {
        var durationSeconds = lastExperienceGain - earnedExperienceStart;
        if (totalEarnedExperience <= 0 || durationSeconds <= 0) return 0d;
        var gainPerSecond = totalEarnedExperience / System.Math.Max(durationSeconds, 3);
        return gainPerSecond * 60d * 60d;
    }

    public void Reset()
    {
        CurrentValue = Mathf.FloorToInt(Level + Bonus);
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

}