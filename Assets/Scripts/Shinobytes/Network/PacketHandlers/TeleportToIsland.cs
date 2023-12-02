using System;
using System.Collections.Generic;
using System.Linq;
using RavenNest.Models;
using Sirenix.Utilities;

public class TeleportToIsland : ChatBotCommandHandler<string>
{
    public TeleportToIsland(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (string.IsNullOrEmpty(inputQuery))
        {
            client.SendReply(gm, Localization.MSG_ITEM_USE_MISSING_ARGS);
            return;
        }

        var inventory = player.Inventory;
        var backpack = inventory.GetBackpackItems();

        var options = Enum.GetValues(typeof(Island)).Cast<Island>();
        if (!Enum.TryParse<Island>(inputQuery, true, out var island))
        {
            var q = inputQuery.ToLower();
            var s = 9999;
            foreach (var i in options)
            {
                var score = ItemResolver.LevenshteinDistance(q, i.ToString().ToLower());
                if (score < s)
                {
                    s = score;
                    island = i;
                }
            }
        }

        options = options.Where(x => x != Island.Any && x != Island.Ferry && x != Island.None).ToArray();

        if (island == Island.Any || island == Island.Ferry || island == Island.None)
        {
            client.SendReply(gm, "{island} is not a valid destination. Available islands are {islandList}", inputQuery, string.Join(", ", options));
            return;
        }

        var isTomeOfTeleportation = false;
        // try to get the exact tome first.
        var tome = backpack.FirstOrDefault(x => x.Name.Equals("tome of " + island, StringComparison.OrdinalIgnoreCase));
        if (tome == null)
        {
            // try get a tome of teleportation if it exists.
            tome = backpack.FirstOrDefault(x => x.Name.Equals("tome of teleportation", StringComparison.OrdinalIgnoreCase));
            isTomeOfTeleportation = true;
        }

        if (tome == null)
        {
            client.SendReply(gm, "You do not have any suitable Teleportation Tomes to teleport to {island}.", inputQuery);
            return;
        }

        var result = await Game.RavenNest.Players.UseItemAsync(player.Id, tome.InstanceId, island.ToString());
        if (result == null || result.InventoryItemId == Guid.Empty || result.EffectIsland == Island.Any || result.EffectIsland == Island.None || result.EffectIsland == Island.Ferry)
        {
            client.SendReply(gm, "{itemName} can not be used right now.", tome.Name);
            return;
        }

        player.Inventory.UpdateInventoryItem(result.InventoryItemId, result.NewStackAmount);

        var targetIsland = Game.Islands.Get(result.EffectIsland);

        player.teleportHandler.Teleport(targetIsland, true);

        client.SendReply(gm, "You have used a {itemName} and teleported to {island}!", tome.Name, result.EffectIsland.ToString());
    }
}
