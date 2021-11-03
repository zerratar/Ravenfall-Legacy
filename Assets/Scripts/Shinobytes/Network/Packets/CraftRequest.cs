public class CraftRequest
{
    public TwitchPlayerInfo Player { get; }
    public string Category { get; }
    public string Type { get; }

    public CraftRequest(TwitchPlayerInfo player, string category, string type)
    {
        Player = player;
        Category = category;
        Type = type;
    }
}