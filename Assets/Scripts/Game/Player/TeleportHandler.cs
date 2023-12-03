using System;
using UnityEngine;
using UnityEngine.AI;

public class TeleportHandler : MonoBehaviour
{
    [SerializeField] public IslandManager islandManager;

    public PlayerController player;
    public Transform parent;

    public void Start()
    {
        if (!islandManager) islandManager = FindAnyObjectByType<IslandManager>();
        if (!player) player = GetComponent<PlayerController>();
    }

    public void Teleport(Vector3 position, bool ignoreParent, bool adjustPlayerToNavmesh)
    {
        if (!ignoreParent)
        {
            Teleport(position, adjustPlayerToNavmesh);
            return;
        }

        if (!this || this == null)
            return;

        parent = null;
        player.transform.parent = null;
        player.transform.localPosition = Vector3.zero;
        player.SetPosition(position, adjustPlayerToNavmesh);
    }

    public void Teleport(IslandController targetIsland, bool silent = false)
    {
        Teleport(targetIsland.SpawnPosition, true);
        // Force player to start "training" the same skill they were training.
        // since this will make them find a target and not randomly "fish" or "mining" in the open.
        var task = player.GetTask();
        if (task != TaskType.None)
        {
            player.SetTask(task, player.taskArgument, silent);
        }
    }

    public void Teleport(Vector3 position, bool adjustPlayerToNavmesh = true)
    {
        // check if player has been removed
        // could have been kicked. *Looks at Solunae*
        if (!this || this == null)
            return;

        if (player.onsenHandler.InOnsen)
        {
            player.GameManager.Onsen.Leave(player);
        }

        var hasParent = !!player.transform.parent;
        if (hasParent)
        {
            parent = player.Transform.parent;
            player.transform.SetParent(null);
        }

        player.taskTarget = null;

        player.Movement.Lock();
        player.SetPosition(position, adjustPlayerToNavmesh);
        //player.SetDestination(position);


        if (!hasParent && parent)
        {
            player.transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.position = position;
        }
        player.InCombat = false;
        player.ClearAttackers();
        player.Island = islandManager.FindPlayerIsland(player);
        if (player.Island)
        {
            player.Island.AddPlayer(player);
        }
    }

}
