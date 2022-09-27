using RavenNest.Models;

public interface IGameEventHandler
{
    void Handle(GameManager gameManager, string data);
}

public interface IGameEventHandler<TEventData> : IGameEventHandler
{
    void Handle(GameManager gameManager, TEventData data);
}
