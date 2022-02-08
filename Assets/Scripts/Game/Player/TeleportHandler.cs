﻿using UnityEngine;
using UnityEngine.AI;

public class TeleportHandler : MonoBehaviour
{
    [SerializeField] public IslandManager islandManager;

    public PlayerController player;
    public Transform parent;

    public void Start()
    {
        if (!islandManager) islandManager = FindObjectOfType<IslandManager>();
        if (!player) player = GetComponent<PlayerController>();
    }

    public void Teleport(Vector3 position, bool ignoreParent)
    {
        if (!ignoreParent)
        {
            Teleport(position);
            return;
        }

        if (!this || this == null)
            return;

        parent = null;
        player.transform.parent = null;
        player.transform.localPosition = Vector3.zero;
        player.SetPosition(position);
    }

    public void Teleport(Vector3 position)
    {
        // check if player has been removed
        // could have been kicked. *Looks at Solunae*
        if (!this || this == null)
            return;

        var hasParent = !!player.transform.parent;
        if (hasParent)
        {
            parent = player.Transform.parent;
            player.transform.SetParent(null);
        }

        player.taskTarget = null;

        player.SetPosition(position);
        player.GotoPosition(position);
        player.Lock();

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
    }
}
