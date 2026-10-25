using System;
using System.Collections.Generic;
using MessagePack;
using RavenNest.Protocol;

namespace RavenNest.SDK.V2
{
    /// <summary>
    /// Every player's changes the server has not stored yet. Each change gets a version; a save carries the newest
    /// values of everything unconfirmed, and is sent again until the server says that version is stored. Replaces
    /// the old delta socket's "changed" flags, which were cleared before sending, so a failed send was lost.
    /// Main thread only.
    /// </summary>
    public sealed class SaveOutbox
    {
        /// <summary>An unconfirmed save is sent again after this long without an answer.</summary>
        public static readonly TimeSpan ResendAfter = TimeSpan.FromSeconds(10);

        /// <summary>A player who left is kept this long for their last save to be stored, then given up on.</summary>
        public static readonly TimeSpan KeepLeftPlayersFor = TimeSpan.FromMinutes(10);

        private readonly Dictionary<Guid, PlayerEntry> players = new Dictionary<Guid, PlayerEntry>();
        private long lastVersion;

        public int UnconfirmedPlayers
        {
            get
            {
                var count = 0;
                foreach (var e in players.Values)
                    if (e.Unconfirmed.Count > 0) count++;
                return count;
            }
        }

        public long Resends, Dropped;

        /// <summary>
        /// Refusals in a row per player, reset by a stored save. A player in the game whose saves the server keeps
        /// refusing as not in this session (they play on another stream) is taken out by the game (NetworkV2).
        /// </summary>
        private readonly Dictionary<Guid, int> refusedInARow = new Dictionary<Guid, int>();
        private readonly HashSet<Guid> refusalLogged = new HashSet<Guid>();

        /// <summary>How many saves in a row the server refused for this player as not in this session.</summary>
        public int RefusedInARow(Guid characterId) => refusedInARow.TryGetValue(characterId, out var count) ? count : 0;

        private sealed class PlayerEntry
        {
            public readonly List<Change> Unconfirmed = new List<Change>();
            public PlayerStateSave LastState;
            public PlayerSave LastSave;
            public DateTime LastSent = DateTime.MinValue;
            public DateTime LastSeen;
            public long Stored;
        }

        private struct Change
        {
            public long Version;
            public uint Skills;
            public PlayerStateFields State;
        }

        /// <summary>
        /// Versions only ever go up, also across game restarts (they start from the clock), so a save the server
        /// already has from an earlier run can never look newer than one from this run.
        /// </summary>
        private long NextVersion()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            lastVersion = Math.Max(lastVersion + 1, now);
            return lastVersion;
        }

        /// <summary>
        /// Records what changed for a player since the last call (dirty skills and the state compared with the last
        /// one) and returns the save to send now, or null.
        /// </summary>
        public PlayerSave Track(Guid characterId, uint dirtySkills, PlayerStateSave state, Func<uint, SkillSave[]> readSkills, DateTime now)
        {
            if (!players.TryGetValue(characterId, out var entry))
            {
                players[characterId] = entry = new PlayerEntry();
            }
            entry.LastSeen = now;

            var stateChanges = Diff(entry.LastState, state);
            entry.LastState = state;

            var changed = dirtySkills != 0 || stateChanges != PlayerStateFields.None;
            if (changed)
            {
                Add(entry, new Change { Version = NextVersion(), Skills = dirtySkills, State = stateChanges });
            }

            if (entry.Unconfirmed.Count == 0)
            {
                return null;
            }

            if (!changed && now - entry.LastSent < ResendAfter)
            {
                return null;
            }

            if (!changed)
            {
                Resends++;
            }

            uint skillMask = 0;
            var stateMask = PlayerStateFields.None;
            foreach (var c in entry.Unconfirmed)
            {
                skillMask |= c.Skills;
                stateMask |= c.State;
            }

            var save = new PlayerSave
            {
                CharacterId = characterId,
                Version = entry.Unconfirmed[entry.Unconfirmed.Count - 1].Version,
                Full = skillMask == uint.MaxValue && stateMask == PlayerStateFields.All,
                Skills = skillMask != 0 ? readSkills(skillMask) : null,
                State = stateMask != PlayerStateFields.None ? WithChanged(state, stateMask) : null
            };

            entry.LastSave = save;
            entry.LastSent = now;
            return save;
        }

