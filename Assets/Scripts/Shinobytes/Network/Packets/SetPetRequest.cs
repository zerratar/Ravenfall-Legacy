public class SetPetRequest
{
    public TwitchPlayerInfo Player { get; }
    public string Pet { get; }

    public SetPetRequest(TwitchPlayerInfo player, string pet)
    {
        Player = player;
        Pet = pet;
    }
}