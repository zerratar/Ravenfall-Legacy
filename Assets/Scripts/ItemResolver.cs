using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

using Shinobytes.Core.ScriptParser;
using RavenNest.Models;

public class ItemResolver : IItemResolver
{
    private ItemManager itemManager;
    private PlayerManager playerManager;
    private ItemMaterial[] itemMaterials;
    private void EnsureManagers()
    {
        if (itemMaterials == null || itemMaterials.Length == 0)
        {
            itemMaterials = Enum.GetValues(typeof(ItemMaterial))
                .Cast<ItemMaterial>()
                .ToArray();
        }

        if (!itemManager)
            itemManager = GameObject.FindObjectOfType<ItemManager>();

        if (!playerManager)
            playerManager = GameObject.FindObjectOfType<PlayerManager>();
    }

    public IReadOnlyList<Item> GetItemSet(string setName)
    {
        var setItems = new List<Item>();

        // since elder can match with all elder items, so skip elder if that is being used.
        if (string.IsNullOrEmpty(setName) || setName.Trim().Equals("elder"))
        {
            return setItems;
        }

        foreach (var item in itemManager.GetItems())
        {
            if (item.Name.StartsWith(setName, StringComparison.OrdinalIgnoreCase))
            {
                // good start, now lets make sure its an equipment
                if (item.Category == ItemCategory.Armor || item.Category == ItemCategory.Weapon || item.Category == ItemCategory.Ring || item.Category == ItemCategory.Amulet)
                {
                    setItems.Add(item);
                }
            }
        }

        return setItems;
    }

    public ItemResolveResult ResolveItemAndAmount(string query)
    {
        return ResolveTradeQuery(query, false, false, true);
    }

