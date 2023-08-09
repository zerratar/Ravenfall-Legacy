using System;
using System.Linq;
using RavenNest.Models;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private IoCContainer ioc;
    [SerializeField] private GameManager game;

    void Start()
    {
        if (!settings) settings = GetComponent<GameSettings>();
        if (!game) game = GetComponent<GameManager>();
        if (!ioc) ioc = GetComponent<IoCContainer>();
    }

    public CraftValidationStatus CanCraftItem(PlayerController player, Item item)
    {
        if (player.Chunk == null || player.Chunk.ChunkType != TaskType.Crafting)
        {
            return CraftValidationStatus.NeedCraftingStation;
        }

        if (player.Arena.InArena || player.Raid.InRaid)
        {
            return CraftValidationStatus.NeedCraftingStation;
        }

        if (player.GetTask() != TaskType.Crafting)
        {
            return CraftValidationStatus.NeedCraftingStation;
        }

        if (!item.Craftable)
        {
            return CraftValidationStatus.NotCraftable;
        }

        if (item.RequiredCraftingLevel > player.Stats.Crafting.Level)
        {
            return CraftValidationStatus.NotEnoughSkill;
        }

        //if (!GotEnoughResources(player, item))
        //{
        //    return CraftValidationStatus.NotEnoughResources;
        //}

        return CraftValidationStatus.OK;
    }

    //public RavenNest.Models.Item CraftItem(PlayerController player, ItemCategory itemCategory, ItemType type)
    //{
    //    var item = GetCraftableItemForPlayer(player, itemCategory, type);
    //    if (item == null) return null;
    //    if (!GotEnoughResources(player, item)) return null;
    //    player.RemoveResources(item);
    //    player.SaveResourcesAsync();
    //    return item;
    //}

    //private bool CanCraftItem(PlayerController player, RavenNest.Models.Item item)
    //{
    //    var requirements = item;
    //    if (requirements.RequiredCraftingLevel > player.Stats.Crafting.CurrentValue)
    //        return false;

    //    //if (requirements.MinCookingLevel > player.Stats.Cooking.CurrentValue)
    //    //    return false;

    //    return true;
    //}

    //private bool GotEnoughResources(PlayerController player, RavenNest.Models.Item item)
    //{
    //    if (item.OreCost > player.Resources.Ore)
    //        return false;

    //    if (item.WoodCost > player.Resources.Wood)
    //        return false;

    //    //if (item.FishCost > player.Resources.Fish)
    //    //    return false;

    //    //if (item.WheatCost > player.Resources.Wheat)
    //    //    return false;

    //    return true;
    //}

    public RavenNest.Models.Item GetCraftableItemForPlayer(PlayerController player, ItemCategory itemCategory, ItemType itemType)
    {
        //var items = this.itemRepository.All();

        var craftables = game.Items.GetItems()
            .Where(x =>
            {
                if (!x.Craftable)
                {
                    return false;
                }

                var itemDefinition = x;
                var sameCategory = itemDefinition.Category == itemCategory;
                var sameType = (itemType == ItemType.None || itemDefinition.Type == itemType);
                var enoughCraftingLevel = x.RequiredCraftingLevel <= player.Stats.Crafting.MaxLevel;
                var enoughCookingLevel =
                    true; //x.CraftingRequirements.MinCookingLevel <= player.Stats.Cooking.CurrentValue;

                var enoughDefense = x.RequiredDefenseLevel <= player.Stats.Defense.MaxLevel;
                var enoughAttack = x.RequiredAttackLevel <= player.Stats.Attack.MaxLevel;
                var enoughRanged = x.RequiredRangedLevel <= player.Stats.Ranged.MaxLevel;
                var enoughMagic = x.RequiredMagicLevel <= player.Stats.Magic.MaxLevel;
                var enoughSlayer = x.RequiredSlayerLevel <= player.Stats.Slayer.MaxLevel;

                return sameCategory &&
                       sameType &&
                       enoughCraftingLevel &&
                       enoughCookingLevel &&
                       enoughDefense &&
                       enoughAttack &&
                       enoughRanged &&
                       enoughMagic &&
                       enoughSlayer;

            }).OrderBy(x =>
                Math.Abs(x.RequiredCraftingLevel - player.Stats.Crafting.MaxLevel) +
                Math.Abs( /*x.CraftingRequirements.MinCookingLevel*/ 0 - player.Stats.Cooking.MaxLevel))
            .ToList();

        if (itemType == ItemType.None)
        {
            foreach (var craftable in craftables)
            {
                var eqItem = player.Inventory.GetEquipmentOfType(craftable.Category, craftable.Type);
                if (eqItem == null || eqItem.GetTotalStats() < craftable.GetTotalStats())
                {
                    return craftable;
                }
            }
        }

        return craftables.FirstOrDefault();
    }
}