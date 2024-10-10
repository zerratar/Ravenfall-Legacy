using RavenNest.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Skill = RavenNest.Models.Skill;
namespace Assets.Scripts
{
    public class VillageManager : MonoBehaviour
    {
        [SerializeField] private TownHallManager townHallManager;
        [SerializeField] private TownHouseManager townHouseManager;
        [SerializeField] private TownHallInfoManager info;
        [SerializeField] private GameManager gameManager;

        private readonly ConcurrentDictionary<int, TownHouseExpBonus> expBonusBySlot = new ConcurrentDictionary<int, TownHouseExpBonus>();
        private readonly ConcurrentDictionary<TownHouseSlotType, TownHouseExpBonus> expBonusByType = new ConcurrentDictionary<TownHouseSlotType, TownHouseExpBonus>();

        private LoadingState state = LoadingState.None;
        private DateTime lastVillageLoad = DateTime.UnixEpoch;
        public TownHallManager TownHall => townHallManager;
        public TownHouseManager TownHouses => townHouseManager;

        public bool HousesAreUpdating { get; private set; }

        // Start is called before the first frame update
        void Start()
        {
            if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
            if (!info) info = FindAnyObjectByType<TownHallInfoManager>();
            if (!townHallManager) townHallManager = FindAnyObjectByType<TownHallManager>();
            if (!townHouseManager) townHouseManager = FindAnyObjectByType<TownHouseManager>();

            //if (!gameManager) return;
            //gameManager.SetLoadingState("village", state);
        }

        private void Update()
        {
            if (gameManager == null || gameManager.RavenNest == null)
            {
                return;
            }

            if (state == LoadingState.None && gameManager && gameManager.RavenNest.SessionStarted)
            {
                LoadVillageAsync();
            }
        }

        private async void LoadVillageAsync()
        {
            state = LoadingState.Loading;
            try
            {
                if (DateTime.UtcNow - lastVillageLoad >= TimeSpan.FromSeconds(5))
                {
                    lastVillageLoad = DateTime.UtcNow;
                    var data = await gameManager.RavenNest.Village.GetAsync();
                    if (data != null)
                    {
                        SetTierByLevel(data.Level);
                        if (!HousesAreUpdating)
                        {
                            SetHouses(data.Houses);
                        }
                        TownHall.SetExp(data.Experience);
                    }
                    state = LoadingState.Loaded;
                }
            }
            catch { state = LoadingState.None; }
            //gameManager.SetLoadingState("village", state);
        }

        public void SetHouses(IReadOnlyList<VillageHouseInfo> houses)
        {
            // TODO: Check if this is the same data as before, if it is. (not changed) then ignore it.
            //              Even better; maybe dont even send the update from the server.

            if (!RequireUpdate(houses))
            {
                return;
            }

            townHouseManager.SetHouses(houses);
            townHallManager.SetSlotCount(GetUsedSlotCount(houses), houses.Count);

            gameManager.villageBoostLabel.Update();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBonusWithoutNotify(int slot, TownHouseSlotType slotType, float bonus)
        {
            expBonusBySlot[slot] = new TownHouseExpBonus(slotType, bonus);
            expBonusByType[slotType] = new TownHouseExpBonus(slotType, expBonusBySlot.Values.Where(x => slotType == x.SlotType).Sum(x => x.Bonus));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBonus(int slot, TownHouseSlotType slotType, float bonus)
        {
            SetBonusWithoutNotify(slot, slotType, bonus);
            UpdateBoostPerType();
            UpdateExpBonusText();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateExpBonusText()
        {
            if (info) info.UpdateExpBonusTexts();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateBoostPerType()
        {
            foreach (var i in expBonusByType.Keys)
            {
                expBonusByType[i] = new TownHouseExpBonus(i, expBonusBySlot.Values.Where(x => i == x.SlotType).Sum(x => x.Bonus));
            }
        }

        internal async void AssignBestPlayers()
        {
            //throw new NotImplementedException();
            if (TownHouses.SlotCount > 0)
            {
                var firstHut = TownHouses.TownHouses.FirstOrDefault();
                // check if all huts are the same type
                if (firstHut != null && TownHouses.TownHouses.All(x => x.Type == firstHut.Type))
                {
                    Shinobytes.Debug.Log("All huts are the same type, assigning players to huts.");
                    await SetVillageBoostTarget(firstHut.Type, false);
                }
                else
                {
                    Shinobytes.Debug.Log("Not all huts were the same type, skipping auto-assign players.");
                }
            }
            else
            {
                Shinobytes.Debug.Log("No huts available, skipping auto-assigning players.");
            }
        }

        internal async Task SetVillageBoostTarget(TownHouseSlotType slotType, bool announceInChat = true)
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

                var toAssign = new List<Guid>();

                for (var i = 0; i < houses.Length; ++i)
                {
                    var plr = i < assignablePlayers.Count ? assignablePlayers[i] : null;
                    var ownerUserId = plr?.PlatformId;
                    houses[i] = new VillageHouseInfo()
                    {
                        Slot = i,
                        Type = (int)slotType,
                        OwnerUserId = plr?.UserId,
                        Owner = ownerUserId
                    };

                    if (!string.IsNullOrEmpty(ownerUserId))
                    {
                        toAssign.Add(assignablePlayers[i].Id);
                    }
                }

                if (await gameManager.RavenNest.Village.AssignVillageAsync((int)slotType, toAssign.ToArray()))
                {
                    SetHouses(houses);
                    Shinobytes.Debug.Log("All " + houses.Length + " houses has been set to " + slotType + " with the most suitable players!");
                }
                else
                {
                    if (announceInChat)
                    {
                        gameManager.RavenBot.Announce(failedToUpdate);
                    }
                }
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError($"Failed to assign the town to {slotType}. " + exc);
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
                    gameManager.RavenBot.Announce(failedToUpdate);
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

        public float GetExpBonusBySkill(Skill skill)
        {
            if (expBonusByType.TryGetValue(GameMath.GetHouseTypeBySkill(skill), out var bonus))
            {
                return bonus.Bonus / 100f;
            }
            return 0;
        }

        private int GetUsedSlotCount(IReadOnlyList<VillageHouseInfo> houses)
        {
            var count = 0;
            for (var i = 0; i < houses.Count; i++)
            {
                var x = houses[i];
                if (x.Type != (int)TownHouseSlotType.Empty && x.Type != -1)
                {
                    ++count;
                }
            }
            return count;
            // houses.Count(x => x.Type != (int)TownHouseSlotType.Empty && x.Type != -1)
        }

        private bool RequireUpdate(IReadOnlyList<VillageHouseInfo> newHouses)
        {
            var slots = townHouseManager.Slots;
            if (newHouses.Count != slots.Length)
            {
                return true;
            }

            for (var i = 0; i < newHouses.Count; ++i)
            {
                var house = newHouses[i];
                var slot = slots[i];
                if (slot.OwnerUserId != house.OwnerUserId || slot.SlotType != (TownHouseSlotType)house.Type)
                {
                    return true;
                }
            }

            return false;
        }

    }

}