        /// <summary>
        /// Saves to send again for players who are gone from the game (left, kicked, the game is closing) and not
        /// stored yet: their last save, since they can no longer be read.
        /// </summary>
        public void CollectLeftPlayers(HashSet<Guid> present, DateTime now, List<PlayerSave> into)
        {
            List<Guid> forget = null;
            foreach (var (id, entry) in players)
            {
                if (present.Contains(id))
                {
                    continue;
                }

                if (entry.Unconfirmed.Count == 0 || now - entry.LastSeen > KeepLeftPlayersFor)
                {
                    if (entry.Unconfirmed.Count > 0) Dropped++;
                    (forget ??= new List<Guid>()).Add(id);
                    continue;
                }

                if (entry.LastSave != null && now - entry.LastSent >= ResendAfter)
                {
                    entry.LastSent = now;
                    Resends++;
                    into.Add(entry.LastSave);
                }
            }

            if (forget != null)
            {
                foreach (var id in forget) players.Remove(id);
            }
        }

        /// <summary>Everything not stored yet, as last sent: for a reconnect, the journal, and closing the game.</summary>
        public List<PlayerSave> Unconfirmed()
        {
            var list = new List<PlayerSave>();
            foreach (var entry in players.Values)
            {
                if (entry.Unconfirmed.Count > 0 && entry.LastSave != null)
                {
                    list.Add(entry.LastSave);
                }
            }
            return list;
        }

        /// <summary>The server lost track (new session, restart): send everyone in full again.</summary>
        public void RequireFull(Guid[] only)
        {
            foreach (var (id, entry) in players)
            {
                if (only != null && only.Length > 0 && Array.IndexOf(only, id) < 0)
                {
                    continue;
                }
                entry.LastState = null;
                Add(entry, new Change { Version = NextVersion(), Skills = uint.MaxValue, State = PlayerStateFields.All });
                entry.LastSent = DateTime.MinValue;
            }
        }

        /// <summary>Sends everything unconfirmed at the next chance, without waiting for the resend time.</summary>
        public void ResendSoon()
        {
            foreach (var entry in players.Values)
            {
                entry.LastSent = DateTime.MinValue;
            }
        }

        public void OnAcks(SaveAcks acks)
        {
            if (acks?.Acks == null)
            {
                return;
            }

            foreach (var ack in acks.Acks)
            {
                if (!players.TryGetValue(ack.CharacterId, out var entry))
                {
                    continue;
                }

                if (acks.Stage == SaveStage.Stored)
                {
                    entry.Stored = Math.Max(entry.Stored, ack.Version);
                    entry.Unconfirmed.RemoveAll(c => c.Version <= ack.Version);
                    refusedInARow.Remove(ack.CharacterId);
                }
                else if (ack.Status == SaveStatus.UnknownCharacter || ack.Status == SaveStatus.NotInSession || ack.Status == SaveStatus.Invalid)
                {
                    // the server will never take it: stop sending
                    Dropped++;
                    entry.Unconfirmed.RemoveAll(c => c.Version <= ack.Version);
                    if (ack.Status == SaveStatus.NotInSession)
                    {
                        refusedInARow[ack.CharacterId] = RefusedInARow(ack.CharacterId) + 1;
                    }

                    // once per player: a player in the game sends a save every few seconds, and each was logged
                    if (refusalLogged.Add(ack.CharacterId))
                    {
                        Shinobytes.Debug.LogWarning("The server refused a save for " + ack.CharacterId + ": " + ack.Status);
                    }
                }
            }
        }

        /// <summary>Saves read back from the journal of an earlier run; sent first, kept until stored.</summary>
        public void AddFromJournal(IEnumerable<PlayerSave> saves, DateTime now)
        {
            foreach (var save in saves)
            {
                if (save == null || players.ContainsKey(save.CharacterId))
                {
                    continue;
                }

                var entry = new PlayerEntry { LastSave = save, LastSeen = now };
                entry.Unconfirmed.Add(new Change { Version = save.Version });
                players[save.CharacterId] = entry;
                lastVersion = Math.Max(lastVersion, save.Version);
            }
        }

