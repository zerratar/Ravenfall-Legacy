public class CraftRequest
{
    public string Category { get; }
    public string Type { get; }

    public CraftRequest(string category, string type)
    {
        Category = category;
        Type = type;
    }
}