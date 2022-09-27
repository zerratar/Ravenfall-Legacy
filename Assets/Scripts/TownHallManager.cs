using System;
using UnityEngine;

public class TownHallManager : MonoBehaviour
{
    [SerializeField] private TownHallController[] townHallBuildings;
    [SerializeField] private TownHallInfoManager info;

    private int tier = -1;
    private int level;
    private double exp;

    private TownHallController activeTownHall;

    void Start()
    {
        if (!info) info = FindObjectOfType<TownHallInfoManager>();

        if (townHallBuildings == null || townHallBuildings.Length == 0)
            townHallBuildings = GetComponentsInChildren<TownHallController>();

        if (townHallBuildings.Length > 0)
            activeTownHall = townHallBuildings[0];
    }

    public double Experience => exp;
    public int Level => level;
    public int Tier => tier;
    //public decimal ExperienceToLevel => GameMath.VillageLevelToExperience(Level + 1);

    public void SetTierByLevel(int level)
    {
        SetLevel(level);
        SetTier(GetTownHallTier(level));
    }

    public void SetTier(int tier)
    {
        if (this.tier == tier)
        {
            return;
        }


        this.tier = tier;

        foreach (var townHall in townHallBuildings)
            townHall.gameObject.SetActive(false);

        if (tier >= townHallBuildings.Length)
            tier = townHallBuildings.Length - 1;

        activeTownHall = townHallBuildings[tier];
        activeTownHall.gameObject.SetActive(true);

        if (info)
        {
            info.Tier = tier;
        }
    }

    internal void SetSlotCount(int usedCount, int count)
    {
        if (info)
        {
            info.UsedSlots = usedCount;
            info.Slots = count;
        }
    }

    internal void OpenVillageDialog()
    {
        if (info)
        {
            info.Toggle();
        }
    }

    private void SetLevel(int level)
    {
        this.level = level;
        if (info)
        {
            info.Level = level;
        }
    }

    internal void SetExp(double experience)
    {
        this.exp = experience;
        if (info)
        {
            info.Experience = experience;
        }
    }

    private int GetTownHallTier(int level)
    {
        // < 10: tier 0 -- 10: Tier 1
        // < 30: tier 1 -- 30: Tier 2
        // < 50: tier 2 -- 50: Tier 3

        if (level < 10) return 0;
        if (level < 30) return 1;
        if (level < 80) return 2;
        if (level < 150) return 3;
        if (level < 200) return 4;
        return 4;

        //var tier = 1;
        //for (; tier < 4; ++tier)
        //{
        //    if ((level - 20 * tier) <= 0)
        //    {
        //        break;
        //    }
        //}

        //return tier;
    }
}
