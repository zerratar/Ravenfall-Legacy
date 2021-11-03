public class BuyItemRequest
{
    public BuyItemRequest(
        TwitchPlayerInfo player,
        string itemQuery)
    {
        Player = player;
        ItemQuery = itemQuery;
    }

    public TwitchPlayerInfo Player { get; }
    public string ItemQuery { get; }
}

public class SellItemRequest
{
    public SellItemRequest(
        TwitchPlayerInfo player,
        string itemQuery)
    {
        Player = player;
        ItemQuery = itemQuery;
    }

    public TwitchPlayerInfo Player { get; }
    public string ItemQuery { get; }
}