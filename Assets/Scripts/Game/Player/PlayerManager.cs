using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private readonly List<PlayerController> activePlayers = new List<PlayerController>();
    private readonly object mutex = new object();

    [SerializeField] private GameSettings settings;
    [SerializeField] private GameObject playerControllerPrefab;
    [SerializeField] private IoCContainer ioc;

    public readonly ConcurrentQueue<RavenNest.Models.Player> PlayerQueue
        = new ConcurrentQueue<RavenNest.Models.Player>();

    void Start()
    {
        if (!settings) settings = GetComponent<GameSettings>();
        if (!ioc) ioc = GetComponent<IoCContainer>();
    }

    public bool Contains(string userId)
    {
        return GetPlayerByUserId(userId);
    }

    void Update()
    {
    }

    public void SaveRepository()
    {
        //playerRepository.UpdateMany(activePlayers.Select(x => x.CreatePlayerDefinition()));
        //playerRepository.Save();
    }

    public IReadOnlyList<PlayerController> GetAllPlayers()
    {
        return activePlayers;
    }

    public PlayerController Spawn(
        Vector3 position,
        RavenNest.Models.Player playerDefinition,
        Player streamUser,
        StreamRaidInfo raidInfo)
    {
        if (activePlayers.Any(x => x.PlayerName == playerDefinition.Name))
        {
            return null; // player is already in game
        }

        var player = Instantiate(playerControllerPrefab);
        if (!player)
        {
            Debug.LogError("Player Prefab not found!!!");
            return null;
        }

        player.transform.position = position;

        return Add(player.GetComponent<PlayerController>(), playerDefinition, streamUser, raidInfo);
    }

    internal IReadOnlyList<PlayerController> GetAllModerators()
    {
        return activePlayers.Where(x => x.IsModerator).ToList();
    }

    public PlayerController GetPlayer(Player taskPlayer)
    {
        var player = GetPlayerByName(taskPlayer.Username);
        return player ? player : GetPlayerByUserId(taskPlayer.UserId);
    }

    public PlayerController GetPlayerByUserId(string userId)
    {
        lock (mutex)
        {
            return activePlayers.FirstOrDefault(x =>
                x.UserId.Equals(userId, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    public PlayerController GetPlayerByName(string playerName)
    {
        playerName = playerName.StartsWith("@") ? playerName.Substring(1) : playerName;
        lock (mutex)
        {
            return activePlayers.FirstOrDefault(x =>
            x.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    public int GetPlayerCount(bool includeNpc = false)
    {
        lock (mutex)
            return activePlayers?.Count(x => includeNpc || !x.IsNPC) ?? 0;
    }

    public PlayerController GetPlayerByIndex(int index)
    {
        lock (mutex)
        {
            if (activePlayers == null || activePlayers.Count <= index)
            {
                return null;
            }

            return activePlayers[index];
        }
    }

    public void Remove(PlayerController player)
    {
        lock (mutex)
        {
            if (!activePlayers.Contains(player))
            {
                return;
            }

            SaveRepository();
            player.OnRemoved();
            activePlayers.Remove(player);
            Destroy(player.gameObject);
        }
    }

    public IReadOnlyList<PlayerController> FindPlayers(string query)
    {
        lock (mutex)
            return activePlayers.Where(x => x.PlayerName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PlayerController Add(
        PlayerController player,
        RavenNest.Models.Player def,
        Player streamUser,
        StreamRaidInfo raidInfo)
    {
        player.SetPlayer(def, streamUser, raidInfo);
        lock (mutex)
        {
            activePlayers.Add(player);
            return player;
        }
    }
}