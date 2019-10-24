public class TradeItemRequest
{
    public TradeItemRequest(
        Player player,
        string itemQuery)
    {
        Player = player;
        ItemQuery = itemQuery;
    }

    public Player Player { get; }
    public string ItemQuery { get; }
}