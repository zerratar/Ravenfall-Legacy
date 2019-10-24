public class FerryTravelRequest
{
    public Player Player { get; }
    public string Destination { get; }

    public FerryTravelRequest(Player player, string destination)
    {
        Player = player;
        Destination = destination;
    }
}