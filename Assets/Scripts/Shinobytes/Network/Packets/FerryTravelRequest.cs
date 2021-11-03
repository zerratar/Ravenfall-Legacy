public class FerryTravelRequest
{
    public TwitchPlayerInfo Player { get; }
    public string Destination { get; }

    public FerryTravelRequest(TwitchPlayerInfo player, string destination)
    {
        Player = player;
        Destination = destination;
    }
}