using System;

internal interface IItemResolver
{
    TradeItem Resolve(string itemTradeQuery, bool parsePrice = true, bool parseUsername = false, bool parseAmount = true);
}