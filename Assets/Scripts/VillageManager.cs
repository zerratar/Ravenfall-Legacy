using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class VillageManager : MonoBehaviour
    {
        [SerializeField] private TownHallManager townHallManager;
        [SerializeField] private TownHouseManager townHouseManager;
        [SerializeField] private TownHallInfoManager info;
        [SerializeField] private GameManager gameManager;

        private readonly Dictionary<int, TownHouseExpBonus> expBonusBySlot = new Dictionary<int, TownHouseExpBonus>();
        private readonly Dictionary<TownHouseSlotType, TownHouseExpBonus> expBonusByType = new Dictionary<TownHouseSlotType, TownHouseExpBonus>();
        public TownHallManager TownHall => townHallManager;
        public TownHouseManager TownHouses => townHouseManager;

        public bool HousesAreUpdating { get; private set; }

        // Start is called before the first frame update
        void Start()
        {
            if (!gameManager) gameManager = FindObjectOfType<GameManager>();
            if (!info) info = FindObjectOfType<TownHallInfoManager>();
            if (!townHallManager) townHallManager = FindObjectOfType<TownHallManager>();
            if (!townHouseManager) townHouseManager = FindObjectOfType<TownHouseManager>();
        }

        public void SetHouses(IReadOnlyList<VillageHouseInfo> houses)
        {
            // TODO: Check if this is the same data as before, if it is. (not changed) then ignore it.
            //              Even better; maybe dont even send the update from the server.

            townHouseManager.SetHouses(houses);
            townHallManager.SetSlotCount(houses.Count(x => x.Type != (int)TownHouseSlotType.Empty && x.Type != -1), houses.Count);

            gameManager.UpdateVillageBoostText();
        }

        public void SetSlotCount(int count)
        {
            townHouseManager.SetSlotCount(count);
            var usedSlots = 0;
            if (this.TownHouses?.TownHouses != null && this.TownHouses?.TownHouses.Length > 0)
            {
                usedSlots = this.TownHouses?.TownHouses.Count(x => x.Type != (int)TownHouseSlotType.Empty) ?? 0;
            }
            townHallManager.SetSlotCount(usedSlots, count);
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
            expBonusBySlot[slot] = new TownHouseExpBonus(slotType, bonus);
            expBonusByType[slotType] = new TownHouseExpBonus(slotType, expBonusBySlot.Values.Where(x => slotType == x.SlotType).Sum(x => x.Bonus));

            if (info) info.UpdateExpBonusTexts();
        }

        internal async Task SetVillageBoostTarget(TownHouseSlotType slotType)
        {
            try
            {
                HousesAreUpdating = true;
                var failedToUpdate = "Failed to update the huts right now. Please try again later.";

                if (slotType == TownHouseSlotType.Empty || slotType == TownHouseSlotType.Undefined)
                {
                    await ClearAllHousesAsync();
                    return;
                }

                var houses = new VillageHouseInfo[TownHouses.SlotCount];
                var players = this.gameManager.Players.GetAllPlayers().Where(x => !x.IsBot).ToList();
                var assignablePlayers = new List<PlayerController>();
                if (players.Count > 0)
                {
                    assignablePlayers = players.OrderByDescending(x => GameMath.GetSkillByHouseType(x.Stats, slotType).Level).Take(houses.Length).ToList();
                }

                for (var i = 0; i < houses.Length; ++i)
                {
                    if (!await gameManager.RavenNest.Village.BuildHouseAsync(i, (int)slotType))
                    {
                        gameManager.RavenBot.SendMessage("", failedToUpdate);
                        return;
                    }

                    var ownerUserId = i < assignablePlayers.Count ? assignablePlayers[i].UserId : null;
                    houses[i] = new VillageHouseInfo()
                    {
                        Slot = i,
                        Type = (int)slotType,
                        Owner = ownerUserId
                    };

                    if (!string.IsNullOrEmpty(ownerUserId))
                    {
                        await gameManager.RavenNest.Village.AssignPlayerAsync(i, assignablePlayers[i].Id);
                    }
                }

                SetHouses(houses);
            }
            finally
            {
                HousesAreUpdating = false;
            }
        }

        internal async Task ClearAllHousesAsync()
        {
            var failedToUpdate = "Failed to update the huts right now. Please try again later.";
            var houses = new VillageHouseInfo[TownHouses.TownHouses.Length];
            for (var i = 0; i < houses.Length; ++i)
            {
                if (!await gameManager.RavenNest.Village.RemoveHouseAsync(i))
                {
                    gameManager.RavenBot.SendMessage("", failedToUpdate);
                    return;
                }

                houses[i] = new VillageHouseInfo()
                {
                    Slot = i,
                    Type = (int)TownHouseSlotType.Empty,
                    Owner = null,
                };
            }

            SetHouses(houses);
        }

        internal float GetTotalExpBonus()
        {
            var values = expBonusBySlot.Values;
            if (values.Count == 0)
                return 0;

            return values.Sum(x => x.Bonus) / 100f;
        }

        public ICollection<TownHouseExpBonus> GetGroupedExpBonuses()
        {
            return expBonusByType.Values;
        }

        public ICollection<TownHouseExpBonus> GetExpBonuses()
        {
            return expBonusBySlot.Values;
        }

        public float GetExpBonusBySkill(CombatSkill skill)
        {
            if (expBonusByType.TryGetValue(GameMath.GetHouseTypeBySkill(skill), out var bonus))
            {
                return bonus.Bonus / 100f;
            }
            return 0;
        }

        public float GetExpBonusBySkill(TaskSkill skill)
        {
            if (expBonusByType.TryGetValue(GameMath.GetHouseTypeBySkill(skill), out var bonus))
            {
                return bonus.Bonus / 100f;
            }
            return 0;
        }

        public float GetExpBonusBySkill(Skill skill)
        {
            if (expBonusByType.TryGetValue(GameMath.GetHouseTypeBySkill(skill), out var bonus))
            {
                return bonus.Bonus / 100f;
            }
            return 0;
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