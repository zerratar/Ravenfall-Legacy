using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RavenNest.Models;

public class UseItem : ChatBotCommandHandler<string>
{
    public UseItem(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string inputQuery, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (string.IsNullOrEmpty(inputQuery))
        {
            client.SendReply(gm, Localization.MSG_ITEM_USE_MISSING_ARGS);
            return;
        }

        // an experience book names the skill it is read for after the item: "gathering book fishing", "large combat book magic 2"
        if (ExperienceBooks.IsBookQuery(inputQuery))
        {
            try
            {
                await ExperienceBooks.UseAsync(Game, player, inputQuery, gm, client);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("!use book failed: " + exc);
                client.SendReply(gm, Localization.MSG_USE_ITEM_NOT_NOW, inputQuery);
            }
            return;
        }

        if (inputQuery.Contains("scroll", StringComparison.OrdinalIgnoreCase))
        {
            if (inputQuery.Contains("exp", StringComparison.OrdinalIgnoreCase))
            {
                // redirect to UseExpMultiplierScroll instead

                // try and get a number out of all parts of the string
                var parts = inputQuery.Split(' ');
                if (parts.Length > 2)
                {
                    var useCount =
                        parts.Any(parts => parts.Contains("max", StringComparison.OrdinalIgnoreCase) || 
                        parts.Contains("all", StringComparison.OrdinalIgnoreCase)) ? 100 : 0;

                    if (useCount == 0)
                    {
                        // "use exp scroll 5": the first number is the count, none means one
                        useCount = parts
                            .Select(x => int.TryParse(x, out var result) ? result : 0)
                            .FirstOrDefault(x => x > 0);
                        if (useCount == 0) useCount = 1;
                    }

                    new UseExpMultiplierScroll(Game, Server, PlayerManager).Handle(useCount, gm, client);
                    return;
                }
                new UseExpMultiplierScroll(Game, Server, PlayerManager).Handle(1, gm, client);
                return;
            }

            if (inputQuery.Contains("raid", StringComparison.OrdinalIgnoreCase))
            {
                // redirect to UseExpMultiplierScroll instead
                new RaidForce(Game, Server, PlayerManager).Handle(gm, client);
                return;
            }

            if (inputQuery.Contains("dungeon", StringComparison.OrdinalIgnoreCase))
            {
                // redirect to UseExpMultiplierScroll instead
                new DungeonForce(Game, Server, PlayerManager).Handle(gm, client);
                return;
            }
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveTradeQuery(inputQuery, parsePrice: false, parseUsername: false, parseAmount: false, playerToSearch: player);

        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, inputQuery, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (queriedItem.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (queriedItem.InventoryItem == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_OWNED, inputQuery);
            return;
        }

        // an experience book found by its name alone ("!use small gathering"): it needs the skill it is read for
        var bookCategory = ExperienceBooks.CategoryOfItem(queriedItem.Item.Name);
        if (bookCategory != null)
        {
            client.SendReply(gm, Localization.MSG_BOOK_USAGE, bookCategory.Key, bookCategory.Key, ExperienceBooks.SkillNames(bookCategory));
            return;
        }

        if (queriedItem.Item.Type != ItemType.Food && queriedItem.Item.Type != ItemType.Scroll && queriedItem.Item.Type != ItemType.Potion)
        {
            client.SendReply(gm, Localization.MSG_USE_ITEM_CANNOT_USE, queriedItem.Item.Name);
            return;
        }

        if (queriedItem.Item.Name.Contains("tome of", StringComparison.OrdinalIgnoreCase))
        {
            client.SendReply(gm, Localization.MSG_USE_ITEM_TOME, queriedItem.Item.Name);
            return;
        }

        if (queriedItem.Item.Type == ItemType.Scroll && queriedItem.Item.Name.Contains("ferry", StringComparison.OrdinalIgnoreCase))
        {
            // check if we have an active ferry boost.
            if (Game.Ferry.IsFerryBoostActive)
            {
                client.SendReply(gm, Localization.MSG_USE_ITEM_FERRY_BOOST_ACTIVE, Game.Ferry.GetRemainingBoostTime());
                return;
            }
        }

        var result = await Game.RavenNest.Players.UseItemAsync(player.Id, queriedItem.InventoryItem.InstanceId);
        if (result == null || result.InventoryItemId == Guid.Empty)
        {
            client.SendReply(gm, Localization.MSG_USE_ITEM_NOT_NOW, queriedItem.InventoryItem.Name);
            return;
        }

        if (result.Effects.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_USE_ITEM_NO_EFFECT, queriedItem.InventoryItem.Name);
            return;
        }

        if (!player.Inventory.FedByServer) player.Inventory.UpdateInventoryItem(result.InventoryItemId, result.NewStackAmount); // else the server's feed carries the change (server authority)

        // depending on item type, change the term.
        var message = Localization.MSG_USE_ITEM_USED;
        var startsWithVowel = Utility.IsVocal(queriedItem.Item.Name.ToLower()[0]);

        // special case until its been corrected into a potion
        if (queriedItem.Item.Name.Equals("red wine", StringComparison.OrdinalIgnoreCase) || queriedItem.Item.Type == ItemType.Potion)
        {
            message = startsWithVowel ? Localization.MSG_USE_ITEM_DRANK_AN : Localization.MSG_USE_ITEM_DRANK_A;
        }
        else if (queriedItem.Item.Type == ItemType.Food)
        {
            message = startsWithVowel ? Localization.MSG_USE_ITEM_ATE_AN : Localization.MSG_USE_ITEM_ATE_A;
        }

        var args = new List<object> { queriedItem.InventoryItem.Item.Name };

        if (result.Teleport)
        {
            var targetIsland = Game.Islands.Get(result.EffectIsland);
            player.teleportHandler.Teleport(targetIsland, true);
            message += " " + Localization.MSG_USE_ITEM_TELEPORTED;
            args.Add(result.EffectIsland.ToString());
        }

        for (int i = 0; i < result.Effects.Count; i++)
        {
            CharacterStatusEffect effect = result.Effects[i];

            if (effect.Type == StatusEffectType.IncreasedFerrySpeed)
            {
                Game.Ferry.ApplyFerryBoost(effect);
                message += " " + Indexed(Localization.MSG_USE_ITEM_FERRY_FASTER, i);
                args.Add(Game.Ferry.GetRemainingBoostTime());
                continue;
            }

            if (effect.Type == StatusEffectType.TeleportToIsland)
            {
                // this is handled above.
                continue;
            }

            if (effect.Type == StatusEffectType.Heal)
            {
                var amount = player.ApplyInstantHealEffect(effect);
                message += " " + Indexed(Localization.MSG_USE_ITEM_HEALED, i);
                args.Add(amount.ToString());
                continue;
            }

            if (effect.Type == StatusEffectType.Damage)
            {
                var amount = player.ApplyInstantDamageEffect(effect);
                message += " " + Indexed(Localization.MSG_USE_ITEM_DAMAGED, i);
                args.Add(amount.ToString());
                continue;
            }

            var timeLeft = TimeSpan.FromSeconds(effect.TimeLeft);
            var proc = UnityEngine.Mathf.FloorToInt((float)effect.Amount * 100);
            if (effect.Type == StatusEffectType.HealOverTime)
            {
                message += " " + Indexed(Localization.MSG_USE_ITEM_HEAL_OVER_TIME, i);
                args.Add(proc.ToString().Replace(",", "."));
                args.Add(((int)timeLeft.TotalSeconds).ToString());
            }
            else
            {
                if (timeLeft.TotalMinutes > 1)
                {
                    if (timeLeft.Seconds > 0)
                    {
                        message += " " + Indexed(Localization.MSG_USE_ITEM_GAINED_MINUTES_SECONDS, i);
                        args.Add(proc.ToString().Replace(",", "."));
                        args.Add(Utility.AddSpacesToSentence(effect.Type.ToString()));
                        args.Add(timeLeft.Minutes.ToString());
                        args.Add(timeLeft.Seconds.ToString());
                    }
                    else
                    {
                        message += " " + Indexed(Localization.MSG_USE_ITEM_GAINED_MINUTES, i);
                        args.Add(proc.ToString().Replace(",", "."));
                        args.Add(Utility.AddSpacesToSentence(effect.Type.ToString()));
                        args.Add(((int)timeLeft.TotalMinutes).ToString());
                    }
                }
                else
                {
                    message += " " + Indexed(Localization.MSG_USE_ITEM_GAINED_SECONDS, i);
                    args.Add(proc.ToString().Replace(",", "."));
                    args.Add(Utility.AddSpacesToSentence(effect.Type.ToString()));
                    args.Add(((int)timeLeft.TotalSeconds).ToString());
                }
            }

            player.ApplyStatusEffect(effect);
        }

        player.UpdateEquipmentEffect();
        client.SendReply(gm, message.Trim(), args.ToArray());
    }

