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

public class PlayerAndString
{
    public PlayerAndString(TwitchPlayerInfo player, string value)
    {
        Player = player;
        Value = value;
    }

    public TwitchPlayerInfo Player { get; }
    public string Value { get; }
}

public class PlayerPlayerAndString
{
    public PlayerPlayerAndString(TwitchPlayerInfo player, TwitchPlayerInfo target, string value)
    {
        Player = player;
        Target = target;
        Value = value;
    }

    public TwitchPlayerInfo Player { get; }
    public TwitchPlayerInfo Target { get; }
    public string Value { get; }
}

public class PlayerAndPlayer
{
    public TwitchPlayerInfo Player { get; }
    public TwitchPlayerInfo TargetPlayer { get; }

    public PlayerAndPlayer(TwitchPlayerInfo player, TwitchPlayerInfo targetPlayer)
    {
        Player = player;
        TargetPlayer = targetPlayer;
    }
}