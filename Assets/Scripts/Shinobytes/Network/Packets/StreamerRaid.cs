public class StreamerRaid
{
    public StreamerRaid(
        TwitchPlayerInfo player,
        bool war)
    {
        Player = player;
        War = war;
    }

    public TwitchPlayerInfo Player { get; }
    public bool War { get; }
}
