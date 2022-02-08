public class TradeItemRequest
{
    public TradeItemRequest(
        TwitchPlayerInfo player,
        string itemQuery)
    {
        Player = player;
        ItemQuery = itemQuery;
    }

    public TwitchPlayerInfo Player { get; }
    public string ItemQuery { get; }
}
