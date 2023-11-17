using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaidDifficultySystem
{
    public const int MAX_RAID_HISTORY = 5;

    private Dictionary<int, List<PlayerController>> joinedPlayers = new();
    private Dictionary<int, RaidDifficultyContext> contexts = new();

    private int internalRaidIndex = 0;
    private GameManager gameManager;

    public RaidDifficulty GetDifficulty(int raidIndex)
    {
        var ctx = GetContext(raidIndex);
        return ctx.Difficulty = GenerateDifficulty();
    }

    private RaidDifficulty GenerateDifficulty()
    {
        // so, based on the average total values from the past MAX_RAID_HISTORY
        // we will get the stats to use for our raid boss
        // create a temporary RaidDifficultyContext to store our data in
        var tmp = new RaidDifficultyContext();
        var d = new RaidDifficulty();

        if (gameManager == null)
        {
            gameManager = GameObject.FindAnyObjectByType<GameManager>();
        }

        var players = gameManager.Players.GetAllPlayers();
        try
        {
            // if we don't have enough data, we will fill it up using the lowest levels on the stream active in the game
            if (contexts.Count < MAX_RAID_HISTORY)
            {
                var generateCount = MAX_RAID_HISTORY - contexts.Count;

                for (var i = 0; i < generateCount; ++i)
                {
                    var fillers = GetLowLevelPlayers(players);
                    foreach (var filler in fillers)
                    {
                        tmp.Add(filler);
                    }
                }
            }

            foreach (var c in contexts)
            {
                tmp.Append(c.Value);
            }

            var pCount = (float)Mathf.Max(tmp.PlayerCount, 1f);
            d.BossSkills = new Skills
            {
                Attack = new(tmp.TotalAttack / pCount),
                Defense = new(tmp.TotalDefense / pCount),
                Strength = new(tmp.TotalStrength / pCount),
                Health = new((tmp.TotalHealth / pCount) * 100),
                Ranged = new(tmp.TotalRanged / pCount),
                Magic = new(tmp.TotalMagic / pCount)
            };

            d.BossEquipmentStats = new EquipmentStats
            {
                BaseWeaponPower = Mathf.CeilToInt(tmp.TotalWeaponPower / pCount),
                BaseWeaponAim = Mathf.CeilToInt(tmp.TotalWeaponAim / pCount),
                BaseArmorPower = Mathf.CeilToInt(tmp.TotalArmorPower / pCount),
            };
        }
        catch
        {
        }

        // TODO: Need to implement an algorithm to track difficulty ratings over time, what is the best way to calculate?
        d.Rating = d.BossSkills.CombatLevel;

        if (d.Rating == 0)
        {
            // failed to generate a difficulty setting? pick random stats.
            d.BossSkills = new Skills
            {
                Attack = new(Random(players, x => x.Stats.Attack.Level, 1, 99)),
                Defense = new(Random(players, x => x.Stats.Defense.Level, 1, 99)),
                Strength = new(Random(players, x => x.Stats.Strength.Level, 1, 99)),
                Health = new(Random(players, x => x.Stats.Health.Level, 10, 99) * 100),
                Ranged = new(Random(players, x => x.Stats.Ranged.Level, 1, 99)),
                Magic = new(Random(players, x => x.Stats.Magic.Level, 1, 99))
            };

            d.BossEquipmentStats = new EquipmentStats
            {
                BaseWeaponPower = Random(players, x => x.EquipmentStats.WeaponPower, 1, 50),
                BaseWeaponAim = Random(players, x => x.EquipmentStats.WeaponAim, 1, 50),
                BaseArmorPower = Random(players, x => x.EquipmentStats.ArmorPower, 1, 50),
            };
        }

        return d;
    }

    private static int Random<T>(IReadOnlyList<T> input, System.Func<T, int> selector, int defaultMin = 1, int defaultMax = 1)
    {
        var min = input.Count > 0 ? input.Min(selector) : defaultMin;
        var max = input.Count > 0 ? input.Max(selector) : defaultMax;
        return UnityEngine.Random.Range(min, max);
    }

    private List<PlayerController> GetLowLevelPlayers(IReadOnlyList<PlayerController> playerControllers)
    {
        var list = new List<PlayerController>();
        if (playerControllers.Count == 0)
        {
            return list; // we really cant do anything
        }

        // take 75% or minimum 5, if enough players exists of the lowest level ones. To simplify things we will sort them by combat level
        var sorted = playerControllers.OrderBy(x => x.Stats.CombatLevel);
        var toTake = Mathf.Max(1, Mathf.Min(5, (int)(playerControllers.Count * 0.75)));
        return sorted.Take(toTake).ToList();
    }

    private RaidDifficultyContext GetContext(int raidIndex)
    {
        if (!contexts.TryGetValue(raidIndex, out var ctx))
        {
            ctx = new RaidDifficultyContext();
            ctx.Difficulty = new();
            contexts[raidIndex] = ctx;
        }

        return ctx;
    }

    public void Track(int raidIndex, PlayerController player)
    {
        if (!joinedPlayers.TryGetValue(raidIndex, out var players))
        {
            players = new List<PlayerController>();
            joinedPlayers[raidIndex] = players;
        }

        // check user Id rather than player id, this will prevent a user from !leave !join with different character to "boost" boss
        if (players.Any(x => x.UserId == player.UserId))
        {
            return;
        }

        players.Add(player);
        var ctx = GetContext(raidIndex);
        ctx.Add(player);
    }

    internal void Next()
    {
        internalRaidIndex++;
        if (contexts.Count > MAX_RAID_HISTORY)
        {
            var lowestKey = contexts.Keys.OrderBy(x => x).First();
            contexts.Remove(lowestKey);
        }
    }
}

public class RaidDifficulty
{
    public int Rating { get; set; }
    /*
        The final generated stats for the raid boss
     */
    public Skills BossSkills { get; set; }
    public EquipmentStats BossEquipmentStats { get; set; }
}


public class RaidDifficultyContext
{
    public RaidDifficulty Difficulty { get; set; }

    public int PlayerCount { get; set; }

    public int TotalAttack { get; set; }
    public int TotalStrength { get; set; }
    public int TotalDefense { get; set; }
    public int TotalHealth { get; set; }
    public int TotalRanged { get; set; }
    public int TotalMagic { get; set; }

    public int TotalArmorPower { get; set; }
    public int TotalWeaponAim { get; set; }
    public int TotalWeaponPower { get; set; }

    public void Append(RaidDifficultyContext other)
    {
        PlayerCount += other.PlayerCount;
        TotalAttack += other.TotalAttack;
        TotalStrength += other.TotalStrength;
        TotalDefense += other.TotalDefense;
        TotalHealth += other.TotalHealth;
        TotalRanged += other.TotalRanged;
        TotalMagic += other.TotalMagic;
        TotalArmorPower += other.TotalArmorPower;
    }

    public void Add(PlayerController player)
    {
        PlayerCount++;

        var stats = player.Stats;
        TotalAttack += stats.Attack.Level;
        TotalDefense += stats.Defense.Level;
        TotalStrength += stats.Strength.Level;
        TotalHealth += stats.Health.Level;
        TotalMagic += stats.Magic.Level;
        TotalRanged += stats.Ranged.Level;

        var eq = player.GetEquipmentStats();
        TotalArmorPower += eq.ArmorPower;
        TotalWeaponAim += eq.WeaponAim;
        TotalWeaponPower += eq.WeaponPower;
    }
}
