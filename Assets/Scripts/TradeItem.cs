using RavenNest.Models;

public class TradeItem
{
	public TradeItem(Item item, decimal amount, decimal pricePerItem, PlayerController player)
	{
		Item = item;
		Amount = amount;
		PricePerItem = pricePerItem;
        Player = player;
	}

	public Item Item { get; }
	public decimal Amount { get; }
	public decimal PricePerItem { get; }
	public PlayerController Player { get; }
}