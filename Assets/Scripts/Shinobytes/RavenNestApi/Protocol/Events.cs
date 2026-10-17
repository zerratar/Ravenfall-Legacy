// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.
using System;
using MessagePack;

namespace RavenNest.Protocol
{
    /// <summary>
    /// Server to client: events in sequence order. The server keeps each one until the client confirms it with an
    /// <see cref="EventAck"/> (or <see cref="SaveBatch.EventAck"/>), and replays the unconfirmed ones after a reconnect.
    /// </summary>
    [MessagePackObject]
    public sealed class EventBatch : IMessage
    {
        [Key(0)] public SessionEvent[] Events { get; set; }
    }

    [MessagePackObject]
    public sealed class SessionEvent
    {
        /// <summary>
        /// Numbered per session from 1 with no gaps. The client applies an event only if its sequence is above the
        /// last one it applied, which also makes a replayed event harmless.
        /// </summary>
        [Key(0)] public long Sequence { get; set; }

        /// <summary>The GameEventType, as on the old event socket.</summary>
        [Key(1)] public int Type { get; set; }

        /// <summary>The event's data, encoded as on the old event socket, so the game's existing handlers read it.</summary>
        [Key(2)] public byte[] Data { get; set; }

        /// <summary>The character the event is for, or empty. Events for a player still joining wait until the join is done.</summary>
        [Key(3)] public Guid CharacterId { get; set; }

        /// <summary>Inventory changes that came with the event (loot, a gift), as the server made them; otherwise null.</summary>
        [Key(4)] public InventoryUpdate Inventory { get; set; }
    }

    /// <summary>Client to server: every event up to and including <see cref="Sequence"/> is applied.</summary>
    [MessagePackObject]
    public sealed class EventAck : IMessage
    {
        [Key(0)] public long Sequence { get; set; }
    }
}
