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
}