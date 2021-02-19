using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using ZerraBot.Core.ScriptParser;

internal class ItemResolver : IItemResolver
{
    private ItemManager itemManager;
    private PlayerManager playerManager;

    public TradeItem Resolve(string itemTradeQuery, bool parsePrice = true, bool parseUsername = false, bool parseAmount = true)
    {
        if (string.IsNullOrEmpty(itemTradeQuery)) return null;
        itemTradeQuery = itemTradeQuery.Trim();
        
        if (!itemManager)
            itemManager = GameObject.FindObjectOfType<ItemManager>();

        if (!playerManager)
            playerManager = GameObject.FindObjectOfType<PlayerManager>();

        if (!itemManager || !itemManager.Loaded) return null;

        PlayerController player = null;
        if (parseUsername) {
            var username = itemTradeQuery.Split(' ')[0];
            player = playerManager.GetPlayerByName(username);
            itemTradeQuery = itemTradeQuery.Substring(username.Length).Trim();
        }

        if (string.IsNullOrEmpty(itemTradeQuery)) return null;

        var lexer = new Lexer();
        var tokens = lexer.Tokenize(itemTradeQuery, true);
        var index = tokens.Count - 1;

        var amount = 1L;
        var price = 0m;
        var modifiedQuery = "";


        while (true)
        {
            var token = tokens[index];
            if (token.Type == TokenType.Identifier)
            {
                if (parsePrice && price <= 0 && TryParsePrice(token, out var p))
                {
                    price = p;
                }                
                else if (parseAmount && TryParseAmount(token, out var a))
                {
                    amount = a;
                }
                else
                {
                    modifiedQuery = token.Value + modifiedQuery;
                }
            }
            else
            {
                modifiedQuery = token.Value + modifiedQuery;
            }
            if (--index < 0) break;
        }

        var itemQuery = modifiedQuery.Trim();
        var item = itemManager.GetItems().FirstOrDefault(x => IsMatch(x.Name, itemQuery));
        if (item == null)
        {
            return null;
        }

        return new TradeItem(item, amount, price, player);
    }

    private static bool TryParsePrice(Token token, out decimal price)
    {
        price = 0m;

        var values = new Dictionary<string, decimal>
            {
                { "k", 1000 },
                { "m", 1000_000 },
                { "b", 1000_000_000 },
            };

        var lastChar = token.Value[token.Value.Length - 1];
        if (values.TryGetValue(char.ToLower(lastChar).ToString(), out var m))
        {
            if (decimal.TryParse(token.Value.Remove(token.Value.Length - 1), NumberStyles.Any, new NumberFormatInfo(), out var p))
            {
                price = p * m;
                return true;
            }
        }

        if (!char.IsDigit(lastChar)) return false;
        {
            if (!decimal.TryParse(token.Value, NumberStyles.Any, new NumberFormatInfo(), out var p)) return false;
            price = p;
            return true;
        }
    }

    private static bool TryParseAmount(Token token, out long amount)
    {
        if (long.TryParse(token.Value, out amount))
        {
            return true;
        }

        var values = new Dictionary<string, decimal>
            {
                { "k", 1000 },
                { "m", 1000_000 },
                { "b", 1000_000_000 },
            };

        if (token.Value.StartsWith("x", StringComparison.OrdinalIgnoreCase))
        {
            var lastChar = token.Value[token.Value.Length - 1];
            if (values.TryGetValue(char.ToLower(lastChar).ToString(), out var m))
            {
                if (decimal.TryParse(token.Value.Remove(token.Value.Length - 1).Substring(1), NumberStyles.Any, new NumberFormatInfo(), out var p))
                {
                    amount = (long)(p * m);
                    return true;
                }
            }

            if (long.TryParse(token.Value.Substring(1), out amount))
            {
                return true;
            }
        }
        else if (token.Value.EndsWith("x", StringComparison.OrdinalIgnoreCase))
        {
            if (long.TryParse(token.Value.Remove(token.Value.Length - 1), out amount))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMatch(string name, string itemQuery)
    {
        //if (name.Equals(itemQuery, StringComparison.OrdinalIgnoreCase))
        //{
        //    return true;
        //}
        return name.Equals(itemQuery, StringComparison.OrdinalIgnoreCase);
        //return GetItemNameAbbreviations(name)
        //    .Any(abbr => abbr.Equals(itemQuery, StringComparison.OrdinalIgnoreCase));
    }

    private static string[] GetItemNameAbbreviations(string name)
    {
        var nameList = new HashSet<string>(new List<string>
            {
                name.Trim(),
                name.Replace("-", "").Trim(),
                name.Replace("-", " ").Trim(),
                //name.Replace(" Sword", "").Trim(),
                name.Replace("-", " ").Replace(" Sword", "").Trim(),
                name.Replace("Helmet", "Helm").Trim()
            }
        .Distinct());

        var tempList = nameList.ToArray();
        foreach (var item in tempList)
        {
            var items = item.Split(' ');
            var count = Math.Pow(2, items.Length);
            for (var i = 1; i <= count; ++i)
            {
                var newList = items
                    .Skip(1)
                    .Where((t, j) => (i >> j) % 2 != 0)
                    .ToList();
                newList.Insert(0, items.First());
                if (newList.Count > 1)
                {
                    nameList.Add(string.Join(" ", newList));
                }
            }
        }

        var nameParts = name.Split(' ');
        var abbreviation = "";
        foreach (var part in nameParts)
        {
            if (part.Contains("-"))
                abbreviation += string.Join("", part.Split('-').Select(x => x[0]));
            else
                abbreviation += part[0];
        }

        if (abbreviation.Length >= 3)
        {
            nameList.Add(string.Join("", abbreviation.Take(3)));
        }

        return nameList.ToArray();
    }

}