    public ItemResolveResult ResolveTradeQuery(
        string query,
        bool parsePrice = true,
        bool parseUsername = false,
        bool parseAmount = true,
        PlayerController playerToSearch = null,
        EquippedState equippedState = EquippedState.Any)
    {
        try
        {
            if (string.IsNullOrEmpty(query))
                return ItemResolveResult.Empty;

            query = query.Trim();
            EnsureManagers();
            if (!itemManager || !itemManager.Loaded)
                return ItemResolveResult.Empty;

            PlayerController player = null;
            if (parseUsername)
            {
                var username = query.Split(' ')[0];
                player = playerManager.GetPlayerByName(username);
                query = query.Substring(username.Length).Trim();
            }

            if (string.IsNullOrEmpty(query))
                return ItemResolveResult.Empty;

            var lexer = new Lexer();
            var tokens = lexer.Tokenize(query, true);
            var index = tokens.Count - 1;

            var amount = 1L;
            var price = 0d;
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

            if (playerToSearch != null)
            {
                var result = ResolveInventoryItem(playerToSearch, itemQuery, equippedState: equippedState);
                result.Count = amount;
                result.Price = price;
                result.Player = player;
                return result;
            }
            else
            {
                var result = Resolve(itemQuery, 5);
                result.Count = amount;
                result.Price = price;
                result.Player = player;
                return result;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
            return null;
        }
    }

    public ItemResolveResult ResolveInventoryItem(PlayerController player, string itemName, int maxSuggestions = 5
        , EquippedState equippedState = EquippedState.Any)
    {
        EnsureManagers();

        var itemQuery = itemName.Trim();

        var items = player.Inventory.GetAllItems();

        var matches = items
            .Select(x => new ItemMatchPair<GameInventoryItem> { Item = x, Match = Match(x.Name, x.Item.Type, itemQuery) })
            .Where(x => x.Match.IsCloseMatch)
            .OrderBy(x => LevenshteinDistance(x.Item.Name, itemQuery))
            .ToArray();

        var exactMatches = matches.Where(x => x.Match.IsExactMatch).ToArray();
        var invItem = exactMatches.FirstOrDefault(
                x => equippedState == EquippedState.Any || (equippedState == EquippedState.Equipped && player.Inventory.IsEquipped(x.Item)) || (equippedState == EquippedState.NotEquipped && !player.Inventory.IsEquipped(x.Item))
            )?.Item;

        var suggestedItemNames = new string[0];

        if (invItem == null && matches.Length > 0)
        {
            var startingCases = matches.Where(x => x.Item.Name.StartsWith(itemName, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (startingCases.Length > 0)
            {
                matches = startingCases;
            }

            var m = matches.Where(x => x.Match.IsMatch).ToList();
            if (m.Count > 0)
            {
                invItem = m[0].Item;
                suggestedItemNames = m.Select(x => x.Item.Name).Distinct().Take(maxSuggestions).ToArray();
            }
            else
            {
                suggestedItemNames = matches.Where(x => x.Match.IsCloseMatch).Select(x => x.Item.Name).Distinct().Take(maxSuggestions).ToArray();
            }
        }
        else if (invItem == null)
        {
            suggestedItemNames = items
                .Where(x => IsCloseMatch(x.Name, x.Item.Type, itemQuery))
                .Select(x => x.Name)
                .Distinct()
                .Take(maxSuggestions)
                .ToArray();
        }

        // if we didnt find an inventory item, lets do a final search for the item at least.
        Item targetItem = null;
        if (invItem == null)
        {
            var itemResolve = Resolve(itemName);
            targetItem = itemResolve.Item;
        }
        else
        {
            targetItem = invItem.Item;
        }

        if (suggestedItemNames.Length == 1 && invItem != null && invItem.Name.Equals(suggestedItemNames[0], StringComparison.OrdinalIgnoreCase))
        {
            // if there is only one item and the suggested item was found.
            // then its an exact match? So why wasnt it chosen?
            suggestedItemNames = new string[0];
        }

        return new ItemResolveResult
        {
            Item = targetItem,
            Count = 1,
            Price = -1,
            Query = itemName,
            SuggestedItemNames = suggestedItemNames,
            InventoryItem = invItem
        };
    }

    public ItemResolveResult ResolveAny(params string[] query)
    {
        if (query == null || query.Length == 0)
        {
            return new();
        }

        EnsureManagers();
        HashSet<string> suggestions = new HashSet<string>();
        foreach (var q in query)
        {
            var result = Resolve(q);
            if (result != null)
            {
                // return exact match directly.
                if (result.Item != null)
                    return result;

                // keep suggestions in mind
                if (result.SuggestedItemNames != null && result.SuggestedItemNames.Length > 0)
                {
                    foreach (var s in result.SuggestedItemNames)
                        suggestions.Add(s);
                }
            }
        }

        return new ItemResolveResult
        {
            SuggestedItemNames = suggestions.ToArray()
        };
    }

    public ItemResolveResult Resolve(string query, int maxSuggestions = 5)
    {
        return Resolve(query, ItemType.None, maxSuggestions);
    }
    public ItemResolveResult Resolve(string query, ItemType expectedItemType, int maxSuggestions = 5)
    {
        return Resolve(query, expectedItemType == ItemType.None ? null : x => x.Type == expectedItemType, maxSuggestions);
    }

    public ItemResolveResult Resolve(string query, Func<Item, bool> itemFilter, int maxSuggestions = 5)
    {
        EnsureManagers();

        var itemQuery = query.Trim();
        var items = itemManager.GetItems();

        if (itemFilter != null)
        {
            items = items.Where(itemFilter).ToList();
        }

        var matches = items
            .Select(x => new ItemMatchPair<RavenNest.Models.Item> { Item = x, Match = Match(x.Name, x.Type, itemQuery) })
            .Where(x => x.Match.IsCloseMatch)
            .OrderBy(x => LevenshteinDistance(x.Item.Name, itemQuery))
            .ToArray();

        var exactMatches = matches.Where(x => x.Match.IsExactMatch).ToArray();
        var item = exactMatches.Length == 1 ? exactMatches[0].Item : null;
        var suggestedItemNames = new string[0];

        if (item == null && matches.Length > 0)
        {
            var startingCases = matches.Where(x => x.Item.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (startingCases.Length > 0)
            {
                matches = startingCases;
            }

            var m = matches.Where(x => x.Match.IsMatch).ToList();
            if (m.Count > 0)
            {
                item = m[0].Item;
                suggestedItemNames = m.Select(x => x.Item.Name).Distinct().Take(maxSuggestions).ToArray();
            }
            else
            {
                suggestedItemNames = matches
                    .Where(x => x.Match.IsCloseMatch)
                    .Select(x => x.Item.Name)
                    .Distinct()
                    .OrderBy(x => LevenshteinDistance(x, itemQuery))
                    .Take(maxSuggestions)
                    .ToArray();
            }
        }
        else if (item == null)
        {
            suggestedItemNames = items
                .Where(x => IsCloseMatch(x.Name, x.Type, itemQuery, 3)) // 3: as it will allow for 3 character deltas
                .Select(x => x.Name)
                .Distinct()
                .OrderBy(x => LevenshteinDistance(x, itemQuery))
                .Take(maxSuggestions)
                .ToArray();
        }

        return new ItemResolveResult
        {
            Item = item,
            Count = 1,
            Price = item != null ? item.ShopSellPrice : 0,
            Query = query,
            SuggestedItemNames = suggestedItemNames
        };
    }
    public static int LevenshteinDistance(string s, string t)
    {
        int[,] d = new int[s.Length + 1, t.Length + 1];

        for (int i = 0; i <= s.Length; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= t.Length; j++)
        {
            d[0, j] = j;
        }

        for (int j = 1; j <= t.Length; j++)
        {
            for (int i = 1; i <= s.Length; i++)
            {
                if (s[i - 1] == t[j - 1])
                {
                    d[i, j] = d[i - 1, j - 1];
                }
                else
                {
                    d[i, j] = Math.Min(d[i - 1, j] + 1, Math.Min(d[i, j - 1] + 1, d[i - 1, j - 1] + 1));
                }
            }
        }

        return d[s.Length, t.Length];
    }

    private static bool TryParsePrice(Token token, out double price)
    {
        price = 0d;

        var values = new Dictionary<string, double>
            {
                { "k", 1000 },
                { "m", 1000_000 },
                { "b", 1000_000_000 },
            };

        var lastChar = token.Value[token.Value.Length - 1];
        if (values.TryGetValue(char.ToLower(lastChar).ToString(), out var m))
        {
            if (double.TryParse(token.Value.Remove(token.Value.Length - 1), NumberStyles.Any, new NumberFormatInfo(), out var p))
            {
                price = p * m;
                return true;
            }
        }

        if (!char.IsDigit(lastChar)) return false;
        {
            if (!double.TryParse(token.Value, NumberStyles.Any, new NumberFormatInfo(), out var p)) return false;
            price = p;
            return true;
        }
    }

    private static readonly Dictionary<string, double> amountLookup = new Dictionary<string, double>
    {
        { "k", 1_000 },
        { "m", 1_000_000 },
        { "g", 1_000_000_000 },
        { "b", 1_000_000_000 },
        { "t", 1_000_000_000_000 },
    };

    private static bool TryParseAmount(Token token, out long amount)
    {
        if (long.TryParse(token.Value, out amount))
        {
            return true;
        }

        var values = amountLookup;

        if (token.Value.StartsWith("x", StringComparison.OrdinalIgnoreCase))
        {
            var lastChar = token.Value[token.Value.Length - 1];
            if (values.TryGetValue(char.ToLower(lastChar).ToString(), out var m))
            {
                if (double.TryParse(token.Value.Remove(token.Value.Length - 1).Substring(1), NumberStyles.Any, new NumberFormatInfo(), out var p))
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
    private bool IsCloseMatch(string name, ItemType type, string itemQuery, double levenshteinThreshold = 0)
    {
        if (name.Contains(itemQuery, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IsEnchanted(name) && IsEnchanted(itemQuery))
        {
            return false;
        }

        if (itemQuery.Contains(' '))
        {
            var items = itemQuery.Split(' ');
            var materialName = items[0].ToLower();
            var itemName = name.ToLower();
            var dist = int.MaxValue;

            var possibleMaterialName = "";

            foreach (var mat in itemMaterials)
            {
                if (mat == ItemMaterial.None) continue; // skip none. that will allow certain misspelled "rune"
                var n = mat.ToString().ToLower();
                var d = LevenshteinDistance(n, materialName);
                if (d < dist)
                {
                    dist = d;
                    possibleMaterialName = n;
                }
            }

            if (!MaterialMatch(name.ToLower(), materialName) && !MaterialMatch(possibleMaterialName, itemName))
            {
                return false;
            }

            // now that we have a material match, we should check name/type match.

            var queryItemName = itemQuery.Substring(materialName.Length).Trim();
            if (name.Contains(queryItemName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (MaterialMatch(name.ToLower(), itemQuery.Trim().ToLower()))
        {
            return true;
        }

        if (levenshteinThreshold > 0)
        {
            return LevenshteinDistance(name, itemQuery) <= levenshteinThreshold;
        }

        return false;
    }

    private ItemMatchResult Match(string testItemName, ItemType testItemType, string itemQuery)
    {
        var name = testItemName.ToLower();
        if (name.Equals(itemQuery, StringComparison.OrdinalIgnoreCase))
            return ItemMatchResult.ExactMatch;

        var target = itemQuery.ToLower();
        if (target == "i2h" && name == "iron 2h sword") return ItemMatchResult.ExactMatch;
        if (target == "s2h" && name == "steel 2h sword") return ItemMatchResult.ExactMatch;
        if (target == "r2h" && name == "rune 2h sword") return ItemMatchResult.ExactMatch;
        if (target == "a2h" && name == "adamantite 2h sword") return ItemMatchResult.ExactMatch;
        if (target == "d2h" && name == "dragon 2h sword") return ItemMatchResult.ExactMatch;
        if (target == "p2h" && name == "phantom 2h sword") return ItemMatchResult.ExactMatch;
        if (target == "at2h" && name == "atlarus 2h sword") return ItemMatchResult.ExactMatch;
        if (target == "ax2h" && name == "abraxas 2h sword") return ItemMatchResult.ExactMatch;

        var q = itemQuery.ToLower();

        if (q.IndexOf("staff") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.TwoHandedStaff);
        }

        if (q.IndexOf(" spear") > 0 ||
            q.IndexOf(" 2h") > 0 || q.IndexOf(" two-h") > 0 || q.IndexOf(" two h") > 0 || (q.IndexOf(" 2 ") > 0 && q.IndexOf("sword") > 0) || q.IndexOf(" 2 h") > 0)
        {
            if (q.IndexOf("sword") > 0) return Match(testItemName, testItemType, itemQuery, ItemType.TwoHandedSword);
            if (q.IndexOf("bow") > 0) return Match(testItemName, testItemType, itemQuery, ItemType.TwoHandedBow);
            if (q.IndexOf("staff") > 0) return Match(testItemName, testItemType, itemQuery, ItemType.TwoHandedStaff);
            if (q.IndexOf("spear") > 0) return Match(testItemName, testItemType, itemQuery, ItemType.TwoHandedSpear);
            if (q.IndexOf("axe") > 0) return Match(testItemName, testItemType, itemQuery, ItemType.TwoHandedAxe);
        }

        if (q.IndexOf(" kat") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.TwoHandedSword, true);
        }

        if (q.IndexOf(" axe") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.OneHandedAxe);
        }

        if (q.IndexOf(" sword") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.OneHandedSword);
        }

        if (q.IndexOf(" bow") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.TwoHandedBow);
        }

        if (q.IndexOf(" helm") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.Helmet);
        }

        if (q.IndexOf(" plate") > 0 || q.IndexOf(" chest") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.Chest);
        }

        if (q.IndexOf(" glove") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.Gloves);
        }

        if (q.IndexOf(" leg") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.Leggings);
        }

        if (q.EndsWith("foot") || q.EndsWith("feet") || q.IndexOf(" boot") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.Boots);
        }
        if (q.IndexOf(" ammy") > 0 || q.IndexOf("amulet") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.Amulet);
        }
        if (q.IndexOf(" shield") > 0 || q.IndexOf(" kite") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.Shield);
        }
        if (q.IndexOf(" ring") > 0)
        {
            return Match(testItemName, testItemType, itemQuery, ItemType.Ring);
        }

