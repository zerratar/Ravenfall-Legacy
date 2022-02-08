using RavenNest.Models;

public class TradeItem
{
    public TradeItem(Item item, double amount, double pricePerItem, PlayerController player)
    {
        Item = new GameInventoryItem(player, new InventoryItem
        {
            Id = System.Guid.NewGuid(),
            Amount = (long)amount,
            ItemId = item.Id
        }, item);
        Amount = amount;
        PricePerItem = pricePerItem;
        Player = player;
    }
    public TradeItem(GameInventoryItem item, double amount, double pricePerItem, PlayerController player)
    {
        Item = item;
        Amount = amount;
        PricePerItem = pricePerItem;
        Player = player;
    }
    public System.Guid Id => Item.Item.Id;
    public GameInventoryItem Item { get; }
    public double Amount { get; }
    public double PricePerItem { get; }
    public PlayerController Player { get; }
}