    private static readonly Regex PlaceholderName = new Regex(@"\{([A-Za-z0-9]+)\}", RegexOptions.Compiled);

    // The message is built from several sentences, each effect gets its own placeholder names
    // ({healthAmount0}, {healthAmount1}) so they stay unique within the one message.
    private static string Indexed(string format, int index)
    {
        return PlaceholderName.Replace(format, m => "{" + m.Groups[1].Value + index + "}");
    }
}

/// <summary>
/// Experience books (v0.9.7): what a quest gives in place of experience for a skill that is at the top level already.
/// A book belongs to a category of skills and is read for one of them: !use gathering book fishing, with a number or
/// "all" after it for more than one, and "small", "medium" or "large" before it to pick a size (the smallest go first). A
/// book is a share of a level, not an amount of experience: each one gives that share of the next level of the skill
/// it is read for, so a book earned at level 999 does not send a low skill up dozens of levels.
/// </summary>
/// <remarks>
/// The server has the same categories and the sizes (RavenNest's ExperienceBooks), takes the books and says how many
/// it took and the share of a level each is; this words the replies and adds the experience, as the game does for a
/// quest's reward (the game's skills are what get saved).
/// </remarks>
public static class ExperienceBooks
{
    public sealed class Category
    {
        /// <summary>"gathering": the word in the item's name, and what players type.</summary>
        public string Key;

