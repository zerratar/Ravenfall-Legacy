using System;
using System.Collections.Generic;
using System.Diagnostics;
using RavenNest.Models;
using RavenNest.Protocol;

namespace RavenNest.SDK.V2
{
    /// <summary>
    /// The main thread's half of the v2 connection: saves every player through the <see cref="SaveOutbox"/>,
    /// applies events in order within a time budget per frame and confirms them, and answers commands. Driven by
    /// <see cref="Update"/> from the game's update.
    /// </summary>
    public sealed class NetworkV2
    {
        /// <summary>Time per frame for applying events, so a burst never stalls the game.</summary>
        private const double EventBudgetMs = 4;

        /// <summary>After the first Welcome, players wait this long at most for the journal's saves to be taken.</summary>
        private static readonly TimeSpan JournalWait = TimeSpan.FromSeconds(5);

        private readonly GameManager gameManager;
        private readonly RavenNestClient client;
        private readonly GameConnectionV2 connection;
        private readonly Queue<SessionEvent> events = new Queue<SessionEvent>();
        private readonly HashSet<Guid> present = new HashSet<Guid>();
        private readonly List<PlayerSave> batch = new List<PlayerSave>();
        private readonly HashSet<Guid> journalWaiting = new HashSet<Guid>();
        private readonly Stopwatch budget = new Stopwatch();
        private readonly string journalPath;

        private DateTime nextSave;
        private DateTime nextEventAck;
        private DateTime nextJournalWrite;
        private DateTime firstWelcome;
        private long lastAckedEvent;
        private long batchSequence;
        private long journalSequence;
        private Shinobytes.DeltaTcpLib.GameStateRequest lastGameState;
        private bool journalRead;

        public NetworkV2(GameManager gameManager, RavenNestClient client, GameConnectionV2 connection)
        {
            this.gameManager = gameManager;
            this.client = client;
            this.connection = connection;
            Commands = new CommandChannel(connection);
            journalPath = Shinobytes.IO.Path.Combine(Shinobytes.IO.Path.GameFolder, "states", "v2-unsaved-players.bin");
        }

        public SaveOutbox Outbox { get; } = new SaveOutbox();

        /// <summary>The server's inventory revisions per character, to spot changes the game never got.</summary>
        public InventoryRevisions Inventory { get; } = new InventoryRevisions();
        public CommandChannel Commands { get; }
        public GameConnectionV2 Connection => connection;

        public long EventsApplied;

        /// <summary>
        /// Connected, and the saves left from the last run (the journal) are with the server, so players loaded now
        /// come back with that progress.
        /// </summary>
        public bool IsReady => connection.IsReady && journalWaiting.Count == 0;

        public int UnconfirmedPlayers => Outbox.UnconfirmedPlayers;

        public void Update()
        {
            var now = DateTime.UtcNow;
            if (!journalRead && client.UserId != Guid.Empty)
            {
                journalRead = true;
                var saves = SaveOutbox.ReadJournal(journalPath, client.UserId);
                if (saves.Length > 0)
                {
                    Shinobytes.Debug.Log("Sending " + saves.Length + " player saves the server had not confirmed when the game last closed.");
                    Outbox.AddFromJournal(saves, now);
                    foreach (var s in saves) journalWaiting.Add(s.CharacterId);
                }
            }

            ReadIncoming(now);
            ApplyEvents();

            if (journalWaiting.Count > 0 && firstWelcome != default && now - firstWelcome > JournalWait)
            {
                Shinobytes.Debug.LogWarning("The server did not take " + journalWaiting.Count + " saves from the journal in time; loading players anyway.");
                journalWaiting.Clear();
            }

            if (now >= nextSave)
            {
                nextSave = now.AddMilliseconds(ProtocolInfo.SaveIntervalMs);
                SaveTick(now);
            }

            var applied = connection.LastAppliedEventSequence;
            if (applied > lastAckedEvent && now >= nextEventAck && connection.IsReady)
            {
                nextEventAck = now.AddSeconds(1);
                lastAckedEvent = applied;
                connection.Send(new EventAck { Sequence = applied });
            }

            if (now >= nextJournalWrite)
            {
                nextJournalWrite = now.AddSeconds(10);
                SaveOutbox.WriteJournal(journalPath, client.UserId, Outbox.Unconfirmed());
            }

            Commands.Tick(now);
            Inventory.Tick(now);
        }