        /// <summary>
        /// Adds a change. Past 32 unconfirmed ones (a long time without the server) they are merged into one: it is
        /// confirmed only when the newest of them is, which is never too early.
        /// </summary>
        private static void Add(PlayerEntry entry, Change change)
        {
            if (entry.Unconfirmed.Count >= 32)
            {
                var merged = change;
                foreach (var c in entry.Unconfirmed)
                {
                    merged.Skills |= c.Skills;
                    merged.State |= c.State;
                }
                entry.Unconfirmed.Clear();
                change = merged;
            }
            entry.Unconfirmed.Add(change);
        }

        /// <summary>
        /// The fields named in <paramref name="changed"/>, and nothing else: the others go out empty, so a save does not
        /// carry every string (task argument, platform names) each time. The server reads only the named ones.
        /// </summary>
        private static PlayerStateSave WithChanged(PlayerStateSave s, PlayerStateFields changed)
        {
            bool has(PlayerStateFields f) => (changed & f) != 0;
            return new PlayerStateSave
            {
                Changed = changed,
                Health = has(PlayerStateFields.Health) ? s.Health : 0,
                Island = has(PlayerStateFields.Island) ? s.Island : (byte)0,
                Destination = has(PlayerStateFields.Destination) ? s.Destination : (byte)0,
                State = has(PlayerStateFields.State) ? s.State : 0,
                TrainingSkillIndex = has(PlayerStateFields.TrainingSkill) ? s.TrainingSkillIndex : 0,
                TaskArgument = has(PlayerStateFields.TaskArgument) ? s.TaskArgument : null,
                ExpPerHour = has(PlayerStateFields.ExpPerHour) ? s.ExpPerHour : 0,
                EstimatedTimeForLevelUp = has(PlayerStateFields.LevelUpEta) ? s.EstimatedTimeForLevelUp : default,
                X = has(PlayerStateFields.Position) ? s.X : 0,
                Y = has(PlayerStateFields.Position) ? s.Y : 0,
                Z = has(PlayerStateFields.Position) ? s.Z : 0,
                AutoJoinRaidCounter = has(PlayerStateFields.AutoJoinRaid) ? s.AutoJoinRaidCounter : 0,
                AutoJoinRaidCount = has(PlayerStateFields.AutoJoinRaid) ? s.AutoJoinRaidCount : 0,
                AutoJoinDungeonCounter = has(PlayerStateFields.AutoJoinDungeon) ? s.AutoJoinDungeonCounter : 0,
                AutoJoinDungeonCount = has(PlayerStateFields.AutoJoinDungeon) ? s.AutoJoinDungeonCount : 0,
                IsAutoResting = has(PlayerStateFields.IsAutoResting) && s.IsAutoResting,
                AutoTrainTargetLevel = has(PlayerStateFields.AutoTrainLevel) ? s.AutoTrainTargetLevel : 0,
                AutoRestTarget = has(PlayerStateFields.AutoRestTarget) ? s.AutoRestTarget : null,
                AutoRestStart = has(PlayerStateFields.AutoRestStart) ? s.AutoRestStart : null,
                DungeonCombatStyle = has(PlayerStateFields.DungeonStyle) ? s.DungeonCombatStyle : null,
                RaidCombatStyle = has(PlayerStateFields.RaidStyle) ? s.RaidCombatStyle : null,
                Platform = has(PlayerStateFields.Platform) ? s.Platform : null,
                PlatformUserId = has(PlayerStateFields.PlatformUserId) ? s.PlatformUserId : null,
                PlatformUserName = has(PlayerStateFields.PlatformUserName) ? s.PlatformUserName : null,
                EnemiesDefeated = has(PlayerStateFields.EnemiesDefeated) ? s.EnemiesDefeated : 0
            };
        }

