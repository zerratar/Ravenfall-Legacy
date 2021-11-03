public class PlayerAndNumber
{
    public PlayerAndNumber(TwitchPlayerInfo player, int number)
    {
        Player = player;
        Number = number;
    }

    public TwitchPlayerInfo Player { get; }
    public int Number { get; }
}
