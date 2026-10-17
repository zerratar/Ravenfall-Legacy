// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.
using System;
using MessagePack;

namespace RavenNest.Protocol
{
    /// <summary>
    /// Client to server, every <see cref="ProtocolInfo.SaveIntervalMs"/>: every player whose version moved since the
    /// last version the server stored. A save is kept by the client until a <see cref="SaveAcks"/> with
    /// <see cref="SaveStage.Stored"/> covers its version, and sent again until then.
    /// </summary>
    [MessagePackObject]
    public sealed class SaveBatch : IMessage
    {
        [Key(0)] public PlayerSave[] Players { get; set; }

        /// <summary>The stream's raid and dungeon state, when it changed; otherwise null.</summary>
        [Key(1)] public GameStateSave GameState { get; set; }

        /// <summary>The last event the client applied, sent along to save a message; 0 when nothing new.</summary>
        [Key(2)] public long EventAck { get; set; }

        /// <summary>
        /// Numbered by the client from 1 per connection run, so the server can acknowledge a whole batch at once
        /// (<see cref="SaveAcks.ThroughSequence"/>) instead of every player in it.
        /// </summary>
        [Key(3)] public long Sequence { get; set; }
    }

    /// <summary>
    /// One player's changes. <see cref="Version"/> goes up whenever the player's saved state changes; the server
    /// applies a save only if its version is newer than the one it has, so repeats and reordering are harmless.
    /// </summary>
    [MessagePackObject]
    public sealed class PlayerSave
    {
        [Key(0)] public Guid CharacterId { get; set; }
        [Key(1)] public long Version { get; set; }

        /// <summary>Every skill and every state field, not just the changed ones (after a resync or a reconnect).</summary>
        [Key(2)] public bool Full { get; set; }

        /// <summary>Changed skills (all of them when <see cref="Full"/>); null when none.</summary>
        [Key(3)] public SkillSave[] Skills { get; set; }

        /// <summary>Changed state fields; null when none.</summary>
        [Key(4)] public PlayerStateSave State { get; set; }
    }

    [MessagePackObject]
    public struct SkillSave
    {
        /// <summary>The skill's index, as in the game's and the server's skill lists.</summary>
        [Key(0)] public byte Index { get; set; }
        [Key(1)] public int Level { get; set; }

        /// <summary>The experience within <see cref="Level"/>, the total the server stores.</summary>
        [Key(2)] public double Experience { get; set; }

        /// <summary>
        /// Experience gained since the last version the server acknowledged as stored. Unused for now; it lets a
        /// later server check gains against what the activity could produce, without a protocol change.
        /// </summary>
        [Key(3)] public double Gained { get; set; }
    }

    /// <summary>Which <see cref="PlayerStateSave"/> fields carry a value. The same bits as the old delta socket.</summary>
    [Flags]
    public enum PlayerStateFields : uint
    {
        None = 0,
        Health = 1 << 0,
        Island = 1 << 1,
        Destination = 1 << 2,
        State = 1 << 3,
        TrainingSkill = 1 << 4,
        TaskArgument = 1 << 5,
        ExpPerHour = 1 << 6,
        LevelUpEta = 1 << 7,
        Position = 1 << 8,
        AutoJoinRaid = 1 << 9,
        AutoJoinDungeon = 1 << 10,
        IsAutoResting = 1 << 11,
        AutoTrainLevel = 1 << 12,
        AutoRestTarget = 1 << 13,
        AutoRestStart = 1 << 14,
        DungeonStyle = 1 << 15,
        RaidStyle = 1 << 16,
        Platform = 1 << 17,
        PlatformUserId = 1 << 18,
        PlatformUserName = 1 << 19,
        All = (1u << 20) - 1
    }

    /// <summary>
    /// A player's state fields. Only the fields named in <see cref="Changed"/> mean anything; that way a field can
    /// also change to empty (a task argument cleared), which a null-means-unchanged scheme could not say.
    /// </summary>
    [MessagePackObject]
    public sealed class PlayerStateSave
    {
        [Key(0)] public PlayerStateFields Changed { get; set; }
        [Key(1)] public int Health { get; set; }

        /// <summary>The game's Island enum value.</summary>
        [Key(2)] public byte Island { get; set; }
        [Key(3)] public byte Destination { get; set; }