        /// <summary>Other words players may use for it.</summary>
        public string[] Words;
        public Skill[] Skills;
    }

    /// <summary>The word every book's name ends with ("Small Gathering Book").</summary>
    public const string ItemWord = "Book";

    // the shares the server's books carry, only to work out how many it takes to reach the top level
    private static readonly double[] Shares = { 0.15, 0.5, 0.9 };

    /// <summary>The sizes, smallest first, as the items are named ("Medium Gathering Book").</summary>
    private static readonly string[] SizeWords = { "small", "medium", "large" };

    /// <summary>The most books one !use reads (the server's limit too).</summary>
    private const int MaxPerUse = 100;

    // Every skill is in exactly one category. Health is the skill players see as Vitality.
    public static readonly Category[] Categories =
    {
        new Category { Key = "combat", Words = new[] { "fighting", "battle" }, Skills = new[] { Skill.Attack, Skill.Defense, Skill.Strength, Skill.Health, Skill.Ranged, Skill.Magic, Skill.Healing } },
        new Category { Key = "gathering", Words = new[] { "gather", "resource" }, Skills = new[] { Skill.Woodcutting, Skill.Mining, Skill.Fishing, Skill.Farming, Skill.Gathering } },
        new Category { Key = "artisan", Words = new[] { "production", "artisans" }, Skills = new[] { Skill.Crafting, Skill.Cooking, Skill.Alchemy } },
        new Category { Key = "adventure", Words = new[] { "adventurer", "adventures" }, Skills = new[] { Skill.Slayer, Skill.Sailing, Skill.Taming } },
    };

    public static Category CategoryOf(Skill skill)
    {
        foreach (var c in Categories)
        {
            if (Array.IndexOf(c.Skills, skill) >= 0) return c;
        }
        return null;
    }

