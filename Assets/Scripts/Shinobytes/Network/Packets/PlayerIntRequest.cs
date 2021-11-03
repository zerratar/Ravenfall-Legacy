public class PlayerIntRequest : IBotRequest<int>
{
    public TwitchPlayerInfo Player { get; }
    public int Value { get; }
    public PlayerIntRequest(TwitchPlayerInfo player, int value)
    {
        Player = player;
        Value = value;
    }
}
