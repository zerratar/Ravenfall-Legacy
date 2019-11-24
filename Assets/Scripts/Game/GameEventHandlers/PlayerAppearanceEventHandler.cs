using RavenNest.Models;

public class PlayerAppearanceEventHandler : GameEventHandler<AppearanceUpdate>
{
    protected override void Handle(GameManager gameManager, AppearanceUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (player)
        {
            player.Appearance.TryUpdate(data.Values);
        }
    }
}
