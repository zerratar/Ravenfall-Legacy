using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportToDocks : MonoBehaviour
{
    private IslandController island;
    private Bounds bounds;
    private Vector3 center;

    // Start is called before the first frame update
    void Start()
    {
        this.island = GetComponentInParent<IslandController>();
        var bc = GetComponent<BoxCollider>();
        this.bounds = bc.bounds;
        this.center = bc.center;
        bc.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        // only do this once every 60 frames.
        if (Time.frameCount % 60 != 0)
        {
            return;
        }

        var min = this.bounds.min;
        var max = this.bounds.max;

        var minX = min.x;
        var minY = min.y;
        var minZ = min.z;

        var maxZ = max.z;
        var maxY = max.y;
        var maxX = max.x;

        foreach (var player in island.GetPlayers())
        {
            if (!player || player == null || player.Removed || player.isDestroyed || player.dungeonHandler.InDungeon || player.raidHandler.InRaid || player.Stats.IsDead)
                continue;

            var p = player._transform.position;
            if (p.x >= minX && p.x <= maxX && p.z >= minZ && p.z <= maxZ && p.y >= minY && p.y <= maxY)
            {
                player.teleportHandler.Teleport(island.DockingArea.DockPosition);
            }
        }
    }

}
