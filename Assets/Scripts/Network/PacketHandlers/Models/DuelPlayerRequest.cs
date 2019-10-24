public class DuelPlayerRequest
{
    public DuelPlayerRequest(Player playerA, Player playerB)
    {
        this.playerA = playerA;
        this.playerB = playerB;
    }

    public Player playerA { get; }
    public Player playerB { get; }
}