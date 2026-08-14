using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Text;
using MessagePack;
using RavenNest.Models.TcpApi;
using RavenNest.SDK.Endpoints;
using System.Threading;
using Shinobytes.Linq;
using System.Numerics;
using Skill = RavenNest.Models.Skill;
using RavenNest.Models;
using System.IO;

namespace RavenNest.SDK
{
    /// <summary>
    ///     A “typed” packet header so we don’t do repeated TryDeserialize for each message type.
    /// </summary>
    public enum TcpMessageType : byte
    {
        None = 0,
        AuthenticationRequest,
        SaveExperienceRequest,
        SaveStateRequest,
        GameStateRequest
        // You can add others (e.g. partial updates, commands, etc.)
    }

    /// <summary>
    /// Lightweight “envelope” containing the message type, session token, and
    /// the actual payload in a serialized form. This avoids multiple deserialization attempts.
    /// </summary>
    [MessagePackObject]
    public class TypedPacket
    {
        [Key(0)]
        public TcpMessageType MessageType { get; set; }

        // We store the session token at the “envelope” level, so we can validate
        // before fully deserializing the payload (if you want).
        [Key(1)]
        public string SessionToken { get; set; }

        [Key(2)]
        public DateTimeOffset Timestamp { get; set; }

        // The “raw” payload. We’ll decode this into the correct struct/class.
        [Key(3)]
        public byte[] Payload { get; set; }
    }

    public class TcpApi : IDisposable
    {
        public const int MaxMessageSize = 2_097_152 * 10; // 20mb
        public int ServerPort = 3920;
        public const int MinDelayBetweenSaveSeconds = 2;
        private readonly GameManager gameManager;
        private readonly string server;
        private readonly ITokenProvider tokenProvider;
        private readonly Thread thread;
        private DateTime lastConnectionTry;
        private Telepathy.Client client;
        private bool connecting;
        private string sessionToken;
        private bool disposed = false;

        public bool Enabled = true;
        private Action onReconnect;
        private bool hasBeenConnected;
        private Models.TcpApi.GameStateRequest lastSentGameStateRequest;

        public bool Connected => client?.Connected ?? false;
        public bool IsReady => Connected && Enabled;


#if UNITY_EDITOR
    // Data tracking fields - only compiled in editor
    private long _totalBytesSent;
    private long _totalBytesReceived;
    private readonly Dictionary<string, long> _endpointBytesSent;
    private DateTime _trackingStartTime;
    private DateTime _lastLogTime;
    
    // Properties to expose statistics
    public long TotalBytesSent => _totalBytesSent;
    public long TotalBytesReceived => _totalBytesReceived;
    public TimeSpan TrackingDuration => DateTime.UtcNow - _trackingStartTime;
    public float BytesSentPerSecond => 
        (float)_totalBytesSent / (float)Math.Max(1, TrackingDuration.TotalSeconds);
    public Dictionary<string, long> EndpointBytesSent => 
        new Dictionary<string, long>(_endpointBytesSent);
#endif

        public TcpApi(
            GameManager gameManager,
            string tcpApiEndpoint,
            int tcpApiPort,
            ITokenProvider tokenProvider)
        {
            this.gameManager = gameManager;
            this.server = tcpApiEndpoint ?? "127.0.0.1";
            if (tcpApiPort > 0)
            {
                ServerPort = tcpApiPort;
            }
            this.tokenProvider = tokenProvider;
            this.thread = new System.Threading.Thread(Update);
            this.thread.Start();

#if UNITY_EDITOR
        // Initialize tracking in editor only
        _totalBytesSent = 0;
        _totalBytesReceived = 0;
        _trackingStartTime = DateTime.UtcNow;
        _lastLogTime = DateTime.MinValue;
        _endpointBytesSent = new Dictionary<string, long>
        {
            { "Auth", 0 },
            { "Experience", 0 },
            { "PlayerState", 0 },
            { "GameState", 0 }
        };
#endif
        }


#if UNITY_EDITOR
    // Method to get statistics as a formatted string - only in editor
    public string GetStatisticsReport()
    {
        var duration = TrackingDuration.TotalSeconds;
        var sb = new StringBuilder();
        
        sb.AppendLine("TcpApi Network Statistics:");
        sb.AppendLine($"Duration: {duration:F1} seconds");
        sb.AppendLine($"Total sent: {FormatByteSize(_totalBytesSent)} ({FormatByteSize((long)BytesSentPerSecond)}/sec)");
        
        sb.AppendLine("\nBy message type:");
        foreach (var kvp in _endpointBytesSent)
        {
            var bytesPerSec = duration > 0 ? kvp.Value / duration : 0;
            sb.AppendLine($"  {kvp.Key}: {FormatByteSize(kvp.Value)} ({FormatByteSize((long)bytesPerSec)}/sec)");
        }
        
        return sb.ToString();
    }
    
