using RavenNest.Models;

public class TradeItem
{
    public TradeItem(Item item, decimal amount, decimal pricePerItem)
    {
        Item = item;
        Amount = amount;
        PricePerItem = pricePerItem;
    }

    public Item Item { get; }
    public decimal Amount { get; }
    public decimal PricePerItem { get; }
}