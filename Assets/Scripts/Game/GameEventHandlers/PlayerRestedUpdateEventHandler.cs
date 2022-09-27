using RavenNest.Models;

public class PlayerRestedUpdateEventHandler : GameEventHandler<PlayerRestedUpdate>
{
    public override void Handle(GameManager gameManager, PlayerRestedUpdate data)
    {
        gameManager.Players.UpdateRestedState(data);
    }
}
