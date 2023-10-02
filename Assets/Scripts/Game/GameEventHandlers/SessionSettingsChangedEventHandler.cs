using RavenNest.Models;

public class SessionSettingsChangedEventHandler : GameEventHandler<SessionSettings>
{
    public override void Handle(GameManager gameManager, SessionSettings data)
    {
        //Shinobytes.Debug.LogWarning("User Permission Update received.");
        gameManager.SessionSettings = data;

        ChunkManager.StrictLevelRequirements = data.StrictLevelRequirements;
    }
}
