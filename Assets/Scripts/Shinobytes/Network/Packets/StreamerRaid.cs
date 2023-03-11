public class StreamerRaid
{
    public StreamerRaid(
        User player,
        bool war)
    {
        Player = player;
        War = war;
    }

    public User Player { get; }
    public bool War { get; }
}