    // Format byte size to human-readable format
    private string FormatByteSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
    
    // Reset statistics
    public void ResetStatistics()
    {
        _totalBytesSent = 0;
        _totalBytesReceived = 0;
        _trackingStartTime = DateTime.UtcNow;
        
        foreach (var key in _endpointBytesSent.Keys.ToList())
        {
            _endpointBytesSent[key] = 0;
        }
    }
    
    // Periodic logging for real-time monitoring
    private void LogPeriodicStatistics()
    {
        // Log statistics every 10 seconds
        if ((DateTime.UtcNow - _lastLogTime).TotalSeconds >= 10)
        {
            _lastLogTime = DateTime.UtcNow;
            UnityEngine.Debug.Log(GetStatisticsReport());
        }
    }
    
    // Track data sent helper
    private void TrackDataSent(TcpMessageType messageType, int bytesSent)
    {
        _totalBytesSent += bytesSent;
        
        string endpointName = GetEndpointName(messageType);
        if (_endpointBytesSent.ContainsKey(endpointName))
        {
            _endpointBytesSent[endpointName] += bytesSent;
        }
        
        // Optional: Log statistics periodically
        LogPeriodicStatistics();
    }
    
    // Track data received helper
    private void TrackDataReceived(int bytesReceived)
    {
        _totalBytesReceived += bytesReceived;
    }
    
    // Helper to convert message type to string name
    private string GetEndpointName(TcpMessageType messageType)
    {
        switch (messageType)
        {
            case TcpMessageType.AuthenticationRequest: return "Auth";
            case TcpMessageType.SaveExperienceRequest: return "Experience";
            case TcpMessageType.SaveStateRequest: return "PlayerState";
            case TcpMessageType.GameStateRequest: return "GameState";
            default: return $"Unknown({messageType})";
        }
    }
#endif

        public void OnReconnect(Action onReconnect)
        {
            this.onReconnect = onReconnect;
        }


        public void Connect()
        {
            if (connecting)
                return;

            lastConnectionTry = DateTime.UtcNow;
            client = new Telepathy.Client(MaxMessageSize);
            client.OnConnected = OnClientConnected;
            client.OnDisconnected = OnClientDisconnected;
            client.OnData = (data) => OnData(data);
            client.Connect(server, ServerPort);
        }

        public void Dispose()
        {
            Disconnect();
            client = null;
            disposed = true;
        }

        public void Disconnect()
        {
            try
            {
                if (client != null && client.Connected)
                {
                    client.Disconnect();
                }
            }
            catch { }
        }

        private void Update()
        {
            while (!disposed)
            {
                try
                {
                    if (!Enabled)
                    {
                        if (Connected)
                            Disconnect();

                        Thread.Sleep(1000);
                        continue;
                    }

                    if (client != null && Connected)
                    {
                        client.Tick(1000);
                    }

                    if (tokenProvider.HasSessionToken && !Connected &&
                        (DateTime.UtcNow - lastConnectionTry) >= TimeSpan.FromSeconds(5))
                    {
                        Connect();
                    }

                    Thread.Sleep(5);
                }
                catch
                {
                    // Ignored
                }
            }
        }

        public bool Send(object data)
        {
            var packetData = MessagePackSerializer.Serialize(data, MessagePack.Resolvers.ContractlessStandardResolver.Options);
#if UNITY_EDITOR
        // Track as "Unknown" since we don't know the message type
        _totalBytesSent += packetData.Length;
#endif
            return client.Send(packetData);
        }

        /// <summary>
        /// Telepathy's callback for incoming data from server.
        /// If your server also uses typed packets to talk back,
        /// you'd parse them similarly. Here we assume the server
        /// still just sends `EventList`.
        /// </summary>
        private void OnData(ReadOnlyMemory<byte> data)
        {
            if (data.IsEmpty)
                return;

            try
            {

#if UNITY_EDITOR
            // Track bytes received
            TrackDataReceived(data.Length);
#endif

                var eventList = MessagePackSerializer.Deserialize<EventList>(
                    data,
                    MessagePack.Resolvers.ContractlessStandardResolver.Options);
                gameManager.HandleGameEvents(eventList);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Failed to deserialize server event list: " + exc);
            }
        }

