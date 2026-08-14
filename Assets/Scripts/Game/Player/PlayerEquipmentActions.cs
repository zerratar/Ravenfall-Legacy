using System;
using System.Collections.Generic;
using Shinobytes.Linq;
using System.Threading.Tasks;
using Assets.Scripts;
using RavenNest.Models;
using UnityEngine;
using Debug = Shinobytes.Debug;

/// <summary>
/// Equip and unequip operations for a single player: putting an inventory item on, taking it off,
/// and the async variants that also tell the server.
///
/// <para>
/// Distinct from PlayerEquipment, which is the component that owns what is currently worn and its
/// visuals. This class is the set of actions performed against it, driven by chat commands and by
/// the inventory.
/// </para>
///
/// <para>
/// A plain class, not a component. See docs/player-component-architecture.md. This was the
/// cleanest of the extractions so far: the moved code touches nothing private on the controller,
/// only GameManager, Id, Inventory, IsBot, Name and Stats, all of which were already public.
/// </para>
/// </summary>
public class PlayerEquipmentActions
{
    private readonly PlayerController owner;

    public PlayerEquipmentActions(PlayerController owner)
    {
        this.owner = owner;
    }

    internal async Task<GameInventoryItem> CycleEquippedPetAsync()
    {
        var equippedPet = owner.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        var pets = owner.Inventory.GetInventoryItemsOfType(ItemCategory.Pet, ItemType.Pet);
        if (pets.Count == 0) return null;

        var equippedPetId = equippedPet?.ItemId ?? Guid.Empty;

        var petToEquip = pets
            .Where(x => x.Item.Id != equippedPetId)
            .DistinctBy(x => x.Item.Id)
            .Random();

        if (petToEquip == null)
        {
            var pet = pets.FirstOrDefault();
            owner.Inventory.Equip(pet);
            return pet;
        }

        if (!owner.IsBot)
        {
            await owner.GameManager.RavenNest.Players.EquipInventoryItemAsync(owner.Id, petToEquip.InstanceId);
        }
        owner.Inventory.Equip(petToEquip);
        return petToEquip;
    }

    internal async Task UnequipAllItemsAsync()
    {
        owner.UnequipAllItems();
        if (!owner.IsBot)
        {
            await owner.GameManager.RavenNest.Players.UnequipAllItemsAsync(owner.Id);
        }
    }

    internal async Task UnequipAsync(GameInventoryItem item)
    {
        owner.Inventory.Unequip(item, true);
        owner.Inventory.UpdateEquipmentEffect();

        if (!owner.IsBot)
        {
            await owner.GameManager.RavenNest.Players.UnequipInventoryItemAsync(owner.Id, item.InstanceId);
        }
    }

    internal void Unequip(GameInventoryItem item)
    {
        owner.Inventory.Unequip(item);
    }

    public void Equip(GameInventoryItem item, bool reportShieldWarning = true)
    {
        if (item.Type == ItemType.Shield)
        {
            var thw = owner.Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedSword); // we will get either.
            if (thw != null && (thw.Type == ItemType.TwoHandedAxe || thw.Type == ItemType.TwoHandedSword || thw.Type == ItemType.TwoHandedSpear))
            {
                if (reportShieldWarning)
                {
                    owner.GameManager.RavenBot.SendReply(owner, Localization.EQUIP_SHIELD_AND_TWOHANDED);
                }
                return;
            }
        }

