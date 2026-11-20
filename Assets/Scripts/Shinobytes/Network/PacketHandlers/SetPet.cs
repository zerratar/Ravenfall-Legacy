using RavenNest.Models;
using System.Linq;

public class SetPet : ChatBotCommandHandler<string>
{
    public SetPet(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
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

        // !pet starter fox, !pet wake raven...: battle pet commands sent to the cosmetic one, which looked for an item
        // called "starter fox"
        var first = (inputQuery ?? "").Trim().Split(' ')[0].ToLowerInvariant();
        if (System.Array.IndexOf(BattlePetActions, first) >= 0)
        {
            client.SendReply(gm, Localization.MSG_PET_IS_BATTLE_COMMAND, inputQuery.Trim());
            return;
        }

        var itemQuery = inputQuery;
        var ioc = Game.gameObject.GetComponent<IoCContainer>();
        var itemResolver = ioc.Resolve<IItemResolver>();
        var item = itemResolver.ResolveInventoryItem(player, itemQuery + " pet", 5, EquippedState.NotEquipped);

        if (item.SuggestedItemNames.Length > 0)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND_SUGGEST, itemQuery, string.Join(", ", item.SuggestedItemNames));
            return;
        }

        if (item.Item == null)
        {
            client.SendReply(gm, Localization.MSG_ITEM_NOT_FOUND, inputQuery);
            return;
        }

        if (item.Item.Type != ItemType.Pet)
        {
            client.SendReply(gm, Localization.MSG_SET_PET_NOT_PET, item.Item.Name);
            return;
        }

        var invItem = item.InventoryItem;
        if (invItem == null)
        {
            client.SendReply(gm, Localization.MSG_SET_PET_NOT_OWNED, item.Item.Name);
            return;
        }

        var equippedPet = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        if (equippedPet == null || equippedPet.ItemId != item.Id)
        {
            await player.EquipAsync(item.Item);
        }

        // while a battle pet is out the cosmetic one is hidden: say so, or the change looks like it did nothing
        var battlePet = player.BattlePets != null ? player.BattlePets.Active : null;
        client.SendReply(gm, Localization.MSG_SET_PET + (battlePet != null ? string.Format(Localization.MSG_PET_COSMETIC_HIDDEN, battlePet.Name) : ""), item.Item.Name);
    }

    private static readonly string[] BattlePetActions = { "starter", "start", "stable", "use", "away", "wake", "awaken", "hatch", "evolve", "rename", "skills", "slot", "release", "about" };
}
