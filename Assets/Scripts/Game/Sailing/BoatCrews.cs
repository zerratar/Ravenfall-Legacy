using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guests aboard own boats (docs/own-boats.md, "Guests aboard"): who has asked whom, and who sails with whom. Both must
/// agree, as with duels (<see cref="DuelHandler"/>): the owner invites (!boat invite) or a player asks to come along
/// (!boat join), and the one asked has 30 seconds to !boat accept or !boat decline. One request at a time per player.
/// </summary>
/// <remarks>
/// A crew is formed ashore, on one island, and sets out with the owner: when the owner's boat comes alongside the dock
/// every guest still on that island climbs aboard (FerryHandler). It ends when the boat is put away (landing, a raid,
/// the owner leaving), or for one guest with !boat leave. Crews live in the game only; the server hears of a guest
/// aboard (BoatsApi) for the owner's cut of their fish and the trips that earn the Ferryman titles.
/// </remarks>
public static class BoatCrews
{
    public const float RequestSeconds = 30f;

    /// <summary>Seats on each boat, the owner's included. Bigger boats may come with more.</summary>
    public static int Seats(BoatItems.Tier tier) => tier switch
    {
        BoatItems.Tier.Sloop => 4,
        BoatItems.Tier.FishingBoat => 2,
        _ => 1,
    };

    public sealed class Request
    {
        public PlayerController Owner;
        public PlayerController Guest;
        /// <summary>The owner asked (an invite); otherwise the guest asked to join.</summary>
        public bool Invite;
        public float Expires;
        public PlayerController Asker => Invite ? Owner : Guest;
        public PlayerController Asked => Invite ? Guest : Owner;
    }

    private static readonly Dictionary<PlayerController, Request> requests = new Dictionary<PlayerController, Request>();
    private static readonly Dictionary<PlayerController, List<PlayerController>> crews = new Dictionary<PlayerController, List<PlayerController>>();
    private static readonly Dictionary<PlayerController, PlayerController> guestOf = new Dictionary<PlayerController, PlayerController>();
    private static readonly List<PlayerController> scratch = new List<PlayerController>();
    private static int lastTick = -1;

    /// <summary>The boat the player has equipped, or null.</summary>
    public static GameInventoryItem BoatOf(PlayerController player) =>
        player && player.Inventory != null ? player.Inventory.GetEquipmentBySlot(ItemSlot.Boat) : null;

    public static int SeatsOf(PlayerController owner)
    {
        var boat = BoatOf(owner);
        return boat != null ? Seats(BoatItems.TierOf(boat.Item)) : 0;
    }

    /// <summary>The owner whose boat this player comes along in, ashore or aboard; null for none.</summary>
    public static PlayerController OwnerOf(PlayerController guest)
    {
        if (!guest || !guestOf.TryGetValue(guest, out var owner)) return null;
        if (owner) return owner;
        guestOf.Remove(guest);
        return null;
    }

    /// <summary>The players coming along in this owner's boat (never null).</summary>
    public static IReadOnlyList<PlayerController> GuestsOf(PlayerController owner)
    {
        if (!owner || !crews.TryGetValue(owner, out var guests)) return System.Array.Empty<PlayerController>();
        guests.RemoveAll(g => !g || g.isDestroyed);
        return guests;
    }

    public static bool HasRoom(PlayerController owner) => GuestsOf(owner).Count + 1 < SeatsOf(owner);

    public static Request PendingFor(PlayerController asked)
    {
        Tick();
        return asked && requests.TryGetValue(asked, out var r) ? r : null;
    }

    /// <summary>Whether the player has a request out or one waiting for an answer.</summary>
    public static bool Busy(PlayerController player)
    {
        Tick();
        if (!player) return false;
        if (requests.ContainsKey(player)) return true;
        foreach (var r in requests.Values) if (r.Asker == player) return true;
        return false;
    }

    public static Request Ask(PlayerController owner, PlayerController guest, bool invite)
    {
        var request = new Request { Owner = owner, Guest = guest, Invite = invite, Expires = Time.time + RequestSeconds };
        requests[request.Asked] = request;
        return request;
    }

    public static void Forget(Request request)
    {
        if (request != null && request.Asked && requests.TryGetValue(request.Asked, out var r) && r == request)
            requests.Remove(request.Asked);
    }

    /// <summary>The guest comes along in the owner's boat from now on.</summary>
    public static void Join(PlayerController owner, PlayerController guest)
    {
        Leave(guest);
        if (!crews.TryGetValue(owner, out var guests)) crews[owner] = guests = new List<PlayerController>();
        guests.Add(guest);
        guestOf[guest] = owner;
    }

    /// <summary>The guest no longer comes along; false if they were in no crew.</summary>
    public static bool Leave(PlayerController guest)
    {
        if (!guest || !guestOf.TryGetValue(guest, out var owner)) return false;
        guestOf.Remove(guest);
        if (owner && crews.TryGetValue(owner, out var guests))
        {
            guests.Remove(guest);
            if (guests.Count == 0) crews.Remove(owner);
        }
        return true;
    }

    /// <summary>Everyone leaves the owner's crew (their boat was put away).</summary>
    public static void Disband(PlayerController owner)
    {
        if (!owner || !crews.TryGetValue(owner, out var guests)) return;
        foreach (var g in guests) if (g) guestOf.Remove(g);
        crews.Remove(owner);
    }

    /// <summary>A player gone from the game takes their requests and crew places with them.</summary>
    public static void Remove(PlayerController player)
    {
        if (!player) return;
        Leave(player);
        Disband(player);
        scratch.Clear();
        foreach (var r in requests.Values) if (r.Owner == player || r.Guest == player) scratch.Add(r.Asked);
        foreach (var asked in scratch) requests.Remove(asked);
    }

    /// <summary>Unanswered requests run out once a frame: both are told.</summary>
    public static void Tick()
    {
        if (lastTick == Time.frameCount || requests.Count == 0) return;
        lastTick = Time.frameCount;
        scratch.Clear();
        foreach (var r in requests.Values)
        {
            if (!r.Owner || !r.Guest || Time.time >= r.Expires) scratch.Add(r.Asked);
        }

        foreach (var asked in scratch)
        {
            if (!requests.TryGetValue(asked, out var r)) continue;
            requests.Remove(asked);
            if (r.Asker && r.Asked) r.Asker.ferryHandler.Tell(Localization.MSG_BOAT_REQUEST_TIMEOUT, r.Asked.Name);
        }
    }
}