        if (item.Type == ItemType.OneHandedAxe || item.Type == ItemType.OneHandedSword)
        {
            var eqShield = owner.Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Shield);
            if (eqShield == null)
            {
                var shields = owner.Inventory.GetInventoryItemsOfType(ItemCategory.Armor, ItemType.Shield);
                var shield = shields.OrderByDescending(Inventory.GetItemValue)
                    .FirstOrDefault(owner.Inventory.CanEquipItem);

                if (shield != null)
                {
                    owner.Inventory.Equip(shield);
                }
            }
        }

        var equipped = owner.Inventory.Equip(item);
        if (!equipped)
        {
            if (!item.IsEquippableType)
            {
                owner.GameManager.RavenBot.SendReply(owner, "{itemName} can't be equipped.", item.Name);
                return;
            }

            var reqLevels = new List<string>();
            var requirement = "You require level ";
            if (item.RequiredAttackLevel > owner.Stats.Attack.Level) reqLevels.Add(item.RequiredAttackLevel + " Attack.");
            if (item.RequiredDefenseLevel > owner.Stats.Defense.Level) reqLevels.Add(item.RequiredDefenseLevel + " Defense.");
            if (item.RequiredMagicLevel > owner.Stats.Magic.Level || item.RequiredMagicLevel > owner.Stats.Healing.Level) reqLevels.Add(item.RequiredMagicLevel + " Magic or Healing.");
            if (item.RequiredRangedLevel > owner.Stats.Ranged.Level) reqLevels.Add(item.RequiredRangedLevel + " Ranged.");
            if (item.RequiredSlayerLevel > owner.Stats.Slayer.Level) reqLevels.Add(item.RequiredSlayerLevel + " Slayer.");
            if (reqLevels.Count > 0)
            {
                owner.GameManager.RavenBot.SendReply(owner, "You do not meet the requirements to equip " + item.Name + ". " + requirement + string.Join(" ", reqLevels.ToArray()));
            }
            return;
        }
    }

    public void AnnounceLevelToLowToEquip(GameInventoryItem item)
    {

        if (!item.IsEquippableType)
        {
            owner.GameManager.RavenBot.SendReply(owner, "{itemName} can't be equipped.", item.Name);
            return;
        }

        var reqLevels = new List<string>();
        var requirement = "You require level ";
        if (item.RequiredAttackLevel > owner.Stats.Attack.Level) reqLevels.Add(item.RequiredAttackLevel + " Attack.");
        if (item.RequiredDefenseLevel > owner.Stats.Defense.Level) reqLevels.Add(item.RequiredDefenseLevel + " Defense.");
        if (item.RequiredMagicLevel > owner.Stats.Magic.Level || item.RequiredMagicLevel > owner.Stats.Healing.Level) reqLevels.Add(item.RequiredMagicLevel + " Magic or Healing.");
        if (item.RequiredRangedLevel > owner.Stats.Ranged.Level) reqLevels.Add(item.RequiredRangedLevel + " Ranged.");
        if (item.RequiredSlayerLevel > owner.Stats.Slayer.Level) reqLevels.Add(item.RequiredSlayerLevel + " Slayer.");
        if (reqLevels.Count > 0)
        {
            owner.GameManager.RavenBot.SendReply(owner, "You do not meet the requirements to equip " + item.Name + ". " + requirement + string.Join(" ", reqLevels.ToArray()));
        }
    }

    internal async Task<bool> EquipAsync(GameInventoryItem item)
    {
        if (owner.IsBot)
        {
            return true;
        }

        if (await owner.GameManager.RavenNest.Players.EquipInventoryItemAsync(owner.Id, item.InstanceId))
        {
            Equip(item);

            return true;
        }

        return false;
    }

    internal async Task<bool> EquipAsync(Item item)
    {
        if (item.Type == ItemType.Shield)
        {
            var thw = owner.Inventory.GetEquipmentOfType(ItemCategory.Weapon, ItemType.TwoHandedSword); // we will get either.
            if (thw != null && (thw.Type == ItemType.TwoHandedAxe || thw.Type == ItemType.TwoHandedSword || thw.Type == ItemType.TwoHandedSpear))
            {
                owner.GameManager.RavenBot.SendReply(owner, Localization.EQUIP_SHIELD_AND_TWOHANDED);
                return false;
            }
        }

        if (item.Type == ItemType.OneHandedAxe || item.Type == ItemType.OneHandedSword)
        {
            var eqShield = owner.Inventory.GetEquipmentOfType(ItemCategory.Armor, ItemType.Shield);
            if (eqShield == null)
            {
                var shields = owner.Inventory.GetInventoryItemsOfType(ItemCategory.Armor, ItemType.Shield);
                var shield = shields.OrderByDescending(Inventory.GetItemValue)
                    .FirstOrDefault(owner.Inventory.CanEquipItem);

                if (shield != null)
                {
                    owner.Inventory.Equip(shield);
                }
            }
        }

        var equipped = owner.Inventory.EquipByItemId(item.Id);
        if (!equipped)
        {
            var reqLevels = new List<string>();
            var requirement = "You require level ";
            if (item.RequiredAttackLevel > owner.Stats.Attack.Level) reqLevels.Add(item.RequiredAttackLevel + " Attack.");
            if (item.RequiredDefenseLevel > owner.Stats.Defense.Level) reqLevels.Add(item.RequiredDefenseLevel + " Defense.");
            if (item.RequiredMagicLevel > owner.Stats.Magic.Level || item.RequiredMagicLevel > owner.Stats.Healing.Level) reqLevels.Add(item.RequiredMagicLevel + " Magic or Healing.");
            if (item.RequiredRangedLevel > owner.Stats.Ranged.Level) reqLevels.Add(item.RequiredRangedLevel + " Ranged.");
            if (item.RequiredSlayerLevel > owner.Stats.Slayer.Level) reqLevels.Add(item.RequiredSlayerLevel + " Slayer.");
            if (reqLevels.Count > 0)
            {
                owner.GameManager.RavenBot.SendReply(owner, "You do not meet the requirements to equip " + item.Name + ". " + requirement + string.Join(" ", reqLevels.ToArray()));
            }
            return false;
        }

        if (owner.IsBot)
        {
            return true;
        }

        if (await owner.GameManager.RavenNest.Players.EquipItemAsync(owner.Id, item.Id))
        {
            return false;
        }

        return true;
    }
}
