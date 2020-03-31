using System;
using UnityEngine;

public class TownHallManager : MonoBehaviour
{
    [SerializeField] private TownHallController[] townHallBuildings;

    private int tier;
    private TownHallController activeTownHall;

    void Start()
    {
        if (townHallBuildings == null || townHallBuildings.Length == 0)
            townHallBuildings = GetComponentsInChildren<TownHallController>();

        if (townHallBuildings.Length > 0)
            activeTownHall = townHallBuildings[0];
    }

    public void SetTierByLevel(int level)
    {
        SetTier(GetTownHallTier(level));
    }

    public void SetTier(int tier)
    {
        this.tier = tier;

        foreach (var townHall in townHallBuildings)
            townHall.gameObject.SetActive(false);

        if (tier >= townHallBuildings.Length)
            tier = townHallBuildings.Length - 1;

        activeTownHall = townHallBuildings[tier];
        activeTownHall.gameObject.SetActive(true);
    }

    private int GetTownHallTier(int level)
    {
        // < 10: tier 0 -- 10: Tier 1
        // < 30: tier 1 -- 30: Tier 2
        // < 50: tier 2 -- 50: Tier 3

        if (level < 10) return 0;
        if (level < 30) return 1;
        if (level < 80) return 2;
        return 2;

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
