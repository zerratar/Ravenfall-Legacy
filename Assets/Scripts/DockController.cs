using System;
using System.Collections.Concurrent;
using UnityEngine;

public class DockController : MonoBehaviour
{
    private readonly ConcurrentDictionary<Guid, PlayerController> players
        = new ConcurrentDictionary<Guid, PlayerController>();

    private BoxCollider boxCollider;

    public Transform DockPositionTransform;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (DockPositionTransform)
        {
            DockPosition = DockPositionTransform.position;
        }
        else
        {
            DockPosition = boxCollider.center + transform.position;
        }
    }

    public Vector3 DockPosition;

    //public Vector3 RandomDockPosition => DockPosition + (Random.value * (boxCollider.size / 2f));

    public bool OnDock(PlayerController player)
    {
        if (players.TryGetValue(player.Id, out _))
        {
            return true;
        }

        return Vector3.Distance(DockPosition, player.Position) < 2;
    }

    public void Enter(PlayerController player)
    {
        players[player.Id] = player;
    }

    public void Exit(PlayerController player)
    {
        players.TryRemove(player.Id, out _);
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.gameObject.GetComponent<PlayerController>();
        if (!player)
        {
            return;
        }

        players[player.Id] = player;
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.gameObject.GetComponent<PlayerController>();
        if (!player)
        {
            return;
        }

        players.TryRemove(player.Id, out _);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(DockPosition, 1);
    }
}
