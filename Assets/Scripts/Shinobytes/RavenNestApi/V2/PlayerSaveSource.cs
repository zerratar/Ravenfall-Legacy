using System;
using RavenNest.Models;
using RavenNest.Models.TcpApi;
using UnityEngine;

/// <summary>
/// What a player save holds, read from the game: one place for both the old delta socket
/// (<see cref="DeltaClientBehaviour"/>) and the v2 connection, so they always save the same thing.
/// </summary>
public struct PlayerStateSnapshot
{
    public Guid CharacterId;
    public short Health;
    public Island Island;
    public Island Destination;
    public CharacterFlags State;
    public int TrainingSkillIndex;
    public string TaskArgument;
    public long ExpPerHour;
    public DateTime EstimatedTimeForLevelUp;
    public Vector3 Position;
    public int AutoJoinRaidCounter;
    public int AutoJoinDungeonCounter;
    public long AutoJoinRaidCount;
    public long AutoJoinDungeonCount;
    public bool IsAutoResting;
    public int AutoTrainTargetLevel;
    public double? AutoRestTarget;
    public double? AutoRestStart;
    public int? DungeonCombatStyle;
    public int? RaidCombatStyle;
    public string Platform;
    public string PlatformUserId;
    public string PlatformUserName;

    /// <summary>Enemies the player helped defeat since the game started, for quests (a running count).</summary>
    public long EnemiesDefeated;
}

public static class PlayerSaveSource
{
    /// <summary>Whether the player is one whose progress is saved (not a bot, not destroyed).</summary>
    public static bool IsSaved(PlayerController p)
    {
        return p != null && !p.isDestroyed && !p.IsBot;
    }

    /// <summary>Reads the player's saved state. Also puts a player who lost track of their island back on one.</summary>
    public static PlayerStateSnapshot ReadState(GameManager gameManager, PlayerController p)
    {
        var stats = p.Stats;
        var ferryHandler = p.ferryHandler;
        var raidHandler = p.raidHandler;
        var dungeonHandler = p.dungeonHandler;
        var onsenHandler = p.onsenHandler;

        var skill = p.GetActiveSkillStat();
        var isTraining = skill != null;

        var currentIsland = p.Island;
        if (!ferryHandler.OnFerry && !dungeonHandler.InDungeon && !p.streamRaidHandler.InWar && currentIsland == null)
        {
            // we should be on an island.
            p.Island = gameManager.Islands.FindPlayerIsland(p);
        }
        var island = p.Island?.Island ?? Island.None;

        if (ferryHandler.OnFerry)
        {
            island = Island.Ferry;
            if (ferryHandler.Destination == null || ferryHandler.Destination.Island == Island.Ferry)
            {
                skill = p.Stats.Sailing;
            }
        }

        var expPerHour = 0L;
        var skillIndex = -1;
        var levelUpETA = DateTime.MaxValue;

        if (isTraining)
        {
            var v = skill.GetExperiencePerHour();
            levelUpETA = skill.GetEstimatedTimeToLevelUp();
            expPerHour = v > long.MaxValue ? long.MaxValue : (long)v;
            skillIndex = skill.Index;
        }
        else if (ferryHandler.OnFerry)
        {
            // in case we are sailing, we still want to send the sailing exp per hour.
            var v = stats.Sailing.GetExperiencePerHour();
            levelUpETA = stats.Sailing.GetEstimatedTimeToLevelUp();
            expPerHour = v > long.MaxValue ? long.MaxValue : (long)v;
        }

        var rested = p.Rested;
        var pos = p.transform.position;

        // in case we are in a dungeon, we should send the position and island of the player prior to entering the dungeon.
        // this will allow the player to return to the same position if game is restarted during the dungeon.
        if (dungeonHandler.InDungeon && dungeonHandler.PreviousIsland != null && !dungeonHandler.Ferry.OnFerry)
        {
            pos = dungeonHandler.PreviousPosition;
            island = dungeonHandler.PreviousIsland.Island;
        }

        // same with raid, in case we were not on the ferry.
        if (raidHandler.InRaid && raidHandler.PreviousIsland != null && !raidHandler.ferryState.OnFerry)
        {
            pos = raidHandler.PreviousPosition;
            island = raidHandler.PreviousIsland.Island;
        }

        return new PlayerStateSnapshot
        {
            CharacterId = p.Id,
            Health = (short)stats.Health.CurrentValue,
            Island = island,
            Destination = ferryHandler.OnFerry ? (ferryHandler.Destination?.Island ?? Island.Ferry) : Island.None,
            State = p.GetFlags(),
            TrainingSkillIndex = skillIndex,
            TaskArgument = p.taskArgument,
            ExpPerHour = expPerHour,
            EstimatedTimeForLevelUp = levelUpETA,
            Position = pos,
            AutoJoinRaidCounter = raidHandler.AutoJoinCounter,
            AutoJoinDungeonCounter = dungeonHandler.AutoJoinCounter,
            AutoJoinRaidCount = raidHandler.AutoJoinCount,
            AutoJoinDungeonCount = dungeonHandler.AutoJoinCount,
            IsAutoResting = onsenHandler.IsAutoResting,
            AutoTrainTargetLevel = p.AutoTrainTargetLevel,
            AutoRestTarget = rested.AutoRestTarget,
            AutoRestStart = rested.AutoRestStart,
            DungeonCombatStyle = AsInt(p.DungeonSkill),
            RaidCombatStyle = AsInt(p.RaidSkill),
            Platform = string.IsNullOrEmpty(p.User?.Platform) ? "twitch" : p.User.Platform,
            PlatformUserId = p.PlatformId,
            PlatformUserName = p.User?.Username ?? p.Name,
            EnemiesDefeated = p.EnemiesDefeated,
        };
    }

