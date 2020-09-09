public class GetPetRequest
{
    public Player Player { get; }

    public GetPetRequest(Player player)
    {
        Player = player;
    }
}
