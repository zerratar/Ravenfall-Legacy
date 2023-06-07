using RavenNest.Models;

public interface IGameEventHandler
{
    void Handle(GameManager gameManager, byte[] data);
}

public interface IGameEventHandler<TEventData> : IGameEventHandler
{
    void Handle(GameManager gameManager, TEventData data);
}
