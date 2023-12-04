using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportToDocks : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player)
        {
            var island = GetComponentInParent<IslandController>();
            player.teleportHandler.Teleport(island.DockingArea.DockPosition);
        }
    }
}
