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

        if (player.dungeonHandler.InDungeon)
        {
            client.SendReply(gm, Localization.MSG_TELEPORT_FAILED_DUNGEON);
            return;
        }

        if (player.InRavebridge)
        {
            client.SendReply(gm, Localization.MSG_RAVEBRIDGE_BLOCKED);
            return;
        }

        if (player.streamRaidHandler.InWar)
        {
            client.SendReply(gm, Localization.MSG_TELEPORT_FAILED_WAR);
            return;
        }

        if (player.arenaHandler.InArena)
        {
            client.SendReply(gm, Localization.MSG_TELEPORT_FAILED_ARENA);
            return;
        }

        if (player.duelHandler.InDuel)
        {
            client.SendReply(gm, Localization.MSG_TELEPORT_FAILED_DUEL);
            return;
        }

        if (player.raidHandler.InRaid)
        {
            client.SendReply(gm, Localization.MSG_TELEPORT_FAILED_RAID);
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
            client.SendReply(gm, Localization.MSG_TELEPORT_INVALID_ISLAND, inputQuery, string.Join(", ", options));
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
            client.SendReply(gm, Localization.MSG_TELEPORT_NO_TOME, inputQuery);
            return;
        }

        var result = await Game.RavenNest.Players.UseItemAsync(player.Id, tome.InstanceId, island.ToString());
        if (result == null || result.InventoryItemId == Guid.Empty || result.EffectIsland == Island.Any || result.EffectIsland == Island.None || result.EffectIsland == Island.Ferry)
        {
            client.SendReply(gm, Localization.MSG_USE_ITEM_NOT_NOW, tome.Name);
            return;
        }

        player.Inventory.UpdateInventoryItem(result.InventoryItemId, result.NewStackAmount);

        var targetIsland = Game.Islands.Get(result.EffectIsland);

        player.teleportHandler.Teleport(targetIsland, true);

        client.SendReply(gm, Localization.MSG_TELEPORT_SUCCESS, tome.Name, result.EffectIsland.ToString());
    }
}
