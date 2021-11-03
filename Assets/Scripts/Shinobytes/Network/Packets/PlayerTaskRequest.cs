public class PlayerTaskRequest
{
    public PlayerTaskRequest
        (TwitchPlayerInfo player, string task, string[] arguments)
    {
        Player = player;
        Task = task;
        Arguments = arguments;
    }

    public TwitchPlayerInfo Player { get; }
    public string Task { get; }
    public string[] Arguments { get; }
}