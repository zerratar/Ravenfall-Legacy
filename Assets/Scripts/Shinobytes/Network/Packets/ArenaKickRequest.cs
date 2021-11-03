public class ArenaKickRequest
{
    public TwitchPlayerInfo Player { get; }
    public TwitchPlayerInfo TargetPlayer { get; }

    public ArenaKickRequest(TwitchPlayerInfo player, TwitchPlayerInfo targetPlayer)
    {
        Player = player;
        TargetPlayer = targetPlayer;
    }
}