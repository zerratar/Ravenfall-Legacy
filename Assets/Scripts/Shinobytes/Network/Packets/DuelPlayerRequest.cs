public class DuelPlayerRequest
{
    public DuelPlayerRequest(TwitchPlayerInfo playerA, TwitchPlayerInfo playerB)
    {
        this.playerA = playerA;
        this.playerB = playerB;
    }

    public TwitchPlayerInfo playerA { get; }
    public TwitchPlayerInfo playerB { get; }
}
