using System;
using System.Collections.Generic;
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
            client.SendReply(gm, "{itemName} cannot be used.", queriedItem.Item.Name);
            return;
        }

        if (queriedItem.Item.Name.Contains("tome of", StringComparison.OrdinalIgnoreCase))
        {
            client.SendReply(gm, "{itemName} cannot be used directly, use !teleport <island> instead.", queriedItem.Item.Name);
            return;
        }

        var result = await Game.RavenNest.Players.UseItemAsync(player.Id, queriedItem.InventoryItem.InstanceId);
        if (result == null || result.InventoryItemId == Guid.Empty)
        {
            client.SendReply(gm, "{itemName} can not be used right now.", queriedItem.InventoryItem.Name);
            return;
        }

        if (result.Effects.Count == 0)
        {
            client.SendReply(gm, "Nothing good will happen if you play with {itemName}.", queriedItem.InventoryItem.Name);
            return;
        }

        player.Inventory.UpdateInventoryItem(result.InventoryItemId, result.NewStackAmount);

        // depending on item type, change the term.
        var term = "have used";


        // special case until its been corrected into a potion
        if (queriedItem.Item.Name.Equals("red wine", StringComparison.OrdinalIgnoreCase) || queriedItem.Item.Type == ItemType.Potion)
        {
            term = "drank " + (Utility.IsVocal(queriedItem.Item.Name.ToLower()[0]) ? "an" : "a");
        }
        else if (queriedItem.Item.Type == ItemType.Food)
        {
            term = "ate " + (Utility.IsVocal(queriedItem.Item.Name.ToLower()[0]) ? "an" : "a");
        }

        var message = "You " + term + " {itemName}. ";
        var args = new List<object> { queriedItem.InventoryItem.Item.Name };

        if (result.Teleport)
        {
            var targetIsland = Game.Islands.Get(result.EffectIsland);
            player.Teleporter.Teleport(targetIsland);
            message += "You were teleported to {island}! ";
            args.Add(result.EffectIsland.ToString());
        }

        for (int i = 0; i < result.Effects.Count; i++)
        {
            CharacterStatusEffect effect = result.Effects[i];

            if (effect.Type == StatusEffectType.TeleportToIsland)
            {
                // this is handled above.
                continue;
            }

            if (effect.Type == StatusEffectType.Heal)
            {
                var amount = player.ApplyInstantHealEffect(effect);
                message += "You have recovered {healthAmount" + i + "} HP! ";
                args.Add(amount.ToString());
                continue;
            }

            if (effect.Type == StatusEffectType.Damage)
            {
                var amount = player.ApplyInstantDamageEffect(effect);
                message += "You took {damageAmount" + i + "} damage! ";
                args.Add(amount.ToString());
                continue;
            }

            var timeLeft = TimeSpan.FromSeconds(effect.TimeLeft);
            var proc = UnityEngine.Mathf.FloorToInt((float)effect.Amount * 100);
            if (effect.Type == StatusEffectType.HealOverTime)
            {
                message += "You will be healed {healPercent" + i + "}% of your total health over {healTotalSeconds" + i + "} seconds. ";
                args.Add(proc.ToString().Replace(",", "."));
                args.Add(((int)timeLeft.TotalSeconds).ToString());
            }
            else
            {
                if (timeLeft.TotalMinutes > 1)
                {
                    if (timeLeft.Seconds > 0)
                    {
                        message += "You gained {effectPercent" + i + "}% {effectType" + i + "} for {effectMinutes" + i + "} minutes and {effectSecconds" + i + "} seconds. ";
                        args.Add(proc.ToString().Replace(",", "."));
                        args.Add(Utility.AddSpacesToSentence(effect.Type.ToString()));
                        args.Add(timeLeft.Minutes.ToString());
                        args.Add(timeLeft.Seconds.ToString());
                    }
                    else
                    {
                        message += "You gained {effectPercent" + i + "}% {effectType" + i + "} for {effectMinutes" + i + "} minutes. ";
                        args.Add(proc.ToString().Replace(",", "."));
                        args.Add(Utility.AddSpacesToSentence(effect.Type.ToString()));
                        args.Add(((int)timeLeft.TotalMinutes).ToString());
                    }
                }
                else
                {
                    message += "You gained {effectPercent" + i + "}% {effectType" + i + "} for {effectSeconds" + i + "} seconds. ";
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
}
