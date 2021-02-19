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

        if (player.StreamRaid.InWar)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_TRAVEL_WAR);
            return;
        }

        var islandName = data.Destination;
        var island = Game.Islands.Find(islandName);
        if (!island)
        {

            client.SendMessage(data.Player.Username, Localization.MSG_TRAVEL_NO_SUCH_ISLAND, islandName, string.Join(", ", Game.Islands.All.Select(x => x.Identifier)));
            return;
        }

        player.Ferry.Embark(island);
    }
}
