// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.
using System;
using MessagePack;

namespace RavenNest.Protocol
{
    /// <summary>
    /// Client to server: something a player asked for (craft, equip, gift, vendor, ...). Repeating a request id
    /// returns the first result instead of running the command again, for <see cref="ProtocolInfo.CommandResultKeepSeconds"/>,
    /// so a client that timed out asks again with the same id; a time-out never means "it failed".
    /// </summary>
    [MessagePackObject]
    public sealed class CommandRequest : IMessage
    {
        /// <summary>From the bot when the command came from chat, so the whole chain is idempotent; otherwise made by the client.</summary>
        [Key(0)] public Guid RequestId { get; set; }

        /// <summary>The command's name, one of <see cref="Commands"/>.</summary>
        [Key(1)] public string Command { get; set; }

        /// <summary>The arguments, written with <see cref="CommandArgs"/> in the order the command defines.</summary>
        [Key(2)] public byte[] Args { get; set; }
    }

    public enum CommandStatus
    {
        Ok = 0,

        /// <summary>A long command started; its progress and outcome arrive as events.</summary>
        Accepted = 1,

        /// <summary>The command ran and said no (not enough coins, item not found, ...). <see cref="CommandResult.Error"/> says why.</summary>
        Failed = 2,

        UnknownCommand = 3,

        /// <summary>The arguments could not be read.</summary>
        BadArguments = 4,

        /// <summary>Too many commands; try again later with the same request id.</summary>
        Busy = 5,

        /// <summary>The server threw. Logged there; the client may retry with the same request id.</summary>
        Error = 6
    }

    /// <summary>Server to client: the answer to a <see cref="CommandRequest"/>.</summary>
    [MessagePackObject]
    public sealed class CommandResult : IMessage
    {
        [Key(0)] public Guid RequestId { get; set; }
        [Key(1)] public CommandStatus Status { get; set; }

        /// <summary>For the player, when the command did not succeed.</summary>
        [Key(2)] public string Error { get; set; }

        /// <summary>
        /// The command's return value, the same model type the REST endpoint returns, MessagePack contractless
        /// (<see cref="ProtocolSerializer.SerializeValue{T}"/>). Null for commands that return nothing.
        /// </summary>
        [Key(3)] public byte[] Value { get; set; }

        /// <summary>
        /// Every inventory the command changed, as the server changed it. The client applies these rather than its
        /// own arithmetic.
        /// </summary>
        [Key(4)] public InventoryUpdate[] Inventory { get; set; }
    }

    /// <summary>
    /// A character's inventory changes. <see cref="Revision"/> goes up by one per change on the server, so a gap
    /// (41 after 39) tells the client it missed one and should fetch the full inventory.
    /// </summary>
    /// <remarks>
    /// Since 0.9.6 the server sends one of these as an event (type -1) for every change to a character's inventory,
    /// whichever code made it, a few times a second at most (see <see cref="Welcome.InventoryFeed"/>). Command
    /// results still list what the command changed, without a revision (0): the event carries the change.
    /// </remarks>
    [MessagePackObject]
    public sealed class InventoryUpdate
    {
        [Key(0)] public Guid CharacterId { get; set; }
        [Key(1)] public long Revision { get; set; }

        /// <summary>The whole inventory: stacks the client has that are not listed are gone.</summary>
        [Key(2)] public bool Full { get; set; }

        /// <summary>Changed stacks; an amount of 0 means the stack is gone.</summary>
        [Key(3)] public InventoryStack[] Stacks { get; set; }

        /// <summary>
        /// The whole inventory's <see cref="InventoryChecksum"/> after these changes, or 0 from an older server. A
        /// client whose copy of the inventory adds up to something else missed a change.
        /// </summary>
        [Key(4)] public ulong Checksum { get; set; }
    }

    [MessagePackObject]
    public sealed class InventoryStack
    {
        [Key(0)] public Guid Id { get; set; }
        [Key(1)] public Guid ItemId { get; set; }
        [Key(2)] public long Amount { get; set; }
        [Key(3)] public bool Equipped { get; set; }
        [Key(4)] public bool Soulbound { get; set; }
        [Key(5)] public string Name { get; set; }
        [Key(6)] public string Enchantment { get; set; }
        [Key(7)] public string Tag { get; set; }
        [Key(8)] public Guid? TransmogrificationId { get; set; }
        [Key(9)] public int Flags { get; set; }
    }
}
