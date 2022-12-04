using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RavenNest.Models;
using UnityEngine;

public class OverlayPlayerManager
{
    private readonly GameObject playerPrefab;
    private ConcurrentDictionary<Guid, PlayerMap> players = new ConcurrentDictionary<Guid, PlayerMap>();

    public OverlayPlayerManager(GameObject playerPrefab)
    {
        this.playerPrefab = playerPrefab;
    }

    internal bool AddOrUpdate(OverlayPlayer data, out PlayerController playerController)
    {
        if (!players.TryGetValue(data.Character.Id, out var pm) || !pm.PlayerController)
        {
            playerController = ReplacePlayerController(data, out _);
            return true;
        }

        Update(data, out var playerMap);
        playerController = playerMap.PlayerController;
        return false;
    }

    private PlayerController ReplacePlayerController(OverlayPlayer data, out PlayerMap map)
    {
        PlayerController playerController = SpawnPlayer(data);
        map = new PlayerMap
        {
            PlayerController = playerController,
            OverlayPlayer = data
        };
        players[data.Character.Id] = map;
        return playerController;
    }

    private PlayerController SpawnPlayer(OverlayPlayer data)
    {
        var player = GameObject.Instantiate(playerPrefab);
        if (!player)
        {
            Shinobytes.Debug.LogError("Player Prefab not found!!!");
            return null;
        }


        var playerController = player.GetComponent<PlayerController>();
        if (!playerController)
        {
            Shinobytes.Debug.LogError("No PlayerController found on the player prefab: " + playerPrefab.name);
            return null;
        }
        playerController.IsBot = true;
        playerController.SetPlayer(data.Character, data.Twitch, null, null);

        var rotation = player.AddComponent<AutoRotate>();
        rotation.rotationSpeed = new Vector3(0, Overlay.CharacterRotationSpeed, 0);
        return playerController;
    }

    private void Update(OverlayPlayer player, out PlayerMap playerMap)
    {
        if (players.TryGetValue(player.Character.Id, out playerMap))
        {
            // if equipment changes
            // we have to destroy/recreate the player
            // if user changes appearance, we have to do the same too.

            if (HasAppearanceChanged(playerMap.OverlayPlayer.Character.Appearance, player.Character.Appearance) ||
                HasEquipmentChanged(playerMap.OverlayPlayer.Character.InventoryItems, player.Character.InventoryItems))
            {
                if (playerMap.PlayerController)
                {
                    GameObject.Destroy(playerMap.PlayerController.gameObject);
                }

                ReplacePlayerController(player, out playerMap);
                return;
            }

            var state = player.Character.State;

            playerMap.PlayerController.SetTask(state.Task, new string[] { state.TaskArgument });

            Update(playerMap.OverlayPlayer, player);

        }
    }

    private bool HasAppearanceChanged(SyntyAppearance a, SyntyAppearance b)
    {
        return ValuesChanged(a, b);
    }

    private bool HasEquipmentChanged(IReadOnlyList<InventoryItem> a, IReadOnlyList<InventoryItem> b)
    {
        var equippedA = a.Where(x => x.Equipped).ToList();
        var equippedB = b.Where(x => x.Equipped).ToList();
        if (equippedA.Count != equippedB.Count)
        {
            return true;
        }

        for (var i = 0; i < equippedA.Count; ++i)
        {
            var eA = equippedA[i];
            var contains = equippedB.Any(x => x.ItemId == eA.ItemId);
            if (!contains)
            {
                return true;
            }
        }
        return false;
    }

    private bool ValuesChanged<T>(T a, T b)
    {
        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var propValueA = prop.GetValue(a);
            var propValueB = prop.GetValue(b);
            if (!System.ValueType.Equals(propValueA, propValueB) ||
                !object.Equals(propValueA, propValueB))
            {
                return true;
            }
        }
        return false;
    }

    private void Update(OverlayPlayer src, OverlayPlayer newValues)
    {
        if (src == null || newValues == null)
            return;

        if (System.Object.ReferenceEquals(src, newValues)) // no need to update itself
            return;

        Update(src.Twitch, newValues.Twitch);
        Update(src.Character, newValues.Character);
    }

    private void Update(Player src, Player newValues)
    {
        // this is not good, we are replacing references here.
        // this will require us to re-set these things all anew later in the UI
        // 
        src.Appearance = newValues.Appearance;
        src.Clan = newValues.Clan;
        src.ClanRole = newValues.ClanRole;
        src.InventoryItems = newValues.InventoryItems;
        src.IsAdmin = newValues.IsAdmin;
        src.IsModerator = newValues.IsModerator;
        src.Skills = newValues.Skills;
        src.State = newValues.State;
        src.PatreonTier = newValues.PatreonTier;
        src.Resources = newValues.Resources;
        src.Statistics = newValues.Statistics;
    }

    private void Update(TwitchPlayerInfo src, TwitchPlayerInfo newValues)
    {
        if (src == null)
        {
            Shinobytes.Debug.LogWarning("Unable to update Twitch Player Info, source is null.");
            return;
        }
        if (newValues == null)
        {
            Shinobytes.Debug.LogWarning("Unable to update Twitch Player Info, new Values is null.");
            return;
        }

        src.IsBroadcaster = newValues.IsBroadcaster;
        src.IsModerator = newValues.IsModerator;
        src.IsSubscriber = newValues.IsSubscriber;
        src.IsVip = newValues.IsVip;
    }
    private class PlayerMap
    {
        public OverlayPlayer OverlayPlayer;
        public PlayerController PlayerController;
    }
}