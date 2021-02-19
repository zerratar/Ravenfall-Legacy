using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class VillageManager : MonoBehaviour
    {
        [SerializeField] private TownHallManager townHallManager;
        [SerializeField] private TownHouseManager townHouseManager;
        [SerializeField] private TownHallInfoManager info;

        private readonly ConcurrentDictionary<int, TownHouseExpBonus>
              expBonus = new ConcurrentDictionary<int, TownHouseExpBonus>();

        public TownHallManager TownHall => townHallManager;
        public TownHouseManager TownHouses => townHouseManager;

        // Start is called before the first frame update
        void Start()
        {
            if (!info) info = FindObjectOfType<TownHallInfoManager>();
            if (!townHallManager) townHallManager = FindObjectOfType<TownHallManager>();
            if (!townHouseManager) townHouseManager = FindObjectOfType<TownHouseManager>();
        }

        internal void SetExp(long experience)
        {
            TownHall.SetExp(experience);
        }


        public void SetHouses(IReadOnlyList<VillageHouseInfo> houses)
        {
            // do something
            townHouseManager.SetHouses(houses);
            townHallManager.SetSlotCount(houses.Count);
        }

        public void SetSlotCount(int count)
        {
            townHouseManager.SetSlotCount(count);
            townHallManager.SetSlotCount(count);
        }

        public void SetTierByLevel(int level)
        {
            townHallManager.SetTierByLevel(level);
        }

        public void SetTier(int tier)
        {
            townHallManager.SetTier(tier);
        }

        public void SetBonus(int slot, TownHouseSlotType slotType, float bonus)
        {
            expBonus[slot] = new TownHouseExpBonus(slotType, bonus);
        }

        internal float GetTotalExpBonus()
        {
            var values = expBonus.Values;
            if (values.Count == 0)
                return 1f;

            return 1f + values.Sum(x => x.Bonus) / 100f;
        }

        public ICollection<TownHouseExpBonus> GetExpBonuses()
        {
            return expBonus.Values;
        }

        public float GetExpBonusBySkill(CombatSkill skill)
        {
            var values = expBonus.Values
                .Where(x => GameMath.GetHouseTypeBySkill(skill) == x.SlotType)
                .ToList();

            if (values.Count == 0)
                return 1f;

            return 1f + values.Sum(x => x.Bonus) / 100f;
        }

        public float GetExpBonusBySkill(Skill skill)
        {
            var values = expBonus.Values
                .Where(x => GameMath.GetHouseTypeBySkill(skill) == x.SlotType)
                .ToList();

            if (values.Count == 0)
                return 1f;

            return 1f + values.Sum(x => x.Bonus) / 100f;
        }
    }

    public struct TownHouseExpBonus
    {
        public readonly TownHouseSlotType SlotType;
        public readonly float Bonus;

        public TownHouseExpBonus(TownHouseSlotType slotType, float bonus)
        {
            SlotType = slotType;
            Bonus = bonus;
        }
    }
}