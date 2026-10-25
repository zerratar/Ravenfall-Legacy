// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.
using System;
using MessagePack;

namespace RavenNest.Protocol
{
    /// <summary>
    /// Every message on the connection: one WebSocket binary message each, serialized as a MessagePack union
    /// (<c>[type, body]</c>). The type numbers below never change meaning; new messages take new numbers, and a
    /// peer that does not know a number skips the message (see <see cref="ProtocolSerializer"/>).
    /// </summary>
    [Union(0, typeof(Hello))]
    [Union(1, typeof(Welcome))]
    [Union(2, typeof(Rejected))]
    [Union(3, typeof(Ping))]
    [Union(4, typeof(Pong))]
    [Union(10, typeof(SaveBatch))]
    [Union(11, typeof(SaveAcks))]
    [Union(12, typeof(ResyncRequired))]
    [Union(20, typeof(CommandRequest))]
    [Union(21, typeof(CommandResult))]
    [Union(30, typeof(EventBatch))]
    [Union(31, typeof(EventAck))]
    public interface IMessage
    {
    }

    // ---------------------------------------------------------------------------------------------------------
    // Session
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>Client to server, first message on every connection.</summary>
    [MessagePackObject]
    public sealed class Hello : IMessage
    {
        [Key(0)] public int ProtocolVersion { get; set; } = ProtocolInfo.Version;
        [Key(1)] public string ClientVersion { get; set; }

        /// <summary>The signed v2 session token from BeginSession.</summary>
        [Key(2)] public string Token { get; set; }

        /// <summary>
        /// Made by the client when its game session begins and kept across reconnects. The same id within a few
        /// minutes lets the server resume the session as it was, replaying events after <see cref="LastEventSequence"/>.
        /// </summary>
        [Key(3)] public Guid ResumeId { get; set; }

        /// <summary>The last event the client applied, or 0.</summary>
        [Key(4)] public long LastEventSequence { get; set; }
    }

    /// <summary>Server to client: the connection is attached to the session.</summary>
    [MessagePackObject]
    public sealed class Welcome : IMessage
    {
        [Key(0)] public Guid SessionId { get; set; }
        [Key(1)] public DateTime ServerTimeUtc { get; set; }

        /// <summary>True when the session was resumed: events after the client's last one follow.</summary>
        [Key(2)] public bool Resumed { get; set; }

        /// <summary>
        /// The server could not resume (new session, server restart without the event log, too long away): the
        /// client sends full saves of every player, as after a <see cref="ResyncRequired"/>.
        /// </summary>
        [Key(3)] public bool ResyncRequired { get; set; }

        /// <summary>
        /// The server's latest event sequence. Lower than the client's own cursor means the server started a new
        /// sequence (for example after a restart): the client resets its cursor to 0.
        /// </summary>
        [Key(4)] public long EventSequence { get; set; }

        /// <summary>
        /// The server sends every inventory change as an <see cref="InventoryUpdate"/> event, with checksums (0.9.6).
        /// The game can then keep its own copy of each inventory as the server has it and put its own right from it.
        /// </summary>
        [Key(5)] public bool InventoryFeed { get; set; }
    }

    public enum RejectReason
    {
        Unknown = 0,
        InvalidToken = 1,
        ClientTooOld = 2,
        ProtocolMismatch = 3,
        SessionEnded = 4,

        /// <summary>A newer connection for the same session took over; do not reconnect from this one.</summary>
        Replaced = 5,

        /// <summary>v2 is switched off for this streamer or everyone: use the old path.</summary>
        Disabled = 6,

        RateLimited = 7,
        ServerShuttingDown = 8,
        MessageTooLarge = 9,
        BadMessage = 10
    }

    /// <summary>Server to client, just before the server closes the connection, saying why.</summary>
    [MessagePackObject]
    public sealed class Rejected : IMessage
    {
        [Key(0)] public RejectReason Reason { get; set; }
        [Key(1)] public string Message { get; set; }

        /// <summary>Set with <see cref="RejectReason.ClientTooOld"/>.</summary>
        [Key(2)] public string ExpectedClientVersion { get; set; }

        /// <summary>Whether the client should reconnect (after its usual back-off).</summary>
        [Key(3)] public bool Reconnect { get; set; }
    }

    [MessagePackObject]
    public sealed class Ping : IMessage
    {
        /// <summary>The sender's clock (UTC ticks), returned in the <see cref="Pong"/> to measure the round trip.</summary>
        [Key(0)] public long Timestamp { get; set; }
    }

    [MessagePackObject]
    public sealed class Pong : IMessage
    {
        [Key(0)] public long Timestamp { get; set; }
    }
}
