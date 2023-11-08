using RavenNest.Models;

public class SessionSettingsChangedEventHandler : GameEventHandler<SessionSettings>
{
    public override void Handle(GameManager gameManager, SessionSettings data)
    {
        //Shinobytes.Debug.LogWarning("User Permission Update received.");
        gameManager.SessionSettings = data;

        // check so values are more than 0. This ensures that leveling is not disrupted.
        if (data.XP_IncrementMins > 0)
            GameMath.Exp.IncrementMins = data.XP_IncrementMins; // 14

        if (data.XP_EasyLevel > 0)
            GameMath.Exp.EasyLevel = data.XP_EasyLevel; // 70

        if (data.XP_EasyLevelIncrementDivider > 0)
            GameMath.Exp.EasyLevelIncrementDivider = data.XP_EasyLevelIncrementDivider; // 8

        if (data.XP_GlobalMultiplierFactor > 0)
            GameMath.Exp.GlobalMultiplierFactor = data.XP_GlobalMultiplierFactor; //1;

        ChunkManager.StrictLevelRequirements = data.StrictLevelRequirements;
    }
}
