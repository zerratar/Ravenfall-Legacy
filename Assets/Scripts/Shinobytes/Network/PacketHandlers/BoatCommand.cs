using System;
using System.Globalization;
using System.Linq;
using RavenNest.SDK.Endpoints;

/// <summary>
/// !boat (docs/own-boats.md, phase 4): with nothing more, the player's boat and where they are with it; !boat build
/// fishing boat (or sloop) has the shipwright on Away build one from crafted parts and coins; !boat list says what
/// each boat asks for. Guests ("Guests aboard"): !boat invite and !boat join ask, !boat accept and !boat decline answer,
/// !boat leave gets a guest off.
/// </summary>
public class BoatCommand : ChatBotCommandHandler<string>
{
    public BoatCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string arguments, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player)) return;

        var text = (arguments ?? "").Trim();
        var space = text.IndexOf(' ');
        var sub = (space < 0 ? text : text.Substring(0, space)).ToLowerInvariant();
        var rest = space < 0 ? "" : text.Substring(space + 1).Trim();

        try
        {
            if (sub == "build" || sub == "buy")
            {
                await Build(player, rest, gm, client);
                return;
            }

            switch (sub)
            {
                case "invite": Ask(player, rest, true, gm, client); return;
                case "join": Ask(player, rest, false, gm, client); return;
                case "accept": Accept(player, gm, client); return;
                case "decline": Decline(player, gm, client); return;
                case "leave": Leave(player, gm, client); return;
            }

            if (sub == "list" || sub == "boats" || sub == "parts")
            {
                var blueprints = await Game.RavenNest.Boats.GetBlueprintsAsync();
                client.SendReply(gm, Localization.MSG_BOAT_LIST, Describe(blueprints));
                return;
            }

            Status(player, gm, client);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("!boat failed: " + exc);
            client.SendReply(gm, Localization.MSG_BOAT_FAILED);
        }
    }

    // ---- Guests aboard (docs/own-boats.md, "Guests aboard"): both agree, as with duels ---------------------------

    /// <summary>!boat invite (the owner asks someone aboard) or !boat join (a player asks to come along).</summary>
    private void Ask(PlayerController player, string name, bool invite, GameMessage gm, GameClient client)
    {
        name = (name ?? "").Trim().TrimStart('@');
        if (name.Length == 0)
        {
            client.SendReply(gm, Localization.MSG_BOAT_CREW_USAGE);
            return;
        }

        var other = PlayerManager.GetPlayerByName(name);
        if (!other)
        {
            client.SendReply(gm, Localization.MSG_BOAT_PLAYER_NOT_FOUND, name);
            return;
        }

        if (other == player)
        {
            client.SendReply(gm, Localization.MSG_BOAT_NOT_SELF);
            return;
        }

        var owner = invite ? player : other;
        var guest = invite ? other : player;
        if (!Check(owner, guest, player, gm, client)) return;
        if (BoatCrews.Busy(other))
        {
            client.SendReply(gm, Localization.MSG_BOAT_BUSY, other.Name);
            return;
        }

        BoatCrews.Ask(owner, guest, invite);
        var boatName = BoatCrews.BoatOf(owner).Item.Name;
        if (invite)
        {
            client.SendReply(gm, Localization.MSG_BOAT_INVITE_SENT, guest.Name, boatName);
            guest.ferryHandler.Tell(Localization.MSG_BOAT_INVITE_RECEIVED, owner.Name, boatName);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_BOAT_JOIN_SENT, owner.Name, boatName);
            owner.ferryHandler.Tell(Localization.MSG_BOAT_JOIN_RECEIVED, guest.Name, boatName);
        }
    }

    private void Accept(PlayerController player, GameMessage gm, GameClient client)
    {
        var request = BoatCrews.PendingFor(player);
        if (request == null)
        {
            client.SendReply(gm, Localization.MSG_BOAT_NO_REQUEST);
            return;
        }

        BoatCrews.Forget(request);
        var owner = request.Owner;
        var guest = request.Guest;
        // things may have changed in the half minute (a seat taken, someone sailed off)
        if (!owner || !guest || !Check(owner, guest, player, gm, client)) return;

        BoatCrews.Join(owner, guest);
        var boatName = BoatCrews.BoatOf(owner).Item.Name;
        owner.ferryHandler.Tell(Localization.MSG_BOAT_ACCEPTED_OWNER, guest.Name, boatName);
        guest.ferryHandler.Tell(Localization.MSG_BOAT_ACCEPTED_GUEST, owner.Name, boatName);

        // already on the way to the dock: the guest goes with them
        if (owner.ferryHandler.Embarking) guest.ferryHandler.FollowOwner();
    }

    private void Decline(PlayerController player, GameMessage gm, GameClient client)
    {
        var request = BoatCrews.PendingFor(player);
        if (request == null)
        {
            client.SendReply(gm, Localization.MSG_BOAT_NO_REQUEST);
            return;
        }

        BoatCrews.Forget(request);
        var asker = request.Asker;
        if (!asker) return;
        client.SendReply(gm, Localization.MSG_BOAT_DECLINE_DONE, asker.Name);
        asker.ferryHandler.Tell(Localization.MSG_BOAT_DECLINED, player.Name);
    }

    /// <summary>A guest off the boat (at the nearest dock) or out of the crew ashore; the owner lands with !disembark.</summary>
    private void Leave(PlayerController player, GameMessage gm, GameClient client)
    {
        var ferry = player.ferryHandler;
        if (ferry.IsGuest)
        {
            ferry.LeaveBoatAsGuest();
            return;
        }

        if (ferry.QuitCrew()) return;

        if (BoatCrews.GuestsOf(player).Count > 0 || (ferry.OwnBoat && ferry.OwnBoat.Guests.Count > 0))
        {
            client.SendReply(gm, Localization.MSG_BOAT_OWNER_LEAVE);
            return;
        }

        client.SendReply(gm, Localization.MSG_BOAT_NOT_GUEST);
    }

    /// <summary>
    /// Whether the guest can come along in the owner's boat now; if not, says why to the one asking (the viewer), in
    /// their words: "your boat" to the owner, "their boat" to the guest.
    /// </summary>
    private bool Check(PlayerController owner, PlayerController guest, PlayerController viewer, GameMessage gm, GameClient client)
    {
        var ownerSees = viewer == owner;
        var boat = BoatCrews.BoatOf(owner);
        if (boat == null)
        {
            if (ownerSees) client.SendReply(gm, Localization.MSG_BOAT_NEED_BOAT);
            else client.SendReply(gm, Localization.MSG_BOAT_THEY_HAVE_NONE, owner.Name);
            return false;
        }

        var boatName = boat.Item.Name;
        if (BoatCrews.SeatsOf(owner) <= 1)
        {
            if (ownerSees) client.SendReply(gm, Localization.MSG_BOAT_NO_ROOM_TYPE, boatName);
            else client.SendReply(gm, Localization.MSG_BOAT_THEIR_NO_ROOM, owner.Name, boatName);
            return false;
        }

        if (owner.ferryHandler.OnFerry)
        {
            if (ownerSees) client.SendReply(gm, Localization.MSG_BOAT_OWN_AT_SEA);
            else client.SendReply(gm, Localization.MSG_BOAT_AT_SEA, owner.Name);
            return false;
        }

        if (guest.ferryHandler.OnFerry)
        {
            if (ownerSees) client.SendReply(gm, Localization.MSG_BOAT_AT_SEA, guest.Name);
            else client.SendReply(gm, Localization.MSG_BOAT_YOU_AT_SEA);
            return false;
        }

        if (!BoatCrews.HasRoom(owner))
        {
            if (ownerSees) client.SendReply(gm, Localization.MSG_BOAT_FULL_OWN, boatName);
            else client.SendReply(gm, Localization.MSG_BOAT_FULL, owner.Name, boatName);
            return false;
        }

        var guestsOwner = BoatCrews.OwnerOf(guest);
        if (guestsOwner)
        {
            if (ownerSees) client.SendReply(gm, Localization.MSG_BOAT_GUEST_IN_CREW, guest.Name);
            else client.SendReply(gm, Localization.MSG_BOAT_YOU_IN_CREW, guestsOwner.Name);
            return false;
        }

        if (!owner.Island || owner.Island != guest.Island)
        {
            client.SendReply(gm, Localization.MSG_BOAT_NOT_SAME_ISLAND, ownerSees ? guest.Name : owner.Name);
            return false;
        }

        return true;
    }

    /// <summary>The boat equipped (or owned), and what it is doing.</summary>
    private void Status(PlayerController player, GameMessage gm, GameClient client)
    {
        // a guest, aboard or waiting to set out
        var ferry = player.ferryHandler;
        var host = ferry.IsGuest && ferry.GuestBoat ? ferry.GuestBoat.Owner : BoatCrews.OwnerOf(player);
        if (host)
        {
            var hostBoat = BoatCrews.BoatOf(host);
            client.SendReply(gm, ferry.IsGuest ? Localization.MSG_BOAT_GUEST_STATUS : Localization.MSG_BOAT_GUEST_WAITING, host.Name, hostBoat != null ? hostBoat.Item.Name : "");
            return;
        }

        var equipped = player.Inventory.GetEquipmentBySlot(ItemSlot.Boat);
        if (equipped == null)
        {
            var owned = player.Inventory.GetAllItems().FirstOrDefault(i => BoatItems.Is(i.Item));
            if (owned != null) client.SendReply(gm, Localization.MSG_BOAT_NOT_EQUIPPED, owned.Item.Name);
            else client.SendReply(gm, Localization.MSG_BOAT_NONE);
            return;
        }

        var name = equipped.Item.Name;
        var speed = SailingSpeeds.Boat(BoatItems.TierOf(equipped.Item), player.Stats.Sailing.Level).ToString("0", CultureInfo.InvariantCulture);
        var boat = player.ferryHandler.OwnBoat;
        if (!boat) client.SendReply(gm, Localization.MSG_BOAT_ASHORE, name, speed);
        else if (boat.State == OwnBoat.Course.Anchored) client.SendReply(gm, Localization.MSG_BOAT_ANCHORED, name);
        else if (!string.IsNullOrEmpty(boat.Destination)) client.SendReply(gm, Localization.MSG_BOAT_SAILING, name, boat.Destination, speed);
        else client.SendReply(gm, Localization.MSG_BOAT_WANDERING, name, speed);
    }

    private async System.Threading.Tasks.Task Build(PlayerController player, string boat, GameMessage gm, GameClient client)
    {
        if (string.IsNullOrWhiteSpace(boat))
        {
            client.SendReply(gm, Localization.MSG_BOAT_BUILD_USAGE);
            return;
        }

        var result = await Game.RavenNest.Boats.BuildAsync(player.Id, boat);
        if (result == null)
        {
            client.SendReply(gm, Localization.MSG_BOAT_FAILED);
            return;
        }

        switch (result.Ok ? "ok" : result.Error)
        {
            case "ok":
                client.SendReply(gm, Localization.MSG_BOAT_BUILT, result.Boat, result.Fee.ToString("N0", CultureInfo.InvariantCulture));
                break;
            case "unknown":
                client.SendReply(gm, Localization.MSG_BOAT_UNKNOWN, boat, string.Join(", ", (result.Blueprints ?? Array.Empty<BoatBlueprintData>()).Select(b => b.Boat)));
                break;
            case "island":
                client.SendReply(gm, Localization.MSG_BOAT_ISLAND, result.Island);
                break;
            case "level":
                client.SendReply(gm, Localization.MSG_BOAT_LEVEL, result.Boat, result.Skill, result.Level);
                break;
            case "parts":
                client.SendReply(gm, Localization.MSG_BOAT_PARTS, result.Boat, string.Join(", ", (result.Missing ?? Array.Empty<BoatPartData>()).Select(p => p.Have + "/" + p.Amount + " " + p.Item)));
                break;
            case "coins":
                client.SendReply(gm, Localization.MSG_BOAT_COINS, result.Boat, result.Fee.ToString("N0", CultureInfo.InvariantCulture));
                break;
            default:
                client.SendReply(gm, Localization.MSG_BOAT_FAILED);
                break;
        }
    }

    private static string Describe(BoatBlueprintData[] blueprints)
    {
        if (blueprints == null || blueprints.Length == 0) return "-";
        return string.Join(" | ", blueprints.Select(b =>
        {
            var needs = "Sailing " + b.Sailing + (b.Fishing > 0 ? ", Fishing " + b.Fishing : "") + (b.Crafting > 0 ? ", Crafting " + b.Crafting : "");
            var parts = string.Join(", ", b.Parts.Select(p => p.Amount + " " + p.Item));
            return b.Boat + ": " + needs + "; " + parts + "; " + b.Fee.ToString("N0", CultureInfo.InvariantCulture) + " coins";
        }));
    }
}
