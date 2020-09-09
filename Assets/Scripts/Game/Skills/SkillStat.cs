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
        decimal exp)
    {
        CurrentValue = GameMath.ExperienceToLevel(exp);
        Level = GameMath.ExperienceToLevel(exp);
        Experience = exp;
        Name = name;

    }

    public void SetExp(decimal exp)
    {
        AddExp(exp - Experience, out _);
    }

    public bool AddExp(decimal exp, out int newLevels)
    {

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
        var newLevel = GameMath.ExperienceToLevel(Experience);
        var levelDiff = newLevel - Level;
        if (levelDiff > 0)
        {
            // celebrate!
            CurrentValue = newLevel;
            Level = newLevel;
            newLevels = levelDiff;
        }

        //Debug.Log(Name + ":: Add exp: " + exp + ", cur exp: " + this.Experience + ", cur lvl: " + Level + " level from exp: " + newLevel);

        return newLevels > 0;
    }

    public decimal GetExperiencePerHour()
    {
        //if (lastExperienceGain <= Mathf.Abs(Time.time - refreshRate)) return 0m;
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
    }

    public override string ToString()
    {
        var min = GameMath.LevelToExperience(Level);
        var nextLevel = GameMath.LevelToExperience(Level + 1) - min;
        var currentExp = Experience - min;
        var proc = Mathf.FloorToInt((float)(currentExp / nextLevel) * 100);
        return $"{Name} {Level} ({proc}%)";
    }

}