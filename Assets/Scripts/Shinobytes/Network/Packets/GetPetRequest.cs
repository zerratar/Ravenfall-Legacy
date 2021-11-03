public class GetPetRequest
{
    public TwitchPlayerInfo Player { get; }

    public GetPetRequest(TwitchPlayerInfo player)
    {
        Player = player;
    }
}
