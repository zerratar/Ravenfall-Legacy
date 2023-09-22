using RavenNest.Models;
using System.Collections.Generic;

public class ItemProductionState
{
    public ItemProductionState(PlayerController player, ItemRecipe recipe, int amount, GameMessage message, GameClient client)
    {
        Player = player;
        Recipe = recipe;
        Amount = amount;
        Message = message;
        Client = client;
        AmountLeftToCraft = amount;
    }
    public List<ItemProductionResultItem> ProducedItems { get; } = new List<ItemProductionResultItem>();
    public PlayerController Player { get; }
    public ItemRecipe Recipe { get; }
    public long Amount { get; }
    public GameMessage Message { get; }
    public GameClient Client { get; }
    public long AmountLeftToCraft { get; set; }
    public bool Continue { get; set; }
}