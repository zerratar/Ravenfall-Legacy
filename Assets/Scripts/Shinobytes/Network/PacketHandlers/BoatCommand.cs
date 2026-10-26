using System;
using System.Globalization;
using System.Linq;
using RavenNest.SDK.Endpoints;

/// <summary>
/// !boat (docs/own-boats.md, phase 4): with nothing more, the player's boat and where they are with it; !boat build
/// fishing boat (or sloop) has the shipwright on Away build one from crafted parts and coins; !boat list says what
/// each boat asks for.
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

    /// <summary>The boat equipped (or owned), and what it is doing.</summary>
    private void Status(PlayerController player, GameMessage gm, GameClient client)
    {
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
