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

        player.Inventory.UpdateInventoryItem(result.InventoryItemId, result.NewStackAmount);

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
