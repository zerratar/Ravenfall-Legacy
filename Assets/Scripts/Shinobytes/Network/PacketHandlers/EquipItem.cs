using System.Collections.Generic;
using RavenNest.Models;

public class EquipItem : ChatBotCommandHandler<string>
{
    public EquipItem(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string targetItem, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (string.IsNullOrEmpty(targetItem))
        {
            return;
        }

        if (targetItem.Trim().Equals("all", System.StringComparison.OrdinalIgnoreCase))
        {
            // back to what is trained: an outfit asked for earlier ("!equip tank") is over
            player.Inventory.AskedOutfit = null;
            player.EquipBestItems();
            await Game.RavenNest.Players.EquipBestItemsAsync(player.Id);
            client.SendReply(gm, Localization.MSG_EQUIPPED_ALL);
            return;
        }

        // a role or a class gear line ("!equip tank", "!equip rogue"): only as the whole argument, and an item the
        // player owns by exactly that name comes first
        var outfit = GearOutfit.Parse(targetItem);
        if (outfit != null && !OwnsItemNamed(player, targetItem.Trim()))
        {
            await EquipOutfit(player, outfit, gm, client);
            return;
        }

        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var queriedItem = itemResolver.ResolveInventoryItem(player, targetItem, 5, EquippedState.NotEquipped);

        if (queriedItem.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, targetItem, string.Join(", ", queriedItem.SuggestedItemNames));
            return;
        }

        if (queriedItem.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, targetItem);
            return;
        }

        if (queriedItem.InventoryItem == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_OWNED, queriedItem.Item.Name);
            return;
        }

        if (!queriedItem.InventoryItem.IsEquippableType)
        {
            client.SendReply(gm, Localization.MSG_EQUIP_NOT_EQUIPPABLE, queriedItem.InventoryItem.Name);
            return;
        }

        if (await player.EquipAsync(queriedItem.InventoryItem))
        {
            client.SendReply(gm, Localization.MSG_EQUIPPED, queriedItem.InventoryItem.Name);
        }
        else
        {
            player.AnnounceLevelToLowToEquip(queriedItem.InventoryItem);
        }
    }

    private static bool OwnsItemNamed(PlayerController player, string name)
    {
        foreach (var item in player.Inventory.GetAllItems())
        {
            if (item == null || item.Item == null) continue;
            if (name.Equals(item.Item.Name, System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals(item.Name, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///     "!equip tank", "!equip rogue": the game dresses the player for the role or in the line, then the server
    ///     does the same from the same word (RavenNest.Models.GearOutfit), and the reply names what is worn.
    /// </summary>
    private async System.Threading.Tasks.Task EquipOutfit(PlayerController player, GearOutfit outfit, GameMessage gm, GameClient client)
    {
        var before = WornArmor(player);
        if (!player.Inventory.EquipOutfit(outfit))
        {
            // a line by name the player has nothing of: nothing changed, here or on the server
            client.SendReply(gm, Localization.MSG_EQUIP_OUTFIT_NO_GEAR, GearOutfit.NameOf(outfit.Line));
            return;
        }

        // what is worn now, read before the server answers. Compared by item, not by stack: "equip best" may take a
        // piece off and the outfit puts it back on, which makes a new entry for the same item here
        var after = WornArmor(player);
        var wornBefore = new HashSet<System.Guid>();
        foreach (var item in before) wornBefore.Add(item.Item.Id);
        var changed = after.Count != before.Count;
        for (var i = 0; !changed && i < after.Count; ++i) changed = !wornBefore.Contains(after[i].Item.Id);
        var worn = WornOf(outfit, after, out var line);
        var kept = !outfit.OnlyLine
            ? outfit.Skill != player.ActiveSkill
            : !ItemLines.Has(ItemLines.Fits(player.ActiveSkill), outfit.Line);

        await Game.RavenNest.Players.EquipBestItemsAsync(player.Id, outfit.Word);

        if (worn == null)
        {
            // a role, and the player has no class gear for it: the best of everything is on, as after "!equip all"
            client.SendReply(gm, Localization.MSG_EQUIP_OUTFIT_NO_ROLE_GEAR, outfit.Skill.ToString().ToLowerInvariant());
            return;
        }

        var format = !changed ? Localization.MSG_EQUIPPED_OUTFIT_ALREADY
            : kept ? Localization.MSG_EQUIPPED_OUTFIT_KEPT
            : Localization.MSG_EQUIPPED_OUTFIT;
        client.SendReply(gm, format, GearOutfit.NameOf(line), worn);
    }

    private static List<GameInventoryItem> WornArmor(PlayerController player)
    {
        var worn = new List<GameInventoryItem>();
        foreach (var item in player.Inventory.GetEquipmentsOfCategory(ItemCategory.Armor))
        {
            if (item != null && item.Item != null && Inventory.IsArmorSlot(Inventory.GetEquipmentSlot(item.Item.Type))) worn.Add(item);
        }
        return worn;
    }

    /// <summary>
    ///     What the reply names: the line of the outfit that is worn (for a role, the fitting line with the most
    ///     pieces on) and its tiers by name, the best first: "Aegis", "Windrunner and Warden". Null when no piece of
    ///     the outfit is worn.
    /// </summary>
    private static string WornOf(GearOutfit outfit, List<GameInventoryItem> worn, out ItemLine line)
    {
        line = outfit.Line;
        if (!outfit.OnlyLine)
        {
            var most = 0;
            foreach (var candidate in ItemLines.In(outfit.Fits))
            {
                var count = 0;
                foreach (var item in worn)
                {
                    if (ItemLines.IsClassGear(item.Item.Material) && ItemLines.Of(item.Item.Material) == candidate) count++;
                }
                // between two lines with as many pieces on, the one the word names
                if (count > most || (count == most && count > 0 && candidate == outfit.Line))
                {
                    most = count;
                    line = candidate;
                }
            }
            if (most == 0) return null;
        }

        // the tiers worn of that line, the highest first (materials are in the order of their tiers); three names at most
        var pieces = new List<GameInventoryItem>();
        foreach (var item in worn)
        {
            var material = item.Item.Material;
            if (ItemLines.IsOf(line, material)) pieces.Add(item);
        }
        if (pieces.Count == 0) return null;
        pieces.Sort((a, b) => ((int)b.Item.Material).CompareTo((int)a.Item.Material));
        var names = new List<string>();
        foreach (var item in pieces)
        {
            var name = item.Item.Material != ItemMaterial.None ? Utility.AddSpacesToSentence(item.Item.Material.ToString()) : item.Item.Name;
            if (!names.Contains(name) && names.Count < 3) names.Add(name);
        }
        if (names.Count == 1) return names[0];
        return string.Join(", ", names.GetRange(0, names.Count - 1)) + " and " + names[names.Count - 1];
    }
}
