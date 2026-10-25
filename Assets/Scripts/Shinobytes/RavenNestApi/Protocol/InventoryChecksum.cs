// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.
using System;

namespace RavenNest.Protocol
{
    /// <summary>
    /// A short fingerprint of a character's inventory, the same on the server and in the game, to tell whether the
    /// two still agree without sending the inventory. Each stack counts by its id, its item and its amount (not
    /// whether it is equipped: the game equips some things on its own); stacks with no amount do not count. The
    /// stacks' values are added up, so the order does not matter and one stack can be taken out or put back in
    /// without going over the others again.
    /// </summary>
    public static class InventoryChecksum
    {
        /// <summary>What one stack adds to the checksum; 0 for an empty stack.</summary>
        public static ulong Of(Guid id, Guid itemId, long amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            Span<byte> bytes = stackalloc byte[16];
            var hash = 14695981039346656037UL; // FNV-1a
            id.TryWriteBytes(bytes);
            for (var i = 0; i < 16; ++i) hash = (hash ^ bytes[i]) * 1099511628211UL;
            itemId.TryWriteBytes(bytes);
            for (var i = 0; i < 16; ++i) hash = (hash ^ bytes[i]) * 1099511628211UL;
            for (var i = 0; i < 8; ++i) hash = (hash ^ (byte)(amount >> (8 * i))) * 1099511628211UL;

            // spread the bits (splitmix64's finish), so sums of similar stacks do not cancel out
            hash ^= hash >> 30; hash *= 0xbf58476d1ce4e5b9UL;
            hash ^= hash >> 27; hash *= 0x94d049bb133111ebUL;
            hash ^= hash >> 31;
            return hash;
        }
    }
}
