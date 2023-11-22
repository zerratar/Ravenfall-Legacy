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
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var islandName = data;
        var island = Game.Islands.Find(islandName);
        if (!island || !island.Sailable)
        {
            client.SendReply(gm, Localization.MSG_TRAVEL_NO_SUCH_ISLAND, islandName, string.Join(", ", Game.Islands.All.Where(x => x.Sailable).Select(x => x.Identifier)));
            return;
        }

        if (island == player.Island)
        {
            client.SendReply(gm, Localization.MSG_TRAVEL_ALREADY_ON_ISLAND);
            return;
        }

        if (player.streamRaidHandler.InWar)
        {
            client.SendReply(gm, Localization.MSG_TRAVEL_WAR);
            return;
        }

        if (player.arenaHandler.InArena)
        {
            if (!Game.Arena.Leave(player))
            {
                client.SendReply(gm, Localization.MSG_TRAVEL_ARENA);
                return;
            }
        }

        if (player.duelHandler.InDuel)
        {
            client.SendReply(gm, Localization.MSG_TRAVEL_DUEL);
            return;
        }

        if (player.dungeonHandler.InDungeon)
        {
            client.SendReply(gm, Localization.MSG_TRAVEL_DUNGEON);
            return;
        }

        if (player.onsenHandler.InOnsen)
        {
            Game.Onsen.Leave(player);
        }

        if (player.raidHandler)
        {
            Game.Raid.Leave(player);
        }

        player.ferryHandler.Embark(island);
    }
}