        /// <summary>The game's CharacterFlags value.</summary>
        [Key(4)] public int State { get; set; }
        [Key(5)] public int TrainingSkillIndex { get; set; }
        [Key(6)] public string TaskArgument { get; set; }
        [Key(7)] public long ExpPerHour { get; set; }
        [Key(8)] public DateTime EstimatedTimeForLevelUp { get; set; }
        [Key(9)] public float X { get; set; }
        [Key(10)] public float Y { get; set; }
        [Key(11)] public float Z { get; set; }
        [Key(12)] public int AutoJoinRaidCounter { get; set; }
        [Key(13)] public long AutoJoinRaidCount { get; set; }
        [Key(14)] public int AutoJoinDungeonCounter { get; set; }
        [Key(15)] public long AutoJoinDungeonCount { get; set; }
        [Key(16)] public bool IsAutoResting { get; set; }
        [Key(17)] public int AutoTrainTargetLevel { get; set; }
        [Key(18)] public double? AutoRestTarget { get; set; }
        [Key(19)] public double? AutoRestStart { get; set; }
        [Key(20)] public int? DungeonCombatStyle { get; set; }
        [Key(21)] public int? RaidCombatStyle { get; set; }
        [Key(22)] public string Platform { get; set; }
        [Key(23)] public string PlatformUserId { get; set; }
        [Key(24)] public string PlatformUserName { get; set; }

        public bool Has(PlayerStateFields field) => (Changed & field) == field;

        public PlayerStateSave Clone() => (PlayerStateSave)MemberwiseClone();
    }

    [MessagePackObject]
    public sealed class GameStateSave
    {
        [Key(0)] public int PlayerCount { get; set; }
        [Key(1)] public RaidInfo Raid { get; set; }
        [Key(2)] public DungeonInfo Dungeon { get; set; }
    }

    [MessagePackObject]
    public sealed class RaidInfo
    {
        [Key(0)] public bool IsActive { get; set; }
        [Key(1)] public int BossCombatLevel { get; set; }
        [Key(2)] public int CurrentBossHealth { get; set; }
        [Key(3)] public int MaxBossHealth { get; set; }
        [Key(4)] public int PlayersJoined { get; set; }
        [Key(5)] public DateTime EndTime { get; set; }
        [Key(6)] public DateTime NextRaid { get; set; }
    }

    [MessagePackObject]
    public sealed class DungeonInfo
    {
        [Key(0)] public bool IsActive { get; set; }
        [Key(1)] public string Name { get; set; }
        [Key(2)] public bool HasStarted { get; set; }
        [Key(3)] public int BossCombatLevel { get; set; }
        [Key(4)] public int CurrentBossHealth { get; set; }
        [Key(5)] public int MaxBossHealth { get; set; }
        [Key(6)] public int PlayersAlive { get; set; }
        [Key(7)] public int PlayersJoined { get; set; }
        [Key(8)] public int EnemiesLeft { get; set; }
        [Key(9)] public DateTime StartTime { get; set; }
        [Key(10)] public DateTime NextDungeon { get; set; }
    }

    public enum SaveStage
    {
        /// <summary>The server applied the save in memory.</summary>
        Accepted = 0,

        /// <summary>The database holds it. Only now may the client forget it.</summary>
        Stored = 1
    }

    public enum SaveStatus
    {
        Ok = 0,

        /// <summary>The server had a newer version already; treated like Ok.</summary>
        Stale = 1,

        /// <summary>The character does not exist: drop the save, do not retry.</summary>
        UnknownCharacter = 2,

        /// <summary>The character is not in this session (it moved to another stream): drop the save.</summary>
        NotInSession = 3,

        /// <summary>The save broke a rule (for example an impossible level); dropped, and logged by the server.</summary>
        Invalid = 4
    }

    /// <summary>
    /// Server to client: which player versions reached a stage.
    /// <para>
    /// <see cref="SaveStage.Accepted"/> is one message per <see cref="SaveBatch"/>: every save in batches up to
    /// <see cref="ThroughSequence"/> was applied, and <see cref="Acks"/> lists only the saves that were not
    /// (refused, with their status). <see cref="SaveStage.Stored"/> lists every player version the database now
    /// holds.
    /// </para>
    /// </summary>
    [MessagePackObject]
    public sealed class SaveAcks : IMessage
    {
        [Key(0)] public SaveStage Stage { get; set; }
        [Key(1)] public SaveAck[] Acks { get; set; }

        /// <summary>With <see cref="SaveStage.Accepted"/>: the batch this answers.</summary>
        [Key(2)] public long ThroughSequence { get; set; }
    }

    [MessagePackObject]
    public struct SaveAck
    {
        [Key(0)] public Guid CharacterId { get; set; }
        [Key(1)] public long Version { get; set; }
        [Key(2)] public SaveStatus Status { get; set; }
    }

    /// <summary>Server to client: send full saves of these players (all players when empty).</summary>
    [MessagePackObject]
    public sealed class ResyncRequired : IMessage
    {
        [Key(0)] public Guid[] CharacterIds { get; set; }
    }
}
