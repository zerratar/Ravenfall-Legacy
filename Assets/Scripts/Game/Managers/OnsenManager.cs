using RavenNest.Models;
using Shinobytes.Linq;
using System.Collections.Generic;

//using System.Linq;
using UnityEngine;

public class OnsenManager : MonoBehaviour
{
    private OnsenController[] restingAreas;

    [SerializeField] private GameManager game;

    private Vector3[] entryPoints;
    private bool initialized;

    public Vector3[] EntryPoint => entryPoints;//.EntryPoint;

    //private readonly Dictionary<Island, bool> hasRestingArea = new Dictionary<Island, bool>
    //{
    //    { Island.Ferry, false },
    //    { Island.Home, false },
    //    { Island.Away, false },
    //    { Island.Ironhill, false },
    //    { Island.Kyo, true },
    //    { Island.Heim, true },
    //    { Island.Atria, true },
    //    { Island.Eldara, true }
    //};

    private readonly bool[] restingAreasAvailable = new bool[16]
    {
        /* Ferry */ false,
        /* Home */ false,
        /* Away */ false,
        /* Ironhill */ false,
        /* Kyo */ true,
        /* Heim */ true,
        /* Atria */ true,
        /* Eldara */ true,
        
        /*  8 */ false,
        /*  9 */ false,
        /* 10 */ false,
        /* 11 */ false,
        /* 12 */ false,
        /* 13 */ false,
        /* 14 */ false,
        /* 15 */ false,
    };

    private void Start()
    {
        if (!game) game = FindAnyObjectByType<GameManager>();

        restingAreas = FindObjectsByType<OnsenController>(FindObjectsSortMode.None);

        this.entryPoints = restingAreas.Select(x => x.EntryPoint).ToArray();

        initialized = true;
    }

    public bool RestingAreaAvailable(IslandController island)
    {
        if (island == null || !initialized) return false;

        var value = (byte)island.Island;
        if (value >= restingAreasAvailable.Length) return false;
        return restingAreasAvailable[value];

        //if (hasRestingArea.TryGetValue(island.Island, out var r)) return r;
        //for (var i = 0; i < restingAreas.Length; ++i)
        //{
        //    if (restingAreas[i].Island.Identifier == island.Identifier)
        //    {
        //        hasRestingArea[island.Island] = true;
        //        return true;
        //    }
        //}
        //return false;
    }

    public bool Join(PlayerController player)
    {
        var restingArea = restingAreas.FirstOrDefault(x => x.Island.Identifier == player.Island?.Identifier);

        if (!restingArea)
        {
            if (!player.Island)
            {
                game.RavenBot.SendReply(player, Localization.MSG_ONSEN_FERRY.Random());
                return false;
            }

#if DEBUG
            if (!AdminControlData.ControlPlayers)
            {
                game.RavenBot.SendReply(player, Localization.MSG_ONSEN_WRONG_ISLAND, player.Island.Identifier);
            }
#else
            game.RavenBot.SendReply(player, Localization.MSG_ONSEN_WRONG_ISLAND, player.Island.Identifier);
#endif
            return false;
        }

        player.onsenHandler.Enter(restingArea);

        return true;
    }
}