    /// <summary>The category of a book by its item's name ("Small Gathering Book"), or null for any other item.</summary>
    public static Category CategoryOfItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || !itemName.EndsWith(" " + ItemWord, StringComparison.OrdinalIgnoreCase)) return null;
        foreach (var c in Categories)
        {
            for (var size = 0; size < SizeWords.Length; ++size)
            {
                if (itemName.Equals(ItemName(c, size), StringComparison.OrdinalIgnoreCase)) return c;
            }
        }
        return null;
    }

    private static string ItemName(Category category, int size) => SizeWords[size] + " " + category.Key + " book";

    /// <summary>"Woodcutting, Mining, Fishing or Gathering": a category's skills as players see them, without one of them.</summary>
    public static string SkillNames(Category category, Skill except = Skill.None)
    {
        var names = new List<string>();
        foreach (var s in category.Skills)
        {
            if (s != except) names.Add(Localization.SkillName(s));
        }
        if (names.Count == 0) return string.Empty;
        if (names.Count == 1) return names[0];
        return string.Join(", ", names.GetRange(0, names.Count - 1)) + Localization.UI_BOOK_SKILLS_OR + names[names.Count - 1];
    }

    /// <summary>Whether a !use names a book: the word "book" on its own.</summary>
    public static bool IsBookQuery(string query)
    {
        if (string.IsNullOrEmpty(query)) return false;
        foreach (var word in query.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (word == "book" || word == "books") return true;
        }
        return false;
    }

    /// <summary>
    /// !use [small|medium|large] (category) book (skill) [number|all]. The category may be left out when the skill says it
    /// ("!use book fishing"). Says why when the skill is missing, is not one of the book's, or is at the top level too.
    /// </summary>
    public static async System.Threading.Tasks.Task UseAsync(GameManager game, PlayerController player, string query, GameMessage gm, GameClient client)
    {
        Category category = null;
        var skill = Skill.None;
        var count = 1;
        var asked = -1; // the size named, or -1
        foreach (var word in query.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (word == "book" || word == "books" || word == "of" || word == "on" || word == "for" || word == "to" || word == "the" || word == "a") continue;
            if (word == "small" || word == "little") { asked = 0; continue; }
            if (word == "medium" || word == "middle") { asked = 1; continue; }
            if (word == "large" || word == "big") { asked = 2; continue; }
            if (word == "all" || word == "max") { count = MaxPerUse; continue; }
            // a number is how many, never a skill (a number alone would parse as the skill with that index)
            if (int.TryParse(word, out var number)) { count = Math.Min(Math.Max(number, 1), MaxPerUse); continue; }

            // the first word that names a category is the book's ("gathering" is a skill too: the second one is the skill)
            if (category == null)
            {
                var named = Array.Find(Categories, c => c.Key == word || Array.IndexOf(c.Words, word) >= 0);
                if (named != null) { category = named; continue; }
            }
            if (skill == Skill.None)
            {
                var parsed = SkillUtilities.ParseSkill(word);
                if (parsed != Skill.None && CategoryOf(parsed) != null) skill = parsed;
            }
        }

        // "!use book fishing": the skill says which book
        if (category == null && skill != Skill.None) category = CategoryOf(skill);
        if (category == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, query);
            return;
        }

        // the smallest ones first, unless a size is named
        var owned = new GameInventoryItem[SizeWords.Length];
        foreach (var item in player.Inventory.GetBackpackItems().ToArray())
        {
            if (item == null || item.Amount <= 0 || item.Item == null || item.Item.Name == null) continue;
            for (var s = 0; s < owned.Length; ++s)
            {
                if (item.Item.Name.Equals(ItemName(category, s), StringComparison.OrdinalIgnoreCase)) owned[s] = item;
            }
        }
        var size = asked;
        if (size < 0) size = Array.FindIndex(owned, o => o != null);
        var book = size >= 0 ? owned[size] : null;
        if (book == null)
        {
            client.SendReply(gm, Localization.MSG_BOOK_NONE, asked >= 0 ? SizeWords[asked] + " " + category.Key : category.Key);
            return;
        }

        if (skill == Skill.None)
        {
            client.SendReply(gm, Localization.MSG_BOOK_USAGE, category.Key, category.Key, SkillNames(category));
            return;
        }
        if (Array.IndexOf(category.Skills, skill) < 0)
        {
            client.SendReply(gm, Localization.MSG_BOOK_WRONG_SKILL, category.Key, Localization.SkillName(skill), SkillNames(category));
            return;
        }
        var stat = player.GetSkill(skill);
        if (stat == null || stat.Level >= GameMath.MaxLevel)
        {
            client.SendReply(gm, Localization.MSG_BOOK_MAXED, Localization.SkillName(skill), category.Key, SkillNames(category, skill));
            return;
        }

        // no more than the stack has, and no more than it takes to reach the top level: the rest would be wasted
        count = (int)Math.Min(count, book.Amount);
        count = BooksToTop(stat, Shares[size], count);

        var bookName = book.Item.Name;
        // the server reads "Fishing" or "Fishing-3"
        var result = await game.RavenNest.Players.UseItemAsync(player.Id, book.InstanceId, skill.ToString() + (count > 1 ? "-" + count : ""));
        if (result == null || result.InventoryItemId == Guid.Empty || result.Effects == null || result.Effects.Count == 0)
        {
            client.SendReply(gm, Localization.MSG_USE_ITEM_NOT_NOW, bookName);
            return;
        }

        if (!player.Inventory.FedByServer) player.Inventory.UpdateInventoryItem(result.InventoryItemId, result.NewStackAmount); // else the server's feed carries the change (server authority)

        // the server took the books: the share of a level each one is, and how many it took
        var share = (double)result.Effects[0].Amount;
        var read = Math.Max(1, (int)Math.Round(result.Effects[0].Duration));
        double gained = 0;
        for (var i = 0; i < read && stat.Level < GameMath.MaxLevel; ++i)
        {
            var exp = Math.Round(GameMath.ExperienceForLevel(stat.Level + 1) * share);
            if (exp <= 0) break;
            stat.AddExp(exp);
            gained += exp;
        }

        client.SendReply(gm, Localization.MSG_BOOK_READ, bookName, read,
            gained.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), Localization.SkillName(skill), (int)stat.Level);
    }

    /// <summary>How many books of this share of a level it takes to bring a skill to the top level, at most <paramref name="most"/>.</summary>
    private static int BooksToTop(SkillStat stat, double share, int most)
    {
        int level = stat.Level;
        double experience = stat.Experience;
        var books = 0;
        while (books < most && level < GameMath.MaxLevel)
        {
            books++;
            experience += Math.Round(GameMath.ExperienceForLevel(level + 1) * share);
            while (level < GameMath.MaxLevel && experience >= GameMath.ExperienceForLevel(level + 1))
            {
                experience -= GameMath.ExperienceForLevel(level + 1);
                level++;
            }
        }
        return Math.Max(1, books);
    }
}
