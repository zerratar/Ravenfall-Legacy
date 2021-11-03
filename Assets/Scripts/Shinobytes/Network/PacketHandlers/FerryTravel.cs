using System.Linq;

public class FerryTravel : PacketHandler<FerryTravelRequest>
{
    public FerryTravel(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override void Handle(FerryTravelRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var islandName = data.Destination;
        var island = Game.Islands.Find(islandName);
        if (!island)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_TRAVEL_NO_SUCH_ISLAND, islandName, string.Join(", ", Game.Islands.All.Select(x => x.Identifier)));
            return;
        }

        if (island == player.Island)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_TRAVEL_ALREADY_ON_ISLAND);
            return;
        }

        if (player.StreamRaid.InWar)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_TRAVEL_WAR);
            return;
        }

        if (player.Arena.InArena)
        {
            if (!Game.Arena.Leave(player))
            {
                client.SendMessage(data.Player.Username, Localization.MSG_TRAVEL_ARENA);
                return;
            }
        }

        if (player.Duel.InDuel)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_TRAVEL_DUEL);
            return;
        }

        if (player.Dungeon.InDungeon)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_TRAVEL_DUNGEON);
            return;
        }

        if (player.Onsen.InOnsen)
        {
            Game.Onsen.Leave(player);
        }

        if (player.Raid)
        {
            Game.Raid.Leave(player);
        }

        player.Ferry.Embark(island);
    }
}
