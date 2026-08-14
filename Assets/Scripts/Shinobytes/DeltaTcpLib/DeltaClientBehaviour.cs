
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Shinobytes.DeltaTcpLib;
using System;
using RavenNest.Models;
using RavenNest.Models.TcpApi;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

/// <summary>
/// Unity MonoBehaviour wrapper for DeltaClient.
/// Call StartConnecting() after assigning AuthToken.
/// </summary>
public class DeltaClientBehaviour : MonoBehaviour
{
    //[Header("Server Settings")]
    //public string ServerHostOverride = "127.0.0.1";
    //public int ServerPortOverride = 3921;

    [Tooltip("The maximum number of player updates to process at once")]
    public int MaxPlayerUpdates = 2048;

    // Pre-allocated buffers to avoid GC
    private DeltaExperienceUpdate[] expUpdateBuffer;
    private CharacterStateDelta[] stateUpdateBuffer;
    private SkillDelta[] skillDeltaBuffer;

    [Tooltip("The interval, in seconds, how frequently the game will be saved.")]
    public float GameSyncInterval = 2f;

    [Tooltip("Delay between connection attempts when disconnected")]
    public float ReconnectDelay = 5f;

    [Tooltip("Connection timeout in seconds")]
    public float ConnectionTimeout = 5f;

    private bool connected;
    private bool connecting;
    private bool expUpdateInProgress = false;
    private bool stateUpdateInProgress = false;

    private int lastProcessedExpPlayer = 0;
    private int lastProcessedStatePlayer = 0;

    private CancellationTokenSource connectionCts;
    private GameManager gameManager;
    private DeltaClient client;
    private DateTime lastGameSync;
    private DateTime lastReconnectAttempt;
    private Shinobytes.DeltaTcpLib.GameStateRequest lastSentGameState;


    private Dictionary<Guid, CharacterStateDelta> lastPlayerStates = new Dictionary<Guid, CharacterStateDelta>();

    private void Awake()
    {
        var skillCount = Math.Max(32, Skills.SkillTypeList.Length);
        expUpdateBuffer = new DeltaExperienceUpdate[MaxPlayerUpdates];
        stateUpdateBuffer = new CharacterStateDelta[MaxPlayerUpdates];
        skillDeltaBuffer = new SkillDelta[skillCount];
    }

