public interface ITavernGame
{
    string GameStartCommand { get; }
    TavernGameState State { get; }
    bool IsGameOver { get; }
    bool Started { get; }
    void Activate();
}

public enum TavernGameState
{
    None,
    WaitingForPlayers,
    Playing,
    GameOver
}
