public class StreamerRaid
{
    public StreamerRaid(
        Player player,
        bool war)
    {
        Player = player;
        War = war;
    }

    public Player Player { get; }
    public bool War { get; }
}
