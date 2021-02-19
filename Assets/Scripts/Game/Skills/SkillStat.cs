using System;
using UnityEngine;

[Serializable]
public class SkillStat
{
    public string Name;
    public int CurrentValue;
    public int Level;
    public decimal Experience;

    private float refreshRate = 60f;
    private decimal totalEarnedExperience;
    private DateTime earnedExperienceStart;
    private DateTime lastExperienceGain;

    public SkillStat() { }
    public SkillStat(
        string name,
        int level,
        decimal exp)
    {
        CurrentValue = level;//GameMath.ExperienceToLevel(exp);
        Level = level;//GameMath.ExperienceToLevel(exp);
        Experience = exp;
        Name = name;
    }

    public void Set(int newLevel, decimal newExp, bool updateExpPerHour = true)
    {
        if (!updateExpPerHour)
        {
            Level = newLevel;
            CurrentValue = newLevel;
            Experience = newExp;
            return;
        }

        var expToAdd = 0m;
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

    public void SetExp(decimal exp)
    {
        AddExp(exp - Experience, out _, false);
    }

    public void AddExp(decimal exp)
    {
        AddExp(exp, out _);
    }

    public bool AddExp(decimal exp, out int newLevels, bool scale = true)
    {
        if (scale)
        {
            exp *= (((1m + (decimal)(Level / 100f) + (decimal)(Level / 75f)) * (decimal)Math.Pow(2, (double)(Level / 20d))) / 20m) + 1m;
        }

        if (earnedExperienceStart == DateTime.MinValue ||
            (DateTime.UtcNow - earnedExperienceStart).TotalSeconds >= refreshRate)
        {
            earnedExperienceStart = DateTime.UtcNow;
            totalEarnedExperience = 0m;
        }

        lastExperienceGain = DateTime.UtcNow;
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

        return newLevels > 0;
    }

    public decimal GetExperiencePerHour()
    {
        var durationSeconds = lastExperienceGain - earnedExperienceStart;
        if (totalEarnedExperience <= 0 || durationSeconds.TotalSeconds <= 0) return 0m;
        var gainPerSecond = totalEarnedExperience / (decimal)durationSeconds.TotalSeconds;
        return gainPerSecond * 60m * 60m;
    }

    public void Reset()
    {
        CurrentValue = Level;
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
        return $"{Name} {Level} ({proc}%)";
    }
}