        if (name.Equals(itemQuery, StringComparison.OrdinalIgnoreCase))
        {
            return ItemMatchResult.ExactMatch;
        }

        if (IsCloseMatch(name, testItemType, itemQuery))
        {
            return ItemMatchResult.CloseMatch;
        }

        return ItemMatchResult.NoMatch;
    }

    private bool IsEnchanted(string a)
    {
        return a.Contains("+") || a.Contains("enchanted", StringComparison.OrdinalIgnoreCase) || a.Trim().Contains(" of ", StringComparison.OrdinalIgnoreCase) || a.EndsWith(" of", StringComparison.OrdinalIgnoreCase);
    }

    private ItemMatchResult Match(string testItemName, ItemType testItemType, string itemQuery, ItemType type, bool isKatana = false)
    {
        if (testItemType != type) return ItemMatchResult.NoMatch;
        var queryParts = itemQuery.Split(' ');
        var firstQueryPart = queryParts[0].ToLower();
        var name = testItemName.ToLower();

        var nameContainsKatana = name.Contains("katana");
        if ((!isKatana && nameContainsKatana) || (isKatana && !nameContainsKatana))
        {
            return ItemMatchResult.NoMatch;
        }

        if (name.Equals(itemQuery, StringComparison.OrdinalIgnoreCase))
        {
            return ItemMatchResult.ExactMatch;
        }

        // searching for enchanted item.
        if (IsEnchanted(itemQuery) && !IsEnchanted(name))
        {
            return ItemMatchResult.NoMatch;
        }

        if (ResolveAliasName(itemQuery).Equals(name, StringComparison.OrdinalIgnoreCase) || firstQueryPart == name)
        {
            return ItemMatchResult.ExactMatch;
        }

        // filter out anything that has no material match

        var matchCount = 0;
        foreach (var q in queryParts)
        {
            var mat = ResolveAliasName(q);
            if (name.IndexOf(mat) != -1)
            {
                matchCount++;
            }
        }

        if (matchCount == 0)
        {
            return ItemMatchResult.NoMatch;
        }

        if (MaterialMatch(name, firstQueryPart))
        {
            return ItemMatchResult.Match;
        }

        if (IsCloseMatch(name, testItemType, itemQuery))
        {
            return ItemMatchResult.CloseMatch;
        }

        if (name.StartsWith(firstQueryPart))
        {
            return ItemMatchResult.CloseMatch;
        }

        return ItemMatchResult.NoMatch;
    }

    private string ResolveAliasName(string query)
    {
        return query
            .Replace("addy", "adamantite")
            .Replace("legs", "leggings")
            .Replace("legs", "leggings")
            .Replace("ammy", "amulet");
    }

    private bool MaterialMatch(string name, string target)
    {
        if (target == "b" || target.StartsWith("arc"))
        {
            return name.StartsWith("arch", StringComparison.OrdinalIgnoreCase);
        }

        if (target == "b" || target.StartsWith("bron"))
        {
            return name.StartsWith("bronze", StringComparison.OrdinalIgnoreCase);
        }

        if (target == "i" || target.StartsWith("iron"))
        {
            return name.StartsWith("iron", StringComparison.OrdinalIgnoreCase);
        }

        if (target.StartsWith("black"))
        {
            return name.StartsWith("black", StringComparison.OrdinalIgnoreCase);
        }

        if (target == "s" || target.StartsWith("steel"))
        {
            return name.StartsWith("steel", StringComparison.OrdinalIgnoreCase);
        }

        if (target == "m" || target.StartsWith("mith"))
        {
            return name.StartsWith("mithril");
        }

        if (target == "a" || target == "addy" || target.StartsWith("adam"))
        {
            return name.StartsWith("adama");
        }

        if (target == "r" || target == "runite" || target.StartsWith("rune"))
        {
            return name.StartsWith("rune", StringComparison.OrdinalIgnoreCase);
        }

        if (target == "d" || target.StartsWith("dragon"))
        {
            return name.StartsWith("dragon", StringComparison.OrdinalIgnoreCase);
        }

        if (target == "an" || target.StartsWith("anc"))
        {
            return name.StartsWith("ancient", StringComparison.OrdinalIgnoreCase);
        }

        if (target == "at" || target.StartsWith("atla"))
        {
            return name.StartsWith("atlarus", StringComparison.OrdinalIgnoreCase);
        }

        if (target == "ax" || target.StartsWith("abra"))
        {
            return name.StartsWith("abraxas", StringComparison.OrdinalIgnoreCase);
        }

        if (target == "p" || target.StartsWith("phant"))
        {
            return name.StartsWith("phantom", StringComparison.OrdinalIgnoreCase);
        }

        return false;
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

public class ItemMatchPair<T>
{
    public T Item { get; set; }
    public ItemMatchResult Match { get; set; }
}
public class ItemMatchResult
{
    public bool IsExactMatch { get; set; }
    public bool IsMatch { get; set; }
    public bool IsCloseMatch { get; set; }

    public static ItemMatchResult NoMatch => new ItemMatchResult();
    public static ItemMatchResult ExactMatch => new ItemMatchResult { IsExactMatch = true, IsCloseMatch = true, IsMatch = true };
    public static ItemMatchResult Match => new ItemMatchResult { IsExactMatch = false, IsMatch = true, IsCloseMatch = true };
    public static ItemMatchResult CloseMatch => new ItemMatchResult { IsExactMatch = false, IsMatch = false, IsCloseMatch = true };
}

public class ItemResolveResult
{
    private Item item;

    public string Query { get; set; }
    public long Count { get; set; }
    public double Price { get; set; }
    public string[] SuggestedItemNames { get; set; }
    public Guid Id => Item?.Id ?? Guid.Empty;
    public Item Item { get => item ?? InventoryItem?.Item; set => item = value; }
    public GameInventoryItem InventoryItem { get; set; }
    public PlayerController Player { get; set; }

    public static ItemResolveResult Empty { get; } = new ItemResolveResult
    {
        SuggestedItemNames = new string[0]
    };
}