        /// <summary>Which fields differ, grouped as the server stores them. Everything for a first save.</summary>
        public static PlayerStateFields Diff(PlayerStateSave last, PlayerStateSave now)
        {
            if (last == null)
            {
                return PlayerStateFields.All;
            }

            var d = PlayerStateFields.None;
            if (now.Health != last.Health) d |= PlayerStateFields.Health;
            if (now.Island != last.Island) d |= PlayerStateFields.Island;
            if (now.Destination != last.Destination) d |= PlayerStateFields.Destination;
            if (now.State != last.State) d |= PlayerStateFields.State;
            if (now.TrainingSkillIndex != last.TrainingSkillIndex || now.TaskArgument != last.TaskArgument)
                d |= PlayerStateFields.TrainingSkill | PlayerStateFields.TaskArgument;
            if (now.ExpPerHour != last.ExpPerHour) d |= PlayerStateFields.ExpPerHour;
            if (now.EstimatedTimeForLevelUp != last.EstimatedTimeForLevelUp) d |= PlayerStateFields.LevelUpEta;
            // half a metre, so standing still does not count as moving
            if (Math.Abs(now.X - last.X) >= 0.5f || Math.Abs(now.Y - last.Y) >= 0.5f || Math.Abs(now.Z - last.Z) >= 0.5f)
                d |= PlayerStateFields.Position;
            if (now.AutoJoinRaidCounter != last.AutoJoinRaidCounter || now.AutoJoinRaidCount != last.AutoJoinRaidCount)
                d |= PlayerStateFields.AutoJoinRaid;
            if (now.AutoJoinDungeonCounter != last.AutoJoinDungeonCounter || now.AutoJoinDungeonCount != last.AutoJoinDungeonCount)
                d |= PlayerStateFields.AutoJoinDungeon;
            if (now.IsAutoResting != last.IsAutoResting) d |= PlayerStateFields.IsAutoResting;
            if (now.AutoTrainTargetLevel != last.AutoTrainTargetLevel) d |= PlayerStateFields.AutoTrainLevel;
            if (now.AutoRestTarget != last.AutoRestTarget) d |= PlayerStateFields.AutoRestTarget;
            if (now.AutoRestStart != last.AutoRestStart) d |= PlayerStateFields.AutoRestStart;
            if (now.DungeonCombatStyle != last.DungeonCombatStyle) d |= PlayerStateFields.DungeonStyle;
            if (now.RaidCombatStyle != last.RaidCombatStyle) d |= PlayerStateFields.RaidStyle;
            if ((now.Platform != last.Platform || now.PlatformUserId != last.PlatformUserId || now.PlatformUserName != last.PlatformUserName)
                && !string.IsNullOrEmpty(now.PlatformUserId) && !string.IsNullOrEmpty(now.PlatformUserName))
                d |= PlayerStateFields.Platform | PlayerStateFields.PlatformUserId | PlayerStateFields.PlatformUserName;
            if (now.EnemiesDefeated != last.EnemiesDefeated) d |= PlayerStateFields.EnemiesDefeated;
            return d;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Journal: unconfirmed saves on disk, for a game that is killed or closed before the server stored them
        // ---------------------------------------------------------------------------------------------------------

        [MessagePackObject]
        public sealed class Journal
        {
            [Key(0)] public Guid UserId { get; set; }
            [Key(1)] public DateTime WrittenUtc { get; set; }
            [Key(2)] public PlayerSave[] Saves { get; set; }
        }

        public static void WriteJournal(string path, Guid userId, List<PlayerSave> saves)
        {
            try
            {
                if (saves.Count == 0)
                {
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    return;
                }

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)));
                var data = MessagePackSerializer.Serialize(new Journal { UserId = userId, WrittenUtc = DateTime.UtcNow, Saves = saves.ToArray() }, ProtocolSerializer.Messages);
                var temp = path + ".tmp";
                System.IO.File.WriteAllBytes(temp, data);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                System.IO.File.Move(temp, path);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("Could not write the save journal: " + exc.Message);
            }
        }

        /// <summary>The journal's saves when it belongs to this streamer and is less than a week old.</summary>
        public static PlayerSave[] ReadJournal(string path, Guid userId)
        {
            try
            {
                if (!System.IO.File.Exists(path))
                {
                    return Array.Empty<PlayerSave>();
                }

                var journal = MessagePackSerializer.Deserialize<Journal>(System.IO.File.ReadAllBytes(path), ProtocolSerializer.Messages);
                if (journal == null || journal.UserId != userId || DateTime.UtcNow - journal.WrittenUtc > TimeSpan.FromDays(7))
                {
                    return Array.Empty<PlayerSave>();
                }
                return journal.Saves ?? Array.Empty<PlayerSave>();
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("Could not read the save journal: " + exc.Message);
                return Array.Empty<PlayerSave>();
            }
        }
    }
}
