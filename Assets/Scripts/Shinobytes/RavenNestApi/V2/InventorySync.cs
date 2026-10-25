using System;
using System.Collections.Generic;
using RavenNest.Models;
using RavenNest.Protocol;

namespace RavenNest.SDK.V2
{
    /// <summary>
    /// Keeps each player's inventory as the server has it, from the server's inventory feed (0.9.6), and puts the
    /// game's own inventory right from that copy. Main thread only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The game changes inventories itself as things happen (an item dropped, a craft done, a sale) and the server
    /// does the same on its side; the two drifted apart whenever a change reached only one of them ("Phantom Ore 0/1",
    /// October 2026). The server now sends every change to an inventory, whichever code made it, as an
    /// <see cref="InventoryUpdate"/> event holding only the stacks that changed, with a checksum of the whole
    /// inventory (see the server's InventoryFeed). Here those build a copy of each inventory exactly as the server has
    /// it (the mirror), seeded with the inventory a player joins with.
    /// </para>
    /// <para>
    /// The stacks of each update are put into the game's inventory as well. The game's own sums may still run
    /// ahead or behind for a moment (a drop added before the server confirms it, a craft's result counted again after
    /// the update came first), so the game's inventory is compared with the mirror a few seconds after a change, by
    /// checksum, and set from the mirror when the same difference is still there a few seconds later. That costs no
    /// traffic at all.
    /// </para>
    /// <para>
    /// The mirror itself is checked against the server's checksums: with every update, and now and then without one
    /// (with the save acks). Only when it does not add up (a change missed, say over a server restart) is the whole
    /// inventory fetched (<see cref="Commands.InventoryGet"/>), at most every 30 seconds per player.
    /// </para>
    /// </remarks>
    public sealed class InventorySync
    {
        /// <summary>How long a difference must stay the same before it is put right.</summary>
        public static readonly TimeSpan Settle = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan FetchEvery = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ForgetAfter = TimeSpan.FromMinutes(2);

        /// <summary>A revision jump this large is a new server run (each numbers from the clock), not missed changes.</summary>
        private const long NewServerRunJump = 1_000_000;

        private readonly GameManager gameManager;
        private readonly CommandChannel commands;
        private readonly Dictionary<Guid, Mirror> mirrors = new Dictionary<Guid, Mirror>();
        private readonly List<InventoryItem> stackBuffer = new List<InventoryItem>();
        private readonly List<bool> followBuffer = new List<bool>();
        private DateTime nextSweep;

        public InventorySync(GameManager gameManager, CommandChannel commands)
        {
            this.gameManager = gameManager;
            this.commands = commands;
        }

        /// <summary>The server sends the feed (its Welcome said so); until then nothing here runs.</summary>
        public bool Enabled { get; set; }

        public long UpdatesApplied, LocalRepairs, Fetches, MissedUpdates;

        private sealed class Mirror
        {
            public readonly Dictionary<Guid, InventoryItem> Stacks = new Dictionary<Guid, InventoryItem>();

            /// <summary>The checksum of <see cref="Stacks"/>, kept as stacks come and go.</summary>
            public ulong Sum;

            /// <summary>The server's revision the mirror is at, 0 before the first update.</summary>
            public long Revision;

            /// <summary>The server's checksum at <see cref="Revision"/>, 0 when not known.</summary>
            public ulong ServerSum;

            /// <summary>Holds every stack: seeded when the player joined, or fetched whole.</summary>
            public bool Complete;

            /// <summary>When to compare the mirror with the server's checksum and the game's inventory with the mirror.</summary>
            public DateTime CompareAt;

            /// <summary>The difference the last comparison found between the game and the mirror, 0 for none.</summary>
            public ulong LastDifference;

            public bool FetchWanted, Fetching;
            public DateTime LastFetch;
            public DateTime LastSeen;
        }

        private Mirror Get(Guid characterId, DateTime now)
        {
            if (!mirrors.TryGetValue(characterId, out var mirror))
            {
                mirrors[characterId] = mirror = new Mirror();
            }
            mirror.LastSeen = now;
            return mirror;
        }

