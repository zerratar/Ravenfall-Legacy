using System.Collections.Concurrent;
using UnityEngine;

public class DockController : MonoBehaviour
{
    private readonly ConcurrentDictionary<string, PlayerController> players
        = new ConcurrentDictionary<string, PlayerController>();

    private BoxCollider boxCollider;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 DockPosition => boxCollider.center + transform.position;

    //public Vector3 RandomDockPosition => DockPosition + (Random.value * (boxCollider.size / 2f));

    public bool OnDock(PlayerController player)
    {
        return players.TryGetValue(player.UserId, out _);
    }

    public bool OnDock(string userId)
    {
        return players.TryGetValue(userId, out _);
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.gameObject.GetComponent<PlayerController>();
        if (!player)
        {
            return;
        }

        players[player.UserId] = player;
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.gameObject.GetComponent<PlayerController>();
        if (!player)
        {
            return;
        }

        players.TryRemove(player.UserId, out _);
    }
}
