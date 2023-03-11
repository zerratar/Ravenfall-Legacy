public class PlayerTaskRequest
{
    public PlayerTaskRequest(string task, string[] arguments)
    {
        Task = task;
        Arguments = arguments;
    }

    public string Task { get; }
    public string[] Arguments { get; }
}