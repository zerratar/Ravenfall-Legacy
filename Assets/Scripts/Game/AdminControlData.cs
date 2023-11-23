public static class AdminControlData
{
    public static SpawnBotLevelStrategy SpawnBotLevel;
    public static bool TeleportationVisible;
    public static bool BotSpawnVisible;
    public static bool BotTrainingVisible;
    public static bool ControlPlayers;
    public static bool IsAdmin;
    public static bool NoChatBotMessages;
}

public enum SpawnBotLevelStrategy
{
    Random,
    Max,
    Min
}
