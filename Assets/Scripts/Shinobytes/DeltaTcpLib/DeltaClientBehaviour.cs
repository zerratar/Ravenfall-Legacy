
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Shinobytes.DeltaTcpLib;
using System;
using RavenNest.Models;
using RavenNest.Models.TcpApi;
using System.Threading;
using System.Threading.Tasks;

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
    public int MaxPlayerUpdates = 1024;

    // Pre-allocated buffers to avoid GC
    private DeltaExperienceUpdate[] _expUpdateBuffer;
    private CharacterStateDelta[] _stateUpdateBuffer;
    private SkillDelta[] _skillDeltaBuffer;

    [Tooltip("The interval, in seconds, how frequently the game will be saved.")]
    public float GameSyncInterval = 2f;

    [Tooltip("Delay between connection attempts when disconnected")]
    public float ReconnectDelay = 5f;

    [Tooltip("Connection timeout in seconds")]
    public float ConnectionTimeout = 5f;

    private bool _connected;
    private bool _connecting;
    private bool _expUpdateInProgress = false;
    private bool _stateUpdateInProgress = false;

    private int _lastProcessedExpPlayer = 0;
    private int _lastProcessedStatePlayer = 0;

    private CancellationTokenSource _connectionCts;
    private GameManager gameManager;
    private DeltaClient _client;
    private DateTime lastGameSync;
    private DateTime lastReconnectAttempt;
    private Shinobytes.DeltaTcpLib.GameStateRequest _lastSentGameState;


    private Dictionary<Guid, CharacterStateDelta> _lastPlayerStates = new Dictionary<Guid, CharacterStateDelta>();

    private void Awake()
    {
        _expUpdateBuffer = new DeltaExperienceUpdate[MaxPlayerUpdates];
        _stateUpdateBuffer = new CharacterStateDelta[MaxPlayerUpdates];
        _skillDeltaBuffer = new SkillDelta[32]; // Assuming max 32 skills per player
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null) return;
        }

        if (gameManager.RavenNest != null &&
            gameManager.RavenNest.DeltaClient != null)
        {
            if (_client == null)
            {
                _client = gameManager.RavenNest.DeltaClient;
                _client.OnConnected += OnConnected;
                _client.OnDisconnected += OnDisconnected;
                _client.OnConnectionError += OnConnectionError;
            }

            if (_client != null && gameManager.RavenNest.SessionStarted)
            {
                if (!_connected && !_connecting && ShouldAttemptReconnect())
                {
                    StartConnectingAsync();
                    return;
                }

                if (_connected)
                {
                    SyncGame();
                }
            }
        }
    }

    private bool ShouldAttemptReconnect()
    {
        return DateTime.UtcNow - lastReconnectAttempt >= TimeSpan.FromSeconds(ReconnectDelay);
    }

    private void SyncGame()
    {
        var now = DateTime.UtcNow;
        if (now - lastGameSync >= TimeSpan.FromSeconds(GameSyncInterval))
        {
            lastGameSync = DateTime.UtcNow;

            var players = gameManager.Players.GetAllPlayers();

            // Process experience updates with batching support
            if (!_expUpdateInProgress)
            {
                _expUpdateInProgress = true;
                ProcessAllExperienceUpdates(players);
            }

            // Process state updates with batching support
            if (!_stateUpdateInProgress)
            {
                _stateUpdateInProgress = true;
                ProcessAllStateUpdates(players);
            }

            var currentGameState = BuildGameStateRequest();
            if (ShouldSendGameState(currentGameState))
            {
                SendGameState(currentGameState);
                _lastSentGameState = currentGameState;
            }
        }
    }
    private bool ShouldSendGameState(Shinobytes.DeltaTcpLib.GameStateRequest currentState)
    {
        // Always send if this is the first update
        if (_lastSentGameState == null)
            return true;

        // Check if player count changed
        if (currentState.PlayerCount != _lastSentGameState.PlayerCount)
            return true;

        // Check raid state changes
        if (HasRaidStateChanged(currentState.Raid, _lastSentGameState.Raid))
            return true;

        // Check dungeon state changes
        if (HasDungeonStateChanged(currentState.Dungeon, _lastSentGameState.Dungeon))
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

    private async void ProcessAllExperienceUpdates(IReadOnlyList<PlayerController> players)
    {
        // Reset the index if we've processed all players or this is a new sync cycle
        if (_lastProcessedExpPlayer >= players.Count)
            _lastProcessedExpPlayer = 0;

        bool hasMorePlayers = true;

        while (hasMorePlayers)
        {
            int expUpdateCount = BuildExperienceUpdateBatch(players, _lastProcessedExpPlayer);

            // Send the batch if we have updates
            if (expUpdateCount > 0)
            {
                SendExperienceBatch(expUpdateCount);

                // Small delay to avoid network congestion on large batches
                await Task.Delay(10);
            }

            // If we've processed all players, or there were no updates in this batch, we're done
            hasMorePlayers = (_lastProcessedExpPlayer < players.Count) && (expUpdateCount > 0);
        }

        _expUpdateInProgress = false;
    }

    private async void ProcessAllStateUpdates(IReadOnlyList<PlayerController> players)
    {
        // Reset the index if we've processed all players or this is a new sync cycle
        if (_lastProcessedStatePlayer >= players.Count)
            _lastProcessedStatePlayer = 0;

        bool hasMorePlayers = true;

        while (hasMorePlayers)
        {
            int stateUpdateCount = BuildStateUpdateBatch(players, _lastProcessedStatePlayer);

            // Send the batch if we have updates
            if (stateUpdateCount > 0)
            {
                SendStateBatch(stateUpdateCount);

                // Small delay to avoid network congestion on large batches
                await Task.Delay(10);
            }

            // If we've processed all players, or there were no updates in this batch, we're done
            hasMorePlayers = (_lastProcessedStatePlayer < players.Count) && (stateUpdateCount > 0);
        }

        _stateUpdateInProgress = false;
    }

    private int BuildExperienceUpdateBatch(IReadOnlyList<PlayerController> players, int startIndex)
    {
        int updateCount = 0;

        for (int playerIndex = startIndex;
             playerIndex < players.Count && updateCount < MaxPlayerUpdates;
             playerIndex++)
        {
            var p = players[playerIndex];
            if (p == null || p.isDestroyed || p.IsBot) continue;

            // Grab the bitmask of changed skills
            var mask = p.Stats.GetDirtyMask();
            if (mask == 0) continue;

            // Build SkillDelta[] for each dirty skill
            int skillChangeCount = 0;
            var skillList = p.Stats.SkillList;

            for (int i = 0; i < skillList.Length && skillChangeCount < _skillDeltaBuffer.Length; i++)
            {
                if ((mask & (1u << i)) != 0)
                {
                    var s = skillList[i];
                    _skillDeltaBuffer[skillChangeCount++] = new SkillDelta
                    {
                        Index = (byte)i,
                        Experience = (long)s.Experience,
                        Level = (short)s.Level
                    };
                }
            }

            // Clear so we only send new dirty bits next time
            p.Stats.ClearDirtyMask();

            // Copy skill deltas to a properly sized array
            var changes = new SkillDelta[skillChangeCount];
            Array.Copy(_skillDeltaBuffer, changes, skillChangeCount);

            _expUpdateBuffer[updateCount++] = new DeltaExperienceUpdate
            {
                CharacterId = p.Id,
                DirtyMask = mask,
                Changes = changes
            };

            // Update the last processed player index
            _lastProcessedExpPlayer = playerIndex + 1;
        }

        return updateCount;
    }

    private int BuildStateUpdateBatch(IReadOnlyList<PlayerController> players, int startIndex)
    {
        int stateCount = 0;

        for (int i = startIndex; i < players.Count && stateCount < MaxPlayerUpdates; i++)
        {
            var p = players[i];
            if (p == null || p.isDestroyed || p.IsBot) continue;

            var skill = p.GetActiveSkillStat();
            var isTraining = skill != null;

            var expPerHour = 0L;
            var skillIndex = -1;
            if (isTraining)
            {
                var v = skill.GetExperiencePerHour();
                expPerHour = v > long.MaxValue ? long.MaxValue : (long)v;
                skillIndex = skill.Index;
            }

            DateTime levelUpETA = isTraining ? skill.GetEstimatedTimeToLevelUp() : DateTime.MaxValue;

            // Create current state with all fields
            var currentState = new CharacterStateDelta
            {
                CharacterId = p.Id,
                Health = (short)p.Stats.Health.CurrentValue,
                Island = p.Island.Island,
                Destination = p.ferryHandler.Destination?.Island ?? Island.Ferry,
                State = p.GetFlags(),
                TrainingSkillIndex = skillIndex,
                TaskArgument = p.taskArgument,
                ExpPerHour = expPerHour,
                EstimatedTimeForLevelUp = levelUpETA,
                X = (short)p.transform.position.x,
                Y = (short)p.transform.position.y,
                Z = (short)p.transform.position.z,
                AutoJoinRaidCounter = p.raidHandler.AutoJoinCounter,
                AutoJoinDungeonCounter = p.dungeonHandler.AutoJoinCounter,
                AutoJoinRaidCount = p.raidHandler.AutoJoinCount,
                AutoJoinDungeonCount = p.dungeonHandler.AutoJoinCount,
                IsAutoResting = p.onsenHandler.IsAutoResting,
                AutoTrainTargetLevel = p.AutoTrainTargetLevel,
                AutoRestTarget = p.Rested.AutoRestTarget,
                AutoRestStart = p.Rested.AutoRestStart,
                DungeonCombatStyle = AsInt(p.DungeonSkill),
                RaidCombatStyle = AsInt(p.RaidSkill),
            };

            // Check for changes compared to last state
            uint dirtyMask = 0;

            if (_lastPlayerStates.TryGetValue(p.Id, out var lastState))
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

                if (currentState.TrainingSkillIndex != lastState.TrainingSkillIndex)
                    dirtyMask |= (uint)CharacterStateFields.TrainingSkill;

                if (currentState.TaskArgument != lastState.TaskArgument)
                    dirtyMask |= (uint)CharacterStateFields.TaskArgument;

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
                _stateUpdateBuffer[stateCount++] = currentState;

                // Store the current state for next comparison
                _lastPlayerStates[p.Id] = currentState;
            }

            // Update the last processed player index
            _lastProcessedStatePlayer = i + 1;
        }

        // Clean up states for players that no longer exist - do this once we've processed all players
        if (_lastProcessedStatePlayer >= players.Count)
        {
            var keysToRemove = new List<Guid>();

            foreach (var key in _lastPlayerStates.Keys)
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
                _lastPlayerStates.Remove(key);
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
    /// <summary>Begin connect attempts after AuthToken is set.</summary>
    public void StartConnecting()
    {
        StartConnectingAsync();
    }

    private async Task StartConnectingAsync()
    {
        if (_connecting) return;

        lastReconnectAttempt = DateTime.UtcNow;
        _connecting = true;

        try
        {
            // Create a new cancellation token for this connection attempt
            _connectionCts = new CancellationTokenSource();

            // Attempt connection with timeout
            await _client.ConnectAsync(_connectionCts.Token);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"DeltaClient connect failed: {ex.Message}");
        }
        finally
        {
            _connecting = false;
            _connectionCts?.Dispose();
            _connectionCts = null;
        }
    }

    /// <summary>Stop connect attempts and disconnect.</summary>
    public void StopConnecting()
    {
        // Cancel any ongoing connection attempt
        _connectionCts?.Cancel();

        if (_client != null && (_connected || _connecting))
        {
            _client.Disconnect();
        }

        _connecting = false;
    }


    void OnConnected(SessionToken token)
    {
        _connected = true;
        _connecting = false;
        Debug.Log($"DeltaClient connected: {token?.SessionId}");
    }

    void OnDisconnected()
    {
        _connected = false;
        _lastPlayerStates.Clear();
        _lastSentGameState = null;
        Debug.LogWarning("DeltaClient disconnected.");
    }

    void OnConnectionError(Exception ex)
    {
        _connecting = false;
        Debug.LogWarning($"DeltaClient connection error: {ex.Message}");
    }

    // Update the send methods to use our buffer arrays
    public void SendExperienceBatch(int count)
    {
        if (!_connected || count == 0) return;
        _client.SendExperienceDeltas(_expUpdateBuffer, count);
    }

    public void SendStateBatch(int count)
    {
        if (!_connected || count == 0) return;
        _client.SendPlayerState(_stateUpdateBuffer, count);
    }

    public void SendGameState(Shinobytes.DeltaTcpLib.GameStateRequest gs)
    {
        if (!_connected) return;
        _client.SendGameState(gs);
    }

    void OnDestroy()
    {
        StopConnecting();

        // Unsubscribe from events
        if (_client != null)
        {
            _client.OnConnected -= OnConnected;
            _client.OnDisconnected -= OnDisconnected;
            _client.OnConnectionError -= OnConnectionError;
        }
    }

#if UNITY_EDITOR
    public string GetStatisticsReport()
    {
        return _client?.GetStatisticsReport() ?? "Client not initialized";
    }

    public void ResetStatistics()
    {
        _client?.ResetStatistics();
    }
#endif
}
