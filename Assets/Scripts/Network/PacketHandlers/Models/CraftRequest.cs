public class CraftRequest
{
    public Player Player { get; }
    public string Category { get; }
    public string Type { get; }

    public CraftRequest(Player player, string category, string type)
    {
        Player = player;
        Category = category;
        Type = type;
    }
}