        /// <summary>The inventory a player joined with, as the server gave it: the start of the mirror.</summary>
        public void Seed(Guid characterId, IReadOnlyList<InventoryItem> items)
        {
            if (!Enabled || characterId == Guid.Empty || items == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var mirror = Get(characterId, now);
            mirror.Stacks.Clear();
            mirror.Sum = 0;
            foreach (var item in items)
            {
                if (item == null || item.Amount <= 0) continue;
                mirror.Stacks[item.Id] = Copy(item);
                mirror.Sum += InventoryChecksum.Of(item.Id, item.ItemId, item.Amount);
            }
            mirror.Complete = true;
            mirror.LastDifference = 0;
            mirror.CompareAt = now + Settle;
        }

        /// <summary>An inventory update from the server's feed (an event): into the mirror and the game's inventory.</summary>
        public void OnUpdate(InventoryUpdate update, DateTime now)
        {
            if (!Enabled || update == null || update.CharacterId == Guid.Empty)
            {
                return;
            }

            var mirror = Get(update.CharacterId, now);
            var missed = false;
            if (update.Revision > 0 && mirror.Revision > 0)
            {
                if (update.Revision <= mirror.Revision)
                {
                    return; // one the mirror already has (replayed after a reconnect)
                }

                var jump = update.Revision - mirror.Revision;
                if (jump > NewServerRunJump)
                {
                    missed = true; // the server restarted: what changed while it went down never came
                }
                else if (jump > 1)
                {
                    missed = true;
                    MissedUpdates += jump - 1;
                    Shinobytes.Debug.LogWarning("Missed " + (jump - 1) + " inventory update(s) for " + update.CharacterId + ".");
                }
            }

            if (update.Full)
            {
                ApplyFull(update, mirror, now);
                return;
            }

            stackBuffer.Clear();
            followBuffer.Clear();
            if (update.Stacks != null)
            {
                foreach (var s in update.Stacks)
                {
                    mirror.Stacks.TryGetValue(s.Id, out var before);
                    if (before != null) mirror.Sum -= InventoryChecksum.Of(before.Id, before.ItemId, before.Amount);

                    var after = ToItem(s);
                    if (after.Amount > 0)
                    {
                        mirror.Stacks[s.Id] = after;
                        mirror.Sum += InventoryChecksum.Of(after.Id, after.ItemId, after.Amount);
                    }
                    else
                    {
                        mirror.Stacks.Remove(s.Id);
                    }

                    stackBuffer.Add(after);
                    // the game follows the server's equipping only where the server changed it
                    followBuffer.Add(before == null || before.Equipped != after.Equipped);
                }
            }

            if (update.Revision > 0) mirror.Revision = update.Revision;
            if (update.Checksum != 0) mirror.ServerSum = update.Checksum;

            var player = gameManager.Players.GetPlayerById(update.CharacterId);
            if (player)
            {
                player.Inventory.ApplyServerStacks(stackBuffer, followBuffer);
            }
            UpdatesApplied++;

            if (missed) mirror.FetchWanted = true;
            mirror.CompareAt = now + Settle;
        }

        /// <summary>The server's checksums from the save acks: a mirror at that revision must add up to them.</summary>
        public void OnChecks(InventoryCheck[] checks, DateTime now)
        {
            if (!Enabled || checks == null)
            {
                return;
            }

            foreach (var check in checks)
            {
                if (!mirrors.TryGetValue(check.CharacterId, out var mirror) || !mirror.Complete)
                {
                    continue;
                }

                if (mirror.Revision == 0)
                {
                    mirror.Revision = check.Revision; // no update yet since the player joined: counting starts here
                }
                else if (check.Revision != mirror.Revision)
                {
                    continue; // updates still on their way (ahead), or an old check (behind)
                }

                mirror.ServerSum = check.Checksum;
                if (mirror.CompareAt == default || mirror.CompareAt > now) mirror.CompareAt = now;
            }
        }

        public void Tick(DateTime now)
        {
            if (!Enabled)
            {
                return;
            }

            foreach (var (characterId, mirror) in mirrors)
            {
                if (mirror.FetchWanted && !mirror.Fetching && now - mirror.LastFetch >= FetchEvery)
                {
                    Fetch(characterId, mirror, now);
                }

                if (mirror.CompareAt == default || now < mirror.CompareAt || !mirror.Complete)
                {
                    continue;
                }
                mirror.CompareAt = default;

                // the mirror against the server
                if (mirror.ServerSum != 0 && mirror.Sum != mirror.ServerSum)
                {
                    mirror.FetchWanted = true;
                    continue;
                }

                // the game against the mirror
                var player = gameManager.Players.GetPlayerById(characterId);
                if (!player)
                {
                    continue;
                }

                var difference = player.Inventory.Checksum() - VisibleSum(mirror);
                if (difference == 0)
                {
                    mirror.LastDifference = 0;
                }
                else if (difference == mirror.LastDifference)
                {
                    // the same difference a while later: the game counted something the server never did, or missed
                    // something it did; the mirror is the server's
                    mirror.LastDifference = 0;
                    var changed = player.Inventory.SyncWithServer(new List<InventoryItem>(mirror.Stacks.Values), matchEquipment: false);
                    LocalRepairs++;
                    Shinobytes.Debug.Log("Put the inventory of " + player.Name + " right from the server's copy: " + changed + " stack(s) changed.");
                }
                else
                {
                    // perhaps a change on its way; look again later
                    mirror.LastDifference = difference;
                    mirror.CompareAt = now + Settle;
                }
            }

            if (now >= nextSweep)
            {
                nextSweep = now.AddSeconds(30);
                Sweep(now);
            }
        }

        // the mirror's checksum over the items this game knows, as the game's inventory holds only those
        private ulong VisibleSum(Mirror mirror)
        {
            var items = gameManager.Items;
            ulong sum = 0;
            foreach (var s in mirror.Stacks.Values)
            {
                if (items == null || items.Get(s.ItemId) != null)
                {
                    sum += InventoryChecksum.Of(s.Id, s.ItemId, s.Amount);
                }
            }
            return sum;
        }

        private async void Fetch(Guid characterId, Mirror mirror, DateTime now)
        {
            mirror.Fetching = true;
            mirror.FetchWanted = false;
            mirror.LastFetch = now;
            Fetches++;
            try
            {
                var result = await commands.CallForResultAsync(Commands.InventoryGet, CommandArgs.Pack(characterId));
                var full = result?.Inventory != null && result.Inventory.Length > 0 ? result.Inventory[0] : null;
                if (full == null || !full.Full)
                {
                    Shinobytes.Debug.LogWarning("Could not fetch the inventory of " + characterId + ": " + (result == null ? "no answer" : result.Status + " " + result.Error));
                    return;
                }
                ApplyFull(full, mirror, DateTime.UtcNow);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("Could not fetch the inventory of " + characterId + ": " + exc.Message);
            }
            finally
            {
                mirror.Fetching = false;
            }
        }

        private void ApplyFull(InventoryUpdate full, Mirror mirror, DateTime now)
        {
            if (full.Revision > 0 && mirror.Revision > 0 && full.Revision < mirror.Revision && full.Revision > mirror.Revision - NewServerRunJump)
            {
                // older than updates already here: those are not kept to put on top of it, so ask again later
                mirror.FetchWanted = true;
                return;
            }

            mirror.Stacks.Clear();
            mirror.Sum = 0;
            if (full.Stacks != null)
            {
                foreach (var s in full.Stacks)
                {
                    if (s.Amount <= 0) continue;
                    mirror.Stacks[s.Id] = ToItem(s);
                    mirror.Sum += InventoryChecksum.Of(s.Id, s.ItemId, s.Amount);
                }
            }
            mirror.Revision = full.Revision;
            mirror.ServerSum = full.Checksum;
            mirror.Complete = true;
            mirror.LastDifference = 0;

            // fetched because something was missed: the game follows the server's equipment too
            var player = gameManager.Players.GetPlayerById(full.CharacterId);
            if (player)
            {
                var changed = player.Inventory.SyncWithServer(new List<InventoryItem>(mirror.Stacks.Values));
                Shinobytes.Debug.Log("Fetched the inventory of " + player.Name + " from the server: " + changed + " stack(s) changed.");
            }
            mirror.CompareAt = now + Settle;
        }

        // players who left: their mirrors go
        private void Sweep(DateTime now)
        {
            List<Guid> gone = null;
            foreach (var (characterId, mirror) in mirrors)
            {
                if (gameManager.Players.GetPlayerById(characterId))
                {
                    mirror.LastSeen = now;
                }
                else if (now - mirror.LastSeen > ForgetAfter && !mirror.Fetching)
                {
                    (gone ??= new List<Guid>()).Add(characterId);
                }
            }

            if (gone != null)
            {
                foreach (var id in gone) mirrors.Remove(id);
            }
        }

        private static InventoryItem ToItem(InventoryStack s)
        {
            return new InventoryItem
            {
                Id = s.Id,
                ItemId = s.ItemId,
                Amount = s.Amount,
                Equipped = s.Equipped,
                Soulbound = s.Soulbound,
                Name = s.Name,
                Enchantment = s.Enchantment,
                Tag = s.Tag,
                TransmogrificationId = s.TransmogrificationId,
                Flags = s.Flags
            };
        }

        private static InventoryItem Copy(InventoryItem s)
        {
            return new InventoryItem
            {
                Id = s.Id,
                ItemId = s.ItemId,
                Amount = s.Amount,
                Equipped = s.Equipped,
                Soulbound = s.Soulbound,
                Name = s.Name,
                Enchantment = s.Enchantment,
                Tag = s.Tag,
                TransmogrificationId = s.TransmogrificationId,
                Flags = s.Flags
            };
        }
    }
}