    private static int? AsInt(Skill? skill)
    {
        if (skill == null) return null;
        return (int)skill.Value;
    }

    /// <summary>The stream's raid and dungeon state, as the extension and website show it.</summary>
    public static Shinobytes.DeltaTcpLib.GameStateRequest ReadGameState(GameManager gameManager)
    {
        var now = DateTime.UtcNow;
        var players = gameManager.Players.GetAllRealPlayers();
        var gameStateRequest = new Shinobytes.DeltaTcpLib.GameStateRequest();
        gameStateRequest.PlayerCount = players.Count;

        var r = gameStateRequest.Raid = new RaidState();
        r.NextRaid = gameManager.Raid.SecondsUntilNextRaid >= 0 ? now.AddSeconds(gameManager.Raid.SecondsUntilNextRaid) : now;

        if (gameManager.Raid.Started)
        {
            r.IsActive = true;
            r.EndTime = gameManager.Raid.SecondsLeft >= 0 ? now.AddSeconds(gameManager.Raid.SecondsLeft) : now;

            var boss = gameManager.Raid.Boss;
            if (boss && !boss.Enemy.Stats.IsDead)
            {
                var health = boss.Enemy.Stats.Health;
                r.CurrentBossHealth = health.CurrentValue;
                r.MaxBossHealth = health.Level;
                r.BossCombatLevel = boss.Enemy.Stats.CombatLevel;
                r.PlayersJoined = gameManager.Raid.Raiders.Count;
            }
        }

        var manager = gameManager.Dungeons;
        var d = gameStateRequest.Dungeon = new DungeonState();
        d.NextDungeon = manager.SecondsUntilStart >= 0 ? now.AddSeconds(manager.SecondsUntilStart) : now;

        if (manager.Active)
        {
            var dungeon = manager.Dungeon;
            if (dungeon != null)
            {
                d.IsActive = true;
                d.Name = dungeon.Name;
                d.HasStarted = manager.Started;
                d.StartTime = now.AddSeconds(manager.SecondsUntilStart);

                d.PlayersAlive = manager.GetAlivePlayerCount();
                d.PlayersJoined = d.PlayersAlive + manager.GetDeadPlayerCount();
                d.EnemiesLeft = manager.GetAliveEnemies().Count;
                var boss = manager.Boss;
                if (boss)
                {
                    var health = boss.Enemy.Stats.Health;
                    d.CurrentBossHealth = health.CurrentValue;
                    d.MaxBossHealth = health.Level;
                    d.BossCombatLevel = boss.Enemy.Stats.CombatLevel;
                }
            }
            else
            {
#if UNITY_EDITOR
                Shinobytes.Debug.LogError("(Only logged in Editor) Potential bug: Dungeon is active but dungeon is null.");
#endif
            }
        }

        return gameStateRequest;
    }

    /// <summary>Whether the game state changed enough to be worth sending.</summary>
    public static bool HasGameStateChanged(Shinobytes.DeltaTcpLib.GameStateRequest current, Shinobytes.DeltaTcpLib.GameStateRequest previous)
    {
        if (previous == null)
            return true;

        if (current.PlayerCount != previous.PlayerCount)
            return true;

        return HasRaidStateChanged(current.Raid, previous.Raid) || HasDungeonStateChanged(current.Dungeon, previous.Dungeon);
    }

    private static bool HasRaidStateChanged(RaidState current, RaidState previous)
    {
        if (current == null || previous == null)
            return true;

        if (current.IsActive != previous.IsActive)
            return true;

        if (current.IsActive)
        {
            if (Math.Abs(current.CurrentBossHealth - previous.CurrentBossHealth) > current.MaxBossHealth * 0.05)
                return true;
            if (current.MaxBossHealth != previous.MaxBossHealth)
                return true;
            if (current.BossCombatLevel != previous.BossCombatLevel)
                return true;
            if (current.PlayersJoined != previous.PlayersJoined)
                return true;
            if (Math.Abs((current.EndTime - previous.EndTime).TotalSeconds) > 5)
                return true;
        }

        return Math.Abs((current.NextRaid - previous.NextRaid).TotalSeconds) > 30;
    }

    private static bool HasDungeonStateChanged(DungeonState current, DungeonState previous)
    {
        if (current == null || previous == null)
            return true;

        if (current.IsActive != previous.IsActive)
            return true;

        if (current.IsActive)
        {
            if (current.Name != previous.Name)
                return true;
            if (current.HasStarted != previous.HasStarted)
                return true;
            if (Math.Abs(current.CurrentBossHealth - previous.CurrentBossHealth) > current.MaxBossHealth * 0.05)
                return true;
            if (current.MaxBossHealth != previous.MaxBossHealth)
                return true;
            if (current.BossCombatLevel != previous.BossCombatLevel)
                return true;
            if (current.PlayersAlive != previous.PlayersAlive || current.PlayersJoined != previous.PlayersJoined)
                return true;
            if (current.EnemiesLeft != previous.EnemiesLeft)
                return true;
            if (Math.Abs((current.StartTime - previous.StartTime).TotalSeconds) > 5)
                return true;
        }

        return Math.Abs((current.NextDungeon - previous.NextDungeon).TotalSeconds) > 30;
    }
}