        private void ReadIncoming(DateTime now)
        {
            budget.Restart();
            while (budget.Elapsed.TotalMilliseconds < EventBudgetMs && connection.Incoming.TryDequeue(out var message))
            {
                switch (message)
                {
                    case Welcome welcome:
                        OnWelcome(welcome, now);
                        break;
                    case SaveAcks acks:
                        Outbox.OnAcks(acks);
                        if (journalWaiting.Count > 0)
                        {
                            if (acks.Stage == SaveStage.Accepted && journalSequence > 0 && acks.ThroughSequence >= journalSequence)
                            {
                                journalWaiting.Clear(); // the batch with the journal's saves was applied
                            }
                            else if (acks.Acks != null)
                            {
                                foreach (var ack in acks.Acks) journalWaiting.Remove(ack.CharacterId);
                            }
                        }
                        break;
                    case EventBatch eventBatch:
                        if (eventBatch.Events != null)
                        {
                            foreach (var e in eventBatch.Events) events.Enqueue(e);
                        }
                        break;
                    case CommandResult result:
                        if (result.Inventory != null)
                        {
                            foreach (var update in result.Inventory) Inventory.Saw(update, now);
                        }
                        Commands.OnResult(result);
                        break;
                    case ResyncRequired resync:
                        Outbox.RequireFull(resync.CharacterIds);
                        break;
                    case Rejected rejected:
                        client.OnV2Rejected(rejected);
                        break;
                }
            }
        }

        private void OnWelcome(Welcome welcome, DateTime now)
        {
            if (firstWelcome == default)
            {
                firstWelcome = now;
            }

            // the server's numbering started over (a new session, or it lost its log): our cursor means nothing there
            if (welcome.EventSequence < connection.LastAppliedEventSequence)
            {
                connection.LastAppliedEventSequence = 0;
                lastAckedEvent = 0;
                events.Clear();
            }

            if (welcome.ResyncRequired)
            {
                Outbox.RequireFull(null);
            }
            else
            {
                Outbox.ResendSoon();
            }

            lastGameState = null;
            Commands.ResendAll();
            nextSave = now; // save right away
        }

        private void ApplyEvents()
        {
            budget.Restart();
            while (events.Count > 0 && budget.Elapsed.TotalMilliseconds < EventBudgetMs)
            {
                var e = events.Dequeue();
                if (e.Sequence <= connection.LastAppliedEventSequence)
                {
                    continue; // a replay of one we have
                }

                if (e.Type >= 0)
                {
                    try
                    {
                        gameManager.HandleGameEvent(new GameEvent { Type = e.Type, Data = e.Data, GameSessionId = client.SessionId });
                    }
                    catch (Exception exc)
                    {
                        Shinobytes.Debug.LogError("Failed to apply server event " + (GameEventType)e.Type + ": " + exc);
                    }
                }

                Inventory.Saw(e.Inventory, DateTime.UtcNow);
                connection.LastAppliedEventSequence = e.Sequence;
                EventsApplied++;
            }
        }

        private void SaveTick(DateTime now)
        {
            var players = gameManager.Players.GetAllPlayers();
            present.Clear();
            batch.Clear();

            foreach (var p in players)
            {
                if (!PlayerSaveSource.IsSaved(p))
                {
                    continue;
                }

                try
                {
                    present.Add(p.Id);
                    var dirty = p.Stats.GetDirtyMask();
                    p.Stats.ClearDirtyMask();
                    var state = ToStateSave(PlayerSaveSource.ReadState(gameManager, p));
                    var stats = p.Stats;
                    var save = Outbox.Track(p.Id, dirty, state, mask => ReadSkills(stats, mask), now);
                    if (save != null)
                    {
                        batch.Add(save);
                    }
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogError("Could not build a save for " + p?.Name + ": " + exc);
                }
            }

            Outbox.CollectLeftPlayers(present, now, batch);

            GameStateSave gameState = null;
            var currentGameState = PlayerSaveSource.ReadGameState(gameManager);
            if (PlayerSaveSource.HasGameStateChanged(currentGameState, lastGameState))
            {
                gameState = ToGameStateSave(currentGameState);
                lastGameState = currentGameState;
            }

            if (!connection.IsReady || (batch.Count == 0 && gameState == null))
            {
                return; // kept in the outbox; sent once the connection is back
            }

            // big streams go in pieces, so one message never gets huge
            const int maxPerMessage = 500;
            for (var i = 0; i < batch.Count || (i == 0 && gameState != null); i += maxPerMessage)
            {
                var count = Math.Min(maxPerMessage, batch.Count - i);
                var players2 = new PlayerSave[Math.Max(0, count)];
                if (count > 0) batch.CopyTo(i, players2, 0, count);
                connection.Send(new SaveBatch
                {
                    Players = players2,
                    GameState = i == 0 ? gameState : null,
                    EventAck = i == 0 ? connection.LastAppliedEventSequence : 0,
                    Sequence = ++batchSequence
                });
                if (count <= 0) break;
            }

            // the journal's saves went out with this tick's batches (they are players not in the game yet)
            if (journalWaiting.Count > 0 && journalSequence == 0)
            {
                journalSequence = batchSequence;
            }
        }