    public async UniTaskVoid Update()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null) return;
        }

        if (gameManager.RavenNest != null &&
            gameManager.RavenNest.DeltaClient != null)
        {
            if (client == null)
            {
                client = gameManager.RavenNest.DeltaClient;
                client.OnConnected += OnConnected;
                client.OnDisconnected += OnDisconnected;
                client.OnConnectionError += OnConnectionError;
            }

            if (client != null && gameManager.RavenNest.SessionStarted)
            {
                if (!connected && !connecting && ShouldAttemptReconnect())
                {
                    await StartConnectingAsync();
                    return;
                }

                if (connected)
                {
                    await SyncGameAsync();
                }
            }
        }
    }

    /// <summary>
    /// Generates a binary format of the current GameState, Exp State and Player State, saves it to disk under /states/ folder and then returns the 3 binary data as one byte array in the order of 1. GS, 2. XP, 3. PS.
    /// Use this data to upload to server upon request.
    /// </summary>
    /// <returns></returns>
    public byte[] SaveStateToDisk()
    {
        // build the data blob ready to be uploaded to server all in one go just like we do with upload player log.
        // but first we want to store individual blobs on disk.
        var players = gameManager.Players.GetAllPlayers();
        var playerCount = players.Count;
        var skillCount = Math.Max(32, Skills.SkillTypeList.Length);
        var expUpdateBuffer = new DeltaExperienceUpdate[playerCount];
        var stateUpdateBuffer = new CharacterStateDelta[playerCount];
        var skillDeltaBuffer = new SkillDelta[skillCount];

        var now = DateTime.Now;
        var statesFolder = $"states";
        if (!Shinobytes.IO.Directory.Exists(statesFolder))
            Shinobytes.IO.Directory.CreateDirectory(statesFolder);

        var gsBuffer = new byte[DeltaClient.MaxGameStateSize];
        var xpBuffer = new byte[DeltaClient.GetExpBufferSize(playerCount)];
        var psBuffer = new byte[DeltaClient.GetPlayerStateBufferSize(playerCount)];
        var opBuffer = new byte[gsBuffer.Length + psBuffer.Length + xpBuffer.Length];

        // 1. Game State
        var gameState = BuildGameStateRequest();
        var gsLength = DeltaClient.Serialize(gameState, gsBuffer);
        Shinobytes.IO.File.WriteAllBytes(Shinobytes.IO.Path.Combine(statesFolder, "gs" + now.ToString("yyyyMMddHHmmss") + ".bin"), gsBuffer, 0, gsLength);
        Array.Copy(gsBuffer, 0, opBuffer, 0, gsLength);

        // 2. Experience State
        int updateCount = BuildExperienceUpdateBatch(players, expUpdateBuffer, skillDeltaBuffer, 0, playerCount, false);
        var xpLength = DeltaClient.Serialize(expUpdateBuffer, xpBuffer, updateCount);
        Shinobytes.IO.File.WriteAllBytes(Shinobytes.IO.Path.Combine(statesFolder, "xp" + now.ToString("yyyyMMddHHmmss") + ".bin"), xpBuffer, 0, xpLength);
        Array.Copy(xpBuffer, 0, opBuffer, gsLength, xpLength);

        // 3. Player State
        updateCount = BuildStateUpdateBatch(players, stateUpdateBuffer, 0, playerCount, false);
        var psLength = DeltaClient.Serialize(stateUpdateBuffer, psBuffer, updateCount);
        Shinobytes.IO.File.WriteAllBytes(Shinobytes.IO.Path.Combine(statesFolder, "ps" + now.ToString("yyyyMMddHHmmss") + ".bin"), psBuffer, 0, psLength);
        Array.Copy(psBuffer, 0, opBuffer, gsLength + xpLength, psLength);

        // save the full output buffer to disk
        Shinobytes.IO.File.WriteAllBytes(Shinobytes.IO.Path.Combine(statesFolder, "all" + now.ToString("yyyyMMddHHmmss") + ".bin"), opBuffer, 0, gsLength + xpLength + psLength);
        return opBuffer;
    }

    private bool ShouldAttemptReconnect()
    {
        return DateTime.UtcNow - lastReconnectAttempt >= TimeSpan.FromSeconds(ReconnectDelay);
    }

    private async UniTask SyncGameAsync()
    {
        var now = DateTime.UtcNow;
        if (now - lastGameSync >= TimeSpan.FromSeconds(GameSyncInterval))
        {
            lastGameSync = DateTime.UtcNow;

            var players = gameManager.Players.GetAllPlayers();

            // Process experience updates with batching support
            if (!expUpdateInProgress)
            {
                expUpdateInProgress = true;
                await ProcessAllExperienceUpdatesAsync(players);
            }

            // Process state updates with batching support
            if (!stateUpdateInProgress)
            {
                stateUpdateInProgress = true;
                await ProcessAllStateUpdatesAsync(players);
            }

            var currentGameState = BuildGameStateRequest();
            if (ShouldSendGameState(currentGameState))
            {
                await SendGameStateAsync(currentGameState);
                lastSentGameState = currentGameState;
            }
        }
    }
    private bool ShouldSendGameState(Shinobytes.DeltaTcpLib.GameStateRequest currentState)
    {
        // Always send if this is the first update
        if (lastSentGameState == null)
            return true;

        // Check if player count changed
        if (currentState.PlayerCount != lastSentGameState.PlayerCount)
            return true;

        // Check raid state changes
        if (HasRaidStateChanged(currentState.Raid, lastSentGameState.Raid))
            return true;

        // Check dungeon state changes
        if (HasDungeonStateChanged(currentState.Dungeon, lastSentGameState.Dungeon))
            return true;

        // No significant changes detected
        return false;
    }

    private bool HasRaidStateChanged(RaidState current, RaidState previous)
    {
        // If either is null, consider it a change
        if (current == null || previous == null)
            return true;

        // Active state changed
        if (current.IsActive != previous.IsActive)
            return true;

        // Only check details if the raid is active
        if (current.IsActive)
        {
            // Check health changes (significant changes only)
            if (Math.Abs(current.CurrentBossHealth - previous.CurrentBossHealth) > current.MaxBossHealth * 0.05)
                return true;

            // Check if max health changed
            if (current.MaxBossHealth != previous.MaxBossHealth)
                return true;

            // Check boss combat level
            if (current.BossCombatLevel != previous.BossCombatLevel)
                return true;

            // Check player count
            if (current.PlayersJoined != previous.PlayersJoined)
                return true;

            // Check timing changes (only if significant)
            if (Math.Abs((current.EndTime - previous.EndTime).TotalSeconds) > 5)
                return true;
        }

        // Check next raid timing (only if significant change)
        if (Math.Abs((current.NextRaid - previous.NextRaid).TotalSeconds) > 30)
            return true;

        return false;
    }

    private bool HasDungeonStateChanged(DungeonState current, DungeonState previous)
    {
        // If either is null, consider it a change
        if (current == null || previous == null)
            return true;

        // Active state changed
        if (current.IsActive != previous.IsActive)
            return true;

        // Only check details if the dungeon is active
        if (current.IsActive)
        {
            // Name changed
            if (current.Name != previous.Name)
                return true;

            // Started state changed
            if (current.HasStarted != previous.HasStarted)
                return true;

            // Check health changes (significant changes only)
            if (Math.Abs(current.CurrentBossHealth - previous.CurrentBossHealth) > current.MaxBossHealth * 0.05)
                return true;

            // Check if max health changed
            if (current.MaxBossHealth != previous.MaxBossHealth)
                return true;

            // Check boss combat level
            if (current.BossCombatLevel != previous.BossCombatLevel)
                return true;

            // Check player counts
            if (current.PlayersAlive != previous.PlayersAlive ||
                current.PlayersJoined != previous.PlayersJoined)
                return true;

            // Check enemies left
            if (current.EnemiesLeft != previous.EnemiesLeft)
                return true;

            // Check timing changes
            if (Math.Abs((current.StartTime - previous.StartTime).TotalSeconds) > 5)
                return true;
        }

        // Check next dungeon timing (only if significant change)
        if (Math.Abs((current.NextDungeon - previous.NextDungeon).TotalSeconds) > 30)
            return true;

        return false;
    }

    private async UniTask ProcessAllExperienceUpdatesAsync(IReadOnlyList<PlayerController> players)
    {
        try
        {
            // Reset the index if we've processed all players or this is a new sync cycle
            if (lastProcessedExpPlayer >= players.Count)
                lastProcessedExpPlayer = 0;

            bool hasMorePlayers = true;

            while (hasMorePlayers)
            {
                int expUpdateCount = BuildExperienceUpdateBatch(players, expUpdateBuffer, skillDeltaBuffer, lastProcessedExpPlayer, MaxPlayerUpdates);

                // Send the batch if we have updates
                if (expUpdateCount > 0)
                {
                    await SendExperienceBatch(expUpdateCount);

                    // Small delay to avoid network congestion on large batches
                    await UniTask.Delay(10);
                }

                // If we've processed all players, or there were no updates in this batch, we're done
                hasMorePlayers = (lastProcessedExpPlayer < players.Count) && (expUpdateCount > 0);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error processing player exp update: " + exc);
        }
        finally
        {
            expUpdateInProgress = false;
        }
    }

    private async UniTask ProcessAllStateUpdatesAsync(IReadOnlyList<PlayerController> players)
    {
        try
        {
            if (lastProcessedStatePlayer >= players.Count)
                lastProcessedStatePlayer = 0;

            bool hasMorePlayers = true;

            while (hasMorePlayers)
            {
                int stateUpdateCount = BuildStateUpdateBatch(players, stateUpdateBuffer, lastProcessedStatePlayer, MaxPlayerUpdates);

                if (stateUpdateCount > 0)
                    await SendStateBatch(stateUpdateCount);

                // The loop should be based ONLY on whether all players are processed
                hasMorePlayers = (lastProcessedStatePlayer < players.Count);

                await UniTask.Delay(10);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error processing state update: " + exc);
        }
        finally
        {
            stateUpdateInProgress = false;
        }
    }

    private int BuildExperienceUpdateBatch(
        IReadOnlyList<PlayerController> players,
        DeltaExperienceUpdate[] expUpdateBuffer,
        SkillDelta[] skillDeltaBuffer,
        int offset,
        int count,
        bool isDeltaUpdate = true)
    {
        int updateCount = 0;

        for (int playerIndex = offset;
             playerIndex < players.Count && updateCount < count;
             playerIndex++)
        {
            var p = players[playerIndex];
            if (p == null || p.isDestroyed || p.IsBot) continue;

            // Grab the bitmask of changed skills
            var mask = isDeltaUpdate ? p.Stats.GetDirtyMask() : 0xFFFFFFFF; // Set all bits;
            if (mask == 0) continue;

            // Build SkillDelta[] for each dirty skill
            int skillChangeCount = 0;
            var skillList = p.Stats.SkillList;

            for (int i = 0; i < skillList.Length && skillChangeCount < skillDeltaBuffer.Length; i++)
            {
                if ((mask & (1u << i)) != 0)
                {
                    var s = skillList[i];
                    skillDeltaBuffer[skillChangeCount++] = new SkillDelta
                    {
                        Index = (byte)i,
                        Experience = (long)s.Experience,
                        Level = (short)s.Level
                    };
                }
            }

            if (isDeltaUpdate)
                // Clear so we only send new dirty bits next time
                p.Stats.ClearDirtyMask();

            // Copy skill deltas to a properly sized array
            var changes = new SkillDelta[skillChangeCount];
            Array.Copy(skillDeltaBuffer, changes, skillChangeCount);

            expUpdateBuffer[updateCount++] = new DeltaExperienceUpdate
            {
                CharacterId = p.Id,
                DirtyMask = mask,
                Changes = changes
            };

            if (isDeltaUpdate)
                // Update the last processed player index
                lastProcessedExpPlayer = playerIndex + 1;
        }

        return updateCount;

    }

    private int BuildStateUpdateBatch(
        IReadOnlyList<PlayerController> players,
        CharacterStateDelta[] stateUpdateBuffer,
        int offset,
        int count,
        bool isDeltaUpdate = true)
    {
        int stateCount = 0;

        for (int i = offset; i < players.Count && stateCount < count; i++)
        {
            try
            {
                var p = players[i];
                if (p == null || p.isDestroyed || p.IsBot)
                {
                    if (isDeltaUpdate)
                        lastProcessedStatePlayer = i + 1;
                    continue;
                }

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
                var flags = p.GetFlags();
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

                // Create current state with all fields
                var currentState = new CharacterStateDelta
                {
                    CharacterId = p.Id,
                    Health = (short)stats.Health.CurrentValue,
                    Island = island,
                    Destination = ferryHandler.OnFerry ? (ferryHandler.Destination?.Island ?? Island.Ferry) : Island.None,
                    State = flags,
                    TrainingSkillIndex = skillIndex,
                    TaskArgument = p.taskArgument,
                    ExpPerHour = expPerHour,
                    EstimatedTimeForLevelUp = levelUpETA,
                    X = (short)pos.x,
                    Y = (short)pos.y,
                    Z = (short)pos.z,
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
                    PlatformUserId = p.PlatformId,
                    PlatformUserName = p.User?.Username ?? p.Name,
                };

                // Check for changes compared to last state
                uint dirtyMask = 0;

                if (isDeltaUpdate && lastPlayerStates.TryGetValue(p.Id, out var lastState))
                {
                    // Check each field for changes
                    if (currentState.Health != lastState.Health)
                        dirtyMask |= (uint)CharacterStateFields.Health;

                    if (currentState.Island != lastState.Island)
                        dirtyMask |= (uint)CharacterStateFields.Island;

                    if (currentState.Destination != lastState.Destination)
                        dirtyMask |= (uint)CharacterStateFields.Destination;

                    if (currentState.State != lastState.State)
                        dirtyMask |= (uint)CharacterStateFields.State;

                    if (currentState.TrainingSkillIndex != lastState.TrainingSkillIndex || currentState.TaskArgument != lastState.TaskArgument)
                    {
                        dirtyMask |= (uint)CharacterStateFields.TrainingSkill;
                        dirtyMask |= (uint)CharacterStateFields.TaskArgument;
                    }

                    if (currentState.ExpPerHour != lastState.ExpPerHour)
                        dirtyMask |= (uint)CharacterStateFields.ExpPerHour;

                    if (currentState.EstimatedTimeForLevelUp != lastState.EstimatedTimeForLevelUp)
                        dirtyMask |= (uint)CharacterStateFields.LevelUpETA;

                    // Position treated as one unit
                    if (currentState.X != lastState.X || currentState.Y != lastState.Y || currentState.Z != lastState.Z)
                        dirtyMask |= (uint)CharacterStateFields.Position;

                    if (currentState.AutoJoinRaidCounter != lastState.AutoJoinRaidCounter ||
                        currentState.AutoJoinRaidCount != lastState.AutoJoinRaidCount)
                        dirtyMask |= (uint)CharacterStateFields.AutoJoinRaid;

                    if (currentState.AutoJoinDungeonCounter != lastState.AutoJoinDungeonCounter ||
                        currentState.AutoJoinDungeonCount != lastState.AutoJoinDungeonCount)
                        dirtyMask |= (uint)CharacterStateFields.AutoJoinDungeon;

                    if (currentState.IsAutoResting != lastState.IsAutoResting)
                        dirtyMask |= (uint)CharacterStateFields.IsAutoResting;

                    if (currentState.AutoTrainTargetLevel != lastState.AutoTrainTargetLevel)
                        dirtyMask |= (uint)CharacterStateFields.AutoTrainLevel;

                    if (currentState.AutoRestTarget != lastState.AutoRestTarget)
                        dirtyMask |= (uint)CharacterStateFields.AutoRestTarget;

                    if (currentState.AutoRestStart != lastState.AutoRestStart)
                        dirtyMask |= (uint)CharacterStateFields.AutoRestStart;

                    if (currentState.DungeonCombatStyle != lastState.DungeonCombatStyle)
                        dirtyMask |= (uint)CharacterStateFields.DungeonStyle;

                    if (currentState.RaidCombatStyle != lastState.RaidCombatStyle)
                        dirtyMask |= (uint)CharacterStateFields.RaidStyle;

                    if ((currentState.PlatformUserId != lastState.PlatformUserId ||
                        currentState.PlatformUserName != lastState.PlatformUserName) &&
                        !string.IsNullOrEmpty(currentState.PlatformUserName) &&
                        !string.IsNullOrEmpty(currentState.PlatformUserId))
                    {
                        dirtyMask |= (uint)CharacterStateFields.Platform;
                    }
                }
                else
                {
                    // New player - all fields are dirty
                    dirtyMask = 0xFFFFFFFF; // Set all bits
                }

                // If there are changes or this is a new player
                if (dirtyMask != 0)
                {
                    // Set the dirty mask
                    currentState.DirtyMask = dirtyMask;

                    // Add to update batch
                    stateUpdateBuffer[stateCount++] = currentState;

                    if (isDeltaUpdate)
                        // Store the current state for next comparison
                        lastPlayerStates[p.Id] = currentState;
                }

                // Update the last processed player index
                if (isDeltaUpdate)
                    lastProcessedStatePlayer = i + 1;
            }
            catch (Exception ex)
            {
                Shinobytes.Debug.LogError("Error building state delta for player at index " + i + ": " + ex);
                // Always increment so we don't get stuck
                if (isDeltaUpdate)
                    lastProcessedStatePlayer = i + 1;
                continue;
            }
        }

        // Clean up states for players that no longer exist - do this once we've processed all players
        if (isDeltaUpdate && lastProcessedStatePlayer >= players.Count)
        {
            var keysToRemove = new List<Guid>();

            foreach (var key in lastPlayerStates.Keys)
            {
                bool found = false;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p != null && !p.isDestroyed && p.Id == key)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
                lastPlayerStates.Remove(key);
        }

        return stateCount;
    }


    private int? AsInt(Skill? skill)
    {
        if (skill == null) return null;
        return (int)skill.Value;
    }

    private Shinobytes.DeltaTcpLib.GameStateRequest BuildGameStateRequest()
    {
        var now = DateTime.UtcNow;
        var players = gameManager.Players.GetAllRealPlayers();
        var gameStateRequest = new Shinobytes.DeltaTcpLib.GameStateRequest();
        //gameStateRequest.SessionToken = this.sessionToken;
        gameStateRequest.PlayerCount = players.Count;

        var r = gameStateRequest.Raid = new RaidState();
        if (gameManager.Raid.SecondsUntilNextRaid >= 0)
        {
            r.NextRaid = now.AddSeconds(gameManager.Raid.SecondsUntilNextRaid);
        }
        else
        {
            r.NextRaid = now;
        }

        if (gameManager.Raid.Started)
        {
            r.IsActive = true;

            if (gameManager.Raid.SecondsLeft >= 0)
            {
                r.EndTime = now.AddSeconds(gameManager.Raid.SecondsLeft);
            }
            else
            {
                r.EndTime = now;
            }

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

        if (manager.SecondsUntilStart >= 0)
        {
            d.NextDungeon = now.AddSeconds(manager.SecondsUntilStart);
        }
        else
        {
            d.NextDungeon = now;
        }

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

    private async UniTask StartConnectingAsync()
    {
        if (connecting) return;

        lastReconnectAttempt = DateTime.UtcNow;
        connecting = true;

        try
        {
            // Create a new cancellation token for this connection attempt
            connectionCts = new CancellationTokenSource();

            // Attempt connection with timeout
            await client.ConnectAsync(connectionCts.Token);
        }
        catch (Exception ex)
        {
            Shinobytes.Debug.LogWarning($"DeltaClient connect failed: {ex.Message}");
        }
        finally
        {
            connecting = false;
            connectionCts?.Dispose();
            connectionCts = null;
        }
    }

    /// <summary>Stop connect attempts and disconnect.</summary>
    public void StopConnecting()
    {
        // Cancel any ongoing connection attempt
        connectionCts?.Cancel();

        if (client != null && (connected || connecting))
        {
            client.Disconnect();
        }

        connecting = false;
    }


    void OnConnected(SessionToken token)
    {
        connected = true;
        connecting = false;
        Shinobytes.Debug.Log($"DeltaClient connected: {token?.SessionId}");
    }

    void OnDisconnected()
    {
        connected = false;
        lastPlayerStates.Clear();
        lastSentGameState = null;
        Shinobytes.Debug.LogWarning("DeltaClient disconnected.");
    }

    void OnConnectionError(Exception ex)
    {
        connecting = false;
        Shinobytes.Debug.LogWarning($"DeltaClient connection error: {ex.Message}");
    }

    // Update the send methods to use our buffer arrays
    public async UniTask SendExperienceBatch(int count)
    {
        if (!connected || count == 0) return;
        await client.SendExperienceDeltasAsync(expUpdateBuffer, count);
    }

    public async UniTask SendStateBatch(int count)
    {
        if (!connected || count == 0) return;
        await client.SendPlayerStateAsync(stateUpdateBuffer, count);
    }

    public async UniTask SendGameStateAsync(Shinobytes.DeltaTcpLib.GameStateRequest gs)
    {
        if (!connected) return;
        await client.SendGameStateAsync(gs);
    }

    void OnDestroy()
    {
        StopConnecting();

        // Unsubscribe from events
        if (client != null)
        {
            client.OnConnected -= OnConnected;
            client.OnDisconnected -= OnDisconnected;
            client.OnConnectionError -= OnConnectionError;
        }
    }

#if UNITY_EDITOR
    public string GetStatisticsReport()
    {
        return client?.GetStatisticsReport() ?? "Client not initialized";
    }

    public void ResetStatistics()
    {
        client?.ResetStatistics();
    }
#endif
}
