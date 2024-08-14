using System.Linq;

public class FerryTravel : ChatBotCommandHandler<string>
{
    public FerryTravel(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        var cmdPlayer = PlayerManager.GetPlayer(gm.Sender);
        if (!cmdPlayer)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        IslandController island = null;
        PlayerController player = cmdPlayer;
        var islandName = data.Trim();
        var playerName = "";
        var otherPlayer = false;
        if (islandName.Contains(' '))
        {
            var parts = islandName.Split(' ');
            islandName = parts[0];
            playerName = parts[1];

            island = Game.Islands.Find(islandName);
            if (!island || !island.Sailable)
            {
                playerName = parts[0];
                islandName = parts[1];
                island = Game.Islands.Find(islandName);
            }

            // see if the target islandName is an island, if not switch the islandName and playerName

            // remember we can only do this if we are an administrator.
            if (player.IsGameAdmin || player.IsGameModerator || player.IsBroadcaster || player.IsModerator)
            {
                player = PlayerManager.GetPlayerByName(playerName);
                otherPlayer = true;

                if (!player.CanBeControlledByStreamer)
                {
                    client.SendReply(gm, "You cannot use the !sail command for {playerName}.", playerName);
                    return;
                }
            }

            if (!player)
            {
                client.SendReply(gm, Localization.MSG_TRAVEL_NO_SUCH_PLAYER, playerName);
                return;
            }

            player.ResetPlayerControl();
        }

        // if we did not get an island, then we need to find it.
        if (!island || !island.Sailable)
        {
            island = Game.Islands.Find(islandName);
            if (!island || !island.Sailable)
            {
                client.SendReply(gm, Localization.MSG_TRAVEL_NO_SUCH_ISLAND, islandName, string.Join(", ", Game.Islands.All.Where(x => x.Sailable).Select(x => x.Identifier)));
                return;
            }
        }

        if (island == player.Island)
        {
            if (!otherPlayer)
            {
                client.SendReply(gm, Localization.MSG_TRAVEL_ALREADY_ON_ISLAND);
            }
            return;
        }

        if (player.streamRaidHandler.InWar)
        {
            if (!otherPlayer)
            {
                client.SendReply(gm, Localization.MSG_TRAVEL_WAR);
            }
            return;
        }

        if (player.arenaHandler.InArena && !otherPlayer)
        {
            if (!Game.Arena.Leave(player))
            {
                client.SendReply(gm, Localization.MSG_TRAVEL_ARENA);
                return;
            }
        }

        if (player.duelHandler.InDuel)
        {
            if (!otherPlayer) client.SendReply(gm, Localization.MSG_TRAVEL_DUEL);
            return;
        }

        if (player.dungeonHandler.InDungeon)
        {
            if (!otherPlayer) client.SendReply(gm, Localization.MSG_TRAVEL_DUNGEON);
            return;
        }

        if (player.onsenHandler.InOnsen)
        {
            if (!otherPlayer) Game.Onsen.Leave(player);
        }

        if (player.raidHandler)
        {
            if (!otherPlayer) Game.Raid.Leave(player);
        }

        player.ferryHandler.Embark(island);
    }
}