        private void OnClientDisconnected()
        {
            connecting = false;
        }

        private void OnClientConnected()
        {
            connecting = false;


            // Construct sessionToken from tokenProvider
            // Optionally do a Base64 encoding if the server expects it that way
            var rawToken = tokenProvider.GetSessionToken();
            sessionToken = Base64Encode(Newtonsoft.Json.JsonConvert.SerializeObject(rawToken));

            // Build an AuthenticationRequest inside a TypedPacket
            var authReq = new AuthenticationRequest
            {
                SessionToken = sessionToken
            };

            SendTypedPacket(TcpMessageType.AuthenticationRequest, authReq);

            if (hasBeenConnected && onReconnect != null)
            {
                onReconnect();
            }
            hasBeenConnected = true;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static bool IsValidPlayer(PlayerController player)
        {
            return player != null && player && player.UserId != Guid.Empty && !string.IsNullOrEmpty(player.PlatformId) && player.Id != Guid.Empty && !player.IsBot && !player.PlatformId.StartsWith("#");
        }

        internal void SendGameState()
        {
            if (client == null || gameManager == null || gameManager.Players == null || gameManager.Dungeons == null || gameManager.Raid == null)
                return;

            try
            {

                Models.TcpApi.GameStateRequest gameStateRequest = BuildStateRequest();

                if (lastSentGameStateRequest == null || RequiresUpdate(gameStateRequest, lastSentGameStateRequest))
                {
                    SendTypedPacket(TcpMessageType.GameStateRequest, gameStateRequest);
                }
                lastSentGameStateRequest = gameStateRequest;
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Could not save game state: " + exc);
            }
        }

        private Models.TcpApi.GameStateRequest BuildStateRequest()
        {
            var now = DateTime.UtcNow;
            var players = gameManager.Players.GetAllRealPlayers();
            var gameStateRequest = new Models.TcpApi.GameStateRequest();
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

        public void SavePlayerExperience(IReadOnlyList<PlayerController> players, bool saveAllSkills = true)
        {
            var saveRequest = new SaveExperienceRequest();
            //saveRequest.SessionToken = this.sessionToken;

            var toSave = new List<ExperienceUpdate>();

            for (var i = 0; i < players.Count; i++)
            {
                var playerName = "";
                var player = players[i];
                playerName = player.Name;

                try
                {
                    var dirtyMask = player.Stats.GetDirtyMask();
                    if (dirtyMask == 0)
                    {
                        continue;
                    }

                    ExperienceUpdate update = BuildExperienceUpdateRequest(saveAllSkills, player);
                    if (RequiresUpdate(update, player.LastExperienceUpdate))
                    {
                        player.LastExperienceUpdate = update;
                        toSave.Add(update);
                        player.Stats.ClearDirtyMask();
                    }
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogError("Failed to save exp for player '" + playerName + "': " + exc);
                }
            }

            // not fancy as it will allocate. but it will add a better failsafe.
            saveRequest.ExpUpdates = toSave.ToArray();

            if (saveRequest.ExpUpdates.Length == 0)
            {
                return;
            }

            SendTypedPacket(TcpMessageType.SaveExperienceRequest, saveRequest);
        }

        private bool RequiresUpdate(ExperienceUpdate a, ExperienceUpdate b)
        {
            if (a == null || b == null) return true;
            if (a.Skills.Count != b.Skills.Count) return true;
            for (var i = 0; i < a.Skills.Count; ++i)
            {
                var sa = a.Skills[i];
                var sb = b.Skills[i];
                if (sa.Index != sb.Index || sa.Level != sb.Level || sa.Experience != sb.Experience)
                    return true;
            }
            return false;
        }

        private ExperienceUpdate BuildExperienceUpdateRequest(bool saveAllSkills, PlayerController player)
        {
            var update = new Models.TcpApi.ExperienceUpdate();
            update.CharacterId = player.Id;

            if (saveAllSkills)
            {
                update.Skills = GetSkillUpdate(player);
            }
            else
            {
                update.Skills = GetActiveTrainingSkillUpdate(player);
            }
            return update;
        }

        public void SavePlayerState(IReadOnlyList<PlayerController> players)
        {
            var saveRequest = new SaveStateRequest();
            var states = new List<Models.TcpApi.CharacterStateUpdate>();

            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var playerName = player.Name;

                try
                {
                    var update = BuildPlayerStateUpdate(player);
                    if (player.LastSavedState == null || RequiresUpdate(update, player.LastSavedState))
                    {
                        player.LastSavedState = update;
                        states.Add(update);
                    }
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogError("Could not save player state for '" + playerName + "': " + exc);
                }
            }

            if (states.Count == 0)
            {
                return;
            }

            saveRequest.StateUpdates = states.ToArray();

            SendTypedPacket(TcpMessageType.SaveStateRequest, saveRequest);
        }

        private CharacterStateUpdate BuildPlayerStateUpdate(PlayerController player)
        {
            var update = new Models.TcpApi.CharacterStateUpdate();
            (Island island, UnityEngine.Vector3 position) = GetPosition(player);
            (CharacterFlags state, string stateData) = GetFlags(player);

            update.CharacterId = player.Id;

            var skill = player.GetActiveSkillStat();
            var isTraining = skill != null;
            var expPerHour = 0L;

            // temporary work-around for overflowing exp per hour.
            if (isTraining)
            {
                var v = skill.GetExperiencePerHour();
                if (v > long.MaxValue)
                {
                    expPerHour = long.MaxValue;
                }
                else
                {
                    expPerHour = (long)v;
                }
            }

            update.TaskArgument = player.taskArgument;
            update.TrainingSkillIndex = isTraining ? skill.Index : -1;
            update.ExpPerHour = isTraining ? expPerHour : 0L;
            update.EstimatedTimeForLevelUp = isTraining ? skill.GetEstimatedTimeToLevelUp() : DateTime.MaxValue;//GetEstimatedTimeForLevelUp(update.ExpPerHour, skill.Level, skill.Experience) : DateTime.MaxValue;
            update.Health = (short)player.Stats.Health.CurrentValue;
            update.Island = island;
            update.State = state;
            update.AutoJoinDungeonCounter = player.dungeonHandler.AutoJoinCounter;
            update.AutoJoinRaidCounter = player.raidHandler.AutoJoinCounter;

            update.AutoJoinRaidCount = player.raidHandler.AutoJoinCount;
            update.AutoJoinDungeonCount = player.dungeonHandler.AutoJoinCount;
            update.IsAutoResting = player.onsenHandler.IsAutoResting;

            update.AutoTrainTargetLevel = player.AutoTrainTargetLevel;
            update.DungeonCombatStyle = AsInt(player.DungeonSkill);
            update.RaidCombatStyle = AsInt(player.RaidSkill);
            update.AutoRestTarget = player.Rested.AutoRestTarget;
            update.AutoRestStart = player.Rested.AutoRestStart;

            update.X = (short)position.x;
            update.Y = (short)position.y;
            update.Z = (short)position.z;
            update.Destination = player.ferryHandler.Destination?.Island ?? Island.Ferry;
            return update;
        }

        private int? AsInt(Skill? skill)
        {
            if (skill == null) return null;
            return (int)skill.Value;
        }

        private bool RequiresUpdate(Models.TcpApi.GameStateRequest a, Models.TcpApi.GameStateRequest b)
        {
            if (a == null || b == null) return true;
            if (a.PlayerCount != b.PlayerCount) return true;
            if (RequiresUpdate(a.Raid, b.Raid)) return true;
            if (RequiresUpdate(a.Dungeon, b.Dungeon)) return true;
            return false;
        }

        private bool RequiresUpdate(DungeonState a, DungeonState b)
        {
            if (a == null || b == null) return true;
            if (a.IsActive != b.IsActive) return true;
            if (a.PlayersAlive != b.PlayersAlive) return true;
            if (a.PlayersJoined != b.PlayersJoined) return true;
            if (a.EnemiesLeft != b.EnemiesLeft) return true;
            if (a.CurrentBossHealth != b.CurrentBossHealth) return true;
            if (a.MaxBossHealth != b.MaxBossHealth) return true;
            if (a.BossCombatLevel != b.BossCombatLevel) return true;
            if (a.HasStarted != b.HasStarted) return true;
            if (a.Name != b.Name) return true;
            if (a.StartTime != b.StartTime) return true;
            if (b.NextDungeon - a.NextDungeon >= TimeSpan.FromSeconds(30)) return true;
            return false;
        }

        private bool RequiresUpdate(RaidState a, RaidState b)
        {
            if (a == null || b == null) return true;
            if (a.IsActive != b.IsActive) return true;
            if (a.PlayersJoined != b.PlayersJoined) return true;
            if (a.CurrentBossHealth != b.CurrentBossHealth) return true;
            if (a.MaxBossHealth != b.MaxBossHealth) return true;
            if (a.BossCombatLevel != b.BossCombatLevel) return true;
            if (a.EndTime != b.EndTime) return true;
            if (b.NextRaid - a.NextRaid >= TimeSpan.FromSeconds(30)) return true;
            return false;
        }

        private bool RequiresUpdate(Models.TcpApi.CharacterStateUpdate a, Models.TcpApi.CharacterStateUpdate b)
        {
            if (a == null || b == null) return true;
            if (a.State != b.State
                || a.TrainingSkillIndex != b.TrainingSkillIndex
                || a.TaskArgument != b.TaskArgument
                || a.Health != b.Health
                || a.AutoJoinRaidCounter != b.AutoJoinRaidCounter
                || a.AutoJoinDungeonCounter != b.AutoJoinDungeonCounter
                || a.AutoTrainTargetLevel != b.AutoTrainTargetLevel
                || a.IsAutoResting != b.IsAutoResting
                || a.AutoJoinRaidCount != b.AutoJoinRaidCount
                || a.AutoJoinDungeonCount != b.AutoJoinDungeonCount
                || a.AutoRestTarget != b.AutoRestTarget
                || a.AutoRestStart != b.AutoRestStart
                || a.DungeonCombatStyle != b.DungeonCombatStyle
                || a.RaidCombatStyle != b.RaidCombatStyle
                || a.Island != b.Island
                || Distance(a.X, a.Y, a.Z, b.X, b.Y, b.Z) >= 3f
                || a.ExpPerHour != b.ExpPerHour
                || a.EstimatedTimeForLevelUp != b.EstimatedTimeForLevelUp)
                return true;

            return false;
        }

        public static float Distance(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            if (Vector.IsHardwareAccelerated)
            {
                var x = x1 - x2;
                var y = y1 - y2;
                var z = z1 - z2;
                float num = Dot(x, y, z, x, y, z);
                return (float)Math.Sqrt(num);
            }

            float num2 = x1 - x2;
            float num3 = y1 - y2;
            float num4 = z1 - z2;
            float num5 = num2 * num2 + num3 * num3 + num4 * num4;
            return (float)Math.Sqrt(num5);
        }

        public static float Dot(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            return x1 * x2 + y1 * y2 + z1 * z2;
        }

        private IReadOnlyList<SkillUpdate> GetActiveTrainingSkillUpdate(PlayerController player)
        {
            // check if we need to add Slayer and/or Sailing
            var updates = new List<SkillUpdate>();
            var sailing = GetSkillUpdate(player, Skill.Sailing);

            if (player.LastSailingSaved == null || player.LastSailingSaved.Experience != sailing.Experience || player.LastSailingSaved.Level != sailing.Level)
            {
                updates.Add(sailing);
                player.LastSailingSaved = sailing;
            }

            var slayer = GetSkillUpdate(player, Skill.Slayer);
            if (player.LastSlayerSaved == null || player.LastSlayerSaved.Experience != slayer.Experience || player.LastSlayerSaved.Level != slayer.Level)
            {
                updates.Add(slayer);
                player.LastSlayerSaved = slayer;
            }

            var activeSkill = player.GetActiveSkillStat();
            if (activeSkill == null)
            {
                return new SkillUpdate[0]; // none
            }

            if (activeSkill.Type == Skill.Health || activeSkill.Type == Skill.Melee)
            {
                updates.Add(GetSkillUpdate(player, Skill.Attack));
                updates.Add(GetSkillUpdate(player, Skill.Defense));
                updates.Add(GetSkillUpdate(player, Skill.Strength));
                updates.Add(GetSkillUpdate(player, Skill.Health));
            }
            else
            {
                updates.Add(GetSkillUpdate(player, activeSkill.Type));
                if (activeSkill.Type == Skill.Ranged || activeSkill.Type == Skill.Magic || activeSkill.Type == Skill.Healing)
                {
                    updates.Add(GetSkillUpdate(player, Skill.Health));
                }
            }

            return updates;
        }
        private SkillUpdate GetSkillUpdate(PlayerController player, Skill targetSkill)
        {
            var skill = player.GetSkill(targetSkill);
            if (skill == null)
            {
                return null;
            }

            return new SkillUpdate
            {
                Experience = skill.Experience,
                Index = (byte)skill.Index,
                Level = (short)skill.Level,
            };
        }

        private IReadOnlyList<SkillUpdate> GetSkillUpdate(PlayerController player)
        {
            var skills = player.Stats.SkillList;
            var result = new SkillUpdate[skills.Length];
            for (var i = 0; i < skills.Length; ++i)
            {
                var skill = skills[i];
                result[i] = new SkillUpdate
                {
                    Experience = skill.Experience,
                    Index = (byte)skill.Index,
                    Level = (short)skill.Level,
                };
            }
            return result;
        }

        private static (RavenNest.Models.Island, UnityEngine.Vector3) GetPosition(PlayerController player)
        {
            //var islandValue = Island.Ferry;

            var island = player.Island?.Island ?? Island.Ferry;
            var pos = player.transform.position;

            try
            {
                if (player.ferryHandler.OnFerry)
                {
                    island = Island.Ferry;
                    return (island, pos);
                }

                if (player.raidHandler.InRaid)
                {
                    pos = player.raidHandler.PreviousPosition;
                    if (player.raidHandler.PreviousIsland != null)
                    {
                        island = player.raidHandler.PreviousIsland?.Island ?? Island.Ferry;
                    }
                }

                if (player.dungeonHandler.InDungeon)
                {
                    pos = player.dungeonHandler.PreviousPosition;
                    if (player.dungeonHandler.PreviousIsland != null)
                    {
                        island = player.dungeonHandler.PreviousIsland?.Island ?? Island.Ferry;
                    }
                }
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Unable to determine player position, player name: " + player?.Name + ", error: " + exc);
            }
            return (island, pos);
        }

        private static (CharacterFlags, string) GetFlags(PlayerController player)
        {
            string stateData = null;
            var flags = CharacterFlags.None;

            try
            {
                if (player.ferryHandler.OnFerry)
                {
                    flags |= CharacterFlags.OnFerry;

                    if (player.ferryHandler.IsCaptain)
                    {
                        flags |= CharacterFlags.IsCaptain;
                    }

                    return (flags, null);
                }

                if (player.raidHandler.InRaid)
                {
                    flags |= CharacterFlags.InRaid;
                }
                if (player.dungeonHandler.InDungeon)
                {
                    flags |= CharacterFlags.InDungeon;
                    stateData = player.GameManager.Dungeons?.Dungeon?.Name;
                }
                if (player.dungeonHandler.Joined)
                {
                    flags |= CharacterFlags.InDungeonQueue;
                    stateData = player.GameManager.Dungeons?.Dungeon?.Name;
                }
                if (player.duelHandler.InDuel)
                {
                    flags |= CharacterFlags.InDuel;
                    stateData = player.duelHandler.Opponent?.Id.ToString();
                }
                if (player.streamRaidHandler.InWar)
                {
                    flags |= CharacterFlags.InStreamRaidWar;
                    stateData = player.GameManager.StreamRaid.Raider?.RaiderUserId.ToString();
                }

                if (player.arenaHandler.InArena)
                {
                    flags |= CharacterFlags.InArena;
                }

                if (player.onsenHandler.InOnsen && !player.InCombat)
                {
                    flags |= CharacterFlags.InOnsen;
                }
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Unable to determine player state, player name: " + player?.Name + ", error: " + exc);
            }

            return (flags, stateData);
        }

        private void SendTypedPacket(TcpMessageType messageType, object payload)
        {
            if (!IsReady) return;

            // 1) Serialize the payload
            var payloadBytes = MessagePackSerializer.Serialize(
                payload,
                MessagePack.Resolvers.ContractlessStandardResolver.Options);

            // 2) Build the typed packet
            var typed = new TypedPacket
            {
                MessageType = messageType,
                SessionToken = sessionToken,
                Payload = payloadBytes,
                Timestamp = DateTimeOffset.UtcNow
            };

            // 3) Serialize the typed packet
            var finalBytes = MessagePackSerializer.Serialize(
                typed,
                MessagePack.Resolvers.ContractlessStandardResolver.Options);

#if UNITY_EDITOR
        // Track bytes sent
        TrackDataSent(messageType, finalBytes.Length);
#endif

            // 4) Send over Telepathy
            client.Send(finalBytes);
        }

    }

    public enum PlayerUpdateType
    {
        Modified,
        Everything,
        Force
    }
}