public class BuyItemRequest
{
    public BuyItemRequest(
        Player player,
        string itemQuery)
    {
        Player = player;
        ItemQuery = itemQuery;
    }

    public Player Player { get; }
    public string ItemQuery { get; }
}

public class SellItemRequest
{
    public SellItemRequest(
        Player player,
        string itemQuery)
    {
        Player = player;
        ItemQuery = itemQuery;
    }

    public Player Player { get; }
    public string ItemQuery { get; }
}