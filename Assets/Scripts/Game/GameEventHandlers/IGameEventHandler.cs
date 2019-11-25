using RavenNest.Models;

public interface IGameEventHandler
{
    void Handle(GameManager gameManager, string data);
}
