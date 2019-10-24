public class ArenaKickRequest
{
    public Player Player { get; }
    public Player TargetPlayer { get; }

    public ArenaKickRequest(Player player, Player targetPlayer)
    {
        Player = player;
        TargetPlayer = targetPlayer;
    }
}