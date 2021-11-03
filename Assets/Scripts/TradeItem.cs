using RavenNest.Models;

public class TradeItem
{
    public TradeItem(Item item, double amount, double pricePerItem, PlayerController player)
    {
        Item = item;
        Amount = amount;
        PricePerItem = pricePerItem;
        Player = player;
    }

    public Item Item { get; }
    public double Amount { get; }
    public double PricePerItem { get; }
    public PlayerController Player { get; }
}