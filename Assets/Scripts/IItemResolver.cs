using RavenNest.Models;
using System;
using System.Collections.Generic;

public interface IItemResolver
{
    IReadOnlyList<Item> GetItemSet(string setName);
    ItemResolveResult ResolveInventoryItem(PlayerController player, string itemName, int maxSuggestions = 5, EquippedState equippedState = EquippedState.Any);
    ItemResolveResult Resolve(string query, int maxSuggestions = 5);
    ItemResolveResult Resolve(string query, ItemType expectedItemType, int maxSuggestions = 5);
    ItemResolveResult Resolve(string query, Func<Item, bool> itemFilter, int maxSuggestions = 5);
    ItemResolveResult ResolveAny(params string[] queries);
    ItemResolveResult ResolveTradeQuery(string itemTradeQuery, bool parsePrice = true, bool parseUsername = false, bool parseAmount = true, PlayerController playerToSearch = null, EquippedState equippedState = EquippedState.Any);
    
    ItemResolveResult ResolveItemAndAmount(string query);
}

public enum EquippedState
{
    Any,
    NotEquipped,
    Equipped
}