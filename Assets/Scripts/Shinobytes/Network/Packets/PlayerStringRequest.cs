public class PlayerStringRequest : IBotRequest<string>
{
    public TwitchPlayerInfo Player { get; }
    public string Value { get; }
    public PlayerStringRequest(TwitchPlayerInfo player, string value)
    {
        Player = player;
        Value = value;
    }
}