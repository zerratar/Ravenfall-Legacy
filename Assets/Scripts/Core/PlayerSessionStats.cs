using UnityEngine;

public class PlayerSessionStats
{
    private float lastDamageTime;
    private float lastHealTime;
    private float lastGatheredTime;
    private float lastTreeCutDown;
    private float lastEnemyKilled;

    private float dpsTimeStart;
    private float hpsTimeStart;

    public int TotalDamageDealt { get; private set; }
    public int TotalAttackCount { get; private set; }
    public int TotalGatheredCount { get; private set; }
    public int TotalTreesCutDownCount { get; private set; }
    public int TotalEnemiesKilled { get; private set; }
    public int TotalHealthHealed { get; private set; }
    public int TotalHealCount { get; private set; }

    public float HighestDPS { get; private set; }
    public int HighestDamage { get; private set; }
    public float HighestHPS { get; private set; }
    public int HighestHeal { get; private set; }

    public int DamageDealt { get; private set; }
    public int HealthHealed { get; private set; }
    public float DPS { get; private set; }
    public float HPS { get; private set; }

    public void AddDamageDealt(int damage)
    {
        var now = Time.time;
        if (now - lastDamageTime > 10)
        {
            dpsTimeStart = now;
            DamageDealt = damage;
        }
        else
        {
            DamageDealt += damage;
        }
        lastDamageTime = now;
        TotalDamageDealt += damage;
        TotalAttackCount++;
        if (damage > HighestDamage)
        {
            HighestDamage = damage;
        }

        DPS = GetDamagePerSecond();
    }

    public void AddHealingDealt(int heal)
    {
        var now = Time.time;
        if (now - lastHealTime > 10)
        {
            hpsTimeStart = now;
            HealthHealed = heal;
        }
        else
        {
            HealthHealed += heal;
        }
        lastHealTime = now;
        TotalHealthHealed += heal;
        TotalHealCount++;
        if (heal > HighestHeal)
        {
            HighestHeal = heal;
        }

        HPS = GetHealPerSecond();
    }

    public void IncrementEnemiesKilled()
    {
        lastEnemyKilled = Time.time;
        TotalEnemiesKilled++;
    }

    public void IncrementGather()
    {
        lastGatheredTime = Time.time;
        TotalGatheredCount++;
    }

    public void IncrementTreeCutDown()
    {
        lastTreeCutDown = Time.time;
        TotalTreesCutDownCount++;
    }

    private float GetDamagePerSecond()
    {
        float elapsedTime = Time.time - dpsTimeStart;
        var val = elapsedTime > 0 ? (float)DamageDealt / elapsedTime : 0;

        if (val > HighestDPS)
        {
            HighestDPS = val;
        }
        return val;
    }

    private float GetHealPerSecond()
    {
        float elapsedTime = Time.time - hpsTimeStart;
        var val = elapsedTime > 0 ? (float)HealthHealed / elapsedTime : 0;
        if (val > HighestHPS)
        {
            HighestHPS = val;
        }

        return val;
    }

    //public float GetGatherPerSecond()
    //{
    //    float elapsedTime = Time.time;
    //    return TotalGatheredCount / elapsedTime;
    //}

    //public float GetTreesCutDownPerSecond()
    //{
    //    float elapsedTime = Time.time;
    //    return TotalTreesCutDownCount / elapsedTime;
    //}

    //public float GetEnemiesKilledPerSecond()
    //{
    //    float elapsedTime = Time.time;
    //    return TotalEnemiesKilled / elapsedTime;
    //}
}
