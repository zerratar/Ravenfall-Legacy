public class ArenaAddRequest
{
    public TwitchPlayerInfo Player { get; }
    public TwitchPlayerInfo TargetPlayer { get; }

    public ArenaAddRequest(TwitchPlayerInfo player, TwitchPlayerInfo targetPlayer)
    {
        Player = player;
        TargetPlayer = targetPlayer;
    }
}