        private static SkillSave[] ReadSkills(global::Skills stats, uint mask)
        {
            var list = stats.SkillList;
            var result = new List<SkillSave>();
            for (var i = 0; i < list.Length && i < 32; i++)
            {
                if ((mask & (1u << i)) == 0) continue;
                var s = list[i];
                result.Add(new SkillSave { Index = (byte)i, Level = (int)s.Level, Experience = (double)s.Experience });
            }
            return result.ToArray();
        }

        private static PlayerStateSave ToStateSave(PlayerStateSnapshot s)
        {
            return new PlayerStateSave
            {
                Changed = PlayerStateFields.All,
                Health = s.Health,
                Island = (byte)s.Island,
                Destination = (byte)s.Destination,
                State = (int)s.State,
                TrainingSkillIndex = s.TrainingSkillIndex,
                TaskArgument = s.TaskArgument,
                ExpPerHour = s.ExpPerHour,
                EstimatedTimeForLevelUp = s.EstimatedTimeForLevelUp,
                X = s.Position.x,
                Y = s.Position.y,
                Z = s.Position.z,
                AutoJoinRaidCounter = s.AutoJoinRaidCounter,
                AutoJoinRaidCount = s.AutoJoinRaidCount,
                AutoJoinDungeonCounter = s.AutoJoinDungeonCounter,
                AutoJoinDungeonCount = s.AutoJoinDungeonCount,
                IsAutoResting = s.IsAutoResting,
                AutoTrainTargetLevel = s.AutoTrainTargetLevel,
                AutoRestTarget = s.AutoRestTarget,
                AutoRestStart = s.AutoRestStart,
                DungeonCombatStyle = s.DungeonCombatStyle,
                RaidCombatStyle = s.RaidCombatStyle,
                Platform = s.Platform,
                PlatformUserId = s.PlatformUserId,
                PlatformUserName = s.PlatformUserName
            };
        }

        private static GameStateSave ToGameStateSave(Shinobytes.DeltaTcpLib.GameStateRequest g)
        {
            return new GameStateSave
            {
                PlayerCount = g.PlayerCount,
                Raid = g.Raid == null ? null : new RaidInfo
                {
                    IsActive = g.Raid.IsActive,
                    BossCombatLevel = g.Raid.BossCombatLevel,
                    CurrentBossHealth = g.Raid.CurrentBossHealth,
                    MaxBossHealth = g.Raid.MaxBossHealth,
                    PlayersJoined = g.Raid.PlayersJoined,
                    EndTime = g.Raid.EndTime,
                    NextRaid = g.Raid.NextRaid
                },
                Dungeon = g.Dungeon == null ? null : new DungeonInfo
                {
                    IsActive = g.Dungeon.IsActive,
                    Name = g.Dungeon.Name,
                    HasStarted = g.Dungeon.HasStarted,
                    BossCombatLevel = g.Dungeon.BossCombatLevel,
                    CurrentBossHealth = g.Dungeon.CurrentBossHealth,
                    MaxBossHealth = g.Dungeon.MaxBossHealth,
                    PlayersAlive = g.Dungeon.PlayersAlive,
                    PlayersJoined = g.Dungeon.PlayersJoined,
                    EnemiesLeft = g.Dungeon.EnemiesLeft,
                    StartTime = g.Dungeon.StartTime,
                    NextDungeon = g.Dungeon.NextDungeon
                }
            };
        }

        /// <summary>
        /// For closing the game: sends every unconfirmed save now. <see cref="UnconfirmedPlayers"/> reaching 0 means
        /// the server has stored them all.
        /// </summary>
        public void FlushForExit()
        {
            var now = DateTime.UtcNow;
            SaveTick(now);
            if (connection.IsReady)
            {
                var all = Outbox.Unconfirmed();
                for (var i = 0; i < all.Count; i += 500)
                {
                    connection.Send(new SaveBatch { Players = all.GetRange(i, Math.Min(500, all.Count - i)).ToArray(), Sequence = ++batchSequence });
                }
            }
            WriteJournal();
        }

        /// <summary>Pumps answers while the game waits to close.</summary>
        public void UpdateWhileExiting()
        {
            ReadIncoming(DateTime.UtcNow);
        }

        public void WriteJournal()
        {
            SaveOutbox.WriteJournal(journalPath, client.UserId, Outbox.Unconfirmed());
        }
    }
}
