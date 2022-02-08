public class EventJoinRequest
{
    public EventJoinRequest(
        TwitchPlayerInfo player,
        string code)
    {
        Player = player;
        Code = code;
    }

    public TwitchPlayerInfo Player { get; }
    public string Code { get; }
}