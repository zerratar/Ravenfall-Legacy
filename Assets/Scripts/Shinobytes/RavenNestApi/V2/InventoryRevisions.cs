using System;
using System.Collections.Generic;
using RavenNest.Protocol;

namespace RavenNest.SDK.V2
{
    /// <summary>
    /// Follows each character's inventory revision as the server numbers it (command results and item drops on the
    /// v2 connection), to find inventory changes the game never got. Command results and events can arrive slightly
    /// out of order, so a missing revision only counts as a gap when it has not turned up within
    /// <see cref="GapAfter"/>. Main thread only.
    /// </summary>
    /// <remarks>
    /// For now gaps are counted and logged, not repaired: rebuilding an inventory in the middle of a session needs
    /// the inventory sync work (Inventory.Create only works for a freshly spawned player).
    /// </remarks>
    public sealed class InventoryRevisions
    {
        public static readonly TimeSpan GapAfter = TimeSpan.FromSeconds(10);

        private readonly Dictionary<Guid, Track> characters = new Dictionary<Guid, Track>();

        public long Gaps;

        private sealed class Track
        {
            /// <summary>Every revision up to here has been seen.</summary>
            public long Contiguous;
            public readonly SortedSet<long> Ahead = new SortedSet<long>();
            public DateTime WaitingSince;
        }

        public void Saw(InventoryUpdate update, DateTime now)
        {
            if (update == null || update.CharacterId == Guid.Empty || update.Revision <= 0)
            {
                return;
            }

            if (!characters.TryGetValue(update.CharacterId, out var track))
            {
                // the first one we see is where we start counting
                characters[update.CharacterId] = new Track { Contiguous = update.Revision };
                return;
            }

            if (update.Revision <= track.Contiguous)
            {
                if (update.Revision < track.Contiguous - 1000)
                {
                    // the server started numbering again (it restarted)
                    track.Contiguous = update.Revision;
                    track.Ahead.Clear();
                }
                return;
            }

            if (track.Ahead.Count == 0)
            {
                track.WaitingSince = now;
            }
            track.Ahead.Add(update.Revision);
            Advance(track);
        }

        /// <summary>Counts revisions that stayed missing for too long, and moves past them.</summary>
        public void Tick(DateTime now)
        {
            foreach (var (id, track) in characters)
            {
                if (track.Ahead.Count == 0 || now - track.WaitingSince < GapAfter)
                {
                    continue;
                }

                var missing = track.Ahead.Min - track.Contiguous - 1;
                Gaps += missing;
                Shinobytes.Debug.LogWarning("Missed " + missing + " inventory change(s) for " + id + " (revisions " + (track.Contiguous + 1) + " to " + (track.Ahead.Min - 1) + ").");
                track.Contiguous = track.Ahead.Min - 1;
                Advance(track);
                track.WaitingSince = now;
            }
        }

        private static void Advance(Track track)
        {
            while (track.Ahead.Count > 0 && track.Ahead.Min == track.Contiguous + 1)
            {
                track.Contiguous = track.Ahead.Min;
                track.Ahead.Remove(track.Ahead.Min);
            }
        }
    }
}
