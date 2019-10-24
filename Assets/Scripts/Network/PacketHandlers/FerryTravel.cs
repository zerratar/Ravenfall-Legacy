using System.Linq;

public class FerryTravel : PacketHandler<FerryTravelRequest>
{
    public FerryTravel(
         GameManager game,
         GameServer server,
         PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override void Handle(FerryTravelRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player)
        {
            client.SendCommand(data.Player.Username, "ferry_travel_failed", $"You are not currently playing. Type !join to start playing.");
            return;
        }

        if (player.StreamRaid.InWar)
        {
            client.SendCommand(data.Player.Username, "ferry_travel_failed",
                "You cannot travel when participating in a war. Please wait for it to be over.");
            return;
        }

        var islandName = data.Destination;
        var island = Game.Islands.Find(islandName);
        if (!island)
        {

            client.SendCommand(data.Player.Username, "ferry_travel_failed", $"No islands named '{islandName}'. You may travel to: '{string.Join(", ", Game.Islands.All.Select(x => x.Identifier))}'");
            return;
        }

        player.Ferry.Embark(island);
    }
}
