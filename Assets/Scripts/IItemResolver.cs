using System;

internal interface IItemResolver
{
    TradeItem Resolve(string itemTradeQuery);
}