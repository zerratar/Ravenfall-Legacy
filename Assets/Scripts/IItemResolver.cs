using System;

internal interface IItemResolver
{
    ItemResolveResult ResolveInventoryItem(PlayerController player, string query, int maxSuggestions = 5);
    ItemResolveResult Resolve(string query, int maxSuggestions = 5);
    ItemResolveResult ResolveAny(params string[] queries);
    ItemResolveResult ResolveTradeQuery(string itemTradeQuery, bool parsePrice = true, bool parseUsername = false, bool parseAmount = true, PlayerController playerToSearch = null);
}