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
    /// A gap raises <see cref="GapFound"/>; <see cref="NetworkV2"/> then fetches the character and makes the game's
    /// inventory match the server's (Inventory.SyncWithServer).
    /// </remarks>
    public sealed class InventoryRevisions
    {
        public static readonly TimeSpan GapAfter = TimeSpan.FromSeconds(10);

        /// <summary>
        /// A jump this large is a new server run, not missed changes: the server numbers each run's revisions from
        /// the clock (milliseconds times 1000), so even a quick restart lands millions above the last run.
        /// </summary>
        private const long NewServerRunJump = 1_000_000;

        private readonly Dictionary<Guid, Track> characters = new Dictionary<Guid, Track>();

        public long Gaps;

        /// <summary>A character whose inventory missed a change; the game fetches and repairs it.</summary>
        public event Action<Guid> GapFound;

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
                return; // already seen, or replayed from before a server restart
            }

            if (update.Revision > track.Contiguous + NewServerRunJump)
            {
                // the server restarted: each run numbers from the clock, far above the last run
                track.Contiguous = update.Revision;
                track.Ahead.Clear();
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
                GapFound?.Invoke(id);
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
