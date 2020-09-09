public class SetPetRequest
{
    public Player Player { get; }
    public string Pet { get; }

    public SetPetRequest(Player player, string pet)
    {
        Player = player;
        Pet = pet;
    }
}